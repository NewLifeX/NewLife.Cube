using System.Reflection;
using System.Threading;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Enums;
using NewLife.Log;
using XCode;
using XCode.DataAccessLayer;
// 用于反射推断路由特性，不直接引入 ASP.NET Core MVC 强类型以保持多框架兼容
//using Microsoft.AspNetCore.Mvc;

namespace NewLife.Cube.Services;

/// <summary>值集自动注册服务。启动时扫描指定命名空间下的 C# 枚举，自动注册为 Enum.xxx 值集</summary>
public class LovAutoRegisterService
{
    /// <summary>默认扫描的命名空间前缀列表</summary>
    public IList<String> NamespacePrefixes { get; } = new List<String> { "NewLife.Cube.Entity", typeof(AuthCategory).Namespace };

    /// <summary>是否已启用</summary>
    public Boolean Enabled { get; set; } = true;

    /// <summary>扫描并注册所有枚举类型</summary>
    public Int32 ScanAndRegister()
    {
        if (!Enabled || NamespacePrefixes.Count == 0) return 0;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(ScanAndRegister));

        var count = 0;
        foreach (var prefix in NamespacePrefixes)
        {
            // 查找匹配命名空间前缀的所有程序集
            var assemblies = GetAssembliesByNamespace(prefix);
            foreach (var asm in assemblies)
            {
                try
                {
                    var types = asm.GetTypes().Where(t => t.IsEnum && t.IsPublic && t.Namespace != null && t.Namespace.StartsWith(prefix));
                    foreach (var enumType in types)
                    {
                        if (RegisterEnum(enumType, prefix))
                            count++;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 跳过无法加载类型的程序集
                }
            }
        }

        // 扫描 [LovList] 特性，自动注册列表型值集（与枚举初始化一并执行）
        count += RegisterListAttributes();

        return count;
    }

