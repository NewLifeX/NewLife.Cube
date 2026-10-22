using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Membership;

/// <summary>
/// 菜单助手
/// </summary>
public static class MenuHelper
{
    /// <summary>获取菜单显示名。自动生成的菜单按当前请求语言翻译，用户自定义名称缺词条时原样返回</summary>
    /// <param name="menu">菜单</param>
    /// <returns>本地化显示名</returns>
    public static String? GetDisplayName(IMenu menu)
    {
        if (menu == null) return null;

        var name = menu.DisplayName;
        if (name.IsNullOrEmpty()) name = menu.Name;

        return name.IsNullOrEmpty() ? name : L.T(name);
    }

    /// <summary>获取权限子项显示名。标准权限位从枚举取源文，自定义动作保留 {type} 模板</summary>
    /// <param name="menu">菜单</param>
    /// <param name="key">权限位</param>
    /// <param name="value">落库的权限名称</param>
    /// <returns>本地化权限名称</returns>
    public static String? GetPermissionName(IMenu menu, Int32 key, String? value)
    {
        var flag = (PermissionFlags)(UInt32)key;
        if (key > 0 && key <= (Int32)PermissionFlags.Delete && Enum.IsDefined(typeof(PermissionFlags), flag))
            return L.T(flag.GetDescription());

        var text = L.T(value);
        if (!text.IsNullOrEmpty() && text.Contains("{type}")) text = text.Replace("{type}", GetDisplayName(menu));

        return text;
    }

    /// <summary>获取当前语言下的权限子项字典</summary>
    /// <param name="menu">菜单</param>
    /// <returns>权限位到本地化名称的映射</returns>
    public static Dictionary<Int32, String> GetPermissions(IMenu menu)
    {
        var dic = new Dictionary<Int32, String>();
        if (menu?.Permissions == null) return dic;

        foreach (var item in menu.Permissions) dic[item.Key] = GetPermissionName(menu, item.Key, item.Value);

        return dic;
    }

    /// <summary>判断控制器菜单在指定模式下是否可见。租户模式（isTenant=true）仅放行含Tenant模式的菜单；管理后台仅放行含Admin模式或未声明模式的菜单</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="isTenant">是否租户模式（TenantId&gt;0）</param>
    /// <returns>模式允许访问返回true，否则返回false（需拦截）</returns>
    public static Boolean CheckVisible(Type controllerType, Boolean isTenant)
    {
        var att = controllerType?.GetCustomAttribute<MenuAttribute>();
        if (att == null) return true;

        // 租户模式：必须声明Tenant模式才可见
        if (isTenant) return att.Mode.Has(MenuModes.Tenant);

        // 管理后台：含Admin模式，或未声明任何模式（默认仅管理后台可见）才可见
        return att.Mode.Has(MenuModes.Admin) || !att.Mode.Has(MenuModes.Tenant);
    }

    /// <summary>判断菜单是否租户相关。用于未开启多租户时隐藏租户菜单（对齐 MVC FixTenantMenu 的 Name 含 Tenant 规则）：
    /// 1) 菜单名包含 "Tenant"（租户管理/租户成员等）；2) 或控制器仅声明 <see cref="MenuModes.Tenant"/> 模式（无 Admin）。
    /// 注意：同时声明 Admin|Tenant 的通用菜单（如用户管理）不视为租户菜单，多租户关闭时仍显示</summary>
    /// <param name="menu">菜单实体</param>
    /// <returns>租户相关菜单返回true</returns>
    public static Boolean IsTenantMenu(IMenu menu)
    {
        if (menu == null) return false;

        // 菜单名含 Tenant（对齐 MVC FixTenantMenu）
        if (!menu.Name.IsNullOrEmpty() && menu.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase)) return true;

