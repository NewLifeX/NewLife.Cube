using System;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Services.Sso;
using NewLife.Cube.Tests.Services;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 安全开关测试：后台关闭密码登录（AllowLogin=false，仅保留SSO）后，
/// 攻击者直接调用账密登录接口必须被拒绝。UserService.LoginByPassword 是 MVC 版与 API 版
/// （Auth/Login、Admin/User/Login）共用的账密登录汇聚点，服务层统一拦截即可覆盖全部入口；
/// OAuth 密码式授权（TokenService.GetAccessTokenByPassword）同样受 AllowLogin 限制。
/// </summary>
[Collection("TenantAuth")]
public class PasswordLoginDisabledTests
{
    private readonly TenantAuthFixture _fx;

    public PasswordLoginDisabledTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>关闭密码登录后，即使账号密码正确，调用登录接口也必须报错，且不颁发令牌</summary>
    [Fact(DisplayName = "关闭密码登录：正确账密调用登录接口也报错，不颁发令牌")]
    public void AllowLogin_Disabled_Reject_Valid_Password()
    {
        var prev = CubeSetting.Current.AllowLogin;
        CubeSetting.Current.AllowLogin = false;
        try
        {
            var ctx = _fx.CreateContext();
            var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password };

            var ex = Assert.Throws<InvalidOperationException>(() => _fx.UserService.Login(model, ctx));
            Assert.Contains("禁止密码登录", ex.Message);

            var token = ctx.Items["jwtToken"] as String;
            Assert.True(String.IsNullOrEmpty(token), "密码登录被拒绝时不应颁发令牌");
        }
        finally
        {
            CubeSetting.Current.AllowLogin = prev;
        }
    }

    /// <summary>关闭密码登录后，错误密码与正确密码返回同一错误，不泄露账号是否存在</summary>
    [Fact(DisplayName = "关闭密码登录：错误账号密码报同一错误，不泄露账号是否存在")]
    public void AllowLogin_Disabled_Reject_Wrong_Password_Same_Message()
    {
        var prev = CubeSetting.Current.AllowLogin;
        CubeSetting.Current.AllowLogin = false;
        try
        {
            var ctx = _fx.CreateContext();
            var model = new LoginModel { Username = "no_such_user_xyz", Password = "Wrong@123" };

            var ex = Assert.Throws<InvalidOperationException>(() => _fx.UserService.Login(model, ctx));
            Assert.Contains("禁止密码登录", ex.Message);
        }
        finally
        {
            CubeSetting.Current.AllowLogin = prev;
        }
    }

    /// <summary>开启密码登录（默认）时，正确账密登录正常通过，守卫不误伤正常登录</summary>
    [Fact(DisplayName = "开启密码登录：正确账密登录正常通过")]
    public void AllowLogin_Enabled_Login_Succeeds()
    {
        var prev = CubeSetting.Current.AllowLogin;
        CubeSetting.Current.AllowLogin = true;
        try
        {
            var ctx = _fx.CreateContext();
            var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password };
            Assert.True(_fx.UserService.Login(model, ctx).IsSuccess);
        }
        finally
        {
            CubeSetting.Current.AllowLogin = prev;
        }
    }

    /// <summary>关闭密码登录后，OAuth密码式授权（/Sso/Token grant_type=password）同样被拒，防止绕过登录接口直连换令牌</summary>
    [Fact(DisplayName = "关闭密码登录：OAuth密码式授权一并拒绝")]
    public void AllowLogin_Disabled_Rejects_OAuth_Password_Grant()
    {
        var prevLogin = CubeSetting.Current.AllowLogin;
        var prevServer = CubeSetting.Current.EnableOAuthServer;
        CubeSetting.Current.AllowLogin = false;
        CubeSetting.Current.EnableOAuthServer = true;
        try
        {
            var svc = new TokenService();

            // 关闭密码登录后，即使启用OAuth服务端，密码式授权也必须在触碰应用验证前被拦截
            var ex = Record.Exception(() => svc.GetAccessTokenByPassword("app", "user", "pass", "127.0.0.1"));
            Assert.NotNull(ex);
            Assert.Contains("禁止密码登录", ex.Message);
        }
        finally
        {
            CubeSetting.Current.AllowLogin = prevLogin;
            CubeSetting.Current.EnableOAuthServer = prevServer;
        }
    }

    /// <summary>关闭密码登录仅限制密码式授权，授权码等其它服务端令牌接口不受影响（由 EnableOAuthServer 统一管控）</summary>
    [Fact(DisplayName = "关闭密码登录：不影响授权码等其它令牌接口")]
    public void AllowLogin_Disabled_DoesNot_Affect_Other_Grants()
    {
        var prevLogin = CubeSetting.Current.AllowLogin;
        var prevServer = CubeSetting.Current.EnableOAuthServer;
        CubeSetting.Current.AllowLogin = false;
        CubeSetting.Current.EnableOAuthServer = true;
        try
        {
            var svc = new TokenService { AppService = new ThrowingAppService() };

            // 若 AllowLogin 误拦截非密码授权，会抛"禁止密码登录"；放行后应进入 AppService.Auth 抛桩的哨兵异常
            var ex = Record.Exception(() => svc.GetAccessToken("app", "secret", "code", "127.0.0.1"));
            Assert.NotNull(ex);
            Assert.Contains("reached-app-service", ex.Message);
            Assert.DoesNotContain("禁止密码登录", ex.Message);
        }
        finally
        {
            CubeSetting.Current.AllowLogin = prevLogin;
            CubeSetting.Current.EnableOAuthServer = prevServer;
        }
    }
}
