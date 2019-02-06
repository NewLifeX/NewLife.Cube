using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Membership;
//using ManagerProviderHelper = NewLife.Cube.Membership.ManagerProviderHelper;

namespace NewLife.Cube
{
    /// <summary>实体授权特性</summary>
    public class EntityAuthorizeAttribute: Attribute,IAuthorizationFilter
    {
        #region 属性
        /// <summary>授权项</summary>
        public PermissionFlags Permission { get; }

        /// <summary>是否全局特性</summary>
        internal Boolean IsGlobal;
        #endregion

        #region 构造
        static EntityAuthorizeAttribute()
        {
            XTrace.WriteLine("注册过滤器：{0}", typeof(EntityAuthorizeAttribute).FullName);
        }

        /// <summary>实例化实体授权特性</summary>
        public EntityAuthorizeAttribute() { }

        /// <summary>实例化实体授权特性</summary>
        /// <param name="permission"></param>
        public EntityAuthorizeAttribute(PermissionFlags permission)
        {
            if (permission <= PermissionFlags.None) throw new ArgumentNullException(nameof(permission));

            Permission = permission;
        }
        #endregion

        #region 方法

       
        /// <summary>授权发生时触发</summary>
        /// <param name="filterContext"></param>
        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            /*
             * 验证范围：
             * 1，魔方区域下的所有控制器
             * 2，所有带有EntityAuthorize特性的控制器或动作
             */
            var act = filterContext.ActionDescriptor;
            var ctrl = (ControllerActionDescriptor)act;

            // 允许匿名访问时，直接跳过检查
            if (
                ctrl.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute)) ||
                ctrl.ControllerTypeInfo.IsDefined(typeof(AllowAnonymousAttribute))) return;

            // 如果控制器或者Action放有该特性，则跳过全局
            var hasAtt =
                ctrl.MethodInfo.IsDefined(typeof(EntityAuthorizeAttribute), true) || 
                ctrl.ControllerTypeInfo.IsDefined(typeof(EntityAuthorizeAttribute));

            if (IsGlobal && hasAtt) return;

            // 只验证管辖范围
            var create = false;
            if (!AreaBase.Contains(ctrl))
            {
                if (!hasAtt) return;
                // 不属于魔方而又加了权限特性，需要创建菜单
                create = true;
            }

            // 根据控制器定位资源菜单
            var menu = GetMenu(filterContext, create);

            // 如果已经处理过，就不处理了
            if (filterContext.Result != null) return;

            if (!AuthorizeCore(filterContext.HttpContext))
            {
                HandleUnauthorizedRequest(filterContext);
            }
        }

        /// <summary>授权核心</summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        protected  Boolean AuthorizeCore(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var prv = ManageProvider.Provider;
            var ctx = httpContext;

            // 判断当前登录用户
            var user = ManagerProviderHelper.TryLogin(prv, httpContext.RequestServices);
            if (user == null) return false;

            // 判断权限
            if (!(ctx.Items["CurrentMenu"] is IMenu menu) || !(user is IUser user2)) return false;
            return user2.Has(menu, Permission);
        }

        /// <summary>未认证请求</summary>
        /// <param name="filterContext"></param>
        protected void HandleUnauthorizedRequest(AuthorizationFilterContext filterContext)
        {
            // 来到这里，有可能没登录，有可能没权限
            var prv = ManageProvider.Provider;
            if (prv?.Current == null)
            {
                var retUrl = filterContext.HttpContext.Request.GetEncodedPathAndQuery();
                    //.Host.ToString();//.Url?.PathAndQuery;

                var rurl = "~/Admin/User/Login".AppendReturn(retUrl);
                filterContext.Result = new RedirectResult(rurl);
            }
            else
            {
                filterContext.Result = filterContext.NoPermission(Permission);
            }
        }

        private IMenu GetMenu(AuthorizationFilterContext filterContext, Boolean create)
        {
            var act = (ControllerActionDescriptor)filterContext.ActionDescriptor;
            //var ctrl = act.ControllerDescriptor;
            var type = act.ControllerTypeInfo;
            var fullName = type.FullName + "." + act.ActionName;

            var ctx = filterContext.HttpContext;
            var mf = ManageProvider.Menu;
            var menu = ctx.Items["CurrentMenu"] as IMenu;
            if (menu == null)
            {
                menu = mf.FindByFullName(fullName) ?? mf.FindByFullName(type.FullName);

                // 当前菜单
                //filterContext.Controller.ViewBag.Menu = menu;
                // 兼容旧版本视图权限
                ctx.Items["CurrentMenu"] = menu;
            }

            // 创建菜单
            if (create)
            {
                if (CreateMenu(type)) menu = mf.FindByFullName(fullName);
                //var name = type.Namespace.TrimEnd(".Controllers");
                //var root = mf.FindByFullName(name);
                //if (root == null)
                //{
                //    root = mf.Root.GetType().CreateInstance() as IMenu;
                //    root.FullName = name;
                //    root.Name = name;
                //    (root as IEntity).Insert();
                //}

                //var node = mf.Root.GetType().CreateInstance() as IMenu;
                //node.FullName = type.FullName + "." + act.ActionName;
                //node.Name = type.Name;
                //node.DisplayName = type.GetDisplayName();
                //node.ParentID = root.ID;
                //(node as IEntity).Insert();
            }

            if (menu == null) XTrace.WriteLine("设计错误！验证权限时无法找到[{0}/{1}]的菜单", type.FullName, act.ActionName);

            return menu;
        }

        private static ConcurrentDictionary<String, Type> _ss = new ConcurrentDictionary<String, Type>();
        private Boolean CreateMenu(Type type)
        {
            if (!_ss.TryAdd(type.Namespace, type)) return false;

            var mf = ManageProvider.Menu;
            var ms = mf.ScanController(type.Namespace.TrimEnd(".Controllers"), type.Assembly, type.Namespace);

            var root = mf.FindByFullName(type.Namespace);
            if (root != null)
            {
                root.Url = "~";
                (root as IEntity).Save();
            }

            // 遍历菜单，设置权限项
            foreach (var controller in ms)
            {
                if (controller.FullName.IsNullOrEmpty()) continue;

                var ctype = type.Assembly.GetType(controller.FullName);
                //ctype = controller.FullName.GetTypeEx(false);
                if (ctype == null) continue;

                // 添加该类型下的所有Action
                var dic = new Dictionary<MethodInfo, Int32>();
                foreach (var method in ctype.GetMethods())
                {
                    if (method.IsStatic || !method.IsPublic) continue;
                    if (!method.ReturnType.As<ActionResult>()) continue;
                    if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null) continue;

                    var att = method.GetCustomAttribute<EntityAuthorizeAttribute>();
                    if (att != null && att.Permission > PermissionFlags.None)
                    {
                        var dn = method.GetDisplayName();
                        var pmName = !dn.IsNullOrEmpty() ? dn : method.Name;
                        if (att.Permission <= PermissionFlags.Delete) pmName = att.Permission.GetDescription();
                        controller.Permissions[(Int32)att.Permission] = pmName;
                    }
                }

                controller.Url = "~/" + ctype.Name.TrimEnd("Controller");

                (controller as IEntity).Save();
            }

            return true;
        }
        #endregion 
    }
}
