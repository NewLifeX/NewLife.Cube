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
        var tenantChanged = false;
        var dataScopeChanged = false;

        try
        {
            // 1. 设置租户上下文
            var set = CubeSetting.Current;
            if (set.EnableTenant && TenantContext.Current == null)
            {
                var tenantId = ctx.GetTenantId();
                if (tenantId >= 0)
                {
                    ctx.SetTenant(tenantId);
                    tenantChanged = true;
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
            if (tenantChanged) TenantContext.Current = null;
            if (dataScopeChanged) DataScopeContext.Current = null;
        }
    }
}