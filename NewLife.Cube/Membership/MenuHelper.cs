using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // 所有控制器
        var types = areaType.Assembly.GetTypes();
        var controllerTypes = types.Where(e => e.Name.EndsWith("Controller") && e.Namespace == nameSpace).ToList();
        if (controllerTypes.Count == 0) return list;

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

            var att = areaType.GetCustomAttribute<MenuAttribute>();
            if (att != null && (!root.Visible || !root.Necessary))
            {
                root.Sort = att.Order;
                root.Visible = att.Visible;
                root.Icon = att.Icon;
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
            }

            // 排序
            if (controller.Sort == 0)
            {
                if (att != null)
                {
                    if (!root.Visible || !root.Necessary)
                    {
                        controller.Sort = att.Order;
                        controller.Visible = att.Visible;
                    }
                }
                else
                {
                    var pi = type.GetPropertyEx("MenuOrder");
                    if (pi != null) controller.Sort = pi.GetValue(null).ToInt();
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
            var task = Task.Run(() =>
            {
                XTrace.WriteLine("新增了菜单，需要检查权限");
                //var fact = ManageProvider.GetFactory<IRole>();
                var fact = typeof(Role).AsFactory();
                fact.EntityType.Invoke("CheckRole");
            });
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
                m2 ??= menu.Parent.Add(name, method.GetDisplayName(), $"{controllerType.FullName}.{name}", $"{menu.Url}/{name}");
                if (m2.Sort == 0) m2.Sort = attMenu.Order;
                if (m2.Icon.IsNullOrEmpty()) m2.Icon = attMenu.Icon;
                if (m2.FullName.IsNullOrEmpty()) m2.FullName = $"{controllerType.FullName}.{name}";
                if (attAuth != null) m2.Permissions[(Int32)attAuth.Permission] = attAuth.Permission.GetDescription();
                if (m2 is IEntity entity) entity.Update();
            }
            else
            {
                //var attAuth = method.GetCustomAttribute<EntityAuthorizeAttribute>();
                if (attAuth != null && attAuth.Permission > PermissionFlags.None) 
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
}