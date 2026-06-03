using NewLife.Cube.Entity;
using NewLife.Web;

namespace NewLife.Cube.Services.Sso;

/// <summary>OAuth 应用验证服务</summary>
public interface IOAuthAppService
{
    /// <summary>验证应用合法性</summary>
    /// <param name="client_id">应用标识</param>
    /// <param name="client_secret">应用密钥</param>
    /// <param name="ip">来源 IP</param>
    /// <returns></returns>
    App Auth(String client_id, String client_secret, String ip);

    /// <summary>获取令牌提供者（DSA 密钥管理）</summary>
    /// <returns></returns>
    TokenProvider GetProvider();
}
