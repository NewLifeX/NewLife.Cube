using System;
using Microsoft.AspNetCore.Http;
using NewLife.Common;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 记住登录状态（保存密码）测试：勾选后后端把令牌有效期延长到 365 天（与 MVC 版 Cookie 行为一致），
/// 前端（SPA 存 localStorage）在有效期内重开系统免登录；令牌仍带 jti，退出登录可吊销。
/// 驱动真实登录管线：UserService.Login → LoginByPassword → CompleteLogin（与 AuthController.Login 同路径）。
/// </summary>
[Collection("TenantAuth")]
public class RememberLoginTests
{
    private readonly TenantAuthFixture _fx;

    public RememberLoginTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>解码 JWT 并返回其过期时间。夹具固定 JWT 密钥 HS256:CubeTenantReproTestSecret</summary>
    private static DateTime DecodeExpire(String token)
    {
        var jwt = new JwtBuilder { Algorithm = "HS256", Secret = "CubeTenantReproTestSecret" };
        Assert.True(jwt.TryDecode(token, out var msg), $"JWT 解码失败：{msg}");
        return jwt.Expire;
    }

    /// <summary>勾选保存密码（Remember=true）：JWT 有效期 ≈ 365 天，Cookie 同步 365 天</summary>
    [Fact(DisplayName = "保存密码：JWT 有效期延长到 365 天（重开系统免登录）")]
    public void Remember_Login_Issues_365Day_Token()
    {
        // NewLife.Cube 完整版默认 TokenCookie=false（SPA 走 localStorage，不依赖 Cookie）；
        // 临时开启以校验"记住登录状态 → Cookie 同步 365 天"的完整链路
        var prevCookie = CubeSetting.Current.TokenCookie;
        CubeSetting.Current.TokenCookie = true;
        try
        {
            var ctx = _fx.CreateContext();
            var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password, Remember = true };
            Assert.True(_fx.UserService.Login(model, ctx).IsSuccess);

            var token = ctx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token), "登录应颁发 JWT");

            var left = DecodeExpire(token) - DateTime.Now;
            Assert.True(left.TotalDays > 364 && left.TotalDays < 366, $"JWT 有效期应约为 365 天，实际 {left.TotalDays:F1} 天");

            // Cookie 同步 365 天（与 MVC 版一致），供 MVC/会话恢复路径使用
            var setCookie = ctx.Response.Headers["Set-Cookie"].ToString();
            Assert.True(setCookie.Contains("expires=", StringComparison.OrdinalIgnoreCase), $"Set-Cookie 头应含过期时间：{setCookie}");
        }
        finally
        {
            CubeSetting.Current.TokenCookie = prevCookie;
        }
    }

    /// <summary>不勾选保存密码：JWT 保持 TokenExpire 短有效期（默认 7200s）</summary>
    [Fact(DisplayName = "不勾选保存密码：JWT 保持 TokenExpire 短有效期")]
    public void NoRemember_Login_Keeps_TokenExpire()
    {
        var prev = CubeSetting.Current.TokenExpire;
        CubeSetting.Current.TokenExpire = 7200;
        try
        {
            var ctx = _fx.CreateContext();
            var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password, Remember = false };
            Assert.True(_fx.UserService.Login(model, ctx).IsSuccess);

            var token = ctx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token), "登录应颁发 JWT");

            var left = DecodeExpire(token) - DateTime.Now;
            Assert.True(left.TotalSeconds > 7100 && left.TotalSeconds < 7300, $"JWT 有效期应约为 TokenExpire(7200s)，实际 {left.TotalSeconds:F0}s");
        }
        finally
        {
            CubeSetting.Current.TokenExpire = prev;
        }
    }

    /// <summary>365 天长令牌仍可吊销：禁用 UserToken（模拟退出登录）后 TryLogin 拒绝访问</summary>
    [Fact(DisplayName = "保存密码的 365 天令牌仍可吊销（退出登录即失效）")]
    public void Remember_Token_Revocable_On_Logout()
    {
        var ctx = _fx.CreateContext();
        var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password, Remember = true };
        Assert.True(_fx.UserService.Login(model, ctx).IsSuccess);

        var token = ctx.Items["jwtToken"] as String;
        var jwt = new JwtBuilder { Algorithm = "HS256", Secret = "CubeTenantReproTestSecret" };
        Assert.True(jwt.TryDecode(token, out _));
        var utId = jwt.Id.ToInt();
        Assert.True(utId > 0, "JWT 应携带 jti（UserToken.Id）");

        // 吊销当前令牌（与 ManageProvider.Logout 多设备模式一致）
        UserToken.RevokeByTokenId(utId);

        // 吊销后携带同一令牌访问被拒（LoadUser 校验 UserToken.Enable=false）
        TenantContext.Current = null!;
        var ctx2 = _fx.CreateContext();
        ctx2.Request.Headers["Authorization"] = $"Bearer {token}";
        Assert.Null(_fx.Provider.TryLogin(ctx2));
    }
}
