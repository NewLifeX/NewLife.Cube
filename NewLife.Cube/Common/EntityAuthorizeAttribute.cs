using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Membership;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>实体授权特性</summary>
public class EntityAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    #region 属性
    /// <summary>授权项</summary>
    public PermissionFlags Permission { get; }
    #endregion

    #region 构造
    static EntityAuthorizeAttribute() => XTrace.WriteLine("Registering filter: {0}", typeof(EntityAuthorizeAttribute).FullName);

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

        // 只验证管辖范围
        var create = false;
        if (!AreaBase.Contains(ctrl))
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
            return;
        }

        // 多租户模式隔离：租户模式禁止访问纯Admin菜单，管理后台禁止访问纯Tenant菜单
        var set = CubeSetting.Current;
        if (set.EnableTenant && menu != null && TenantContext.Current.GetTenantMode() != TenantMode.None)
        {
            var isTenant = TenantContext.Current.GetTenantMode() == TenantMode.Tenant;
            if (!MenuHelper.CheckVisible(ctrl.ControllerTypeInfo, isTenant))
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

        // 判断权限
        if (menu != null)
        {
            if (user is IUser user2) return user2.Has(menu, Permission); 

            var msg = L.T("Unable to verify permission of user [{1}] when accessing resource [{0}].", menu, user);
            LogProvider.Provider.WriteLog(L.T("Access"), L.T("Denied"), false, msg, ip: ctx.GetUserHost());
            DefaultSpan.Current?.AppendTag(msg);
        }
        else
        {
            LogProvider.Provider.WriteLog(L.T("Access"), L.T("Denied"), false, L.T("Menu not found"), ip: ctx.GetUserHost());
        }

        return false;
    }

    private void HandleUnauthorizedRequest(AuthorizationFilterContext filterContext, IMenu menu, IManageUser user)
    {
        // 来到这里，有可能没登录，有可能没权限
        var prv = ManageProvider.Provider;
        if (prv?.Current == null)
        {
            // 已认证但租户校验失败（token 有效、租户不匹配）：属授权失败，返回 403；
            // 与"未认证(401)"区分——租户问题刷新/重登无解（凭证本就有效），
            // 若返回 401 会诱导客户端把租户拒绝误判为 token 过期而触发无意义刷新（死循环、refresh_token 轮换耗尽）
            if (filterContext.HttpContext.Items[ManagerProviderHelper.TenantCheckFailedUserIdKey] is Int32 failedUserId)
            {
                var failedUser = filterContext.HttpContext.Items[ManagerProviderHelper.TenantCheckFailedKey] as String ?? "";
                // 按失败原因输出差异化文案（F4）：显式标识无效 / 缺租户标识 / 非租户成员
                var msg = filterContext.HttpContext.Items[ManagerProviderHelper.TenantCheckFailedReasonKey] as String switch
                {
                    "InvalidIdentifier" => L.T("The tenant identifier carried by user [{0}] is invalid or unavailable. Check the tenant settings and try again.", failedUser),
                    "MissingIdentifier" => L.T("User [{0}] did not carry a tenant identifier, so the tenant cannot be determined. Include the tenant information and try again.", failedUser),
                    _ => L.T("User [{0}] is not allowed to access the data of the current tenant. Switch tenant and try again.", failedUser),
                };
                // 落库审计日志到 Log 表（CreateLog + SaveAsync：一次失败事件一行审计记录，
                // 供管理后台审计日志查询；写入失败不影响主流程）
                try
                {
                    var log = LogProvider.Provider.CreateLog(L.T("Tenant"), L.T("Denied"), false, msg, failedUserId, failedUser, filterContext.HttpContext.GetUserHost());
                    log?.SaveAsync();
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine("Failed to write the tenant denial audit log: {0}", ex.Message);
                }
                filterContext.HttpContext.Response.StatusCode = 403;
                filterContext.Result = new JsonResult(new { code = 403, message = msg });
                return;
            }

            filterContext.HttpContext.Response.StatusCode = 401;
            filterContext.Result = new JsonResult(new { code = 401, message = L.T("Not signed in or the session has expired!") });
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
        LogProvider.Provider.WriteLog(L.T("Access"), L.T("Denied"), false, msg, ip: ctx.GetUserHost());
        DefaultSpan.Current?.AppendTag(msg);

        filterContext.HttpContext.Response.StatusCode = 403;
        return new JsonResult(new { code = 403, message = msg });
    }

    private IMenu ResolveMenu(AuthorizationFilterContext filterContext, Boolean create)
    {
        var act = (ControllerActionDescriptor)filterContext.ActionDescriptor;
        //var ctrl = act.ControllerDescriptor;
        var type = act.ControllerTypeInfo;
        var fullName = type.FullName + "." + act.ActionName;
        // WebAPI版实体/后台控制器路由固定 /api 前缀，菜单存前端路由，查找前去掉 /api
        var url = NewLife.Web.WebHelper.TrimApiPrefix(filterContext.HttpContext.Request.Path + "");

        var ctx = filterContext.HttpContext;
        var mf = ManageProvider.Menu;
        var menu = ManageProvider.CurrentMenu;
        menu ??= ctx.Items["CurrentMenu"] as IMenu;
        if (menu == null)
        {
            menu = mf.FindByFullName(fullName) ?? mf.FindByFullName(type.FullName) ?? mf.FindByUrl(url) ?? mf.FindByUrl("~" + url);

            // 缩短Url后再次尝试
            if (menu == null)
            {
                var url2 = "/" + url.Split("/").Take(3).Join("/");
                menu = mf.FindByUrl(url2);
            }

            if (menu != null)
            {
                ManageProvider.CurrentMenu = menu;

                // 兼容旧版本视图权限
                ctx.Items["CurrentMenu"] = menu;
            }
        }

        // 创建菜单
        if (create && menu == null)
        {
            if (CreateMenu(type)) menu = mf.FindByFullName(fullName);
        }

        if (menu == null) XTrace.WriteLine("Design error! Menu [{0}/{1}] was not found during permission validation.", type.FullName, act.ActionName);

        return menu;
    }

    private static readonly ConcurrentDictionary<String, Type> _ss = new ConcurrentDictionary<String, Type>();
    private Boolean CreateMenu(Type type)
    {
        // 按类型全名防重，避免同一命名空间下多个控制器（如 Admin 下的 UserController/ReactController）互相阻塞，导致独立类库控制器菜单被删后无法重建
        if (!_ss.TryAdd(type.FullName, type)) return false;

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
                // 取消判断返回值类型，避免 ObjectController/ConfigController 的 Update 返回 TObject 而生成不了修改权限项（对齐 MenuHelper.ScanActionMenu）
                //if (!method.ReturnType.As<ActionResult>()) continue;
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
