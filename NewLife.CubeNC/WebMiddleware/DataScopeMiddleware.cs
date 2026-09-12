using System.Text.Json;
using NewLife.Common;
using NewLife.Log;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.WebMiddleware;

/// <summary>上下文中间件。设置租户上下文，并声明宿主数据权限策略（实体层以系统身份运行）</summary>
/// <param name="next"></param>
/// <param name="tenantContext">租户上下文（无状态门面，注册为 Singleton，内部读 AsyncLocal）</param>
public class DataScopeMiddleware(RequestDelegate next, ITenantContext tenantContext)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ITenantContext _tenantContext = tenantContext;

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
            if (set.EnableTenant && _tenantContext.Mode == TenantMode.None)
            {
                var tenantId = ctx.GetTenantId();
                if (tenantId.GetTenantMode() != TenantMode.None)
                {
                    // 中间件只做"租户上下文解析"（三段式之②），不做成员授权（③）。
                    // 成员授权统一由认证后的 ValidateTenant（TenantAccessPolicy）负责——
                    // 中间件不依赖 ManageProvider.User 的加载时机，消除"新令牌刚签发首请求"的时序抖动。
                    ctx.SetTenant(tenantId);
                }
                else if (HasExplicitTenant(ctx))
                {
                    // 显式提供了租户信息但解析无效（不存在/已禁用），拒绝请求，防止伪造租户绕过数据隔离
                    await WriteError(ctx, 400, -1, "租户不存在或已禁用");
                    return;
                }
                else if (set.TenantEnforceMode == TenantEnforceModes.Shadow)
                {
                    // [TenantCompat] 影子期规则A：旧客户端无租户标识，兼容放行（不设租户上下文），仅记录影子日志。
                    // 切 Enforce 后该分支不再需要，等待移除（见评审方案 5.5）
                    XTrace.WriteLine("[TenantCompat] 旧客户端无租户标识，兼容放行：{0}", ctx.Request.Path);
                    // 数据日志（CreateLog 落库）；中间件在认证前无用户上下文
                    ManagerProviderHelper.WriteTenantCompatDataLog("影子兼容放行", $"旧客户端无租户标识，兼容放行 路径[{ctx.Request.Path}]", null, ctx.Connection.RemoteIpAddress + "");
                }
                // Enforce + 无租户标识：不在此拦截，交由认证层 ValidateTenant 处理——
                // 管理员本身不属任何租户、无需租户标识（进管理后台），普通用户缺标识才被拒。
                // 中间件若在此拦截会依赖 ManageProvider.User 加载时机，误拒刚登录的管理员。
            }

            // 2. 宿主数据权限声明：实体层以"系统身份"运行，数据权限统一由接口层负责。
            // XCode 数据权限拦截器会在上下文缺失时以 ManageProvider.User 兜底创建，
            // 若此处不注入，请求内一切业务代码（SSO/服务/自定义 Action）都会被隐式收窄；
            // 而实体层同时服务 Web、服务层与算法层等消费方，不承担 Web 展示层的数据隔离职责。
            // 页面行级过滤由 DataPermissionAttribute 特性经 SearchData/FindData 管道显式执行。
            // 外部已显式设置上下文时尊重之（测试或嵌入式宿主）。
            if (DataScopeContext.Current == null)
            {
                DataScopeContext.Current = CreateHostScope(ManageProvider.User);
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

    /// <summary>创建宿主数据权限上下文。魔方宿主中实体层以系统身份运行，不参与行级数据过滤</summary>
    /// <remarks>
    /// 返回系统态上下文（<see cref="DataScopes.全部"/>），使 XCode 数据权限拦截器在魔方宿主内休眠；
    /// 同时保留用户编号与部门编号，供实体拦截器在新增数据时自动填充审计字段。
    /// 行级数据权限统一由接口层控制器特性（DataPermissionAttribute）经查询管道显式执行。
    /// </remarks>
    /// <param name="user">当前登录用户，可为空</param>
    /// <returns>宿主数据权限上下文</returns>
    public static DataScopeContext CreateHostScope(IUser user) => new()
    {
        UserId = user?.ID ?? 0,
        DepartmentId = user?.DepartmentID ?? 0,
        DataScope = DataScopes.全部,
    };

    /// <summary>是否显式提供了租户信息（请求头或查询参数）</summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    private static Boolean HasExplicitTenant(HttpContext ctx)
        => !ctx.Request.Headers["X-Tenant"].ToString().IsNullOrEmpty()
        || !ctx.Request.Headers["X-Tenant-Id"].ToString().IsNullOrEmpty()
        || !ctx.Request.Query["tenantId"].ToString().IsNullOrEmpty()
        || !ctx.Request.Cookies[$"TenantId-{SysConfig.Current.Name}"].IsNullOrEmpty();

    /// <summary>以统一 ApiResponse JSON 形式写出租户错误，避免纯文本与前端 unwrap 契约不一致（P1-3）</summary>
    private static async Task WriteError(HttpContext ctx, Int32 statusCode, Int32 code, String message)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(new
        {
            code,
            message,
            data = (Object)null,
            traceId = (String)null,
            fieldErrors = (Object)null,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await ctx.Response.WriteAsync(json);
    }
}