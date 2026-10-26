using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// AUTH-12 多设备登录控制测试。覆盖多设备模式保留各方令牌、单设备模式新登录踢掉旧设备、
/// 注销时按模式决定吊销范围（仅当前令牌 / 该账号全部令牌），以及吊销辅助方法的边界输入。
/// </summary>
[Collection("TenantAuth")]
public class MultiDeviceLoginTests
{
    private const String Password = "Test@12345";

    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（User/UserToken 表）</param>
    public MultiDeviceLoginTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private User CreateUser()
    {
        var user = new User
        {
            Name = "dev_" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "多设备测试用户",
            Password = _fx.Provider.PasswordProvider.Hash(Password),
            RoleID = _fx.LegacyUser.RoleID,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        return user;
    }

    private HttpContext Login(User user)
    {
        var ctx = _fx.CreateContext();
        var rs = _fx.UserService.Login(new LoginModel { Username = user.Name, Password = Password }, ctx);
        Assert.True(rs.IsSuccess, rs.Message);

        return ctx;
    }

    private static String RefreshToken(HttpContext ctx) => ctx.Items["refreshToken"] as String;

    private static Int32 EnabledCount(Int32 userId) => UserToken.FindAllByUserID(userId).Count(e => e.Enable);

    #endregion

    #region 登录策略
    [Fact(DisplayName = "多设备模式_两次登录保留两个有效令牌")]
    public void MultiDevice_TwoLogins_BothTokensEnabled()
    {
        var prev = CubeSetting.Current.EnableMultiDeviceLogin;
        CubeSetting.Current.EnableMultiDeviceLogin = true;
        try
        {
            var user = CreateUser();

            var ctx1 = Login(user);
            var ctx2 = Login(user);

            Assert.NotEqual(RefreshToken(ctx1), RefreshToken(ctx2));
            Assert.Equal(2, EnabledCount(user.ID));
            Assert.True(UserToken.FindByToken(RefreshToken(ctx1)).Enable);
            Assert.True(UserToken.FindByToken(RefreshToken(ctx2)).Enable);
        }
        finally
        {
            CubeSetting.Current.EnableMultiDeviceLogin = prev;
        }
    }

    [Fact(DisplayName = "单设备模式_新登录踢掉旧设备只保留最新令牌")]
    public void SingleDevice_NewLogin_RevokesPreviousToken()
    {
        var prev = CubeSetting.Current.EnableMultiDeviceLogin;
        CubeSetting.Current.EnableMultiDeviceLogin = false;
        try
        {
            var user = CreateUser();

            var ctx1 = Login(user);
            var ctx2 = Login(user);

            Assert.NotEqual(RefreshToken(ctx1), RefreshToken(ctx2));
            Assert.Equal(1, EnabledCount(user.ID));
            Assert.False(UserToken.FindByToken(RefreshToken(ctx1)).Enable);
            Assert.True(UserToken.FindByToken(RefreshToken(ctx2)).Enable);
        }
        finally
        {
            CubeSetting.Current.EnableMultiDeviceLogin = prev;
        }
    }
    #endregion

    #region 注销策略
    [Fact(DisplayName = "多设备模式注销_仅吊销当前设备令牌")]
    public void MultiDevice_Logout_RevokesCurrentOnly()
    {
        var prev = CubeSetting.Current.EnableMultiDeviceLogin;
        CubeSetting.Current.EnableMultiDeviceLogin = true;
        try
        {
            var user = CreateUser();
            var ctx1 = Login(user);
            var ctx2 = Login(user);

            // 以第二台设备的令牌发起注销
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {ctx2.Items["jwtToken"]}";
            Assert.True(ctx.GetJti() > 0, "应从请求 JWT 中解析出 jti");
            _fx.Provider.SetCurrent(user);

            _fx.Provider.Logout();

            Assert.True(UserToken.FindByToken(RefreshToken(ctx1)).Enable, "其他设备令牌不应被牵连");
            Assert.False(UserToken.FindByToken(RefreshToken(ctx2)).Enable, "当前设备令牌应被吊销");
            Assert.Equal(1, EnabledCount(user.ID));
        }
        finally
        {
            CubeSetting.Current.EnableMultiDeviceLogin = prev;
        }
    }

    [Fact(DisplayName = "单设备模式注销_吊销该账号全部令牌")]
    public void SingleDevice_Logout_RevokesAllTokens()
    {
        var prev = CubeSetting.Current.EnableMultiDeviceLogin;
        CubeSetting.Current.EnableMultiDeviceLogin = false;
        try
        {
            var user = CreateUser();
            var ctx = Login(user);
            Assert.Equal(1, EnabledCount(user.ID));

            _fx.Provider.SetCurrent(user);
            _fx.Provider.Logout();

            Assert.Equal(0, EnabledCount(user.ID));
            Assert.False(UserToken.FindByToken(RefreshToken(ctx)).Enable);
        }
        finally
        {
            CubeSetting.Current.EnableMultiDeviceLogin = prev;
        }
    }
    #endregion

    #region 边界
    [Fact(DisplayName = "吊销辅助方法_非法编号不产生副作用")]
    public void RevokeHelpers_InvalidIds_NoSideEffect()
    {
        var user = CreateUser();
        Login(user);

        Assert.Equal(0, UserToken.RevokeByUser(0));
        Assert.Equal(0, UserToken.RevokeByUser(-5));
        Assert.False(UserToken.RevokeByTokenId(0));
        Assert.False(UserToken.RevokeByTokenId(Int32.MaxValue));
        Assert.Equal(1, EnabledCount(user.ID));
    }
    #endregion
}
