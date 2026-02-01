using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
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

        // 搜索所有控制器，找到本区域所属控制器，优先属性其次命名空间
        //var types = areaType.Assembly.GetTypes();
        //var controllerTypes = types.Where(e => e.Name.EndsWith("Controller") && e.Namespace == nameSpace).ToList();
        var controllerTypes = new List<Type>();
        var target = areaType.Assembly.GetName().Name;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm != areaType.Assembly && !asm.GetReferencedAssemblies().Any(e => e.Name == target)) continue;

            foreach (var item in asm.GetTypes())
            {
                if (!item.Name.EndsWith("Controller")) continue;

                // 优先使用特性
                var atts = item.GetCustomAttributes();
                if (atts.Any(e => e.GetType() == areaType))
                {
                    controllerTypes.Add(item);
                }
                else if (item.Namespace == nameSpace && !atts.Any(e => e is AreaAttribute))
                {
                    controllerTypes.Add(item);
                }
            }
        }
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
                root.Ex4 = attArea.HelpUrl;
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
            var name = type.Name.TrimEnd("Controller");
            var url = root.Url + "/" + name;
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
                controller ??= node.Add(name, type.GetDisplayName(), type.FullName, url);
            }
            controller.Url = url;
            controller.FullName = type.FullName;
            if (controller.Remark.IsNullOrEmpty()) controller.Remark = type.GetDescription();

            ms.Add(controller);
            list.Add(controller);

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

                    var dn = method.GetDisplayName();
                    if (!dn.IsNullOrEmpty()) dn = dn.Replace("{type}", (controller as Menu)?.FriendName);

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

                            var dn = method.GetDisplayName();
                            if (!dn.IsNullOrEmpty()) dn = dn.Replace("{type}", (controller as Menu)?.FriendName);

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
                if (controller.Ex4.IsNullOrEmpty()) controller.Ex4 = att.HelpUrl;

                // 小于该更新时间的菜单设置将被覆盖
                if ((controller.UpdateTime < att.LastUpdate.ToDateTime() || attArea != null && controller.UpdateTime < attArea.LastUpdate.ToDateTime()) &&
                    (!controller.Visible || !controller.Necessary))
                {
                    controller.Sort = att.Order;
                    controller.Visible = att.Visible;

                    if (!att.Icon.IsNullOrEmpty()) controller.Icon = att.Icon;
                    if (!att.HelpUrl.IsNullOrEmpty()) controller.Ex4 = att.HelpUrl;
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
                XTrace.WriteLine("新增了菜单，需要检查权限");
                //var fact = typeof(Role).AsFactory();
                //fact.EntityType.Invoke("CheckRole");
                Role.CheckRole();
            }, TaskCreationOptions.LongRunning);
            task.Wait(5_000);
        }

        return list;
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

            if (!method.ReturnType.As<ActionResult>() && !method.ReturnType.As<Task<ActionResult>>()) continue;

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
                m2 ??= menu.Parent.Add(name, method.GetDisplayName(), $"{controllerType.FullName}.{name}", $"{menu.Url}/{name}");
                if (m2.Sort == 0) m2.Sort = attMenu.Order;
                if (m2.Icon.IsNullOrEmpty()) m2.Icon = attMenu.Icon;
                if (m2.Ex4.IsNullOrEmpty()) m2.Ex4 = attMenu.HelpUrl;
                if (m2.FullName.IsNullOrEmpty()) m2.FullName = $"{controllerType.FullName}.{name}";
                if (attAuth != null) m2.Permissions[(Int32)attAuth.Permission] = attAuth.Permission.GetDescription();
                if (m2 is IEntity entity) entity.Update();
            }
            else
            {
                //var attAuth = method.GetCustomAttribute<EntityAuthorizeAttribute>();
                if (attAuth != null && attAuth.Permission > PermissionFlags.None) dic.Add(method, (Int32)attAuth.Permission);
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

    /// <summary>根据租户隔离菜单</summary>
    /// <param name="menus"></param>
    /// <param name="isTenant"></param>
    /// <returns></returns>
    public static IList<MenuTree> FilterByTenant(IList<MenuTree> menus, Boolean isTenant)
    {
        var list = new List<MenuTree>();

        foreach (var item in menus)
        {
            if (!item.FullName.IsNullOrEmpty())
            {
                // 控制器菜单是否支持租户显示
                if (CheckVisibleInTenant(item))
                {
                    // 支持租户显示，且当前是租户，则显示
                    if (isTenant)
                        list.Add(item);
                    // 同时支持租户和管理员显示
                    else if (CheckVisibleInAdmin(item))
                        list.Add(item);
                }
                else
                {
                    // 不支持租户显示，且不是租户，则显示
                    if (!isTenant)
                        list.Add(item);
                    else if (item.Children != null)
                    {
                        // 虽然当前大菜单不支持租户显示，但是子菜单支持，则显示
                        if (item.Children.Any(e => CheckVisibleInTenant(e)))
                            list.Add(item);
                    }
                }
            }
        }

        return list;
    }

    static ConcurrentDictionary<String, Boolean> _tenants = [];
    static Boolean CheckVisibleInTenant(MenuTree menu)
    {
        var key = menu.FullName;
        if (_tenants.TryGetValue(key, out var rs)) return rs;

        var type = Type.GetType(menu.FullName);
        if (type == null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(menu.FullName);
                if (type != null) break;
            }
        }
        var att = type?.GetCustomAttribute<MenuAttribute>();
        if (att != null && att.Mode.Has(MenuModes.Tenant))
        {
            return _tenants[key] = true;
        }

        return _tenants[key] = false;
    }

    static ConcurrentDictionary<String, Boolean> _admins = [];
    static Boolean CheckVisibleInAdmin(MenuTree menu)
    {
        var key = menu.FullName;
        if (_admins.TryGetValue(key, out var rs)) return rs;

        var type = Type.GetType(menu.FullName);
        var att = type?.GetCustomAttribute<MenuAttribute>();
        if (att != null && att.Mode.Has(MenuModes.Admin))
        {
            return _admins[key] = true;
        }

        return _admins[key] = false;
    }
}