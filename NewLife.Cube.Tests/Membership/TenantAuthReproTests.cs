using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 多租户测试集合。夹具会改动 ManageProvider.Provider、CubeSetting.EnableTenant、
/// TenantContext.Current、DAL 连接串等进程级状态，因此：
/// 1. 所有相关测试类必须串行执行（DisableParallelization）；
/// 2. 集合内共享同一个夹具实例（ICollectionFixture），SQLite 数据库只构建一次，
///    避免多个 IClassFixture 各自重建数据库造成全局状态反复改写。
/// </summary>
[CollectionDefinition("TenantAuth", DisableParallelization = true)]
public class TenantAuthCollection : ICollectionFixture<TenantAuthFixture>
{
}

/// <summary>
/// 多租户认证夹具：创建SQLite Membership数据库，初始化角色/用户/租户数据，
/// 并按生产环境 UseManagerProvider 的方式装配 <see cref="ManageProvider2"/>；
/// 另提供 <see cref="UserService"/> 用于驱动真实登录管线（AuthController.Login 同路径）。
/// </summary>
public class TenantAuthFixture : IDisposable
{
    /// <summary>统一测试密码</summary>
    public const String Password = "Test@12345";

    /// <summary>租户编码（对应 X-Tenant 请求头所传的值）</summary>
    public const String TenantCode = "t1";

    /// <summary>管理提供者</summary>
    public ManageProvider2 Provider { get; }

    /// <summary>用户服务。驱动真实登录管线：UserService.Login -> LoginByPassword -> CompleteLogin</summary>
    public UserService UserService { get; }

    /// <summary>Http上下文访问器，每个测试请求前由 <see cref="CreateContext"/> 关联当前上下文</summary>
    public HttpContextAccessor Accessor { get; }

    /// <summary>服务提供者（作为 HttpContext.RequestServices）</summary>
    public IServiceProvider Services { get; }

    /// <summary>存量普通用户。非管理员、没有任何租户绑定——模拟多租户开启之前创建的历史账号</summary>
    public User LegacyUser { get; private set; } = null!;

    /// <summary>第二个存量用户。专供"重新登录自救"测试使用，避免污染 LegacyUser 的绑定状态断言</summary>
    public User LegacyUser2 { get; private set; } = null!;

    /// <summary>系统管理员（角色 IsSystem=true）</summary>
    public User AdminUser { get; private set; } = null!;

    /// <summary>已绑定测试租户的普通用户</summary>
    public User BoundUser { get; private set; } = null!;

    /// <summary>测试租户（Code=t1，启用）</summary>
    public Tenant Tenant1 { get; private set; } = null!;

    /// <summary>第二个测试租户（Code=t2，启用）。专供"别家租户/非成员访问"场景，BoundUser 不绑定它</summary>
    public Tenant Tenant2 { get; private set; } = null!;

    /// <summary>别家租户编码（对应 X-Tenant 请求头所传的值），BoundUser 非其成员</summary>
    public const String OtherTenantCode = "t2";

    public TenantAuthFixture()
    {
        // 1. SQLite数据库（每次重建，保证干净环境）
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        var dbFile = Path.Combine(dir, "CubeTenantTests.db");
        if (File.Exists(dbFile)) File.Delete(dbFile);

        DAL.AddConnStr("Membership", $"Data Source={dbFile}", null, "SQLite");
        // 与 CubeService.AddCube 一致：Cube/Log 连接映射到 Membership
        DAL.ConnStrs!.TryAdd("Cube", "MapTo=Membership");
        DAL.ConnStrs!.TryAdd("Log", "MapTo=Membership");

        // 2. 固定JWT密钥，保证测试内令牌签发/校验可复现
        CubeSetting.Current.JwtSecret = "HS256:CubeTenantReproTestSecret";

        // 3. 服务提供者。ManageProvider2 通过 IHttpContextAccessor 定位当前 Http 上下文
        Accessor = new HttpContextAccessor();
        var sc = new ServiceCollection();
        sc.AddSingleton<IHttpContextAccessor>(Accessor);
        Services = sc.BuildServiceProvider();

        // 4. 装配管理提供者。生产环境由 UseManagerProvider 完成同样两步，
        //    ManageProvider2.Context 为 internal static，测试中经反射注入。
        //    建议后续为主程序集添加 InternalsVisibleTo("NewLife.Cube.Tests") 测试接缝以去掉反射
        Provider = new ManageProvider2();
        ManageProvider.Provider = Provider;
        typeof(ManageProvider2)
            .GetField("Context", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, Accessor);

        // 5. 驱动真实登录管线的用户服务。注入内存缓存与密码服务（注册路径的密码强度校验会用到），
        //    其余依赖（短信/邮件/验证码/MFA/追踪/绑定）在账号密码登录/注册路径不会被使用
        UserService = new UserService(null!, null!, new PasswordService(), new TestCacheProvider(), null!, null!, null!, null!, new TenantContextService());

        Seed();
    }

