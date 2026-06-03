using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>登录成功回调扩展点。外部可注册多个实现，按顺序执行</summary>
/// <remarks>
/// 典型场景：企�?钉钉登录完成后，需要额外调�?API 获取更多用户信息、部门信息等�?
/// 实现此接口并注册�?DI 容器（AddSingleton）�?
/// </remarks>
public interface ILoginCallback
{
    /// <summary>登录成功时回�?/summary>
    /// <param name="client">OAuth 客户�?/param>
    /// <param name="uc">用户连接信息</param>
    /// <param name="user">已登录用户</param>
    /// <param name="context">服务提供者，可用于获取 HttpContext 等</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task OnLoginAsync(OAuthClient client, UserConnect uc, IManageUser user, IServiceProvider context, CancellationToken cancellationToken = default);
}
