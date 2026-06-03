using NewLife.Web;

namespace NewLife.Cube.Services.Sso;

/// <summary>用户信息补充提供者扩展点。外部可注册实现来补充额外的用户信息</summary>
/// <remarks>
/// 典型场景：第三方 OAuth 返回的用户信息不完整，需要从业务系统补充更多字段。
/// </remarks>
public interface IUserInfoProvider
{
    /// <summary>获取补充的用户信息</summary>
    /// <param name="client">OAuth 客户端</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>补充的用户信息键值对，将合并到现有信息中</returns>
    Task<IDictionary<String, Object>> GetUserInfoAsync(OAuthClient client, CancellationToken cancellationToken = default);
}
