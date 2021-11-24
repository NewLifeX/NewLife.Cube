using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Membership
{
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
            if (root == null) root = r.FindByPath(rootName);
            //if (root == null) root = r.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase(rootName));
            //if (root == null) root = r.Childs.FirstOrDefault(e => e.Url.EqualIgnoreCase("~/" + rootName));
            if (root == null)
            {
                root = r.Add(rootName, null, nameSpace, "~/" + rootName);
                list.Add(root);

                var att = areaType.GetCustomAttribute<MenuAttribute>();
                if (att != null)
                {
                    root.Sort = att.Order;
                    root.Visible = att.Visible;
                    root.Icon = att.Icon;
                }
            }
            if (root.FullName != nameSpace) root.FullName = nameSpace;
            (root as IEntity).Save();

            var ms = new List<IMenu>();

            // 遍历该程序集所有类型
            foreach (var type in controllerTypes)
            {
                var name = type.Name.TrimEnd("Controller");
                var url = root.Url;
                var node = root;

                // 添加Controller
                var controller = node.FindByPath(name);
                if (controller == null)
                {
                    url += "/" + name;
                    controller = menuFactory.FindByUrl(url);
                    if (controller == null)
                    {
                        // DisplayName特性作为中文名
                        controller = node.Add(name, type.GetDisplayName(), type.FullName, url);

                        //list.Add(controller);
                    }
                }
                if (controller.FullName.IsNullOrEmpty()) controller.FullName = type.FullName;
                if (controller.Remark.IsNullOrEmpty()) controller.Remark = type.GetDescription();

                ms.Add(controller);
                list.Add(controller);

                // 反射调用控制器的方法来获取动作
                var func = type.GetMethodEx("ScanActionMenu");
                if (func == null) continue;

                // 由于控制器使用IOC，无法直接实例化控制器，需要给各个参数传入空
                var ctor = type.GetConstructors()?.FirstOrDefault();
                var ctrl = ctor.Invoke(new Object[ctor.GetParameters().Length]);
                //var ctrl = type.CreateInstance();

                var acts = func.As<Func<IMenu, IDictionary<MethodInfo, Int32>>>(ctrl).Invoke(controller);
                if (acts == null || acts.Count == 0) continue;

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

                // 排序
                if (controller.Sort == 0)
                {
                    var att = type.GetCustomAttribute<MenuAttribute>();
                    if (att != null)
                    {
                        controller.Sort = att.Order;
                        controller.Visible = att.Visible;
                        controller.Icon = att.Icon;
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
                ThreadPoolX.QueueUserWorkItem(() =>
                {
                    XTrace.WriteLine("新增了菜单，需要检查权限");
                    //var fact = ManageProvider.GetFactory<IRole>();
                    var fact = typeof(Role).AsFactory();
                    fact.EntityType.Invoke("CheckRole");
                });
            }

            return list;
        }
    }
}