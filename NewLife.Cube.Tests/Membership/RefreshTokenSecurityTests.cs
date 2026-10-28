using System;
using Microsoft.AspNetCore.Http;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Remoting;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// SEC-01 刷新令牌安全测试。覆盖「令牌用户 ≠ 请求用户」「用户已禁用」「令牌已吊销」三种拒绝路径，
/// 以及正常轮换后旧令牌失效、新令牌可用的正向路径。
/// </summary>
[Collection("TenantAuth")]
public class RefreshTokenSecurityTests
{
    private const String Password = "Test@12345";

    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具，复用 SQLite 用户与令牌表</param>
    public RefreshTokenSecurityTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private User CreateUser()
    {
        var user = new User
        {
            Name = "sec_" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "刷新令牌安全测试用户",
            Password = _fx.Provider.PasswordProvider.Hash(Password),
            RoleID = _fx.LegacyUser.RoleID,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        return user;
    }

    private String Login(User user)
    {
        var ctx = _fx.CreateContext();
        var rs = _fx.UserService.Login(new LoginModel { Username = user.Name, Password = Password }, ctx);
        Assert.True(rs.IsSuccess, rs.Message);

        return ctx.Items["refreshToken"] as String;
    }
    #endregion

    [Fact(DisplayName = "刷新令牌_使用他人用户名_拒绝并返回登出码")]
    public void Refresh_OtherUser_Rejected()
    {
        var userA = CreateUser();
        var userB = CreateUser();
        var tokenA = Login(userA);

        var ctx = _fx.CreateContext();
        var ex = Assert.Throws<ApiException>(() => ctx.RefreshToken(userB, tokenA));
        Assert.Equal(CubeCode.LogOff.ToInt(), ex.Code);
    }

    [Fact(DisplayName = "刷新令牌_用户已禁用_拒绝并返回登出码")]
    public void Refresh_DisabledUser_Rejected()
    {
        var user = CreateUser();
        var token = Login(user);

        user.Enable = false;
        user.Update();

        var ctx = _fx.CreateContext();
        var ex = Assert.Throws<ApiException>(() => ctx.RefreshToken(user, token));
        Assert.Equal(CubeCode.LogOff.ToInt(), ex.Code);
    }

    [Fact(DisplayName = "刷新令牌_令牌已吊销_拒绝并返回登出码")]
    public void Refresh_RevokedToken_Rejected()
    {
        var user = CreateUser();
        var token = Login(user);

        var ut = UserToken.FindByToken(token);
        Assert.NotNull(ut);
        ut.Enable = false;
        ut.Update();

        var ctx = _fx.CreateContext();
        var ex = Assert.Throws<ApiException>(() => ctx.RefreshToken(user, token));
        Assert.Equal(CubeCode.LogOff.ToInt(), ex.Code);
    }

    [Fact(DisplayName = "刷新令牌_正常轮换_旧令牌失效且新令牌可用")]
    public void Refresh_Valid_Rotates()
    {
        var user = CreateUser();
        var token = Login(user);

        var ctx = _fx.CreateContext();
        var newToken = ctx.RefreshToken(user, token);
        Assert.NotNull(newToken);
        Assert.False(String.IsNullOrEmpty(newToken.AccessToken));
        Assert.NotEqual(token, newToken.RefreshToken);

        // 旧令牌已吊销，不能再次刷新
        var oldUt = UserToken.FindByToken(token);
        Assert.NotNull(oldUt);
        Assert.False(oldUt.Enable);

        var ctx2 = _fx.CreateContext();
        Assert.Throws<ApiException>(() => ctx2.RefreshToken(user, token));
    }
}
