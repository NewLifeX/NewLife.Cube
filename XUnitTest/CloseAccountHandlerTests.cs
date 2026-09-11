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
    [Fact(DisplayName = "CloseAccount：多个处理器按注册顺序逐个调用")]
    public void CloseAccount_InvokesAllHandlers_InRegistrationOrder()
    {
        var log = new List<String>();
        var svc = BuildService(new RecordingHandler("A", log), new RecordingHandler("B", log));

        var user = CreateUser();
        var result = svc.CloseAccount(user, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal(new List<String> { "A", "B" }, log);
    }

    [Fact(DisplayName = "CloseAccount：处理器拿到脱敏前快照，数据库行已禁用脱敏")]
    public void CloseAccount_HandlerSeesPreAnonymizedSnapshot()
    {
        var handler = new RecordingHandler("A", []);
        var svc = BuildService(handler);

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
        var db = User.FindByID(user.ID);
        Assert.NotNull(db);
        Assert.False(db.Enable);
        Assert.Null(db.Mail);
        Assert.Null(db.Mobile);
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
        Assert.False(User.FindByID(user.ID).Enable);
    }

    [Fact(DisplayName = "CloseAccount：未注册处理器时行为与旧版一致（注销成功并脱敏）")]
    public void CloseAccount_NoHandler_StillCloses()
    {
        var svc = BuildService();

        var user = CreateUser(mail: "no_handler@test.com");
        var result = svc.CloseAccount(user, "127.0.0.1");

        Assert.True(result.IsSuccess);
        var db = User.FindByID(user.ID);
        Assert.False(db.Enable);
        Assert.Null(db.Mail);
    }
    #endregion

    #region 辅助
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
        };
        user.Insert();

        Assert.True(user.ID > 0);
        return user;
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
