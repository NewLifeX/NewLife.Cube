using NewLife.Model;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Web;

/// <summary>SSO客户端</summary>
public class SsoClient
{
    #region 属性
    /// <summary>服务器</summary>
    public String Server { get; set; }

    /// <summary>应用标识</summary>
    public String AppId { get; set; }

    /// <summary>应用密钥</summary>
    public String Secret { get; set; }

    /// <summary>安全密钥。keyName$keyValue</summary>
    /// <remarks>
    /// 公钥，用于RSA加密用户密码，在通信链路上保护用户密码安全，可以写死在代码里面。
    /// 密钥前面可以增加keyName，形成keyName$keyValue，用于向服务端指示所使用的密钥标识，方便未来更换密钥。
    /// </remarks>
    public String SecurityKey { get; set; }

    private ApiHttpClient _client;
    #endregion

    private ApiHttpClient GetClient()
    {
        if (_client != null) return _client;

        //_client = DefaultTracer.Instance.CreateHttpClient();
        //_client.BaseAddress = new Uri(Server);
        _client = new ApiHttpClient(Server);

        return _client;
    }

    /// <summary>密码式，验证账号密码，并返回令牌</summary>
    /// <param name="username">账号</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public async Task<IToken> GetToken(String username, String password)
    {
        var client = GetClient();

        var key = SecurityKey;
        if (!key.IsNullOrEmpty())
        {
            var name = "";
            var p = key.IndexOf('$');
            if (p >= 0)
            {
                name = key[..p];
                key = key[(p + 1)..];
            }

            // RSA公钥加密
            var pass = RSAHelper.Encrypt(password.GetBytes(), key).ToBase64();
            password = $"$rsa${name}${pass}";
        }

        return await client.GetAsync<TokenModel>("sso/token", new
        {
            grant_type = "password",
            client_id = AppId,
            client_secret = Secret,
            username,
            password
        });
    }

    /// <summary>凭证式，为指定设备颁发令牌</summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public async Task<IToken> GetToken(String deviceId)
    {
        var client = GetClient();

        return await client.GetAsync<TokenModel>("sso/token", new
        {
            grant_type = "client_credentials",
            client_id = AppId,
            client_secret = Secret,
            username = deviceId,
        });
    }

    /// <summary>刷新令牌</summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task<IDictionary<String, Object>> RefreshToken(String accessToken)
    {
        var client = GetClient();

        return await client.GetAsync<IDictionary<String, Object>>("sso/token", new
        {
            grant_type = "refresh_token",
            client_id = AppId,
            client_secret = Secret,
            refresh_token = accessToken,
        });
    }

    /// <summary>通过令牌获取用户信息</summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task<IDictionary<String, Object>> GetUserInfo(String accessToken)
    {
        var client = GetClient();

        return await client.GetAsync<IDictionary<String, Object>>("sso/userinfo", new { access_token = accessToken });
    }

    /// <summary>通过令牌获取用户信息</summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    public async Task<IManageUser> GetUser(String accessToken)
    {
        var dic = await GetUserInfo(accessToken);

        var code = dic["errcode"].ToInt(-1);
        var error = dic["error"] as String;
        if (code > 0 && code != 200) throw new ApiException(code, error);

        var user = JsonHelper.Convert<User>(dic);

        if (dic.TryGetValue("userid", out var str)) user.ID = str.ToInt();
        if (dic.TryGetValue("usercode", out str)) user.Code = str as String;
        if (dic.TryGetValue("username", out str)) user.Name = str as String;
        if (dic.TryGetValue("nickname", out str)) user.DisplayName = str as String;

        return user;
    }

    /// <summary>验证用户，并获取用户信息</summary>
    /// <param name="username">账号</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public async Task<IDictionary<String, Object>> UserAuth(String username, String password)
    {
        var client = GetClient();

        var key = SecurityKey;
        if (!key.IsNullOrEmpty())
        {
            var name = "";
            var p = key.IndexOf('$');
            if (p >= 0)
            {
                name = key[..p];
                key = key[(p + 1)..];
            }

            // RSA公钥加密
            var pass = RSAHelper.Encrypt(password.GetBytes(), key).ToBase64();
            password = $"$rsa${name}${pass}";
        }

        return await client.PostAsync<IDictionary<String, Object>>("sso/userauth", new
        {
            //grant_type = "password",
            client_id = AppId,
            //client_secret = Secret,
            username,
            password
        });
    }

    /// <summary>获取应用公钥，用于验证令牌</summary>
    /// <param name="client_id">应用</param>
    /// <param name="client_secret">密钥</param>
    /// <returns></returns>
    public async Task<IDictionary<String, Object>> GetKey(String client_id, String client_secret)
    {
        var client = GetClient();

        return await client.GetAsync<IDictionary<String, Object>>("sso/getkey", new { client_id, client_secret });
    }
}