using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NewLife.Cube.Controllers;
using NewLife.Cube.Extensions;
using NewLife.Cube.Membership;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>实体授权特性</summary>
public class EntityAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    #region 属性
    /// <summary>授权项。多授权项时取第一个，用于菜单权限项展示</summary>
    public PermissionFlags Permission { get; }

    /// <summary>授权项集合。多个授权项之间是“或”的关系；全部为 None 时表示只需登录</summary>
    public PermissionFlags[] Permissions { get; } = [PermissionFlags.None];
    #endregion

    #region 构造
    static EntityAuthorizeAttribute() => XTrace.WriteLine(L.TLog("Register filter: {0}", typeof(EntityAuthorizeAttribute).FullName));

    /// <summary>实例化实体授权特性。只需登录，不校验具体权限位</summary>
    public EntityAuthorizeAttribute() { }

    /// <summary>实例化实体授权特性</summary>
    /// <param name="permission"></param>
    public EntityAuthorizeAttribute(PermissionFlags permission)
    {
        // None 表示只需登录，All(-1) 为合法的全权限组合；仅拒绝未知负值
        if (permission < PermissionFlags.None && permission != PermissionFlags.All) throw new ArgumentNullException(nameof(permission));

        Permission = permission;
        Permissions = [permission];
    }

    /// <summary>实例化实体授权特性。多个授权项之间是“或”的关系</summary>
    /// <param name="permissions"></param>
    public EntityAuthorizeAttribute(params PermissionFlags[] permissions)
    {
        if (permissions == null || permissions.Length == 0) throw new ArgumentNullException(nameof(permissions));

        Permissions = permissions;
        Permission = permissions[0];
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

        // 只验证管辖范围
        var create = false;
        if (!AreaBase.Contains(ctrl) && ctrl.ControllerTypeInfo.FullName != typeof(CubeController).FullName)
        {
            if (!hasAtt) return;

            // 不属于魔方而又加了权限特性，需要创建菜单
            create = true;
        }

        //using var span = DefaultTracer.Instance?.NewSpan(nameof(OnAuthorization));
        var span = DefaultSpan.Current;

        // 根据控制器定位资源菜单
        var menu = ResolveMenu(filterContext, create);
        span?.AppendTag($"menu: {menu}");

        // 如果已经处理过，就不处理了
        if (filterContext.Result != null) return;

        if (!AuthorizeCore(filterContext.HttpContext, menu, out var user))
        {
            HandleUnauthorizedRequest(filterContext, menu, user);
        }
    }

    private Boolean AuthorizeCore(Microsoft.AspNetCore.Http.HttpContext httpContext, IMenu menu, out IManageUser user)
    {
        var prv = ManageProvider.Provider;
        var ctx = httpContext;

        // 判断当前登录用户
        user = ManagerProviderHelper.TryLogin(prv, httpContext);
        if (user == null) return false;

        // 如果没有指定权限，则只要登录即可
        if (Permissions.All(e => e == PermissionFlags.None)) return true;

        // 判断权限（多个授权项之间是“或”的关系）
        if (menu != null)
        {
            if (user is IUser user2) return user2.Has(menu, Permissions);

            var msg = $"访问资源[{menu}]时无法验证用户[{user}]的权限";
            LogProvider.Provider.WriteLog(L.TLog("Access"), L.TLog("Deny"), false, msg, ip: ctx.GetUserHost());
            DefaultSpan.Current?.AppendTag(msg);
        }
        else
        {
            LogProvider.Provider.WriteLog(L.TLog("Access"), L.TLog("Deny"), false, L.TLog("Cannot find the menu"), ip: ctx.GetUserHost());
        }

        return false;
    }

    private void HandleUnauthorizedRequest(AuthorizationFilterContext filterContext, IMenu menu, IManageUser user)
    {
        // 来到这里，有可能没登录，有可能没权限
        var prv = ManageProvider.Provider;
        if (prv?.Current == null)
        {
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.HttpContext.Response.StatusCode = 401;
                filterContext.Result = new JsonResult(new { code = 401, message = "没有登录或登录超时！" });
            }
            else
            {
                var retUrl = filterContext.HttpContext.Request.GetEncodedPathAndQuery();
                //.Host.ToString();//.Url?.PathAndQuery;

                var rurl = "~/Admin/User/Login".AppendReturn(retUrl);
                filterContext.Result = new RedirectResult(rurl);
            }
        }
        else
        {
            filterContext.Result = NoPermission(filterContext, menu, Permission, user);
        }
    }

    /// <summary>无权访问</summary>
    /// <param name="filterContext"></param>
    /// <param name="menu"></param>
    /// <param name="permission"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public static ActionResult NoPermission(AuthorizationFilterContext filterContext, IMenu menu, PermissionFlags permission, IManageUser user)
    {
        var act = (ControllerActionDescriptor)filterContext.ActionDescriptor;
        var ctrl = act;

        var ctx = filterContext.HttpContext;

        var res = $"[{ctrl.ControllerName}/{act.ActionName}]";
        var msg = L.T("User {0} accessing resource {1} requires {2} permission.", user, res, L.T(permission.GetDescription()));
        LogProvider.Provider.WriteLog(L.TLog("Access"), L.TLog("Deny"), false, msg, ip: ctx.GetUserHost());
        DefaultSpan.Current?.AppendTag(msg);

        if (filterContext.HttpContext.Request.IsAjaxRequest())
        {
            filterContext.HttpContext.Response.StatusCode = 403;
            return new JsonResult(new { code = 403, message = msg });
        }

        var vr = new ViewResult()
        {
            ViewName = "NoPermission"
        };

        vr.ViewData =
            new ViewDataDictionary(new EmptyModelMetadataProvider(), filterContext.ModelState)
            {
                ["Resource"] = res,
                ["Permission"] = permission,
                ["Menu"] = menu
            };
        return vr;
    }

    private IMenu ResolveMenu(AuthorizationFilterContext filterContext, Boolean create)
    {
        var act = (ControllerActionDescriptor)filterContext.ActionDescriptor;
        //var ctrl = act.ControllerDescriptor;
        var type = act.ControllerTypeInfo;
        var fullName = type.FullName + "." + act.ActionName;
        var url = filterContext.HttpContext.Request.Path + "";

        var ctx = filterContext.HttpContext;
        var mf = ManageProvider.Menu;
        if (ctx.Items["CurrentMenu"] is not IMenu menu)
        {
            menu = mf.FindByFullName(fullName) ?? mf.FindByFullName(type.FullName) ?? mf.FindByUrl(url) ?? mf.FindByUrl("~" + url);

            // 缩短Url后再次尝试
            if (menu == null)
            {
                var url2 = "/" + url.Split("/").Take(3).Join("/");
                menu = mf.FindByUrl(url2);
            }

            // 兼容旧版本视图权限
            ctx.Items["CurrentMenu"] = menu;
        }

        // 创建菜单
        if (create && menu == null)
        {
            if (CreateMenu(type)) menu = mf.FindByFullName(fullName);
        }

        if (menu == null) XTrace.WriteLine(L.TLog("Design error! Cannot find menu for [{0}/{1}] during permission check", type.FullName, act.ActionName));

        return menu;
    }

    private static readonly ConcurrentDictionary<String, Type> _ss = new ConcurrentDictionary<String, Type>();
    private Boolean CreateMenu(Type type)
    {
        if (!_ss.TryAdd(type.Namespace, type)) return false;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(CreateMenu), type.FullName);

        var mf = ManageProvider.Menu;
        //var ms = mf.ScanController(type.Namespace.TrimSuffix(".Controllers"), type.Assembly, type.Namespace);
        var ms = MenuHelper.ScanController(mf, type.Namespace.TrimSuffix(".Controllers"), type);

        var root = mf.FindByFullName(type.Namespace);
        if (root != null)
        {
            root.Url = "~";
            (root as IEntity).Update();
        }

        // 遍历菜单，设置权限项
        foreach (var controller in ms)
        {
            if (controller.FullName.IsNullOrEmpty()) continue;

            var ctype = type.Assembly.GetType(controller.FullName);
            //ctype = controller.FullName.GetTypeEx(false);
            if (ctype == null) continue;

            // 添加该类型下的所有Action
            //var dic = new Dictionary<MethodInfo, Int32>();
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

            controller.Url = "~/" + ctype.Name.TrimSuffix("Controller");

            (controller as IEntity).Update();
        }

        return true;
    }
    #endregion 
}
