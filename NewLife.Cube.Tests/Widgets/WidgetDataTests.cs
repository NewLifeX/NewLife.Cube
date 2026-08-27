using System;
using System.IO;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Widgets.System;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>Widget 数据测试集合。夹具一次性构建 SQLite 库并播种固定数据，测试共享同一数据源。</summary>
[CollectionDefinition("WidgetData", DisableParallelization = true)]
public class WidgetDataCollection : ICollectionFixture<WidgetDataFixture>
{
}

/// <summary>
/// Widget 数据测试夹具：创建 SQLite 数据库（Membership 连接，Log 映射到 Membership），
/// 建表并播种固定日志/在线数据。共享夹具避免每个测试重建数据库导致实体工厂缓存污染。
/// </summary>
public class WidgetDataFixture : IDisposable
{
    /// <summary>数据库文件</summary>
    public String DbFile { get; }

    private readonly String? _oldMembership;
    private readonly Boolean _hadLog;

    public WidgetDataFixture()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DbFile = Path.Combine(dir, "WidgetDataTests.db");
        if (File.Exists(DbFile)) File.Delete(DbFile);

        // 记录并覆写全局连接串：Membership 指向 SQLite，Log 映射到 Membership（与生产 AddCube 一致）
        _oldMembership = DAL.ConnStrs != null && DAL.ConnStrs.TryGetValue("Membership", out var v) ? v : null;
        _hadLog = DAL.ConnStrs != null && DAL.ConnStrs.ContainsKey("Log");

        DAL.AddConnStr("Membership", $"Data Source={DbFile}", null, "SQLite");
        if (!_hadLog) DAL.ConnStrs!.TryAdd("Log", "MapTo=Membership");

        DAL.CreateTable();

        Seed();
    }

    public void Dispose()
    {
        // 恢复被覆写的全局连接串，并重置 DAL 缓存，避免污染其它测试集合
        if (DAL.ConnStrs != null)
        {
            if (_oldMembership != null) DAL.ConnStrs["Membership"] = _oldMembership;
            else DAL.ConnStrs.TryRemove("Membership", out _);

            if (!_hadLog) DAL.ConnStrs.TryRemove("Log", out _);
        }
        DAL.Create("Membership").Reset();
        DAL.Create("Log").Reset();

        try
        {
            if (File.Exists(DbFile)) File.Delete(DbFile);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    /// <summary>播种固定数据：24h内3条日志（含1条异常）、1条24h前日志、2条历史登录、2条在线记录</summary>
    private void Seed()
    {
        var now = DateTime.Now;

        InsertLog(now.AddHours(-1), "操作", true);
        InsertLog(now.AddMinutes(-10), "操作", false);            // 异常
        InsertLog(now.AddHours(-25), "超期", true);               // 超出24h，不计入统计
        InsertLog(now.AddHours(-20), "登录", true, "admin");      // 24h内登录
        InsertLog(now.AddDays(-1), "历史", true);                 // 昨日日志
        InsertLog(now.AddDays(-2), "登录", true, "old");          // 前日登录

        new UserOnline { Name = "admin", SessionID = "s1", CreateTime = now.AddMinutes(-5) }.Insert();
        new UserOnline { Name = "old", SessionID = "s2", CreateTime = now.AddHours(-2) }.Insert(); // 超出30分钟
    }

    private static void InsertLog(DateTime time, String action, Boolean success, String userName = "")
    {
        var log = new XLog
        {
            ID = XLog.Meta.Factory.Snow.GetId(time),
            Category = "测试",
            Action = action,
            Success = success,
            UserName = userName,
            CreateTime = time,
        };
        log.Insert();
    }
}

/// <summary>覆盖内置系统组件的数据查询。利用 XCode SQLite 抽象做轻量集成测试，验证统计口径</summary>
[Collection("WidgetData")]
public class WidgetDataTests
{
    [Fact(DisplayName = "LogWidget_统计24h日志与异常数")]
    public void LogWidget_ReturnsStats()
    {
        var widget = new LogWidget();
        dynamic d = widget.GetData();

        // 24h内：操作(成功) + 操作(异常) + 登录 = 3；异常 1
        Assert.Equal(3, (Int32)d.Total24h);
        Assert.Equal(1, (Int32)d.Error24h);
    }

    [Fact(DisplayName = "LogWidget_趋势含最近7天且按天统计")]
    public void LogWidget_Trend_Has7Days()
    {
        var widget = new LogWidget();
        dynamic d = widget.GetData();

        var trend = (Object[])d.Trend;
        Assert.Equal(7, trend.Length);

        // 全部6条日志均在最近7天内，各天计数合计为6（不依赖运行时刻的跨天边界）
        var total = 0;
        Assert.All(trend, e =>
        {
            var item = (dynamic)e;
            Assert.True((Int32)item.count >= 0);
            Assert.False(String.IsNullOrEmpty((String)item.day));
            total += (Int32)item.count;
        });
        Assert.Equal(6, total);
    }

    [Fact(DisplayName = "LogWidget_最近异常按时间倒序")]
    public void LogWidget_RecentErrors_Desc()
    {
        var widget = new LogWidget();
        dynamic d = widget.GetData();

        var errors = (Object[])d.RecentErrors;
        Assert.Single(errors);

        dynamic first = errors[0];
        Assert.Equal("操作", (String)first.Action);
    }

    [Fact(DisplayName = "LoginLogWidget_统计24h登录与在线用户")]
    public void LoginLogWidget_ReturnsStats()
    {
        var widget = new LoginLogWidget();
        dynamic d = widget.GetData();

        // 24h内登录 1 条（admin）；30分钟内在线 1 人
        Assert.Equal(1, (Int32)d.LoginCount24h);
        Assert.Equal(1, (Int32)d.OnlineCount);

        var logins = (Object[])d.Logins;
        Assert.Single(logins);
        dynamic login = logins[0];
        Assert.Equal("admin", (String)login.UserName);

        var onlines = (Object[])d.Onlines;
        Assert.Single(onlines);
    }
}
