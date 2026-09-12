using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>账号注销处理器（IAccountCloseHandler）测试（SQLite 轻量集成）</summary>
[Collection("SqliteDb")]
public class CloseAccountHandlerTests : IDisposable
{
    private readonly TestCacheProvider _cacheProvider = new();

    public CloseAccountHandlerTests()
    {
        // 共享 SQLite 库的表结构（含 User/UserToken/UserConnect/UserOnline）已在 SqliteDb.Ensure 中按表建好
        SqliteDb.Ensure();
    }

    public void Dispose()
    {
        // 清理实体缓存，避免跨用例干扰
        User.Meta.Session.ClearCache(nameof(CloseAccountHandlerTests), true);
        UserToken.Meta.Session.ClearCache(nameof(CloseAccountHandlerTests), true);
    }

    #region 处理器调用
    [Fact(DisplayName = "CloseAccount：多个处理器按注册顺序逐个调用，并兜底禁用账号")]
    public void CloseAccount_InvokesAllHandlers_InRegistrationOrder()
    {
        var log = new List<String>();
        var svc = BuildService(new RecordingHandler("A", log), new RecordingHandler("B", log));

        var user = CreateUser();
        var result = svc.CloseAccount(user, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal(new List<String> { "A", "B" }, log);

        // 兜底禁用：即使没有默认处理器，账号也不可登录
        Assert.False(FindUser(user.ID).Enable);
    }

    [Fact(DisplayName = "CloseAccount：默认处理器清理各表个人数据并脱敏，账号兜底禁用")]
    public void CloseAccount_DefaultHandler_CleansPersonalData()
    {
        var svc = BuildService(new DefaultAccountCloseHandler());

        var mail = $"e2e_{Guid.NewGuid():N}@test.com";
        var mobile = "13800002222";
        var user = CreateUser(mail: mail, mobile: mobile, displayName: "清理测试");
        SeedPersonalData(user, mail, mobile);

        var result = svc.CloseAccount(user, "10.0.0.1");

        Assert.True(result.IsSuccess);

        // 令牌已吐销（非删除，Enable=false）
        Assert.Equal(0L, UserToken.FindCount(UserToken._.UserID == user.ID & UserToken._.Enable == true));
        // 三方绑定、在线、OAuth日志、通知、验证码（含按联系方式匹配）、参数、租户关系、委托代理 均已清理
        Assert.Equal(0L, UserConnect.FindCount(UserConnect._.UserID == user.ID));
        Assert.Equal(0L, UserOnline.FindCount(UserOnline._.UserID == user.ID));
        Assert.Equal(0L, OAuthLog.FindCount(OAuthLog._.UserId == user.ID));
        Assert.Equal(0L, NotificationRecord.FindCount(NotificationRecord._.UserId == user.ID));
        Assert.Equal(0L, VerifyCodeRecord.FindCount(VerifyCodeRecord._.UserId == user.ID));
        Assert.Equal(0L, VerifyCodeRecord.FindCount(VerifyCodeRecord._.Target == mail));
        Assert.Equal(0L, VerifyCodeRecord.FindCount(VerifyCodeRecord._.Target == mobile));
        // 系统参数不能误删：保留 UserID=0
        Assert.Equal(0L, Parameter.FindCount(Parameter._.UserID == user.ID));
        Assert.Equal(0L, TenantUser.FindCount(TenantUser._.UserId == user.ID));
        Assert.Equal(0L, PrincipalAgent.FindCount(PrincipalAgent._.PrincipalId == user.ID));
        Assert.Equal(0L, PrincipalAgent.FindCount(PrincipalAgent._.AgentId == user.ID));

        // 用户行：禁用 + 脱敏 + 保留 ID/Name
        var db = FindUser(user.ID);
        Assert.NotNull(db);
        Assert.False(db.Enable);
        Assert.Null(db.Password);
        Assert.Null(db.Mail);
        Assert.False(db.MailVerified);
        Assert.Null(db.Mobile);
        Assert.False(db.MobileVerified);
        Assert.Null(db.DisplayName);
        Assert.Null(db.Avatar);
        Assert.Null(db.RegisterIP);
        Assert.Equal(0, db.AreaId);
        Assert.Equal(0, db.Ex1);
        Assert.Null(db.Ex4);
        Assert.Equal(user.Name, db.Name);
    }

    [Fact(DisplayName = "CloseAccount：单个处理器异常被隔离，后续处理器仍执行且注销成功")]
    public void CloseAccount_HandlerException_DoesNotAffectOthers()
    {
        var log = new List<String>();
        var svc = BuildService(new ThrowingHandler(), new RecordingHandler("good", log));

        var user = CreateUser();
        var result = svc.CloseAccount(user, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal(new List<String> { "good" }, log);
        Assert.False(FindUser(user.ID).Enable);
    }

    [Fact(DisplayName = "CloseAccount：处理器拿到脱敏前快照，数据库行已禁用脱敏")]
    public void CloseAccount_HandlerSeesPreAnonymizedSnapshot()
    {
        // 快照处理器注册在默认处理器之前，仍应看到脱敏前的数据
        var handler = new RecordingHandler("A", []);
        var svc = BuildService(handler, new DefaultAccountCloseHandler());

        var mail = $"e2e_{Guid.NewGuid():N}@test.com";
        var user = CreateUser(mail: mail, mobile: "13800001111", displayName: "快照测试");
        var result = svc.CloseAccount(user, "10.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.NotNull(handler.Snapshot);
        Assert.Equal(mail, handler.Snapshot.Mail);
        Assert.Equal("13800001111", handler.Snapshot.Mobile);
        Assert.Equal("快照测试", handler.Snapshot.DisplayName);
        Assert.Equal("10.0.0.1", handler.Ip);

        // 数据库中的行已禁用并清空敏感字段，保留 ID/Name
        var db = FindUser(user.ID);
        Assert.NotNull(db);
        Assert.False(db.Enable);
        Assert.Null(db.Mail);
        Assert.Null(db.Mobile);
        Assert.Equal(user.Name, db.Name);
    }

    [Fact(DisplayName = "CloseAccount：未注册任何处理器时仅兜底禁用，不脱敏字段")]
    public void CloseAccount_NoHandler_StillDisables()
    {
        var svc = BuildService();

        var user = CreateUser(mail: "no_handler@test.com");
        var result = svc.CloseAccount(user, "127.0.0.1");

        Assert.True(result.IsSuccess);
        var db = FindUser(user.ID);
        Assert.False(db.Enable);
        Assert.Equal("no_handler@test.com", db.Mail);
    }
    #endregion

    #region 辅助
    /// <summary>从数据库读取用户行。走 SQL 查询而不走单对象缓存，避免跨用例缓存污染</summary>
    private static User FindUser(Int32 id) => User.Find(User._.ID == id);

    private UserService BuildService(params IAccountCloseHandler[] handlers)
    {
        var services = new ServiceCollection();
        foreach (var handler in handlers)
        {
            services.AddSingleton<IAccountCloseHandler>(handler);
        }
        var provider = services.BuildServiceProvider();

        return new UserService(null!, _cacheProvider, null!, null!, null!, null!, provider);
    }

    private static User CreateUser(String name = null, String mail = null, String mobile = null, String displayName = null)
    {
        var user = new User
        {
            Name = name ?? $"E2E_{Guid.NewGuid():N}"[..16],
            Password = "P@ssw0rd!123",
            Enable = true,
            Mail = mail,
            Mobile = mobile,
            DisplayName = displayName,
            RegisterIP = "10.0.0.2",
            AreaId = 5,
            Ex1 = 7,
            Ex4 = "备注",
        };
        user.Insert();

        Assert.True(user.ID > 0);
        return user;
    }

    /// <summary>灌入与账号关联的各表个人数据，供默认处理器清理验证</summary>
    private static void SeedPersonalData(User user, String mail, String mobile)
    {
        new UserToken { UserID = user.ID, Token = "T" + Guid.NewGuid().ToString("N"), Enable = true, Expire = DateTime.Now.AddDays(1) }.Insert();
        new UserConnect { UserID = user.ID, Provider = "test", OpenID = Guid.NewGuid().ToString("N"), Enable = true }.Insert();
        new UserOnline { UserID = user.ID, Name = user.Name, SessionID = Guid.NewGuid().ToString("N"), LastError = new DateTime(1970, 1, 2), CreateTime = DateTime.Now, UpdateTime = DateTime.Now }.Insert();
        new OAuthLog { UserId = user.ID, Provider = "test", Action = "Login", Success = true, CreateTime = DateTime.Now }.Insert();
        new NotificationRecord { UserId = user.ID, Action = "Test", Channel = "InApp", Title = "测试", Content = "测试内容", CreateTime = DateTime.Now }.Insert();
        new VerifyCodeRecord { UserId = user.ID, Action = "Login", Channel = "Mail", Target = mail, Code = "111111", CreateTime = DateTime.Now }.Insert();
        new VerifyCodeRecord { UserId = 0, Action = "Login", Channel = "Mail", Target = mail, Code = "222222", CreateTime = DateTime.Now }.Insert();
        new VerifyCodeRecord { UserId = 0, Action = "Login", Channel = "Sms", Target = mobile, Code = "333333", CreateTime = DateTime.Now }.Insert();
        new Parameter { UserID = user.ID, Name = "Nick", Value = "test", Enable = true, CreateTime = DateTime.Now }.Insert();
        new TenantUser { TenantId = 1, UserId = user.ID, Enable = true, CreateTime = DateTime.Now }.Insert();
        new PrincipalAgent { PrincipalId = user.ID, AgentId = 9999, Enable = true, CreateTime = DateTime.Now }.Insert();
        new PrincipalAgent { PrincipalId = 8888, AgentId = user.ID, Enable = true, CreateTime = DateTime.Now }.Insert();
    }

    /// <summary>记录调用信息的处理器</summary>
    private class RecordingHandler(String tag, List<String> log) : IAccountCloseHandler
    {
        public IUser Snapshot { get; private set; }

        public String Ip { get; private set; }

        public Task HandleAsync(IUser user, String ip, CancellationToken cancellationToken = default)
        {
            log.Add(tag);
            Snapshot = user;
            Ip = ip;
            return Task.CompletedTask;
        }
    }

    /// <summary>模拟下游清理失败的处理器</summary>
    private class ThrowingHandler : IAccountCloseHandler
    {
        public Task HandleAsync(IUser user, String ip, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("模拟下游清理失败");
    }

    /// <summary>简单内存缓存提供者，用于单元测试隔离</summary>
    private class TestCacheProvider : ICacheProvider
    {
        public ICache Cache { get; set; }
        public ICache InnerCache { get; set; }

        public TestCacheProvider()
        {
            var cache = new MemoryCache();
            Cache = cache;
            InnerCache = cache;
        }

        public IProducerConsumer<T> GetQueue<T>(String name, String? topic = null)
            => throw new NotImplementedException();

        public IProducerConsumer<T> GetInnerQueue<T>(String name)
            => throw new NotImplementedException();

        public IDisposable AcquireLock(String name, Int32 msTimeout)
            => throw new NotImplementedException();
    }
    #endregion
}