    /// <summary>根据命名空间前缀获取匹配的程序集</summary>
    private static IEnumerable<Assembly> GetAssembliesByNamespace(String prefix)
    {
        // 获取已加载的程序集
        var list = new List<Assembly>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                // 快速判断：程序集名或任何公开类型是否包含该命名空间
                var types = asm.GetExportedTypes();
                if (types.Any(t => t.Namespace != null && t.Namespace.StartsWith(prefix)))
                    list.Add(asm);
            }
            catch
            {
                // 跳过无法反射的程序集
            }
        }
        return list;
    }

    /// <summary>注册一个枚举类型到值集定义表</summary>
    private static Boolean RegisterEnum(Type enumType, String namespacePrefix)
    {
        // 计算 LovCode: Enum.{完全限定类型名}，确保全局唯一且自解释
        // 如 SmartMES.Data.ProcessCard.EnableStatus → Enum.SmartMES.Data.ProcessCard.EnableStatus
        var lovCode = $"Enum.{enumType.FullName}";

        XTrace.WriteLine("Lov: 检测到枚举 {0} → LovCode={1}", enumType.FullName, lovCode);

        // 查找或创建 LovDefinition
        var def = LovDefinition.Find(LovDefinition._.LovCode == lovCode);
        if (def == null)
        {
            def = new LovDefinition
            {
                LovCode = lovCode,
                Name = GetEnumDisplayName(enumType) ?? enumType.Name,
                Type = "ENUM",
                Source = "AUTO",
                Enabled = true,
            };
            def.Insert();
            XTrace.WriteLine("Lov: 自动注册值集 {0}", lovCode);
        }
        else if (def.Source != "AUTO")
        {
            // 手工管理的跳过
            return false;
        }

        // 同步枚举值（被 LovStringValue 标记的枚举使用成员名作为选项值，而非数字）
        // 按特性名识别：NewLife.Cube 已内置 NewLife.Cube.LovStringValueAttribute；
        // 若公共层不愿引用 Cube，也可在自己程序集定义同名特性（任意命名空间），同样生效，避免编译期耦合
        var useStringValue = enumType.GetCustomAttributes().Any(a => a.GetType().Name == "LovStringValueAttribute");
        RetryDb(() => SyncEnumValues(def, enumType, useStringValue));

        return true;
    }

    /// <summary>同步枚举值到 LovEnumItem（数据落到 Parameter，不触碰真实表）</summary>
    /// <param name="def">值集定义</param>
    /// <param name="enumType">要同步的枚举类型</param>
    /// <param name="useStringValue">为 true 时使用枚举成员名作为选项值（字符串），否则使用数字值</param>
    private static void SyncEnumValues(LovDefinition def, Type enumType, Boolean useStringValue)
    {
        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType);

        // 读取现有记录（全部落到 Parameter）
        var existingItems = LovEnumItem.FindAllByLovDefId(def.Id);
        var existingMap = existingItems.ToDictionary(e => e.Value, e => e);

        // 当前枚举值集合
        var currentValues = new HashSet<String>();
        var result = new List<LovEnumItem>();

        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            // 默认用数字值；被 LovStringValue 标记的枚举改用成员名（字符串），便于与以名存储的字段对应
            var value = useStringValue ? name : Convert.ToInt32(values.GetValue(i)!).ToString();
            currentValues.Add(value);

            // 获取枚举成员的描述（可配合 DisplayName 或 Description 特性）
            var member = enumType.GetMember(name).FirstOrDefault();
            var label = name; // 默认使用枚举名
            if (member != null)
            {
                var displayAttr = member.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
                if (displayAttr != null && !String.IsNullOrEmpty(displayAttr.DisplayName))
                    label = displayAttr.DisplayName;
                else
                {
                    var descAttr = member.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                    if (descAttr != null && !String.IsNullOrEmpty(descAttr.Description))
                        label = descAttr.Description;
                }
            }

            if (existingMap.TryGetValue(value, out var existing))
            {
                // 已存在：复用原行（保留审计字段与手工排序），仅更新可能变化的 label 与启用状态
                existing.Label = label;
                existing.Enabled = true;
                result.Add(existing);
            }
            else
            {
                // 新增枚举值
                result.Add(new LovEnumItem
                {
                    LovDefId = def.Id,
                    Value = value,
                    Label = label,
                    Sort = i,
                    Enabled = true,
                });
            }
        }

        // 逻辑禁用已删除的枚举成员：保留原行并置 Enabled=false
        foreach (var existing in existingItems)
        {
            if (!currentValues.Contains(existing.Value) && existing.Enabled)
            {
                existing.Enabled = false;
                result.Add(existing);
            }
        }

        // 整表覆盖写回 Parameter
        LovEnumItem.SaveAllByLovDefId(def.Id, result);
    }

    /// <summary>获取枚举类型的显示名称，优先 DisplayName 特性，无则返回 null</summary>
    private static String? GetEnumDisplayName(Type enumType)
    {
        var attr = enumType.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
        if (attr != null && !String.IsNullOrEmpty(attr.DisplayName))
            return attr.DisplayName;

        var descAttr = enumType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        if (descAttr != null && !String.IsNullOrEmpty(descAttr.Description))
            return descAttr.Description;

        return null;
    }

    #region 列表型值集（[LovList] 特性）

    /// <summary>扫描已加载程序集中标注了 <see cref="LovListAttribute"/> 的控制器方法，自动注册列表型值集</summary>
    /// <returns>本次新注册/更新的列表型值集数量</returns>
    private static Int32 RegisterListAttributes()
    {
        var count = 0;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 仅扫描引用了 NewLife.Cube 的程序集（业务/入口程序集），跳过系统程序集
            Boolean referencesCube;
            try
            {
                referencesCube = asm.GetReferencedAssemblies().Any(a => a.Name == "NewLife.Cube");
            }
            catch
            {
                continue;
            }
            if (!referencesCube) continue;

            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type == null || !type.IsClass) continue;
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    var attr = method.GetCustomAttribute<LovListAttribute>();
                    if (attr == null) continue;
                    try
                    {
                        if (RegisterList(attr, method)) count++;
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
                }
            }
        }

        return count;
    }

    /// <summary>根据 [LovList] 特性注册/更新一个列表型值集（含数据源配置、表格列、搜索字段）</summary>
    /// <param name="attr">特性实例</param>
    /// <param name="method">标注该特性的 Action 方法，用于自动推断路由等属性</param>
    /// <returns>是否成功注册或更新</returns>
    private static Boolean RegisterList(LovListAttribute attr, MethodInfo method)
    {
        // 零参数 [LovList]：先自动推断 LovCode（含区域段），再校验前缀
        InferLovList(attr, method);

        if (attr.LovCode.IsNullOrEmpty())
            return false;

        // 校验前缀：列表型值集 LovCode 必须以 List. 开头
        if (!attr.LovCode.StartsWith("List.", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"LovList 特性的 LovCode 必须以 'List.' 开头：{attr.LovCode}");

        var def = LovDefinition.Find(LovDefinition._.LovCode == attr.LovCode);
        if (def == null)
        {
            def = new LovDefinition
            {
                LovCode = attr.LovCode,
                Name = attr.Name.IsNullOrEmpty() ? attr.LovCode : attr.Name,
                Type = "LIST",
                ValueField = attr.ValueField,
                LabelField = attr.LabelField,
                Source = "AUTO",
                Enabled = true,
            };
            def.Insert();
            XTrace.WriteLine("Lov: 自动注册列表值集 {0}", attr.LovCode);
        }
        else if (def.Source != "AUTO")
        {
            // 手工管理的值集不覆盖
            return false;
        }
        else
        {
            def.Name = attr.Name.IsNullOrEmpty() ? def.Name : attr.Name;
            def.ValueField = attr.ValueField;
            def.LabelField = attr.LabelField;
            def.Update();
        }

        // 列表数据源配置（1:1，覆盖写入 Parameter）
        var config = LovListConfig.FindByLovDefId(def.Id) ?? new LovListConfig { LovDefId = def.Id };
        config.RequestUrl = attr.RequestUrl;
        config.Method = attr.Method;
        config.Pageable = attr.Pageable;
        config.PageNumField = attr.PageNumField;
        config.PageSizeField = attr.PageSizeField;
        config.DataPath = attr.DataPath;
        config.TotalPath = attr.TotalPath;
        config.FixedParams = attr.FixedParams;
        config.ProxyRequest = attr.ProxyRequest;
        // 首跑建库期间数据库可能处于繁忙/锁定状态，写入 Parameter 需容忍瞬时锁定并重试
        RetryDb(() => LovListConfig.SaveByLovDefId(def.Id, config));

        // 表格列与搜索字段（覆盖写入）
        RetryDb(() => LovTableColumn.SaveAllByLovDefId(def.Id, ParseColumns(attr.Columns, def.Id)));
        RetryDb(() => LovSearchField.SaveAllByLovDefId(def.Id, ParseSearchFields(attr.SearchFields, def.Id)));

        return true;
    }

    /// <summary>解析表格列声明。元素格式 "Field:Title:Width:Align"</summary>
    private static IList<LovTableColumn> ParseColumns(String[]? tokens, Int32 lovDefId)
    {
        var list = new List<LovTableColumn>();
        if (tokens == null) return list;

        for (var i = 0; i < tokens.Length; i++)
        {
            var parts = tokens[i].Split(':');
            if (parts.Length < 2) continue;

            var col = new LovTableColumn
            {
                LovDefId = lovDefId,
                Field = parts[0].Trim(),
                Title = parts[1].Trim(),
                Width = parts.Length > 2 && Int32.TryParse(parts[2].Trim(), out var w) ? w : 0,
                Align = parts.Length > 3 ? parts[3].Trim() : "left",
                Sortable = false,
                Sort = i,
            };
            list.Add(col);
        }

        return list;
    }

    /// <summary>解析搜索字段声明。元素格式 "Field:Title:ComponentType:ParamType:Required"</summary>
    private static IList<LovSearchField> ParseSearchFields(String[]? tokens, Int32 lovDefId)
    {
        var list = new List<LovSearchField>();
        if (tokens == null) return list;

        for (var i = 0; i < tokens.Length; i++)
        {
            var parts = tokens[i].Split(':');
            if (parts.Length < 2) continue;

            var sf = new LovSearchField
            {
                LovDefId = lovDefId,
                Field = parts[0].Trim(),
                Title = parts[1].Trim(),
                ComponentType = parts.Length > 2 ? parts[2].Trim() : "input",
                ParamType = parts.Length > 3 ? parts[3].Trim() : "BODY",
                Required = parts.Length > 4 && Boolean.TryParse(parts[4].Trim(), out var r) && r,
                Sort = i,
            };
            list.Add(sf);
        }

        return list;
    }

    #endregion

    #region 自动推断

    /// <summary>推断 LovList 特性中未指定的属性（RequestUrl、Method、Name）</summary>
    /// <param name="attr">特性实例</param>
    /// <param name="method">标注该特性的方法</param>
    private static void InferLovList(LovListAttribute attr, MethodInfo method)
    {
        // 推断显示名称：方法名未指定时，使用 ControllerName.ActionName
        if (attr.Name.IsNullOrEmpty())
        {
            var controllerName = method.DeclaringType?.Name.Replace("Controller", "");
            attr.Name = $"{controllerName}.{method.Name}";
        }

        // 推断 LovCode：零参数 [LovList] 时自动推断为 List.{Area}.{Controller}.{Action}
        // 控制器无区域（AreaAttribute / AreaBase）则跳过区域段，退化为 List.{Controller}.{Action}
        if (attr.LovCode.IsNullOrEmpty())
        {
            var controllerType = method.DeclaringType;
            var controllerName = controllerType?.Name.Replace("Controller", "");

            // 从控制器类的 AreaAttribute（AreaBase 继承该特性）反射获取区域段；无则跳过
            String area = null;
            if (controllerType != null)
            {
                var areaAttr = controllerType.GetCustomAttributes(inherit: true)
                    .FirstOrDefault(a =>
                    {
                        var t = a.GetType();
                        var baseFullName = t.BaseType?.FullName;
                        return t.FullName == "Microsoft.AspNetCore.Mvc.AreaAttribute"
                            || baseFullName == "Microsoft.AspNetCore.Mvc.AreaAttribute"
                            || t.Name == "AreaBase"
                            || (baseFullName != null && baseFullName.EndsWith("AreaBase"));
                    });
                if (areaAttr != null)
                {
                    area = GetAttributeProperty(areaAttr, "RouteValue") as String;
                    if (area.IsNullOrEmpty()) area = GetAttributeProperty(areaAttr, "Name") as String;
                }
            }

            attr.LovCode = area.IsNullOrEmpty()
                ? $"List.{controllerName}.{method.Name}"
                : $"List.{area}.{controllerName}.{method.Name}";
        }

        // 推断请求地址
        if (attr.RequestUrl.IsNullOrEmpty())
        {
            attr.RequestUrl = InferRequestUrl(method);
        }

        // 推断请求方式（仅当特性中 Method 为空或默认 GET 时）
        if (attr.Method.IsNullOrEmpty() || attr.Method == "GET")
        {
            attr.Method = InferHttpMethod(method);
        }
    }

    /// <summary>从方法所在控制器的路由特性推断请求地址，自动拼接 API 前缀</summary>
    private static String InferRequestUrl(MethodInfo method)
    {
        var controllerType = method.DeclaringType;
        if (controllerType == null) return "";

        // 按类型名反射获取特性，避免编译期强依赖特定 ASP.NET Core 版本
        // RouteAttribute / HttpGetAttribute / HttpPostAttribute 等均位于 Microsoft.AspNetCore.Mvc 命名空间
        const String routeAttrName = "Microsoft.AspNetCore.Mvc.RouteAttribute";
        const String httpMethodAttrName = "Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute";

        var controllerName = controllerType.Name.Replace("Controller", "");

        var segments = new List<String>();

        // 控制器级别 [Route] 特性
        var controllerRoute = controllerType.GetCustomAttributes(inherit: true)
            .FirstOrDefault(a => a.GetType().FullName == routeAttrName);
        if (controllerRoute != null)
        {
            var template = GetAttributeProperty(controllerRoute, "Template") as String;
            if (!template.IsNullOrEmpty())
            {
                template = template.Trim('/');
                // 替换令牌
                if (template.Contains("[controller]"))
                    template = template.Replace("[controller]", controllerName);
                if (template.Contains("[action]"))
                    template = template.Replace("[action]", method.Name);
                segments.Add(template);
            }
        }

        // Action 级别的 [Route] 或 HTTP 方法特性（如 [HttpGet("template")]）
        var actionRoute = method.GetCustomAttributes(inherit: true)
            .FirstOrDefault(a => a.GetType().FullName == routeAttrName);
        var httpAttr = method.GetCustomAttributes(inherit: true)
            .FirstOrDefault(a =>
            {
                var fullName = a.GetType().FullName;
                return fullName != null &&
                    (fullName == httpMethodAttrName ||
                     fullName.EndsWith("HttpGetAttribute") ||
                     fullName.EndsWith("HttpPostAttribute") ||
                     fullName.EndsWith("HttpPutAttribute") ||
                     fullName.EndsWith("HttpDeleteAttribute") ||
                     fullName.EndsWith("HttpPatchAttribute"));
            });

        var actionTemplate = "";
        if (actionRoute != null)
        {
            actionTemplate = GetAttributeProperty(actionRoute, "Template") as String ?? "";
        }
        else if (httpAttr != null)
        {
            actionTemplate = GetAttributeProperty(httpAttr, "Template") as String ?? "";
        }

        // 如果没有任何控制器路由，使用约定路由 controller/action 作为基础路径
        if (segments.Count == 0)
        {
            if (!actionTemplate.IsNullOrEmpty())
                // 有 Action 模板但无控制器路由，用 controller/template 作为路径
                segments.Add($"{controllerName}/{actionTemplate.Trim('/')}");
            else
                // 都没有路由特性，纯约定路由
                segments.Add($"{controllerName}/{method.Name}");
        }
        else if (!actionTemplate.IsNullOrEmpty())
        {
            // 有控制器路由，Action 模板作为独立段追加
            segments.Add(actionTemplate.Trim('/'));
        }

        var path = String.Join("/", segments);

        // 拼接 API 前缀（WebAPI版固定 /api，写死不配置）
        path = $"api/{path}";

        return "/" + path;
    }

    /// <summary>从 HTTP 方法特性推断请求方式（GET/POST/PUT/DELETE/PATCH）</summary>
    private static String InferHttpMethod(MethodInfo method)
    {
        // 查找 HTTP 方法特性，按类型名匹配
        var attr = method.GetCustomAttributes(inherit: true).FirstOrDefault(a =>
        {
            var fullName = a.GetType().FullName;
            return fullName != null &&
                (fullName == "Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute" ||
                 fullName.EndsWith("HttpGetAttribute") ||
                 fullName.EndsWith("HttpPostAttribute") ||
                 fullName.EndsWith("HttpPutAttribute") ||
                 fullName.EndsWith("HttpDeleteAttribute") ||
                 fullName.EndsWith("HttpPatchAttribute"));
        });

        if (attr == null) return "GET";

        var typeName = attr.GetType().Name;
        if (typeName == "HttpGetAttribute") return "GET";
        if (typeName == "HttpPostAttribute") return "POST";
        if (typeName == "HttpPutAttribute") return "PUT";
        if (typeName == "HttpDeleteAttribute") return "DELETE";
        if (typeName == "HttpPatchAttribute") return "PATCH";

        // 对于 HttpMethodAttribute（基类），尝试从 HttpMethods 属性获取
        try
        {
            var methods = GetAttributeProperty(attr, "HttpMethods") as ICollection<String>;
            if (methods != null && methods.Count > 0)
                return methods.First().ToUpper();
        }
        catch { }

        return "GET";
    }

    /// <summary>通过反射获取特性实例的属性值，避免编译期强依赖</summary>
    private static Object? GetAttributeProperty(Object attr, String propertyName)
    {
        try
        {
            var prop = attr.GetType().GetProperty(propertyName);
            return prop?.GetValue(attr);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 写入重试（容忍首跑建库期间的瞬时数据库锁定）

    /// <summary>对数据库写入操作进行重试，容忍 SQLite 首跑建库期间的瞬时锁定（database is locked / Busy）</summary>
    /// <param name="action">写入操作</param>
    /// <param name="maxRetry">最大重试次数</param>
    private static void RetryDb(Action action, Int32 maxRetry = 30)
    {
        for (var i = 0; i < maxRetry; i++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex) when (i < maxRetry - 1 && IsTransient(ex))
            {
                Thread.Sleep(400);
            }
        }

        // 最后一次不再吞异常，便于上层感知真实错误
        action();
    }

    /// <summary>判断是否为可重试的瞬时数据库错误（如 SQLite 忙/锁定）</summary>
    private static Boolean IsTransient(Exception ex)
    {
        var msg = ex?.Message ?? "";
        return msg.Contains("locked", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Busy", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("database is busy", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
