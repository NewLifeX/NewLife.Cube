using System;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Sso;
using NewLife.Web;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// OAuth 服务端安全开关测试：EnableOAuthServer=false 时，OAuth2.0 服务端令牌接口
/// （授权码/密码/凭证/刷新令牌）必须全部拒绝。这些接口是魔方作为 OAuth 服务端时的
/// 账密通道（尤其密码式授权），若后台仅作为 SSO 客户端使用，未关闭时攻击者可绕过
/// 登录页直接调用换取令牌。
/// </summary>
public class OAuthServerDisabledTests
{
    /// <summary>关闭OAuth服务端：四种令牌接口全部报"未启用OAuth服务"，且在触碰应用验证前即被拦截</summary>
    [Fact(DisplayName = "关闭OAuth服务端：四种令牌接口全部拒绝")]
    public void Disabled_Rejects_All_GrantTypes()
    {
        var prev = CubeSetting.Current.EnableOAuthServer;
        CubeSetting.Current.EnableOAuthServer = false;
        try
        {
            var svc = new TokenService();

            var ex1 = Record.Exception(() => svc.GetAccessToken("app", "secret", "code", "127.0.0.1"));
            Assert.NotNull(ex1);
            Assert.Contains("未启用OAuth服务", ex1.Message);

            var ex2 = Record.Exception(() => svc.GetAccessTokenByPassword("app", "user", "pass", "127.0.0.1"));
            Assert.NotNull(ex2);
            Assert.Contains("未启用OAuth服务", ex2.Message);

            var ex3 = Record.Exception(() => svc.GetAccessTokenByClientCredentials("app", "secret", "user", "127.0.0.1"));
            Assert.NotNull(ex3);
            Assert.Contains("未启用OAuth服务", ex3.Message);

            var ex4 = Record.Exception(() => svc.RefreshToken("app", "refresh_token", "127.0.0.1"));
            Assert.NotNull(ex4);
            Assert.Contains("未启用OAuth服务", ex4.Message);
        }
        finally
        {
            CubeSetting.Current.EnableOAuthServer = prev;
        }
    }

    /// <summary>开启OAuth服务端：守卫放行，请求进入应用验证（用桩验证未被误拦截）</summary>
    [Fact(DisplayName = "开启OAuth服务端：令牌接口放行进入应用验证")]
    public void Enabled_Passes_Guard()
    {
        var prev = CubeSetting.Current.EnableOAuthServer;
        CubeSetting.Current.EnableOAuthServer = true;
        try
        {
            var svc = new TokenService { AppService = new ThrowingAppService() };

            // 若守卫误拦截，会抛"未启用OAuth服务"；放行后应进入 AppService.Auth 抛出桩的哨兵异常
            var ex = Record.Exception(() => svc.GetAccessToken("app", "secret", "code", "127.0.0.1"));
            Assert.NotNull(ex);
            Assert.Contains("reached-app-service", ex.Message);
            Assert.DoesNotContain("未启用OAuth服务", ex.Message);
        }
        finally
        {
            CubeSetting.Current.EnableOAuthServer = prev;
        }
    }

    /// <summary>OAuthAppService 未注入 Setting 时回退全局配置，开关依然生效（不依赖控制器构造接线）</summary>
    [Fact(DisplayName = "OAuthAppService：未注入Setting时回退全局配置拒绝")]
    public void AppService_Without_Setting_FallsBack_To_Global()
    {
        var prev = CubeSetting.Current.EnableOAuthServer;
        CubeSetting.Current.EnableOAuthServer = false;
        try
        {
            // 不注入 Setting（模拟服务被直接调用/未走 SsoController 构造的场景）
            var svc = new OAuthAppService();

            var ex = Record.Exception(() => svc.Auth("app", null, "127.0.0.1"));
            Assert.NotNull(ex);
            Assert.Contains("未启用OAuth服务", ex.Message);
        }
        finally
        {
            CubeSetting.Current.EnableOAuthServer = prev;
        }
    }
}

/// <summary>应用验证服务桩：Auth 直接抛出哨兵异常，用于验证守卫放行路径未被误拦截</summary>
internal class ThrowingAppService : IOAuthAppService
{
    /// <summary>验证应用合法性。抛哨兵异常证明守卫已放行、进入应用验证阶段</summary>
    public App Auth(String client_id, String client_secret, String ip)
        => throw new InvalidOperationException("reached-app-service");

    /// <summary>获取令牌提供者。本测试不会走到这一步</summary>
    public TokenProvider GetProvider() => throw new NotSupportedException();
}
