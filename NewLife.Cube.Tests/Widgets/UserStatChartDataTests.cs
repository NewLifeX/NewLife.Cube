using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Entity;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>用户统计图表数据测试集合。兜底查库路径访问数据库，集合禁并行（串行执行），避免与其它测试集合互相污染连接映射</summary>
[CollectionDefinition("UserStatChartData", DisableParallelization = true)]
public class UserStatChartDataCollection
{
}

/// <summary>用户统计图表数据单元测试。验证 OnGetChartData 构建 3 个 ECharts option（主折线 + 箱线 + K线）</summary>
[Collection("UserStatChartData")]
public class UserStatChartDataTests : IDisposable
{
    private readonly String _connName = "UserStatChartUnit";
    private readonly String _dbFile;
    private readonly String _oldConnName;

    public UserStatChartDataTests()
    {
        // OnGetChartData 空列表时兜底查库；测试期间把 UserStat 重映射到独立空库，
        // 保证"空数据"语义与运行机器/其它测试集合的库状态无关
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        _dbFile = Path.Combine(dir, "UserStatChartUnit.db");
        foreach (var f in Directory.GetFiles(dir, "UserStatChartUnit.db*"))
        {
            try { File.Delete(f); } catch { }
        }

        DAL.AddConnStr(_connName, $"Data Source={_dbFile}", null, "SQLite");
        _oldConnName = NewLife.Cube.Entity.UserStat.Meta.ConnName;
        NewLife.Cube.Entity.UserStat.Meta.ConnName = _connName;

        // 显式建空表：兜底查询需要表存在才能稳定返回空集；不依赖 XCode 反向工程（其迁移配置在测试进程内可能关闭）
        DAL.Create(_connName).Execute("Create Table If Not Exists UserStat(Id INTEGER Primary Key AutoIncrement, Date DATETIME, Total INTEGER, Logins INTEGER, OAuths INTEGER, MaxOnline INTEGER, Actives INTEGER, ActivesT7 INTEGER, ActivesT30 INTEGER, News INTEGER, NewsT7 INTEGER, NewsT30 INTEGER, OnlineTime INTEGER, CreateTime DATETIME, UpdateTime DATETIME)");
    }

    public void Dispose()
    {
        NewLife.Cube.Entity.UserStat.Meta.ConnName = _oldConnName;
        try { DAL.Create(_connName).Reset(); } catch { }
        DAL.ConnStrs?.TryRemove(_connName, out _);
        try
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(_dbFile)!, "UserStatChartUnit.db*"))
            {
                File.Delete(f);
            }
        }
        catch
        {
            // 忽略清理失败
        }
    }

    #region 测试控制器
    private class TestUserStatController : UserStatController
    {
        /// <summary>暴露 OnGetChartData 供测试（protected override 无法直接调用）</summary>
        public Object[] TestOnGetChartData(IEnumerable<UserStat> data) => OnGetChartData(data);
    }
    #endregion

    /// <summary>构造 n 天测试数据</summary>
    private static UserStat[] BuildData(Int32 days)
    {
        var rs = new UserStat[days];
        for (var i = 0; i < days; i++)
        {
            rs[i] = new UserStat
            {
                Date = DateTime.Today.AddDays(-days + 1 + i),
                Total = 100 + i,
                Logins = 10 + i,
                OAuths = 2 + i,
                MaxOnline = 20 + i,
                Actives = 30 + i,
                ActivesT7 = 40 + i,
                ActivesT30 = 50 + i,
                News = 3 + i,
                NewsT7 = 4 + i,
                NewsT30 = 5 + i,
                OnlineTime = 3600 + i * 60,
            };
        }
        return rs;
    }

    /// <summary>把 option 对象序列化为 JSON 便于断言（匿名对象无法强转字典）</summary>
    private static JsonElement ToJson(Object obj)
        => JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(obj)).RootElement;

    [Fact(DisplayName = "OnGetChartData_有数据_返回3个图表")]
    public void OnGetChartData_HasData_Returns3Charts()
    {
        var ctrl = new TestUserStatController();
        var list = BuildData(5);

        var rs = ctrl.TestOnGetChartData(list);

        Assert.Equal(3, rs.Length);

        // 主折线图
        var chart = ToJson(rs[0]!);
        Assert.Equal("category", chart.GetProperty("xAxis").GetProperty("type").GetString());
        // 11 系列：9 个统计 + Total + OnlineTime
        Assert.Equal(11, chart.GetProperty("series").GetArrayLength());
        // X 轴 5 天
        Assert.Equal(5, chart.GetProperty("xAxis").GetProperty("data").GetArrayLength());

        // 箱线图
        var box = ToJson(rs[1]!);
        Assert.Equal(1, box.GetProperty("series").GetArrayLength());
        Assert.Equal("boxplot", box.GetProperty("series")[0].GetProperty("type").GetString());
        // 每日 5 个统计维度（Min/Q1/Median/Q3/Max）
        var boxData = box.GetProperty("series")[0].GetProperty("data");
        Assert.Equal(5, boxData.GetArrayLength());
        Assert.Equal(5, boxData[0].GetArrayLength());

        // K线图
        var candle = ToJson(rs[2]!);
        Assert.Equal(1, candle.GetProperty("series").GetArrayLength());
        Assert.Equal("candlestick", candle.GetProperty("series")[0].GetProperty("type").GetString());
        // 每日 4 个值（Open/Close/Lowest/Highest）
        var candleData = candle.GetProperty("series")[0].GetProperty("data");
        Assert.Equal(5, candleData.GetArrayLength());
        Assert.Equal(4, candleData[0].GetArrayLength());
    }

    [Fact(DisplayName = "OnGetChartData_有数据_主折线含三Y轴与系列名")]
    public void OnGetChartData_HasData_LineHas3YAxis()
    {
        var ctrl = new TestUserStatController();
        var list = BuildData(3);

        var rs = ctrl.TestOnGetChartData(list);

        var chart = ToJson(rs[0]!);
        // 三 Y 轴：用户数 / 总数 / 时长
        Assert.Equal(3, chart.GetProperty("yAxis").GetArrayLength());

        // 图例包含关键系列名
        var legend = chart.GetProperty("legend").GetProperty("data").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("总数", legend);
        Assert.Contains("在线时间", legend);
    }

    [Fact(DisplayName = "OnGetChartData_空数据_返回空数组")]
    public void OnGetChartData_Empty_ReturnsEmpty()
    {
        var ctrl = new TestUserStatController();

        // 直接传空数组（OnGetChartData 内部空列表时兜底查库，但传空则返回 []）
        var rs = ctrl.TestOnGetChartData([]);

        Assert.Empty(rs);
    }
}
