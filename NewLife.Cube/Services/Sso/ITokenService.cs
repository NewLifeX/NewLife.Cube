using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using ILog = NewLife.Log.ILog;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>令牌服务接口</summary>
public interface ITokenService
{
    /// <summary>获取日志</summary>
    ILog Log { get; set; }

    /// <summary>写日志</summary>
    void WriteLog(String format, params Object[] args);

    /// <summary>创建令牌</summary>
    /// <param name="app">应用</param>
    /// <param name="name">用户名</param>
    /// <param name="payload">载荷</param>
    /// <param name="refreshName">刷新令牌名</param>
    /// <returns></returns>
    TokenModel CreateToken(App app, String name, Object payload, String refreshName);

    /// <summary>根据 Code 获取令牌</summary>
    /// <param name="code"></param>
    /// <returns></returns>
    TokenModel GetToken(String code);

    /// <summary>解码令牌</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    String Decode(String token);

    /// <summary>授权码方式获取访问令牌</summary>
    /// <param name="client_id"></param>
    /// <param name="client_secret"></param>
    /// <param name="code"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    TokenModel GetAccessToken(String client_id, String client_secret, String code, String ip);

    /// <summary>密码式获取令牌</summary>
    /// <param name="client_id"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    TokenModel GetAccessTokenByPassword(String client_id, String username, String password, String ip);

    /// <summary>凭证式获取令牌</summary>
    /// <param name="client_id"></param>
    /// <param name="client_secret"></param>
    /// <param name="username"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    TokenModel GetAccessTokenByClientCredentials(String client_id, String client_secret, String username, String ip);

    /// <summary>刷新令牌</summary>
    /// <param name="client_id"></param>
    /// <param name="refresh_token"></param>
    /// <param name="ip"></param>
    /// <returns></returns>
    TokenModel RefreshToken(String client_id, String refresh_token, String ip);

    /// <summary>获取用户</summary>
    /// <param name="username"></param>
    /// <returns></returns>
    IManageUser GetUser(String username);

    /// <summary>获取用户信息</summary>
    /// <param name="token"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    Object GetUserInfo(String token, IManageUser user);
}
