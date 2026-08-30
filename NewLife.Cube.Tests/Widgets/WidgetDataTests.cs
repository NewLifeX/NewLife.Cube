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
/// Widget 数据测试夹具：独立 SQLite 连接（Log/UserOnline 实体在测试类构造函数按线程重映射），自动迁移建表并播种固定数据。
/// 使用独立连接名绕开共享 Membership/Log 连接的 DAL 全局缓存（跨测试集合污染根因）。
/// </summary>
public class WidgetDataFixture : IDisposable
{
    /// <summary>独立连接名。避免与其它测试集合共享的 Membership/Log 连接缓存冲突</summary>
    public const String ConnName = "WidgetData";

    /// <summary>数据库文件</summary>
    public String DbFile { get; }

    public WidgetDataFixture()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DbFile = Path.Combine(dir, "WidgetDataTests.db");

        // 清理历史库文件（含 WAL/SHM 残留），保证每次运行干净，避免上一轮数据影响断言
        foreach (var f in Directory.GetFiles(dir, "WidgetDataTests.db*"))
        {
            try { File.Delete(f); } catch { }
        }

        // 注册独立连接；实体重映射在测试类构造函数按测试线程执行（Meta.ConnName 线程级）
        DAL.AddConnStr(ConnName, $"Data Source={DbFile}", null, "SQLite");

        // 本线程（fixture 线程）重映射后播种，确保种子数据落入独立库
        XLog.Meta.ConnName = ConnName;
        UserOnline.Meta.ConnName = ConnName;

        Seed();
    }

    public void Dispose()
    {
        // 释放连接、移除独立连接，避免影响其它测试集合
        try { DAL.Create(ConnName).Reset(); } catch { }
        DAL.ConnStrs?.TryRemove(ConnName, out _);

        // 清理库文件（含 WAL/SHM 残留），避免下次运行脏数据
        try
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(DbFile)!, "WidgetDataTests.db*"))
            {
                File.Delete(f);
            }
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
public class WidgetDataTests : IDisposable
{
    private readonly String _oldLog;
    private readonly String _oldOnline;

    /// <summary>每个测试实例在自己的执行线程上重映射 Log/UserOnline 到独立连接。
    /// Meta.ConnName 是线程级配置，fixture 线程设置不作用于测试线程，故须在测试构造函数中设置</summary>
    public WidgetDataTests()
    {
        _oldLog = XLog.Meta.ConnName;
        _oldOnline = UserOnline.Meta.ConnName;
        XLog.Meta.ConnName = WidgetDataFixture.ConnName;
        UserOnline.Meta.ConnName = WidgetDataFixture.ConnName;
    }

    public void Dispose()
    {
        // 恢复实体连接映射，避免影响其它测试集合
        XLog.Meta.ConnName = _oldLog;
        UserOnline.Meta.ConnName = _oldOnline;
    }

    [Fact(DisplayName = "LoginLogWidget_返回最近登录与在线明细")]
    public void LoginLogWidget_ReturnsDetails()
    {
        var widget = new LoginLogWidget();
        dynamic d = widget.GetData();

        // 24h内登录 1 条（admin）
        var logins = (Object[])d.Logins;
        Assert.Single(logins);
        dynamic login = logins[0];
        Assert.Equal("admin", (String)login.UserName);

        // 当前在线：全部在线会话（非雪花表按 Id 降序），播种 2 条
        var onlines = (Object[])d.Onlines;
        Assert.Equal(2, onlines.Length);
    }

    [Fact(DisplayName = "Log24hWidget_统计24小时内日志数")]
    public void Log24hWidget_Counts24hLogs()
    {
        var widget = new Log24hWidget();
        dynamic d = widget.GetData();

        // 播种：24h 内 3 条（操作/异常/登录），25h 前与昨日/前日不计入
        Assert.Equal("3", (String)d.Value);
        Assert.Equal("最近24小时", (String)d.Trend);
    }

    [Fact(DisplayName = "Error24hWidget_统计24小时内异常数")]
    public void Error24hWidget_Counts24hErrors()
    {
        var widget = new Error24hWidget();
        dynamic d = widget.GetData();

        // 播种：24h 内异常 1 条（Success=false）
        Assert.Equal("1", (String)d.Value);
        Assert.Equal("最近24小时异常", (String)d.Trend);
    }

    [Fact(DisplayName = "OnlineCountWidget_统计当前在线数")]
    public void OnlineCountWidget_CountsOnline()
    {
        var widget = new OnlineCountWidget();
        dynamic d = widget.GetData();

        // 播种：UserOnline 2 条（全部在线会话）
        Assert.Equal("2", (String)d.Value);
        Assert.Equal("当前在线", (String)d.Trend);
    }
}
