using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Cube.Controllers;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Cube.Services.Sso;
using NewLife.Web;
using NewLife.Web.OAuth;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 微信小程序登录复现：无租户用户经 WxMiniLogin 注册/再登录时，
/// 请求已带 AppId 与 X-Tenant，用户仍应落到该租户，且租户角色不能直接抄全局「普通用户」。
/// 网关用桩响应代替 api.weixin.qq.com 的 jscode2session。
/// </summary>
[Collection("TenantAuth")]
public class WxMiniLoginTenantReproTests
{
    private const String OpenId = "o-wxmini-repro-1";
    private const String AppId = "wx-mini-repro-t1";

    private readonly TenantAuthFixture _fx;

    public WxMiniLoginTenantReproTests(TenantAuthFixture fixture) => _fx = fixture;

    [Fact(DisplayName = "WxMiniLogin：无租户用户带 AppId 与 X-Tenant 注册后再登录，绑定到配置租户且使用租户角色")]
    public void WxMiniLogin_Binds_UnboundUser_To_AppTenant_With_TenantRole()
    {
        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.AutoRegister = true;
        TenantContext.Current = null!;
        _fx.Provider.Current = null;
        try
        {
            EnsureWxConfig(_fx.Tenant1.Id);

            var controller = CreateController();
            var first = Login(controller);
            Assert.False(String.IsNullOrEmpty(first.Data?.AccessToken));
            Assert.Equal(TenantAuthFixture.TenantCode, controller.Response.Headers["X-Tenant"].ToString());

            var user = FindWxUser();
            Assert.NotNull(user);
            Assert.Equal("普通用户", user.Role?.Name);

            var tenantRoleId = ManagerProviderHelper.ResolveTenantRole(_fx.Tenant1.Id);
            Assert.NotEqual(user.RoleID, tenantRoleId);

            var tu = TenantUser.FindByTenantIdAndUserId(_fx.Tenant1.Id, user.ID);
            Assert.NotNull(tu);
            Assert.True(tu.Enable);
            Assert.Equal(tenantRoleId, tu.RoleId);

            // 同一微信用户再登录：仍只有这一条绑定
            TenantContext.Current = null!;
            _fx.Provider.Current = null;
            var second = Login(controller);
            Assert.False(String.IsNullOrEmpty(second.Data?.AccessToken));
            Assert.Single(TenantUser.FindAllByUserId(user.ID), e => e.Enable);

            // 已有租户的用户，换到别的小程序租户后再登录，不自动加入别家租户
            MoveWxConfig(_fx.Tenant2.Id);
            TenantContext.Current = null!;
            _fx.Provider.Current = null;
            var other = Login(controller);
            Assert.False(String.IsNullOrEmpty(other.Data?.AccessToken));
            Assert.Null(TenantUser.FindByTenantIdAndUserId(_fx.Tenant2.Id, user.ID));
            Assert.Single(TenantUser.FindAllByUserId(user.ID), e => e.Enable && e.TenantId == _fx.Tenant1.Id);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
            _fx.Provider.Current = null;
        }
    }

    private ApiResponse<TokenModel> Login(SsoController controller)
    {
        var ctx = _fx.CreateContext();
        ctx.Request.Headers["X-App-Id"] = AppId;
        ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
        controller.ControllerContext = new ControllerContext { HttpContext = ctx };

        return controller.WxMiniLogin(new WxLoginModel { Code = "wx-code-repro", AppId = AppId });
    }

    private SsoController CreateController()
    {
        var binding = new UserBindingService { Provider = _fx.Provider };
        var client = new SsoClientService { Provider = _fx.Provider, BindingService = binding };
        var server = new SsoServerService();
        var token = new TokenService { Provider = _fx.Provider };
        var app = new OAuthAppService { Setting = CubeSetting.Current };
        return new ReproSsoController(CubeSetting.Current, new TestCacheProvider(), client, server, token, app, binding, new TenantContextService());
    }

    private static void EnsureWxConfig(Int32 tenantId)
    {
        var cfg = OAuthConfig.FindByAppId(AppId) ?? new OAuthConfig { AppId = AppId, Name = "WxOpen" };
        cfg.Name = "WxOpen";
        cfg.AppId = AppId;
        cfg.Secret = "wx-secret-repro";
        cfg.TenantId = tenantId;
        cfg.Enable = true;
        cfg.AutoRegister = true;
        cfg.GrantType = GrantTypes.AuthorizationCode;
        cfg.Save();
        OAuthConfig.Meta.Cache.Clear("wx mini repro", true);
    }

    private static void MoveWxConfig(Int32 tenantId)
    {
        var cfg = OAuthConfig.FindByAppId(AppId);
        Assert.NotNull(cfg);
        cfg.TenantId = tenantId;
        cfg.Save();
        OAuthConfig.Meta.Cache.Clear("wx mini repro move", true);
    }

    private static User FindWxUser()
    {
        var uc = UserConnect.FindByProviderAndOpenID("WxOpen", OpenId);
        Assert.NotNull(uc);
        return User.FindByID(uc.UserID);
    }

    /// <summary>只替换微信网关响应，配置解析与 SetTenant 仍走生产代码</summary>
    private sealed class ReproSsoController : SsoController
    {
        public ReproSsoController(CubeSetting setting, ICacheProvider cacheProvider,
            ISsoClientService clientService, ISsoServerService serverService,
            ITokenService tokenService, IOAuthAppService appService, IUserBindingService bindingService, ITenantContext tenantContext)
            : base(setting, cacheProvider, clientService, serverService, tokenService, appService, bindingService, tenantContext)
        {
        }

        protected override OAuthClient CreateWxOpenClient(String appId, String wxLoginType)
        {
            var client = base.CreateWxOpenClient(appId, wxLoginType);
            return new FakeWxOpenClient
            {
                Name = client.Name,
                Key = client.Key,
                Secret = client.Secret,
                TenantId = client.TenantId,
                Server = client.Server,
                AccessUrl = client.AccessUrl,
                UserUrl = client.UserUrl,
            };
        }
    }

    private sealed class FakeWxOpenClient : WxOpenClient
    {
        protected override String GetHtml(String action, String url)
            => "{\"openid\":\"" + OpenId + "\",\"session_key\":\"sk-repro\",\"unionid\":\"u-repro-1\"}";
    }
}
