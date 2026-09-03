using System;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 安全开关测试：后台关闭密码登录（AllowLogin=false，仅保留SSO）后，
/// 攻击者直接调用账密登录接口必须被拒绝。UserService.LoginByPassword 是 MVC 版与 API 版
/// （Auth/Login、Admin/User/Login）共用的账密登录汇聚点，服务层统一拦截即可覆盖全部入口。
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
}
