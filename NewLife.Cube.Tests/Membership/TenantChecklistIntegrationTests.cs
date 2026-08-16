using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using NewLife.Common;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Model;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 检查清单集成测试：按《Doc/多租户改造验证清单.md》四节补充场景（M1-M14）逐项验证，
/// 用真实登录/注册管线（UserService.Login / Register / TryLogin + SQLite 库内断言）。
/// 覆盖现有 TenantAuthReproTests 未触及的场景：
///   注册：M1/M2/M1e/M2e/M6/M6e/M4r；X-App-Id：M7/M8；Enforce 规则B自救：M9；
///   Query 源：M11/M11e；刷新令牌：M10。
/// （S 系列与 M3/M4/M5 已由 TenantAuthReproTests 覆盖，不重复。）
/// </summary>
[Collection("TenantAuth")]
public class TenantChecklistIntegrationTests
{
    private readonly TenantAuthFixture _fx;
    private static Int32 _seq;

    public TenantChecklistIntegrationTests(TenantAuthFixture fixture) => _fx = fixture;

    /// <summary>唯一用户名，避免共享 SQLite 库内注册冲突</summary>
    private static String NewUser(String prefix) => $"{prefix}{Interlocked.Increment(ref _seq)}";

    /// <summary>开启多租户并切到指定模式</summary>
    private static void EnableTenant(TenantEnforceModes mode)
    {
        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.TenantEnforceMode = mode;
        TenantContext.Current = null!;
    }

    /// <summary>恢复进程级状态（与 fixture.Dispose 一致）</summary>
    private static void Restore()
    {
        CubeSetting.Current.EnableTenant = false;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
        TenantContext.Current = null!;
    }

    /// <summary>确保 X-App-Id 测试所需的 OAuthConfig 记录存在（幂等，复用夹具共享库）</summary>
    private OAuthConfig EnsureOAuthConfig()
    {
        const String appId = "app-it-t1";
        var cfg = OAuthConfig.FindByAppId(appId);
        if (cfg == null)
        {
            cfg = new OAuthConfig
            {
                AppId = appId,
                Name = "集成测试OAuth应用",
                TenantId = _fx.Tenant1.Id,
                Enable = true,
            };
            cfg.Insert();
        }
        return cfg;
    }

    /// <summary>注册并返回结果（真实管线：Register → ResolveRegisterTenant → RegisterByPassword → CompleteLogin）</summary>
    private ServiceResult Register(HttpContext ctx, String username)
    {
        var model = new AuthRegisterModel
        {
            Username = username,
            Password = TenantAuthFixture.Password,
            ConfirmPassword = TenantAuthFixture.Password,
        };
        return _fx.UserService.Register(model, ctx);
    }

    /// <summary>登录并返回结果（真实管线：UserService.Login → CompleteLogin）</summary>
    private ServiceResult Login(HttpContext ctx, String username)
    {
        var model = new LoginModel { Username = username, Password = TenantAuthFixture.Password };
        return _fx.UserService.Login(model, ctx);
    }

    #region 登录语义（M3 拒绑 / B7 回落）

