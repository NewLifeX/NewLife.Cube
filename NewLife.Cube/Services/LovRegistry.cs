using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using NewLife.Cube.Entity;
using NewLife.Log;

namespace NewLife.Cube.Services;

/// <summary>枚举型值集选项（代码反射生成）</summary>
public class LovEnumOption
{
    /// <summary>存储值</summary>
    public String Value { get; set; } = "";

    /// <summary>显示文本</summary>
    public String Label { get; set; } = "";
}

/// <summary>列表型值集描述符。由 [LovList] 特性反射构建（不落库，运行时直读）</summary>
public class LovListDescriptor
{
    /// <summary>值集编码</summary>
    public String LovCode { get; set; } = "";

    /// <summary>显示名称</summary>
    public String Name { get; set; } = "";

    /// <summary>值字段。列表型的值字段名</summary>
    public String ValueField { get; set; } = "id";

    /// <summary>标签字段。列表型的标签字段名</summary>
    public String LabelField { get; set; } = "name";

    /// <summary>列表数据源配置</summary>
    public LovListConfigModel Config { get; set; } = new();

    /// <summary>搜索字段</summary>
    public IList<LovSearchFieldModel> SearchFields { get; set; } = [];

    /// <summary>表格列</summary>
    public IList<LovTableColumnModel> TableColumns { get; set; } = [];
}

/// <summary>
/// 值集代码注册表（代码优先解析）。把"代码声明的值集"直接提供给运行时读取，不再落库：
/// 1) 枚举型：LovCode 形如 <c>Enum.{Type.FullName}</c>，按全限定名在已加载程序集中定位枚举类型并反射选项；
/// 2) 列表型：扫描引用 NewLife.Cube 的程序集中标注 <see cref="LovListAttribute"/> 的方法，按 LovCode 缓存描述符。
/// 进程内懒扫描 + 字典缓存，请求期只读；运行时手工定义的数据仍由 <see cref="LovStore"/> 承载（手工定义优先）。
/// </summary>
public static class LovRegistry
{
    private static readonly ConcurrentDictionary<String, Type> _enumTypes = new();
    private static readonly ConcurrentDictionary<String, LovListDescriptor> _lists = new();
    private static readonly Object _sync = new();
    private static Boolean _scanned;

    #region 枚举型
    /// <summary>按全限定名查找枚举类型。懒扫描已加载程序集并缓存</summary>
    /// <param name="fullName">枚举类型全限定名（不含 Enum. 前缀）</param>
    /// <returns>枚举类型，未找到返回 null</returns>
    public static Type? FindEnumType(String fullName)
    {
        if (fullName.IsNullOrEmpty()) return null;

        TryScan();

        if (_enumTypes.TryGetValue(fullName, out var type)) return type;

        // 程序集延迟加载场景补扫一次
        lock (_sync)
        {
            ScanAll();
            _scanned = true;
        }
        _enumTypes.TryGetValue(fullName, out type);
        return type;
    }

    /// <summary>反射枚举成员生成选项。值取数字（默认）或成员名（被 LovStringValue 标记，按特性名识别）；
    /// 显示文本取 DisplayName→Description→成员名</summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>选项列表</returns>
    public static IList<LovEnumOption> GetEnumOptions(Type enumType)
    {
        if (enumType == null || !enumType.IsEnum) return [];

        // 按特性名识别，业务层可定义同名特性避免编译期耦合
        var useStringValue = enumType.GetCustomAttributes().Any(a => a.GetType().Name == "LovStringValueAttribute");

        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType);
        var list = new List<LovEnumOption>();
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            // 默认用数字值；被 LovStringValue 标记的枚举改用成员名（字符串）
            var value = useStringValue ? name : Convert.ToInt64(values.GetValue(i)!).ToString();

            // 成员显示文本：DisplayName → Description → 成员名
            var label = name;
            var member = enumType.GetMember(name).FirstOrDefault();
            if (member != null)
            {
                var displayAttr = member.GetCustomAttribute<DisplayNameAttribute>();
                if (displayAttr != null && !displayAttr.DisplayName.IsNullOrEmpty())
                    label = displayAttr.DisplayName;
                else
                {
                    var descAttr = member.GetCustomAttribute<DescriptionAttribute>();
                    if (descAttr != null && !descAttr.Description.IsNullOrEmpty())
                        label = descAttr.Description;
                }
            }

