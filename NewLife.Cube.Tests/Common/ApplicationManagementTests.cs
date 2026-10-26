using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Membership;
using NewLife.Data;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS 系统管理测试（第二批）。覆盖应用系统（名称必填、按名称/密钥查找、回调地址与来源地址白名单校验）、
/// 应用日志（创建落库、按应用与时间检索、按日期批量清理）与用户在线（会话查找、错误状态、条件检索）。
/// </summary>
[Collection("TenantAuth")]
public class ApplicationManagementTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public ApplicationManagementTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private static String Unique(String prefix) => prefix + "_" + Guid.NewGuid().ToString("N")[..8];

    private static App CreateApp(String name = null, String secret = null)
    {
        var app = new App
        {
            Name = name ?? Unique("app"),
            DisplayName = "测试应用",
            Secret = secret ?? Guid.NewGuid().ToString("N"),
            Enable = true,
            Category = "Test",
        };
        app.Insert();

        return app;
    }

    private static T WaitFor<T>(Func<T> read, Func<T, Boolean> ok, Int32 timeoutMs = 6000)
    {
        var end = Environment.TickCount64 + timeoutMs;
        var value = read();
        while (!ok(value) && Environment.TickCount64 < end)
        {
            Thread.Sleep(100);
            value = read();
        }

        return value;
    }
    #endregion

    #region SYS-8 应用系统
    [Fact(DisplayName = "应用系统_名称必填")]
    public void App_Insert_RequiresName()
    {
        var app = new App { Name = null, Secret = Guid.NewGuid().ToString("N") };

        var ex = Assert.Throws<ArgumentNullException>(() => app.Insert());
        Assert.Equal("Name", ex.ParamName);
    }

    [Fact(DisplayName = "应用系统_按名称与密钥查找（不区分大小写）")]
    public void App_FindByNameAndSecret()
    {
        var name = Unique("app");
        var secret = Guid.NewGuid().ToString("N");
        var app = CreateApp(name, secret);
        App.Meta.Cache.Clear("应用系统测试写入", true);

        Assert.Equal(app.Id, App.FindByName(name.ToUpperInvariant())?.Id);
        Assert.Equal(app.Id, App.FindBySecret(secret.ToUpperInvariant())?.Id);
        Assert.Equal(app.Id, App.FindById(app.Id)?.Id);
        Assert.Null(App.FindByName(Unique("missing")));
        Assert.Null(App.FindBySecret(Unique("missing")));
        Assert.Null(App.FindById(0));
    }

    [Fact(DisplayName = "应用系统_回调地址校验支持空配置、localhost 与域名前缀")]
    public void App_ValidCallback_MatchesRules()
    {
        var app = new App { Urls = "example.com/cb,api.foo.com" };

        // 未配置回调地址时全部放行
        Assert.True(new App { Urls = null }.ValidCallback("https://any.example.org/x"));
        // 已配置回调地址时，空地址一律拒绝
        Assert.False(app.ValidCallback(null));
        // 前缀匹配（自动补齐协议）
        Assert.True(app.ValidCallback("https://example.com/cb?code=1"));
        Assert.True(app.ValidCallback("http://api.foo.com/token"));
        Assert.False(app.ValidCallback("https://evil.com/cb"));
        // 本机地址特殊放行
        Assert.True(app.ValidCallback("http://localhost:5000/callback"));
        // 相对地址拒绝
        Assert.False(app.ValidCallback("example.com/cb"));
    }

    [Fact(DisplayName = "应用系统_来源地址校验黑名单优先且白名单收紧")]
    public void App_ValidSource_WhiteBlackLists()
    {
        // 未配置名单时全部放行
        Assert.True(new App().ValidSource("10.0.0.1"));
        Assert.True(new App().ValidSource(null));

        // 名单使用通配符匹配（* 匹配任意字符）
        var app = new App { Black = "10.0.0.*", White = "192.168.*" };
        // 黑名单优先
        Assert.False(app.ValidSource("10.0.0.5"));
        // 白名单内放行
        Assert.True(app.ValidSource("192.168.1.20"));
        // 配置了白名单后，名单外拒绝
        Assert.False(app.ValidSource("8.8.8.8"));

        var onlyBlack = new App { Black = "10.0.0.*" };
        Assert.True(onlyBlack.ValidSource("8.8.8.8"));
        Assert.False(onlyBlack.ValidSource("10.0.0.5"));
    }
    #endregion

    #region SYS-9 应用日志
    [Fact(DisplayName = "应用日志_按应用与关键字检索且异步写入可落库")]
    public void AppLog_CreateAndSearch()
    {
        var app = CreateApp();
        var other = CreateApp();
        var now = DateTime.Now;

        var log = AppLog.Create(app.Id, "Authorize", true, "授权成功");
        Assert.Equal(app.Id, log.AppId);
        Assert.Equal("Authorize", log.Action);
        Assert.True(log.Success);
        Assert.Equal("授权成功", log.Remark);
        Assert.True(log.Id > 0);

        new AppLog { AppId = app.Id, Action = "Token", Success = false, Remark = "令牌失败", CreateTime = now }.Insert();
        new AppLog { AppId = other.Id, Action = "Authorize", Success = true, Remark = "别的应用", CreateTime = now }.Insert();

        // 创建走异步保存，先确认落库再统计数量
        Assert.NotNull(WaitFor(() => AppLog.FindById(log.Id), e => e != null));

        var mine = AppLog.FindAllByAppId(app.Id);
        Assert.Equal(2, mine.Count);
        Assert.All(mine, e => Assert.Equal(app.Id, e.AppId));

        var page = new PageParameter { PageIndex = 1, PageSize = 10 };
        var byApp = AppLog.Search(app.Id, now.AddMinutes(-5), now.AddMinutes(5), null, page);
        Assert.Equal(2, byApp.Count);

        Assert.True(AppLog.FindById(log.Id).Success);
    }

    [Fact(DisplayName = "应用日志_按日期清理只删除旧匿名授权日志")]
    public void AppLog_DeleteBefore_OnlyOldAnonymousAuthorize()
    {
        var app = CreateApp();
        var now = DateTime.Now;
        var cutoff = now.AddDays(-10);

        var oldAuthorize = new AppLog { Id = 1, AppId = app.Id, Action = "Authorize", Success = true, CreateTime = now.AddDays(-30) };
        oldAuthorize.Insert();
        // 匿名授权日志没有创建者：插入后清空（插入时被用户拦截器补上了当前用户）
        oldAuthorize.CreateUser = null;
        oldAuthorize.Update();
        var oldToken = new AppLog { Id = 2, AppId = app.Id, Action = "Token", Success = true, CreateTime = now.AddDays(-30) };
        oldToken.Insert();
        var recent = new AppLog { Id = AppLog.Meta.Factory.Snow.GetId(DateTime.Now), AppId = app.Id, Action = "Authorize", Success = true, CreateTime = now.AddDays(-1) };
        recent.Insert();
        var oldBound = new AppLog { Id = 3, AppId = app.Id, Action = "Authorize", Success = true, CreateTime = now.AddDays(-30), CreateUser = "tester" };
        oldBound.Insert();

        var count = AppLog.DeleteBefore(cutoff);

        Assert.True(count >= 1);
        Assert.Null(AppLog.FindById(oldAuthorize.Id));
        Assert.NotNull(AppLog.FindById(oldToken.Id));
        Assert.NotNull(AppLog.FindById(recent.Id));
        Assert.NotNull(AppLog.FindById(oldBound.Id));
    }
    #endregion

    #region SYS-10 用户在线
    [Fact(DisplayName = "用户在线_按会话查找与错误状态标记")]
    public void UserOnline_FindBySessionAndSetError()
    {
        var session = "sess" + Guid.NewGuid().ToString("N")[..8];
        var online = new UserOnline
        {
            UserID = _fx.LegacyUser.ID,
            Name = _fx.LegacyUser.Name,
            SessionID = session,
            Page = "/admin/index",
            Status = "在线",
            CreateTime = DateTime.Now,
        };
        online.Insert();

        var found = UserOnline.FindBySessionID(session, false);
        Assert.NotNull(found);
        Assert.Equal(online.ID, found.ID);

        found.SetError("会话超时");
        Assert.Equal("会话超时", found.Status);
        Assert.True((DateTime.Now - found.LastError).TotalMinutes < 1);

        // 高级查询走实体缓存之外的数据库查询，插入后清缓存保证可见
        UserOnline.Meta.Cache.Clear("用户在线测试写入", true);

        var start = DateTime.Now.AddHours(-1);
        var end = DateTime.Now.AddHours(1);
        var byUser = UserOnline.Search(_fx.LegacyUser.ID, session, start, end, null, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.Contains(byUser, e => e.ID == online.ID);

        // 每页参数对象独立创建，避免分页状态在多次查询间复用
        var byKey = UserOnline.Search(-1, null, start, end, session, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.Contains(byKey, e => e.ID == online.ID);

        var outside = UserOnline.Search(-1, null, DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-9), session, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.DoesNotContain(outside, e => e.ID == online.ID);

        Assert.Null(UserOnline.FindBySessionID(null, false));
        Assert.Null(UserOnline.FindBySessionID(Unique("missing"), false));
        Assert.Contains(UserOnline.FindAllByUserID(_fx.LegacyUser.ID), e => e.ID == online.ID);
    }
    #endregion
}


