using NewLife.Cube.Entity;
using NewLife.Security;
using NewLife.Web;
using XCode.Membership;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>令牌定制扩展点。外部可注册实现来向 JWT 添加自定义 Claims 或修改令牌</summary>
/// <remarks>
/// 典型场景：业务系统需要在 JWT 中加入自定义字段（如租户ID、业务权限等）。
/// </remarks>
public interface ITokenCustomizer
{
    /// <summary>自定义令牌</summary>
    /// <param name="token">令牌模型</param>
    /// <param name="jwt">JWT 构建器</param>
    /// <param name="app">应用信息</param>
    /// <param name="user">用户信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task CustomizeAsync(TokenModel token, JwtBuilder jwt, App app, IManageUser user, CancellationToken cancellationToken = default);
}
