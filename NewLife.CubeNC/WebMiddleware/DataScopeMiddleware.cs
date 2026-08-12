using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.WebMiddleware;

/// <summary>上下文中间件。设置租户上下文和数据权限上下文</summary>
/// <param name="next"></param>
public class DataScopeMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    /// <summary>调用</summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext ctx)
    {
        // 找到租户，并设置上下文。该上下文将全局影响魔方和XCode
        var dataScopeChanged = false;
        // 保存进入时的租户上下文，finally 无条件恢复，防止 AsyncLocal 跨请求/后台任务泄漏
        var oldTenant = TenantContext.Current;

        try
        {
            // 1. 设置租户上下文
            var set = CubeSetting.Current;
            if (set.EnableTenant && TenantContext.Current == null)
            {
                var tenantId = ctx.GetTenantId();
                if (tenantId >= 0)
                {
                    // 已登录用户校验租户归属，防止越权访问其它租户数据（系统管理员除外）
                    var user = ManageProvider.User;
                    if (user != null && !user.Roles.Any(e => e.IsSystem))
                    {
                        // 租户0（管理后台）仅系统管理员可进入，普通用户访问非成员租户一律拒绝
                        var tu = tenantId > 0 ? TenantUser.FindByTenantIdAndUserId(tenantId, user.ID) : null;
                        if (tenantId == 0 || tu == null || !tu.Enable)
                        {
                            ctx.Response.StatusCode = 403;
                            await ctx.Response.WriteAsync("无权访问该租户");
                            return;
                        }
                    }

                    ctx.SetTenant(tenantId);
                }
                else if (HasExplicitTenant(ctx))
                {
                    // 显式提供了租户信息但解析无效（不存在/已禁用），拒绝请求，防止伪造租户绕过数据隔离
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsync("租户不存在或已禁用");
                    return;
                }
            }

            // 2. 设置数据权限上下文
            if (DataScopeContext.Current == null)
            {
                var user = ManageProvider.User;

                // 从路由或参数获取菜单。专用于菜单级别数据权限作用域（很少用）
                //var menuId = ctx.GetMenuId(); 
                var url = ctx.Request.Path + "";
                var menu = ManageProvider.Menu?.FindByUrl(url);

                DataScopeContext.Current = DataScopeContext.Create(user, menu);
                dataScopeChanged = true;
            }

            await _next.Invoke(ctx);
        }
        finally
        {
            // 无条件恢复租户上下文（无论本请求是否设置过），防止 AsyncLocal 值泄漏到下一个请求/后台任务
            TenantContext.Current = oldTenant;
            if (dataScopeChanged) DataScopeContext.Current = null;
        }
    }

    /// <summary>是否显式提供了租户信息（请求头或查询参数）</summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    private static Boolean HasExplicitTenant(HttpContext ctx)
        => !ctx.Request.Headers["X-Tenant"].ToString().IsNullOrEmpty()
        || !ctx.Request.Headers["X-Tenant-Id"].ToString().IsNullOrEmpty()
        || !ctx.Request.Query["tenantId"].ToString().IsNullOrEmpty();
}