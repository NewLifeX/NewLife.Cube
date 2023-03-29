using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.WebMiddleware;

/// <summary>租户中间件。设置租户上下文</summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>实例化</summary>
    /// <param name="next"></param>
    public TenantMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

    /// <summary>调用</summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext ctx)
    {
        // 找到租户，并设置上下文。该上下文将全局影响魔方和XCode
        var changed = false;
        try
        {
            if (TenantContext.Current == null)
            {
                var tenantId = ctx.Request.Cookies["TenantId"].ToInt(-1);
                if (tenantId > 0)
                {
                    TenantContext.Current = new TenantContext { TenantId = tenantId };
                    ManageProvider.Provider.Tenant = Tenant.FindById(tenantId);

                    changed = true;
                }
            }

            await _next.Invoke(ctx);
        }
        finally
        {
            if (changed)
            {
                TenantContext.Current = null;

                ManageProvider.Provider.Tenant = null;
            }
        }
    }
}