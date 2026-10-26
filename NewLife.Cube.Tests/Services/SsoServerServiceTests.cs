using System;
using NewLife;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Sso;
using NewLife.Cube.Tests.Common;
using NewLife.Cube.Tests.Membership;
using NewLife.Data;
using NewLife.Web;
using XCode.Membership;
using Xunit;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// OAUTH-2 SSO 服务端测试。覆盖应用校验与回调地址白名单、授权码发放与回跳地址拼装（code/token 两种响应类型）、
/// 以及授权码不存在时的拒绝行为。令牌服务以假实现注入，不依赖 JWT 签发细节。
/// </summary>
[Collection("TenantAuth")]
public class SsoServerServiceTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具</param>
    public SsoServerServiceTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 假服务
    private sealed class FakeAppService(App app) : IOAuthAppService
    {
        public App Auth(String client_id, String client_secret, String ip) => app;

        public TokenProvider GetProvider() => null;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public NewLife.Log.ILog Log { get; set; }

        public void WriteLog(String format, params Object[] args) { }

        public TokenModel CreateToken(App app, String name, Object payload, String refreshName) => new()
        {
            AccessToken = "AT-" + name,
            RefreshToken = "RT-" + name,
            ExpireIn = 7200,
        };

        public TokenModel GetToken(String code) => throw new NotImplementedException();
        public String Decode(String token) => throw new NotImplementedException();
        public TokenModel GetAccessToken(String client_id, String client_secret, String code, String ip) => throw new NotImplementedException();
        public TokenModel GetAccessTokenByPassword(String client_id, String username, String password, String ip) => throw new NotImplementedException();
        public TokenModel GetAccessTokenByClientCredentials(String client_id, String client_secret, String username, String ip) => throw new NotImplementedException();
        public TokenModel RefreshToken(String client_id, String refresh_token, String ip) => throw new NotImplementedException();
        public IManageUser GetUser(String username) => throw new NotImplementedException();
        public Object GetUserInfo(String token, IManageUser user) => throw new NotImplementedException();
    }
    #endregion

    #region 辅助
    private SsoServerService CreateService(App app)
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        var svc = new SsoServerService
        {
            AppService = new FakeAppService(app),
            TokenService = new FakeTokenService(),
        };

        return svc;
    }

    private static App CreateApp(String redirect)
    {
        var app = new App
        {
            Name = "sso_" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "SSO 测试应用",
            Secret = Guid.NewGuid().ToString("N"),
            Urls = redirect,
            Enable = true,
            Category = "Test",
        };
        app.Insert();

        return app;
    }
    #endregion

    [Fact(DisplayName = "SSO服务端_合法回调返回授权码并记录成功日志")]
    public void Authorize_ValidCallback_ReturnsCode()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var app = CreateApp("app.example.com/sso");
        var svc = new SsoServerService { AppService = new FakeAppService(app), TokenService = new FakeTokenService() };

        var key = svc.Authorize(app.Name, "https://app.example.com/sso/callback", "code", "userinfo", "st-1", "127.0.0.1");

        Assert.True(Int64.TryParse(key, out var id));
        var log = AppLog.FindById(id);
        Assert.NotNull(log);
        Assert.Equal(nameof(SsoServerService.Authorize), log.Action);
        Assert.True(log.Success);
        Assert.Equal(app.Name, log.ClientId);
        Assert.Equal("st-1", log.State);
    }

    [Fact(DisplayName = "SSO服务端_回调地址不在白名单时拒绝并记录失败日志")]
    public void Authorize_InvalidCallback_Throws()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var app = CreateApp("app.example.com/sso");
        var svc = new SsoServerService { AppService = new FakeAppService(app), TokenService = new FakeTokenService() };

        var ex = Assert.ThrowsAny<Exception>(() => svc.Authorize(app.Name, "https://evil.example.net/callback", "code", null, null, "127.0.0.1"));
        Assert.False(ex.Message.IsNullOrEmpty());

        var logs = AppLog.FindAllByAppId(app.Id);
        Assert.Contains(logs, e => e.Action == nameof(SsoServerService.Authorize) && !e.Success);
    }

    [Fact(DisplayName = "SSO服务端_授权码换回跳地址携带code与state")]
    public void GetResult_CodeResponse_AppendsCodeAndState()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var app = CreateApp("app.example.com/sso");
        var svc = new SsoServerService { AppService = new FakeAppService(app), TokenService = new FakeTokenService() };

        var key = svc.Authorize(app.Name, "https://app.example.com/sso/callback", "code", null, "st-2", "127.0.0.1");

        var url = svc.GetResult(key, _fx.LegacyUser);

        Assert.Equal($"https://app.example.com/sso/callback?code={key}&state=st-2", url);

        var log = AppLog.FindById(key.ToLong());
        Assert.Equal(nameof(SsoServerService.GetResult), log.Action);
        Assert.False(log.AccessToken.IsNullOrEmpty());
        Assert.False(log.CreateUser.IsNullOrEmpty());
    }

    [Fact(DisplayName = "SSO服务端_token响应类型在片段中返回令牌")]
    public void GetResult_TokenResponse_AppendsTokenFragment()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var app = CreateApp("app.example.com/sso");
        var svc = new SsoServerService { AppService = new FakeAppService(app), TokenService = new FakeTokenService() };

        var key = svc.Authorize(app.Name, "https://app.example.com/sso/callback", "token", null, "st-3", "127.0.0.1");

        var url = svc.GetResult(key, _fx.LegacyUser);

        Assert.Contains("state=st-3", url);
        Assert.Contains("#token=AT-", url);
    }

    [Fact(DisplayName = "SSO服务端_授权码不存在时拒绝")]
    public void GetResult_UnknownKey_Throws()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var svc = new SsoServerService { AppService = new FakeAppService(null), TokenService = new FakeTokenService() };

        Assert.Throws<ArgumentOutOfRangeException>(() => svc.GetResult("999999999", _fx.LegacyUser));
    }
}