    public void Dispose()
    {
        // 恢复最易泄漏的进程级状态（开关与租户上下文）。
        // ManageProvider.Provider/Context 保持不动：其它测试类可能依赖当前装配，
        // 且全量套件在本夹具存在时已通过
        CubeSetting.Current.EnableTenant = false;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
        CubeSetting.Current.TenantQueryPolicy = TenantQueryPolicies.DenyWithEmpty;
        TenantContext.Current = null!;
    }

    /// <summary>初始化角色、用户、租户及绑定关系</summary>
    private void Seed()
    {
        var normal = new Role { Name = "普通用户", Enable = true };
        normal.Insert();
        var admin = new Role { Name = "系统管理员", Enable = true, IsSystem = true };
        admin.Insert();

        var hash = Provider.PasswordProvider.Hash(Password);

        LegacyUser = CreateUser("legacy01", normal.ID, hash);
        LegacyUser2 = CreateUser("legacy02", normal.ID, hash);
        AdminUser = CreateUser("admin01", admin.ID, hash);
        BoundUser = CreateUser("tenant01", normal.ID, hash);

        Tenant1 = new Tenant { Code = TenantCode, Name = "测试租户", Enable = true };
        Tenant1.Insert();

        Tenant2 = new Tenant { Code = OtherTenantCode, Name = "别家租户", Enable = true };
        Tenant2.Insert();

        // 仅 tenant01 绑定到租户，legacy01/legacy02 保持无任何绑定（存量用户状态）
        new TenantUser { TenantId = Tenant1.Id, UserId = BoundUser.ID, Enable = true }.Insert();
    }

    private static User CreateUser(String name, Int32 roleId, String passwordHash)
    {
        var user = new User
        {
            Name = name,
            DisplayName = name,
            Password = passwordHash,
            RoleID = roleId,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        return user;
    }

    /// <summary>创建一个 Http 上下文并关联到访问器（同一同步执行流内有效）</summary>
    public HttpContext CreateContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("localhost");
        ctx.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        ctx.RequestServices = Services;
        Accessor.HttpContext = ctx;

        return ctx;
    }
}

/// <summary>简单内存缓存提供者，用于测试隔离（与 XUnitTest.SkiaCaptchaTests 内同名实现一致）</summary>
internal class TestCacheProvider : ICacheProvider
{
    /// <summary>缓存</summary>
    public ICache Cache { get; set; }

    /// <summary>内部缓存</summary>
    public ICache InnerCache { get; set; }

    public TestCacheProvider()
    {
        var cache = new MemoryCache();
        Cache = cache;
        InnerCache = cache;
    }

    /// <summary>队列不支持</summary>
    public IProducerConsumer<T> GetQueue<T>(String name, String? topic = null) => throw new NotImplementedException();

    /// <summary>内部队列不支持</summary>
    public IProducerConsumer<T> GetInnerQueue<T>(String name) => throw new NotImplementedException();

    /// <summary>锁不支持</summary>
    public IDisposable AcquireLock(String name, Int32 msTimeout) => throw new NotImplementedException();
}

/// <summary>
/// 租户行为回归测试。P0-1/P0-2 等缺陷已修复，原"复现测试"（绿色=缺陷仍在）已退役，
/// 保留的用例断言修复后的正确行为：Shadow 期兼容放行、Enforce 严格 fail-closed、令牌幂等等。
/// 期望行为由 <see cref="TenantAuthExpectationTests"/> 与本类共同守护。
/// </summary>
[Collection("TenantAuth")]
public class TenantAuthReproTests
{
    private readonly TenantAuthFixture _fx;

    public TenantAuthReproTests(TenantAuthFixture fixture) => _fx = fixture;