    [Fact(DisplayName = "M3：已绑定t1用户带X-Tenant:t2登录 → 登录成功但拒绝自动绑定（绑定表仍只t1）")]
    public void M3_Login_With_OtherXTenant_Should_Reject_AutoBind()
    {
        EnableTenant(TenantEnforceModes.Shadow);
        try
        {
            // 准备：注册新用户并绑定 t1（带 X-Tenant:t1 注册即自动绑定）
            var regCtx = _fx.CreateContext();
            regCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var username = NewUser("m3u");
            Assert.True(Register(regCtx, username).IsSuccess);
            var user = User.FindByName(username);
            Assert.NotNull(user);
            Assert.Equal(1, TenantUser.FindAllByUserId(user.ID).Count(e => e.Enable)); // 仅 t1

            // 带 X-Tenant:t2 登录 → 规则B 收紧：登录成功但拒绝自动绑定
            TenantContext.Current = null!;
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.OtherTenantCode;
            var result = Login(loginCtx, username);
            Assert.True(result.IsSuccess, result.Message);

            var binds = TenantUser.FindAllByUserId(user.ID).Where(e => e.Enable).ToList();
            Assert.Single(binds); // 仍只有 t1
            Assert.Equal(_fx.Tenant1.Id, binds[0].TenantId);
            Assert.DoesNotContain(binds, e => e.TenantId == _fx.Tenant2.Id); // 无 t2 误绑

            // 登录上下文回落自己的 t1（不落在别家租户）
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "B7：Shadow 下已绑定用户无标识访问 → 回落第一个绑定租户（清单'重点验'项）")]
    public void B7_Shadow_BoundUser_Without_Identifier_Falls_Back_To_First_Tenant()
    {
        EnableTenant(TenantEnforceModes.Shadow);
        try
        {
            // BoundUser 绑定 t1，带 X-Tenant:t1 登录拿令牌
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            Assert.True(Login(loginCtx, "tenant01").IsSuccess);
            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 无任何租户标识访问 → 回落 t1（不是空上下文、不是别家租户）
            TenantContext.Current = null!;
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId); // 回落第一个绑定租户
        }
        finally
        {
            Restore();
        }
    }

    #endregion

    #region 注册（M1/M2/M1e/M2e/M6/M4r）

    [Fact(DisplayName = "M1：Shadow 注册无标识 → 成功、不建任何绑定")]
    public void M1_Shadow_Register_Without_Identifier_Should_Succeed_NoBinding()
    {
        EnableTenant(TenantEnforceModes.Shadow);
        try
        {
            var ctx = _fx.CreateContext();
            var username = NewUser("m1u");
            var result = Register(ctx, username);

            Assert.True(result.IsSuccess, result.Message);
            var user = User.FindByName(username);
            Assert.NotNull(user);
            Assert.Empty(TenantUser.FindAllByUserId(user.ID)); // 无标识不绑定
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "M2：Shadow 注册带 X-Tenant:t1 → 成功 + 自动绑定 t1")]
    public void M2_Shadow_Register_With_XTenant_Should_Bind()
    {
        EnableTenant(TenantEnforceModes.Shadow);
        try
        {
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var username = NewUser("m2u");
            var result = Register(ctx, username);

            Assert.True(result.IsSuccess, result.Message);
            var user = User.FindByName(username);
            Assert.NotNull(user);
            var tu = TenantUser.FindByTenantIdAndUserId(_fx.Tenant1.Id, user.ID);
            Assert.NotNull(tu);
            Assert.True(tu.Enable);
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "M1e：Enforce 注册无标识 → 拒绝且不建用户")]
    public void M1e_Enforce_Register_Without_Identifier_Should_Reject()
    {
        EnableTenant(TenantEnforceModes.Enforce);
        try
        {
            var ctx = _fx.CreateContext();
            var username = NewUser("m1eu");
            var result = Register(ctx, username);

            Assert.False(result.IsSuccess);
            Assert.Contains("租户", result.Message); // 多租户模式下，注册必须携带X-App-Id或X-Tenant租户信息
            Assert.Null(User.FindByName(username)); // 拒绝发生在建用户之前
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "M2e：Enforce 注册带 X-Tenant:t1 → 成功 + 绑定")]
    public void M2e_Enforce_Register_With_XTenant_Should_Bind()
    {
        EnableTenant(TenantEnforceModes.Enforce);
        try
        {
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var username = NewUser("m2eu");
            var result = Register(ctx, username);

            Assert.True(result.IsSuccess, result.Message);
            var user = User.FindByName(username);
            Assert.NotNull(user);
            var tu = TenantUser.FindByTenantIdAndUserId(_fx.Tenant1.Id, user.ID);
            Assert.NotNull(tu);
            Assert.True(tu.Enable);
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "M6/M6e：注册带 X-App-Id（OAuth 配置租户）→ 成功 + 绑定配置租户（Shadow/Enforce）")]
    public void M6_Register_With_XAppId_Should_Bind_ConfigTenant()
    {
        EnsureOAuthConfig();
        foreach (var enforce in new[] { false, true })
        {
            EnableTenant(enforce ? TenantEnforceModes.Enforce : TenantEnforceModes.Shadow);
            try
            {
                var ctx = _fx.CreateContext();
                ctx.Request.Headers["X-App-Id"] = "app-it-t1";
                var username = NewUser(enforce ? "m6eu" : "m6u");
                var result = Register(ctx, username);

                Assert.True(result.IsSuccess, result.Message);
                var user = User.FindByName(username);
                Assert.NotNull(user);
                var tu = TenantUser.FindByTenantIdAndUserId(_fx.Tenant1.Id, user.ID);
                Assert.NotNull(tu);
                Assert.True(tu.Enable);
            }
            finally
            {
                Restore();
            }
        }
    }

    [Fact(DisplayName = "M4r：注册带无效租户编码 X-Tenant:t9 → 拒绝（Shadow/Enforce 一致）")]
    public void M4r_Register_With_Invalid_XTenant_Should_Reject()
    {
        foreach (var enforce in new[] { false, true })
        {
            EnableTenant(enforce ? TenantEnforceModes.Enforce : TenantEnforceModes.Shadow);
            try
            {
                var ctx = _fx.CreateContext();
                ctx.Request.Headers["X-Tenant"] = "t9";
                var username = NewUser(enforce ? "m4reu" : "m4ru");
                var result = Register(ctx, username);

                Assert.False(result.IsSuccess);
                Assert.Contains("租户", result.Message); // 宽松断言：租户相关错误即可（不耦合具体文案，防文案改动误红）
                Assert.Null(User.FindByName(username));
            }
            finally
            {
                Restore();
            }
        }
    }

    #endregion

    #region X-App-Id 登录/访问（M7/M8）

    [Fact(DisplayName = "M7：无绑定存量用户带 X-App-Id 登录 → 自动绑定 OAuth 配置租户")]
    public void M7_Login_With_XAppId_Should_AutoBind_ConfigTenant()
    {
        EnableTenant(TenantEnforceModes.Shadow);
        try
        {
            // 准备：Shadow 无标识注册 → 无绑定存量用户
            var regCtx = _fx.CreateContext();
            var username = NewUser("m7u");
            Assert.True(Register(regCtx, username).IsSuccess);
            var user = User.FindByName(username);
            Assert.NotNull(user);
            Assert.Empty(TenantUser.FindAllByUserId(user.ID));

            // 带 X-App-Id 登录 → 自动绑定配置租户
            EnsureOAuthConfig();
            TenantContext.Current = null!;
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-App-Id"] = "app-it-t1";
            var result = Login(loginCtx, username);
            Assert.True(result.IsSuccess, result.Message);

            var tu = TenantUser.FindByTenantIdAndUserId(_fx.Tenant1.Id, user.ID);
            Assert.NotNull(tu);
            Assert.True(tu.Enable);
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "M8：访问带 X-App-Id → 成员放行（上下文=配置租户），非成员拒绝（Shadow/Enforce）")]
    public void M8_Access_With_XAppId_Member_Pass_NonMember_Reject()
    {
        EnsureOAuthConfig();
        foreach (var enforce in new[] { false, true })
        {
            EnableTenant(enforce ? TenantEnforceModes.Enforce : TenantEnforceModes.Shadow);
            try
            {
                // 成员：BoundUser（绑定 t1）登录拿令牌
                var loginCtx = _fx.CreateContext();
                loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
                Assert.True(Login(loginCtx, "tenant01").IsSuccess);
                var token = loginCtx.Items["jwtToken"] as String;
                Assert.False(String.IsNullOrEmpty(token));

                // 带 Bearer + X-App-Id 访问 → 放行，上下文=配置租户 t1
                TenantContext.Current = null!;
                var ctx = _fx.CreateContext();
                ctx.Request.Headers["Authorization"] = $"Bearer {token}";
                ctx.Request.Headers["X-App-Id"] = "app-it-t1";
                var current = _fx.Provider.TryLogin(ctx);
                Assert.NotNull(current);
                Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);

                // 非成员：legacy01（无绑定）登录拿令牌
                TenantContext.Current = null!;
                var loginCtx2 = _fx.CreateContext();
                Assert.True(Login(loginCtx2, "legacy01").IsSuccess);
                var token2 = loginCtx2.Items["jwtToken"] as String;
                Assert.False(String.IsNullOrEmpty(token2));

                // 带 Bearer + X-App-Id 访问 → 非成员拒绝（显式标识优先校验）
                TenantContext.Current = null!;
                var ctx2 = _fx.CreateContext();
                ctx2.Request.Headers["Authorization"] = $"Bearer {token2}";
                ctx2.Request.Headers["X-App-Id"] = "app-it-t1";
                Assert.Null(_fx.Provider.TryLogin(ctx2));

                // 正向对照：同一 legacy01 令牌带 X-Tenant:t1 也拒绝（无绑定非成员），避免"TryLogin 全局损坏"误绿
                TenantContext.Current = null!;
                var ctx2b = _fx.CreateContext();
                ctx2b.Request.Headers["Authorization"] = $"Bearer {token2}";
                ctx2b.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
                Assert.Null(_fx.Provider.TryLogin(ctx2b));
            }
            finally
            {
                Restore();
            }
        }
    }

    #endregion

    #region Enforce 规则B 自救（M9）

    [Fact(DisplayName = "M9：Enforce 下存量无绑定用户带 X-Tenant 重新登录 → 自动绑定 → 新令牌可用")]
    public void M9_Enforce_LegacyUser_Relogin_With_XTenant_Should_SelfRecover()
    {
        // 准备：Shadow 无标识注册 → 无绑定存量用户，注册令牌（无标识）在 Enforce 下不可用
        EnableTenant(TenantEnforceModes.Shadow);
        var username = NewUser("m9u");
        String? oldToken = null;
        try
        {
            var regCtx = _fx.CreateContext();
            var regResult = Register(regCtx, username);
            Assert.True(regResult.IsSuccess, regResult.Message);
            oldToken = regCtx.Items["jwtToken"] as String; // 注册即登录，令牌已写入 Items
        }
        finally
        {
            Restore();
        }
        var user = User.FindByName(username);
        Assert.NotNull(user);
        Assert.Empty(TenantUser.FindAllByUserId(user.ID));

        // 切 Enforce：旧令牌（无标识）访问 → 拒绝
        EnableTenant(TenantEnforceModes.Enforce);
        try
        {
            Assert.False(String.IsNullOrEmpty(oldToken), "注册即登录应已颁发令牌"); // 防止旧令牌腿被跳过
            TenantContext.Current = null!;
            var ctxOld = _fx.CreateContext();
            ctxOld.Request.Headers["Authorization"] = $"Bearer {oldToken}";
            Assert.Null(_fx.Provider.TryLogin(ctxOld));

            // 带 X-Tenant:t1 重新登录 → 自动绑定（规则B 自救）
            TenantContext.Current = null!;
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var result = Login(loginCtx, username);
            Assert.True(result.IsSuccess, result.Message);

            var tu = TenantUser.FindByTenantIdAndUserId(_fx.Tenant1.Id, user.ID);
            Assert.NotNull(tu);
            Assert.True(tu.Enable);

            // 新令牌 + X-Tenant:t1 访问 → 通过
            var newToken = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(newToken));
            TenantContext.Current = null!;
            var ctxNew = _fx.CreateContext();
            ctxNew.Request.Headers["Authorization"] = $"Bearer {newToken}";
            ctxNew.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var current = _fx.Provider.TryLogin(ctxNew);
            Assert.NotNull(current);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            Restore();
        }
    }

    #endregion

    #region Query 源与刷新令牌（M11/M11e/M10）

    [Fact(DisplayName = "M11/M11e：访问带 Query tenantId → 有效租户放行（上下文=该租户），tenantId=0 普通用户拒绝")]
    public void M11_Access_With_QueryTenantId_Valid_Pass_Zero_Reject()
    {
        EnableTenant(TenantEnforceModes.Shadow);
        try
        {
            // BoundUser 登录拿令牌
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            Assert.True(Login(loginCtx, "tenant01").IsSuccess);
            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // ?tenantId={t1.Id} → 放行，上下文=t1
            TenantContext.Current = null!;
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx.Request.QueryString = new QueryString($"?tenantId={_fx.Tenant1.Id}");
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);

            // ?tenantId=0（管理后台）→ 普通用户拒绝（与 M5 同语义）
            TenantContext.Current = null!;
            var ctx0 = _fx.CreateContext();
            ctx0.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx0.Request.QueryString = new QueryString("?tenantId=0");
            Assert.Null(_fx.Provider.TryLogin(ctx0));
        }
        finally
        {
            Restore();
        }
    }

    [Fact(DisplayName = "M10：刷新令牌 → 旧access/refresh全部失效、新令牌随模式可用（旋转+Enforce分支）")]
    public void M10_RefreshToken_Rotates_And_NewToken_Usable()
    {
        EnableTenant(TenantEnforceModes.Shadow);
        try
        {
            // BoundUser 登录，拿 access + refresh
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var result = Login(loginCtx, "tenant01");
            Assert.True(result.IsSuccess, result.Message);
            var oldAccess = loginCtx.Items["jwtToken"] as String;
            var refresh = loginCtx.Items["refreshToken"] as String;
            Assert.False(String.IsNullOrEmpty(oldAccess));
            Assert.False(String.IsNullOrEmpty(refresh));

            // 刷新：旧 UserToken 吊销（旋转），新 access 颁发
            var user = User.FindByName("tenant01");
            Assert.NotNull(user);
            TenantContext.Current = null!;
            var refreshCtx = _fx.CreateContext();
            var newToken = refreshCtx.RefreshToken(user, refresh);
            Assert.NotNull(newToken);
            Assert.False(String.IsNullOrEmpty(newToken.AccessToken));

            var oldUt = UserToken.FindByToken(refresh);
            Assert.NotNull(oldUt);
            Assert.False(oldUt.Enable); // 旧刷新令牌已吊销

            // 旧 access（jti 指向同一 UserToken）随之失效
            TenantContext.Current = null!;
            var ctxOldAccess = _fx.CreateContext();
            ctxOldAccess.Request.Headers["Authorization"] = $"Bearer {oldAccess}";
            ctxOldAccess.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            Assert.Null(_fx.Provider.TryLogin(ctxOldAccess));

            // Shadow：新 access + X-Tenant:t1 访问 → 通过
            TenantContext.Current = null!;
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {newToken.AccessToken}";
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);

            // Enforce：新 access 无标识 → 拒绝；带 X-Tenant:t1 → 放行
            CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Enforce;
            TenantContext.Current = null!;
            var ctxEnforceNoId = _fx.CreateContext();
            ctxEnforceNoId.Request.Headers["Authorization"] = $"Bearer {newToken.AccessToken}";
            Assert.Null(_fx.Provider.TryLogin(ctxEnforceNoId));

            TenantContext.Current = null!;
            var ctxEnforce = _fx.CreateContext();
            ctxEnforce.Request.Headers["Authorization"] = $"Bearer {newToken.AccessToken}";
            ctxEnforce.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            Assert.NotNull(_fx.Provider.TryLogin(ctxEnforce));
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            Restore();
        }
    }

    #endregion
}