            list.Add(new LovEnumOption { Value = value, Label = label });
        }
        return list;
    }

    /// <summary>获取枚举类型的显示名称。DisplayName → Description → 类型名</summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>显示名称</returns>
    public static String GetEnumName(Type enumType)
    {
        if (enumType == null) return "";

        var displayAttr = enumType.GetCustomAttribute<DisplayNameAttribute>();
        if (displayAttr != null && !displayAttr.DisplayName.IsNullOrEmpty())
            return displayAttr.DisplayName;

        var descAttr = enumType.GetCustomAttribute<DescriptionAttribute>();
        if (descAttr != null && !descAttr.Description.IsNullOrEmpty())
            return descAttr.Description;

        return enumType.Name;
    }
    #endregion

    #region 列表型（[LovList]）
    /// <summary>按 LovCode 查找 [LovList] 声明式列表型值集描述符</summary>
    /// <param name="lovCode">值集编码，须以 List. 开头</param>
    /// <returns>描述符，未找到返回 null</returns>
    public static LovListDescriptor? FindList(String lovCode)
    {
        if (lovCode.IsNullOrEmpty()) return null;

        TryScan();

        if (_lists.TryGetValue(lovCode, out var desc)) return desc;

        // 程序集延迟加载场景补扫一次
        lock (_sync)
        {
            ScanAll();
            _scanned = true;
        }
        _lists.TryGetValue(lovCode, out desc);
        return desc;
    }

    /// <summary>由 [LovList] 特性构建列表型值集描述符（含自动推断与列/搜索字段解析）</summary>
    /// <param name="attr">特性实例</param>
    /// <param name="method">标注该特性的 Action 方法，用于自动推断路由等属性</param>
    /// <returns>描述符</returns>
    public static LovListDescriptor BuildDescriptor(LovListAttribute attr, MethodInfo method)
    {
        if (attr == null) throw new ArgumentNullException(nameof(attr));

        // 零参数 [LovList]：自动推断 LovCode（含区域段）/Name/RequestUrl/Method
        InferLovList(attr, method);

        if (attr.LovCode.IsNullOrEmpty())
            throw new InvalidOperationException("无法推断 LovCode，请在 [LovList] 特性中显式指定");

        // 校验前缀：列表型值集 LovCode 必须以 List. 开头
        if (!attr.LovCode.StartsWith("List.", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"LovList 特性的 LovCode 必须以 'List.' 开头：{attr.LovCode}");

        return new LovListDescriptor
        {
            LovCode = attr.LovCode,
            Name = attr.Name.IsNullOrEmpty() ? attr.LovCode : attr.Name,
            ValueField = attr.ValueField,
            LabelField = attr.LabelField,
            Config = new LovListConfigModel
            {
                RequestUrl = attr.RequestUrl,
                Method = attr.Method,
                Pageable = attr.Pageable,
                PageNumField = attr.PageNumField,
                PageSizeField = attr.PageSizeField,
                DataPath = attr.DataPath,
                TotalPath = attr.TotalPath,
                FixedParams = attr.FixedParams,
                ProxyRequest = attr.ProxyRequest,
            },
            SearchFields = ParseSearchFields(attr.SearchFields),
            TableColumns = ParseColumns(attr.Columns),
        };
    }

    /// <summary>解析表格列声明。元素格式 "Field:Title:Width:Align"（Width/Align 可省略）</summary>
    /// <param name="tokens">列声明数组</param>
    /// <returns>表格列列表</returns>
    public static IList<LovTableColumnModel> ParseColumns(String[]? tokens)
    {
        var list = new List<LovTableColumnModel>();
        if (tokens == null) return list;

        for (var i = 0; i < tokens.Length; i++)
        {
            var parts = tokens[i].Split(':');
            if (parts.Length < 2) continue;

            var col = new LovTableColumnModel
            {
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
    /// <param name="tokens">搜索字段声明数组</param>
    /// <returns>搜索字段列表</returns>
    public static IList<LovSearchFieldModel> ParseSearchFields(String[]? tokens)
    {
        var list = new List<LovSearchFieldModel>();
        if (tokens == null) return list;

        for (var i = 0; i < tokens.Length; i++)
        {
            var parts = tokens[i].Split(':');
            if (parts.Length < 2) continue;

            var sf = new LovSearchFieldModel
            {
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

    #region 扫描
    /// <summary>懒扫描。首次访问建立 枚举 FullName 索引 与 [LovList] 描述符索引</summary>
    private static void TryScan()
    {
        if (_scanned) return;

        lock (_sync)
        {
            if (_scanned) return;

            ScanAll();
            _scanned = true;
        }
    }

    /// <summary>全量扫描已加载程序集（幂等，可重复调用用于补扫延迟加载的程序集）</summary>
    private static void ScanAll()
    {
        using var span = DefaultTracer.Instance?.NewSpan(nameof(ScanAll));
        var asms = AppDomain.CurrentDomain.GetAssemblies();

        // 1) 枚举索引：按全限定名定位所有 public 枚举
        foreach (var asm in asms)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch
            {
                continue;
            }

            foreach (var t in types)
            {
                if (t != null && t.IsEnum && t.IsPublic && t.FullName != null)
                    _enumTypes.TryAdd(t.FullName, t);
            }
        }

        // 2) [LovList] 描述符：仅扫描引用 NewLife.Cube 的程序集（业务/入口程序集），跳过系统程序集
        foreach (var asm in asms)
        {
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
                        var desc = BuildDescriptor(attr, method);
                        _lists.TryAdd(desc.LovCode, desc);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }
                }
            }
        }
    }
    #endregion

    #region 自动推断
    /// <summary>推断 LovList 特性中未指定的属性（LovCode、Name、RequestUrl、Method）</summary>
    /// <param name="attr">特性实例</param>
    /// <param name="method">标注该特性的方法</param>
    public static void InferLovList(LovListAttribute attr, MethodInfo method)
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
        const String routeAttrName = "Microsoft.AspNetCore.Mvc.RouteAttribute";
        const String httpMethodAttrName = "Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute";

        var controllerName = controllerType.Name.Replace("Controller", "");

        var segments = new List<String>();

        // 控制器级别 [Route] 特性
        var controllerRoute = controllerType.GetCustomAttributes(inherit: true)
            .FirstOrDefault(a => a.GetType().FullName == routeAttrName);
        // 解析区域段（无区域特性则视为无区域，约定路由退化为 /api/{controller}/{action}）
        // 匹配逻辑与 InferLovList 保持一致：AreaBase 继承 AreaAttribute，需按继承链/类型名判断
        var areaAttr = controllerType?.GetCustomAttributes(inherit: true)
            .FirstOrDefault(a =>
            {
                var t = a.GetType();
                var baseFullName = t.BaseType?.FullName;
                return t.FullName == "Microsoft.AspNetCore.Mvc.AreaAttribute"
                    || baseFullName == "Microsoft.AspNetCore.Mvc.AreaAttribute"
                    || t.Name == "AreaBase"
                    || (baseFullName != null && baseFullName.EndsWith("AreaBase"));
            });
        var area = GetAreaName(areaAttr);

        if (controllerRoute != null)
        {
            var template = GetAttributeProperty(controllerRoute, "Template") as String;
            if (!template.IsNullOrEmpty())
            {
                template = template.Trim('/');
                // 替换令牌
                if (template.Contains("[area]"))
                    template = template.Replace("[area]", area);
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

        // 约定路由（无显式 [Route]）需补区域段：api/{area}/{controller}/{action}
        // 若 segments 首个元素已含区域路由（如 [area]/... 已被替换），则不重复添加
        var areaPrefix = area.IsNullOrEmpty() ? "" : area + "/";
        if (!area.IsNullOrEmpty() && segments.Count > 0 &&
            !segments[0].StartsWith(area + "/", StringComparison.OrdinalIgnoreCase) &&
            !segments[0].StartsWith("api/", StringComparison.OrdinalIgnoreCase))
        {
            segments.Insert(0, area);
        }

        var path = String.Join("/", segments);

        // 拼接 API 前缀（WebAPI版固定 /api，写死不配置），避免重复前缀
        if (!path.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            path = "api/" + path.TrimStart('/');

        return "/" + path;
    }

    /// <summary>从区域特性解析区域名（AreaBase 继承 AreaAttribute，优先 RouteValue 再 Name）</summary>
    private static String GetAreaName(Object? areaAttr)
    {
        if (areaAttr == null) return "";
        var area = GetAttributeProperty(areaAttr, "RouteValue") as String;
        if (area.IsNullOrEmpty()) area = GetAttributeProperty(areaAttr, "Name") as String;
        return area ?? "";
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
}