    [Fact(DisplayName = "未开启多租户：存量用户正常登录并使用令牌通过认证（基线，修复后仍应有效）")]
    public void LegacyUser_Login_And_Auth_When_Tenant_Disabled()
    {
        CubeSetting.Current.EnableTenant = false;
        TenantContext.Current = null!;
        try
        {
            // 登录：AuthController.Login -> UserService.LoginByPassword -> ManageProvider2.Login
            var loginCtx = _fx.CreateContext();
            var user = _fx.Provider.Login("legacy01", TenantAuthFixture.Password, false);
            Assert.NotNull(user);

            // UserService.CompleteLogin 会调用 ChooseTenant，未开启多租户时为空操作
            loginCtx.ChooseTenant(user.ID);

            // 令牌已颁发
            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 后续请求携带令牌可正常认证
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(user.ID, current.ID);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "Enforce 无租户标识：存量用户登录令牌不可用、请求被拒、不建绑定（回归）")]
    public void LegacyUser_LockedOut_When_Tenant_Enabled_Without_XTenant()
    {
        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Enforce;
        TenantContext.Current = null!;
        try
        {
            // ① 本测试走 ManageProvider2.Login + 手工 ChooseTenant 链（令牌已发、缺陷出现在其后）。
            //    真实 API 管线在更早的 CompleteLogin.EnsureTenantUser 处即 NRE，
            //    见 Repro_RealLoginPipeline_Throws_Before_Token_Response
            var loginCtx = _fx.CreateContext();
            var user = _fx.Provider.Login("legacy01", TenantAuthFixture.Password, false);
            Assert.NotNull(user);

            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // ②（P0-1 已修复）无租户信息时 ChooseTenant 不再抛 NRE，仅不设置租户上下文
            loginCtx.ChooseTenant(user.ID);
            Assert.Equal(0, TenantContext.CurrentId);

            // ③ 无Cookie的API请求携带刚颁发的登录令牌，Enforce 严格 fail-closed 拒绝（TryLogin 返回 null）
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            Assert.Null(_fx.Provider.TryLogin(ctx));

            // ④ fail-closed：若请求能解析出租户0（管理后台，如浏览器携带 TenantId Cookie=0），
            //    无租户绑定的普通用户得不到租户上下文，ValidateTenant 拒绝、TryLogin 返回 null（401“没有登录或登录超时”）
            var cookieKey = $"TenantId-{SysConfig.Current.Name}";

            TenantContext.Current = null!;
            var ctx2 = _fx.CreateContext();
            ctx2.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx2.Request.Headers["Cookie"] = $"{cookieKey}=0";
            Assert.False(ctx2.ValidateTenant(user));

            TenantContext.Current = null!;
            var ctx3 = _fx.CreateContext();
            ctx3.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx3.Request.Headers["Cookie"] = $"{cookieKey}=0";
            Assert.Null(_fx.Provider.TryLogin(ctx3));

            // ⑤ 整个登录流程没有为存量用户建立任何租户绑定，用户被彻底锁死
            Assert.Empty(TenantUser.FindAllByUserId(user.ID));

            // ⑥ 正向对照：Enforce 下已绑定用户带 X-Tenant:t1 应正常放行——防止 TryLogin 全局损坏导致本测试误绿
            TenantContext.Current = null!;
            var okLoginCtx = _fx.CreateContext();
            okLoginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var okUser = _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
            Assert.NotNull(okUser);
            var okToken = okLoginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(okToken));
            TenantContext.Current = null!;
            var okCtx = _fx.CreateContext();
            okCtx.Request.Headers["Authorization"] = $"Bearer {okToken}";
            okCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var ok = _fx.Provider.TryLogin(okCtx);
            Assert.NotNull(ok);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "Enforce 无绑定：存量用户旧令牌携带X-Tenant仍被拒绝（回归）")]
    public void LegacyUser_OldToken_Rejected_Even_With_XTenant()
    {
        // 多租户尚未开启时正常登录，取得旧令牌（存量用户的常态）
        CubeSetting.Current.EnableTenant = false;
        TenantContext.Current = null!;

        var loginCtx = _fx.CreateContext();
        var user = _fx.Provider.Login("legacy01", TenantAuthFixture.Password, false);
        Assert.NotNull(user);
        var token = loginCtx.Items["jwtToken"] as String;
        Assert.False(String.IsNullOrEmpty(token));

        try
        {
            // 开启多租户后，由于不存在 TenantUser 绑定，ChooseTenant 不会接受请求携带的租户；
            // 令牌认证路径（TryLogin）不会补建绑定。唯一自救方式是重新登录并携带 X-Tenant，
            // 由 CompleteLogin 中的 EnsureTenantUser 补建绑定（见期望测试 LegacyUser_Can_SelfRecover_By_Relogin_With_XTenant）
            CubeSetting.Current.EnableTenant = true;

            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;

            Assert.False(ctx.ValidateTenant(user));
            Assert.Null(_fx.Provider.TryLogin(ctx));
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "P2-10修复：真实管线一次成功登录只颁发一条令牌记录（幂等）")]
    public void Login_Issues_Token_Once()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var before = UserToken.FindAllByUserID(_fx.BoundUser.ID);

            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password };
            var result = _fx.UserService.Login(model, loginCtx);
            Assert.True(result.IsSuccess);

            // IssueLoginToken 幂等：ManageProvider2.Login 与 CompleteLogin 复用同一令牌，只新增一条 UserToken
            var after = UserToken.FindAllByUserID(_fx.BoundUser.ID);
            Assert.Equal(before.Count + 1, after.Count);
            Assert.Equal(before.Count(e => e.Enable) + 1, after.Count(e => e.Enable));
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "开启多租户：系统管理员携带租户Cookie正常通过校验（浏览器模式，非复现）")]
    public void Admin_Not_Affected_When_Tenant_Enabled()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            // 说明：完全无Cookie的管理员请求同样会命中 GetTenantId 的 NRE（P0-1），
            // 对应期望测试 Expect_Admin_Login_Without_Cookie_Should_Succeed（当前 Skip）；
            // 此处按浏览器模式携带 TenantId Cookie=0（管理后台）验证设计意图
            var cookieKey = $"TenantId-{SysConfig.Current.Name}";

            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["Cookie"] = $"{cookieKey}=0";
            var user = _fx.Provider.Login("admin01", TenantAuthFixture.Password, false);
            Assert.NotNull(user);
            loginCtx.ChooseTenant(user.ID);

            // 系统管理员进入管理后台（租户0）
            Assert.Equal(0, TenantContext.CurrentId);

            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx.Request.Headers["Cookie"] = $"{cookieKey}=0";
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(user.ID, current.ID);
            Assert.Equal(0, TenantContext.CurrentId); // 管理员进入管理后台（租户0）
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "开启多租户并传X-Tenant：已绑定租户用户正常登录并访问（非复现）")]
    public void BoundUser_With_XTenant_Works()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;

            var user = _fx.Provider.Login("tenant01", TenantAuthFixture.Password, false);
            Assert.NotNull(user);
            loginCtx.ChooseTenant(user.ID);

            // 进入已绑定的租户
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);

            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }
}

