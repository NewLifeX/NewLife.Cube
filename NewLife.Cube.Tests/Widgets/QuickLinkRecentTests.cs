using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Cube.Widgets;
using NewLife.Cube.Widgets.System;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>最近访问存储测试集合。夹具一次性构建 SQLite 库（Membership 连接），测试共享同一数据源。</summary>
[CollectionDefinition("QuickLinkRecent", DisableParallelization = true)]
public class QuickLinkRecentCollection : ICollectionFixture<QuickLinkRecentFixture>
{
}

/// <summary>
/// 最近访问测试夹具：独立 SQLite 连接（Parameter 实体重映射），自动迁移建表。
/// 使用独立连接名绕开 Membership 连接的 DAL 全局缓存（跨测试集合污染根因）。
/// </summary>
public class QuickLinkRecentFixture : IDisposable
{
    /// <summary>独立连接名。避免与其它测试集合共享的 Membership 连接缓存冲突</summary>
    public const String ConnName = "QuickLinkRecent";

    /// <summary>数据库文件</summary>
    public String DbFile { get; }

    public QuickLinkRecentFixture()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        DbFile = Path.Combine(dir, "QuickLinkRecentTests.db");

        // 清理历史库文件（含 WAL/SHM 残留），保证每次运行干净，避免上一轮数据导致 UNIQUE 冲突
        foreach (var f in Directory.GetFiles(dir, "QuickLinkRecentTests.db*"))
        {
            try { File.Delete(f); } catch { }
        }

        // 注册独立连接；实体重映射在测试类构造函数按测试线程执行（Meta.ConnName 线程级）
        DAL.AddConnStr(ConnName, $"Data Source={DbFile}", null, "SQLite");
    }

    public void Dispose()
    {
        // 释放连接、移除独立连接，避免影响其它测试集合
        try { DAL.Create(ConnName).Reset(); } catch { }
        DAL.ConnStrs?.TryRemove(ConnName, out _);

        // 清理库文件（含 WAL/SHM 残留），避免下次运行脏数据
        try
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(DbFile)!, "QuickLinkRecentTests.db*"))
            {
                File.Delete(f);
            }
        }
        catch
        {
            // 忽略清理失败
        }
    }
}

/// <summary>最近访问存储集成测试（Parameter 表，SQLite）。覆盖记录置顶/计数/截断/空数据处理</summary>
[Collection("QuickLinkRecent")]
public class QuickLinkRecentTests : IDisposable
{
    private readonly String _oldConn;

    /// <summary>每个测试实例在自己的执行线程上重映射 Parameter 到独立连接。
    /// Meta.ConnName 是线程级配置，fixture 线程设置不作用于测试线程，故须在测试构造函数中设置</summary>
    public QuickLinkRecentTests()
    {
        _oldConn = Parameter.Meta.ConnName;
        Parameter.Meta.ConnName = QuickLinkRecentFixture.ConnName;
    }

    public void Dispose()
    {
        // 恢复实体连接映射，避免影响其它测试集合
        Parameter.Meta.ConnName = _oldConn;
    }

    [Fact(DisplayName = "RecordVisit_首次访问_置顶且计数为1")]
    public void RecordVisit_FirstVisit_InsertTop()
    {
        const Int32 userId = 301;

        QuickLinkWidget.RecordVisit(userId, "用户管理", "/Admin/User", "fa-users");
        QuickLinkWidget.RecordVisit(userId, "审计日志", "/Admin/Log", "fa-history");

        var list = QuickLinkWidget.GetRecent(userId);
        Assert.Equal(2, list.Count);
        Assert.Equal("审计日志", list[0].Name);
        Assert.Equal("/Admin/Log", list[0].Url);
        Assert.Equal("fa-history", list[0].Icon);
        Assert.Equal(1, list[0].Count);
        Assert.Equal("用户管理", list[1].Name);
    }

    [Fact(DisplayName = "RecordVisit_重复访问_置顶且计数递增")]
    public void RecordVisit_Repeat_ReordersAndIncrements()
    {
        const Int32 userId = 302;

        QuickLinkWidget.RecordVisit(userId, "用户管理", "/Admin/User");
        QuickLinkWidget.RecordVisit(userId, "审计日志", "/Admin/Log");
        QuickLinkWidget.RecordVisit(userId, "用户管理", "/Admin/User");

        var list = QuickLinkWidget.GetRecent(userId);
        Assert.Equal(2, list.Count);
        Assert.Equal("/Admin/User", list[0].Url);
        Assert.Equal(2, list[0].Count);
        Assert.Equal("/Admin/Log", list[1].Url);
        Assert.Equal(1, list[1].Count);
    }

    [Fact(DisplayName = "RecordVisit_超过上限_截断最旧")]
    public void RecordVisit_OverMax_TrimsOldest()
    {
        const Int32 userId = 303;

        for (var i = 1; i <= 12; i++)
            QuickLinkWidget.RecordVisit(userId, $"页面{i}", $"/Admin/Page{i}");

        var list = QuickLinkWidget.GetRecent(userId);
        Assert.Equal(QuickLinkWidget.MaxRecent, list.Count);
        // 最新在前，最旧的 页面1/页面2 被淘汰
        Assert.Equal("/Admin/Page12", list[0].Url);
        Assert.Equal("/Admin/Page3", list[^1].Url);
    }

    [Fact(DisplayName = "GetRecent_无数据_返回空列表")]
    public void GetRecent_NoData_ReturnsEmpty()
    {
        var list = QuickLinkWidget.GetRecent(999);
        Assert.Empty(list);
    }

    [Fact(DisplayName = "RecordVisit_无效参数_不写入")]
    public void RecordVisit_InvalidArgs_Skip()
    {
        QuickLinkWidget.RecordVisit(0, "无用户", "/Admin/User");
        QuickLinkWidget.RecordVisit(304, null, "/Admin/User");
        QuickLinkWidget.RecordVisit(304, "无地址", null);

        Assert.Empty(QuickLinkWidget.GetRecent(0));
        Assert.Empty(QuickLinkWidget.GetRecent(304));
    }

    [Fact(DisplayName = "GetData_未登录_最近访问为空")]
    public void GetData_NoUser_RecentEmpty()
    {
        var manager = new WidgetManager();
        var info = manager.Scan()["QuickLink"];
        var data = manager.GetData(info);
        Assert.NotNull(data);

        dynamic d = data!;
        var recent = (IEnumerable<dynamic>)d.Recent;
        Assert.Empty(recent);
    }
}