        // 控制器仅声明 Tenant 模式（无 Admin）也算租户菜单
        return IsTenantMenu(menu.FullName);
    }

    /// <summary>判断控制器全名（FullName）是否租户相关。菜单名由控制器名派生（去 Controller 后缀，见 ScanController），
    /// 因此：1) 类型名含 "Tenant"；2) 或仅声明 <see cref="MenuModes.Tenant"/> 而无 <see cref="MenuModes.Admin"/></summary>
    /// <param name="fullName">菜单 FullName，通常为控制器类型全名</param>
    /// <returns>租户相关返回true</returns>
    public static Boolean IsTenantMenu(String fullName)
    {
        if (fullName.IsNullOrEmpty()) return false;

        var type = fullName.GetTypeEx();
        if (type == null) return false;

        // 类型名含 Tenant（对应菜单名含 Tenant）
        if (type.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase)) return true;

        var att = type.GetCustomAttribute<MenuAttribute>();
        if (att == null) return false;

        return att.Mode.Has(MenuModes.Tenant) && !att.Mode.Has(MenuModes.Admin);
    }

    /// <summary>扫描命名空间下的控制器并添加为菜单</summary>
    /// <param name="menuFactory">菜单工厂</param>
    /// <param name="rootName">根菜单名称，所有菜单附属在其下</param>
    /// <param name="areaType">区域类型</param>
    /// <returns></returns>
    public static IList<IMenu> ScanController(this IMenuFactory menuFactory, String rootName, Type areaType)
    {
        var nameSpace = areaType.Namespace.EnsureEnd(".Controllers");
        using var span = DefaultTracer.Instance?.NewSpan(nameof(ScanController), rootName);

        var list = new List<IMenu>();

        // 所有控制器
        var types = areaType.Assembly.GetTypes();
        var controllerTypes = types.Where(e => e.Name.EndsWith("Controller") && e.Namespace == nameSpace).ToList();
        if (controllerTypes.Count == 0) return list;

        var attArea = areaType.GetCustomAttribute<MenuAttribute>();

        // 如果根菜单不存在，则添加
        var r = menuFactory.Root;
        var root = menuFactory.FindByFullName(nameSpace);
        root ??= r.FindByPath(rootName);
        //if (root == null) root = r.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase(rootName));
        //if (root == null) root = r.Childs.FirstOrDefault(e => e.Url.EqualIgnoreCase("~/" + rootName));
        if (root == null)
        {
            root = r.Add(rootName, null, nameSpace, "/" + rootName);
            list.Add(root);

            if (attArea != null && (!root.Visible || !root.Necessary))
            {
                root.Sort = attArea.Order;
                root.Visible = attArea.Visible;
                root.Icon = attArea.Icon;
            }

            var dis = areaType.GetDisplayName();
            var des = areaType.GetDescription();

            if (!dis.IsNullOrEmpty())
            {
                root.DisplayName = dis;
            }

            if (!des.IsNullOrEmpty())
            {
                root.Remark = des;
            }
        }
        if (root.FullName != nameSpace) root.FullName = nameSpace;
        (root as IEntity).Save();

        // 菜单迁移 Admin->Cube
        IMenu adminRoot = null;
        if (rootName == "Cube") adminRoot = r.FindByPath("Admin");

        var ms = new List<IMenu>();

        // 遍历该程序集所有类型
        foreach (var type in controllerTypes)
        {
            var name = type.Name.TrimSuffix("Controller");
            // 菜单URL统一为前端路由，去掉开头的 /api（WebAPI版实体路由固定 /api 前缀）
            var url = NewLife.Web.WebHelper.TrimApiPrefix(root.Url + "/" + name);
            var node = root;

            // 添加Controller
            var controller = node.FindByPath(name);
            // 旧菜单迁移到新菜单
            if (adminRoot != null)
            {
                if (controller == null)
                {
                    controller = adminRoot.FindByPath(name);
                    if (controller != null)
                    {
                        controller.ParentID = root.ID;
                        controller.Url = url;
                    }
                }
                else
                {
                    var controller2 = adminRoot.FindByPath(name);
                    if (controller2 is IEntity entity) entity.Delete();
                }
            }
            if (controller == null)
            {
                controller = menuFactory.FindByUrl(url);

                // 菜单名是持久化数据，建菜单时保存源文；页面标题等运行时文案由消费端按请求语言翻译
                var displayName = type.GetDisplayName();

                controller ??= node.Add(name, displayName, type.FullName, url);
            }
            else
            {
                // 旧版本按启动语言落库的自动菜单，安全归一化为源文；用户自定义名称不动
                var source = type.GetDisplayName();
                if (!source.IsNullOrEmpty())
                {
                    var translated = L.T(source);
                    // 旧版本没有 DisplayName 特性，自动菜单落库为 URL 名称或类型名；用户自定义名称不动
                    if (controller.DisplayName.IsNullOrEmpty() || controller.DisplayName == name || controller.DisplayName == type.Name ||
                        (translated != source && controller.DisplayName == translated) ||
                        IsOtherLanguageName(controller.DisplayName, source))
                        controller.DisplayName = source;
                }
            }
            controller.Url = url;
            controller.FullName = type.FullName;
            if (controller.Remark.IsNullOrEmpty()) controller.Remark = type.GetDescription();

            ms.Add(controller);
            list.Add(controller);

            // 注册实体类型到页面的映射，供外键跳转链接使用
            var entityType = GetEntityType(type);
            if (entityType != null)
            {
                var factory = EntityFactory.CreateFactory(entityType);
                var pk = factory?.Unique?.Name ?? "ID";
                EntityPageRegistry.Register(entityType, url, pk);
            }

            // 获取动作
            var acts = ScanActionMenu(type, controller);
            if (acts != null && acts.Count > 0)
            {
                // 可选权限子项
                controller.Permissions.Clear();

                // 添加该类型下的所有Action作为可选权限子项
                foreach (var item in acts)
                {
                    var method = item.Key;

                    // 保留 {type} 模板，渲染期按当前语言翻译后再替换
                    var dn = method.GetDisplayName();

                    var pmName = !dn.IsNullOrEmpty() ? dn : method.Name;
                    if (item.Value <= (Int32)PermissionFlags.Delete) pmName = ((PermissionFlags)item.Value).GetDescription();
                    controller.Permissions[item.Value] = pmName;
                }
            }

            // 反射调用控制器的方法来获取动作。作为过渡，将来取消
            var func = type.GetMethodEx("ScanActionMenu");
            if (func != null)
            {
                // 由于控制器使用IOC，无法直接实例化控制器，需要给各个参数传入空
                var ctor = type.GetConstructors()?.FirstOrDefault();
                if (ctor != null)
                {
                    var ctrl = ctor.Invoke(new Object[ctor.GetParameters().Length]);
                    //var ctrl = type.CreateInstance();

                    acts = func.As<Func<IMenu, IDictionary<MethodInfo, Int32>>>(ctrl).Invoke(controller);
                    if (acts != null && acts.Count > 0)
                    {
                        // 可选权限子项
                        controller.Permissions.Clear();

                        // 添加该类型下的所有Action作为可选权限子项
                        foreach (var item in acts)
                        {
                            var method = item.Key;

                            // 保留 {type} 模板，渲染期按当前语言翻译后再替换
                            var dn = method.GetDisplayName();

                            var pmName = !dn.IsNullOrEmpty() ? dn : method.Name;
                            if (item.Value <= (Int32)PermissionFlags.Delete) pmName = ((PermissionFlags)item.Value).GetDescription();
                            controller.Permissions[item.Value] = pmName;
                        }
                    }
                }
            }

            var att = type.GetCustomAttribute<MenuAttribute>();
            if (att != null)
            {
                if (controller.Icon.IsNullOrEmpty()) controller.Icon = att.Icon;

                // 小于该更新时间的菜单设置将被覆盖
                if ((controller.UpdateTime < att.LastUpdate.ToDateTime() || attArea != null && controller.UpdateTime < attArea.LastUpdate.ToDateTime()) &&
                    (!controller.Visible || !controller.Necessary))
                {
                    controller.Sort = att.Order;
                    controller.Visible = att.Visible;
                }
            }

            // 排序
            if (controller.Sort == 0)
            {
                if (att != null)
                {
                    if (!controller.Visible || !controller.Necessary)
                    {
                        controller.Sort = att.Order;
                        controller.Visible = att.Visible;
                    }
                }
            }
        }

        var rs = 0;
        for (var i = 0; i < ms.Count; i++)
        {
            rs += (ms[i] as IEntity).Save();
        }

        // 如果新增了菜单，需要检查权限
        if (rs > 0)
        {
            var task = Task.Factory.StartNew(() =>
            {
                XTrace.WriteLine("New menus were added; permissions need to be checked.");
                //var fact = typeof(Role).AsFactory();
                //fact.EntityType.Invoke("CheckRole");
                Role.CheckRole();
            }, TaskCreationOptions.LongRunning);
            task.Wait(5_000);
        }

        return list;
    }

    /// <summary>判断落库菜单名是否为旧版本按其它语言源码保存的自动菜单名</summary>
    /// <param name="displayName">菜单落库的显示名</param>
    /// <param name="source">控制器 DisplayName 源文</param>
    /// <returns>需要按源文归一化时返回 true</returns>
    /// <remarks>
    /// i18n 改造期间源码 DisplayName 曾被批量改为英文并启动过站点，自动菜单把英文落库；
    /// 源码恢复中文后，落库值既不是源文也不是当前语言译文，原有判定识别不到。
    /// 这里按"源文含汉字、落库名为纯 ASCII"识别这类旧语言残留。
    /// </remarks>
    internal static Boolean IsOtherLanguageName(String? displayName, String source)
    {
        if (displayName.IsNullOrEmpty()) return false;
        if (!source.Any(e => e >= 0x4E00 && e <= 0x9FFF)) return false;

        return displayName.All(e => e < 0x80);
    }

    /// <summary>获取可用于生成权限菜单的Action集合</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="menu">该控制器所在菜单</param>
    /// <returns></returns>
    private static IDictionary<MethodInfo, Int32> ScanActionMenu(Type controllerType, IMenu menu)
    {
        var dic = new Dictionary<MethodInfo, Int32>();

        //var factory = type.GetProperty("Factory", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty) as IEntityFactory;
        var pi = controllerType.GetPropertyEx("Factory");
        var factory = pi?.GetValue(null, null) as IEntityFactory;
        //if (factory == null) return dic;

        // 设置显示名
        if (menu.DisplayName.IsNullOrEmpty() && factory != null)
        {
            menu.DisplayName = factory.Table.DataTable.DisplayName;
            menu.Visible = true;
            //menu.Save();
        }

        // 添加该类型下的所有Action
        foreach (var method in controllerType.GetMethods())
        {
            if (method.IsStatic || !method.IsPublic) continue;

            // 取消判断返回值类型，避免各种不同的返回值的方法生成不了权限项
            //if (!method.ReturnType.As<ActionResult>() && !method.ReturnType.As<Task<ActionResult>>()) continue;

            //if (method.GetCustomAttribute<HttpPostAttribute>() != null) continue;
            if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null) continue;

            var attAuth = method.GetCustomAttribute<EntityAuthorizeAttribute>();

            // 添加菜单
            var attMenu = method.GetCustomAttribute<MenuAttribute>();
            if (attMenu != null)
            {
                // 添加系统信息菜单
                var name = method.Name;
                var m2 = menu.Parent.Childs.FirstOrDefault(_ => _.Name == name);
                m2 ??= menu.Parent.Add(name, method.GetDisplayName(), $"{controllerType.FullName}.{name}", NewLife.Web.WebHelper.TrimApiPrefix($"{menu.Url}/{name}"));
                if (m2.Sort == 0) m2.Sort = attMenu.Order;
                if (m2.Icon.IsNullOrEmpty()) m2.Icon = attMenu.Icon;
                if (m2.FullName.IsNullOrEmpty()) m2.FullName = $"{controllerType.FullName}.{name}";
                if (attAuth != null) m2.Permissions[(Int32)attAuth.Permission] = attAuth.Permission.GetDescription();
                if (m2 is IEntity entity) entity.Update();
            }
            else
            {
                //var attAuth = method.GetCustomAttribute<EntityAuthorizeAttribute>();
                // 多授权项（或关系）不单独登记权限子项，避免覆盖它已包含的 Insert/Update 等显示名
                if (attAuth != null && attAuth.Permissions.Length == 1 && attAuth.Permission > PermissionFlags.None)
                    dic.Add(method, (Int32)attAuth.Permission);
            }
        }

        // 只写实体类过滤掉添删改权限
        if (factory != null && factory.Table.DataTable.InsertOnly)
        {
            var arr = new[] { PermissionFlags.Insert, PermissionFlags.Update, PermissionFlags.Delete }.Select(e => (Int32)e).ToArray();
            dic = dic.Where(e => !arr.Contains(e.Value)).ToDictionary(e => e.Key, e => e.Value);
        }

        return dic;
    }

    /// <summary>从控制器类型的继承链中提取 ReadOnlyEntityController&lt;TEntity&gt; 的泛型实参</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <returns>实体类型，非实体控制器返回 null</returns>
    private static Type GetEntityType(Type controllerType)
    {
        var t = controllerType;
        while (t != null && t != typeof(Object))
        {
            if (t.IsGenericType)
            {
                var name = t.GetGenericTypeDefinition().Name;
                if (name.StartsWith("ReadOnlyEntityController`") || name.StartsWith("EntityController`"))
                    return t.GetGenericArguments()[0];
            }
            t = t.BaseType;
        }
        return null;
    }
}