/// <summary>
/// 期望行为测试：描述 P0 缺陷修复后应有的行为。
/// 当前被缺陷阻塞的用例以 Skip 标注（理由注明对应 P0 编号），修复后去掉 Skip 即转为回归护栏；
/// 不受缺陷阻塞的行为（如携带 X-Tenant 重新登录自救）直接以绿色测试守护。
/// </summary>
[Collection("TenantAuth")]
public class TenantAuthExpectationTests
{
    private readonly TenantAuthFixture _fx;

    public TenantAuthExpectationTests(TenantAuthFixture fixture) => _fx = fixture;

    [Fact(DisplayName = "期望：开启多租户后（影子期），存量用户未传X-Tenant也能登录并使用令牌")]
    public void Expect_LegacyUser_Login_Should_Succeed_Without_XTenant()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var loginCtx = _fx.CreateContext();
            var model = new LoginModel { Username = "legacy01", Password = TenantAuthFixture.Password };
            var result = _fx.UserService.Login(model, loginCtx);
            Assert.True(result.IsSuccess);

            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 后续无Cookie请求携带令牌应能通过认证
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(0, TenantContext.CurrentId); // 无绑定普通用户：无标识不设置租户上下文（Shadow 规则A）
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "期望：开启多租户后，管理员完全无Cookie也能登录并进入管理后台")]
    public void Expect_Admin_Login_Without_Cookie_Should_Succeed()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            var loginCtx = _fx.CreateContext();
            var model = new LoginModel { Username = "admin01", Password = TenantAuthFixture.Password };
            var result = _fx.UserService.Login(model, loginCtx);
            Assert.True(result.IsSuccess);

            // 系统管理员进入管理后台（租户0）
            Assert.Equal(0, TenantContext.CurrentId);

            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 无Cookie的后续请求同样应通过认证
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "期望：Enforce 下管理员无租户标识也能登录并进入管理后台（管理员不属任何租户，无需租户标识）")]
    public void Expect_Admin_Enforce_NoIdentifier_Should_Succeed()
    {
        CubeSetting.Current.EnableTenant = true;
        CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Enforce;
        TenantContext.Current = null!;
        try
        {
            var loginCtx = _fx.CreateContext();
            var model = new LoginModel { Username = "admin01", Password = TenantAuthFixture.Password };
            var result = _fx.UserService.Login(model, loginCtx);
            Assert.True(result.IsSuccess);

            // 系统管理员进入管理后台（租户0），无需租户标识
            Assert.Equal(0, TenantContext.CurrentId);

            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 无租户标识的后续请求：管理员应放行（进管理后台），而非被"缺少租户信息"拒绝
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(0, TenantContext.CurrentId);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "期望（当前已成立）：存量用户携带X-Tenant重新登录可自救，EnsureTenantUser补建绑定")]
    public void LegacyUser_Can_SelfRecover_By_Relogin_With_XTenant()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            // legacy02 无任何绑定（存量状态），携带 X-Tenant 走真实登录管线
            var loginCtx = _fx.CreateContext();
            loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var model = new LoginModel { Username = "legacy02", Password = TenantAuthFixture.Password };
            var result = _fx.UserService.Login(model, loginCtx);
            Assert.True(result.IsSuccess);

            // CompleteLogin -> EnsureTenantUser 已补建租户绑定
            var tu = TenantUser.FindByTenantIdAndUserId(_fx.Tenant1.Id, _fx.LegacyUser2.ID);
            Assert.NotNull(tu);
            Assert.True(tu.Enable);

            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 重新选择租户后进入已绑定的租户（旧令牌无法自救，见 Repro 测试）
            TenantContext.Current = null!;
            loginCtx.ChooseTenant(_fx.LegacyUser2.ID);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);

            // 后续请求携带令牌与 X-Tenant 正常通过认证
            TenantContext.Current = null!;
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(_fx.LegacyUser2.ID, current.ID);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "期望：显式标识优先校验——已绑定t1用户带X-Tenant:t2（别家租户）访问被拒，不再静默回落t1")]
    public void Expect_NonMember_With_OtherXTenant_Should_Be_Rejected()
    {
        foreach (var enforce in new[] { false, true })
        {
            CubeSetting.Current.EnableTenant = true;
            CubeSetting.Current.TenantEnforceMode = enforce ? TenantEnforceModes.Enforce : TenantEnforceModes.Shadow;
            TenantContext.Current = null!;
            try
            {
                // tenant01 绑定 t1，带 X-Tenant:t1 登录拿令牌
                var loginCtx = _fx.CreateContext();
                loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
                var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password };
                var result = _fx.UserService.Login(model, loginCtx);
                Assert.True(result.IsSuccess);
                var token = loginCtx.Items["jwtToken"] as String;
                Assert.False(String.IsNullOrEmpty(token));

                // 带令牌 + X-Tenant:t2（存在且启用，但用户非成员）访问 → 拒绝。
                // 修复前：ChooseTenant 回落 t1 返回 200（静默换租户）；修复后：显式标识优先校验，非成员拒绝。
                TenantContext.Current = null!;
                var ctx = _fx.CreateContext();
                ctx.Request.Headers["Authorization"] = $"Bearer {token}";
                ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.OtherTenantCode;
                Assert.Null(_fx.Provider.TryLogin(ctx));

                // 正向对照：同一令牌带自己租户 X-Tenant:t1 → 放行（防止 TryLogin 全局损坏导致误绿）
                TenantContext.Current = null!;
                var ctxOk = _fx.CreateContext();
                ctxOk.Request.Headers["Authorization"] = $"Bearer {token}";
                ctxOk.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
                var ok = _fx.Provider.TryLogin(ctxOk);
                Assert.NotNull(ok);
                Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
            }
            finally
            {
                CubeSetting.Current.EnableTenant = false;
                CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
                TenantContext.Current = null!;
            }
        }
    }

    [Fact(DisplayName = "期望：普通用户显式请求管理后台（Cookie=0）被拒，稳定拒绝不再回落（M5 修复）")]
    public void Expect_NormalUser_With_AdminBackendCookie_Should_Be_Rejected()
    {
        foreach (var enforce in new[] { false, true })
        {
            CubeSetting.Current.EnableTenant = true;
            CubeSetting.Current.TenantEnforceMode = enforce ? TenantEnforceModes.Enforce : TenantEnforceModes.Shadow;
            TenantContext.Current = null!;
            try
            {
                var cookieKey = $"TenantId-{SysConfig.Current.Name}";

                // tenant01 绑定 t1，带 X-Tenant:t1 登录拿令牌
                var loginCtx = _fx.CreateContext();
                loginCtx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
                var model = new LoginModel { Username = "tenant01", Password = TenantAuthFixture.Password };
                var result = _fx.UserService.Login(model, loginCtx);
                Assert.True(result.IsSuccess);
                var token = loginCtx.Items["jwtToken"] as String;
                Assert.False(String.IsNullOrEmpty(token));

                // 带令牌 + Cookie=0（管理后台）访问 → 拒绝（普通用户进不了管理后台）。
                // 修复前：回落 t1 返回 200 且时序不稳定；修复后：显式标识优先校验稳定拒绝。
                TenantContext.Current = null!;
                var ctx = _fx.CreateContext();
                ctx.Request.Headers["Authorization"] = $"Bearer {token}";
                ctx.Request.Headers["Cookie"] = $"{cookieKey}=0";
                Assert.Null(_fx.Provider.TryLogin(ctx));

                // 正向对照：同一令牌带 X-Tenant:t1 → 放行（防止 TryLogin 全局损坏导致误绿）
                TenantContext.Current = null!;
                var ctxOk = _fx.CreateContext();
                ctxOk.Request.Headers["Authorization"] = $"Bearer {token}";
                ctxOk.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
                var ok = _fx.Provider.TryLogin(ctxOk);
                Assert.NotNull(ok);
                Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
            }
            finally
            {
                CubeSetting.Current.EnableTenant = false;
                CubeSetting.Current.TenantEnforceMode = TenantEnforceModes.Shadow;
                TenantContext.Current = null!;
            }
        }
    }

    [Fact(DisplayName = "期望：管理员带X-Tenant访问时上下文用所带租户（不落管理后台看全量）")]
    public void Expect_Admin_With_XTenant_Stays_In_Tenant()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            // admin01 无标识登录拿令牌（Shadow：管理员回落管理后台 0）
            var loginCtx = _fx.CreateContext();
            var model = new LoginModel { Username = "admin01", Password = TenantAuthFixture.Password };
            var result = _fx.UserService.Login(model, loginCtx);
            Assert.True(result.IsSuccess);
            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 带令牌 + X-Tenant:t1 → 管理员豁免成员校验，但上下文用所带租户 t1（修复前落 0 看全量）
            TenantContext.Current = null!;
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx.Request.Headers["X-Tenant"] = TenantAuthFixture.TenantCode;
            var current = _fx.Provider.TryLogin(ctx);
            Assert.NotNull(current);
            Assert.Equal(_fx.Tenant1.Id, TenantContext.CurrentId);
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }

    [Fact(DisplayName = "期望：显式但无效租户标识（X-Tenant:t9）访问被拒（认证层兜底，生产路径由中间件400拦截）")]
    public void Expect_Invalid_XTenant_Should_Be_Rejected()
    {
        CubeSetting.Current.EnableTenant = true;
        TenantContext.Current = null!;
        try
        {
            // legacy01 无绑定，Shadow 无标识登录拿令牌
            var loginCtx = _fx.CreateContext();
            var model = new LoginModel { Username = "legacy01", Password = TenantAuthFixture.Password };
            var result = _fx.UserService.Login(model, loginCtx);
            Assert.True(result.IsSuccess);
            var token = loginCtx.Items["jwtToken"] as String;
            Assert.False(String.IsNullOrEmpty(token));

            // 带令牌 + X-Tenant:t9（不存在）→ 认证层显式标识兜底拒绝
            TenantContext.Current = null!;
            var ctx = _fx.CreateContext();
            ctx.Request.Headers["Authorization"] = $"Bearer {token}";
            ctx.Request.Headers["X-Tenant"] = "t9";
            Assert.Null(_fx.Provider.TryLogin(ctx));

            // 正向对照：同一令牌无标识 → Shadow 规则A 放行（防止 TryLogin 全局损坏导致误绿）
            TenantContext.Current = null!;
            var ctxOk = _fx.CreateContext();
            ctxOk.Request.Headers["Authorization"] = $"Bearer {token}";
            var ok = _fx.Provider.TryLogin(ctxOk);
            Assert.NotNull(ok);
            Assert.Equal(0, TenantContext.CurrentId); // 无绑定：不设置租户上下文
        }
        finally
        {
            CubeSetting.Current.EnableTenant = false;
            TenantContext.Current = null!;
        }
    }
}
