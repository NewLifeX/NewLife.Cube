using System;
using System.ComponentModel;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Entity;
using XCode;
using Xunit;

namespace XUnitTest;

/// <summary>
/// 在线高峰告警服务测试（SQLite 轻量集成）。固定近7天窗口 + 比例1.2：
/// 当前在线超过近7天最高纪录×1.2 时写一条系统级广播；每天最多一条；无历史基线/未开统计不告警
/// </summary>
[Collection("SqliteDb")]
public class OnlineAlertServiceTests : IDisposable
{
    public OnlineAlertServiceTests() => SqliteDb.Ensure();

    public void Dispose()
    {
        // 清理实体缓存，避免跨用例干扰
        UserStat.Meta.Session.ClearCache(nameof(OnlineAlertServiceTests), true);
        NotificationRecord.Meta.Session.ClearCache(nameof(OnlineAlertServiceTests), true);
    }

    [Fact]
    [DisplayName("Check_超比例告警_写一条系统级广播_且每天最多一条")]
    public void Check_AboveRatio_AlertOncePerDay()
    {
        var today = new DateTime(2020, 1, 20);
        SeedMax(today, 500);      // 近7天最高 500，阈值 500*1.2=600
        ClearAlerts();

        var t0 = today.AddHours(10);

        // 700 > 600 → 触发，写一条系统级广播
        OnlineAlertService.Check(700, t0, BuildSet());
        Assert.Equal(1, CountAlerts());

        // 验证广播字段
        var list = NotificationRecord.FindAll(NotificationRecord._.Action == "OnlineAlert", NotificationRecord._.Id.Desc(), null, 0, 1);
        var entity = list[0];
        Assert.Equal("InApp", entity.Channel);
        Assert.Equal(0, entity.UserId);
        Assert.Contains("500", entity.Content);
        Assert.Contains("600", entity.Content);   // 阈值写进正文
        Assert.False(entity.Read);

        // 同一天再次超比例 → 不重复提醒
        OnlineAlertService.Check(900, t0.AddMinutes(1), BuildSet());
        Assert.Equal(1, CountAlerts());

        // 次日再次超比例 → 再提醒
        OnlineAlertService.Check(700, t0.AddDays(1), BuildSet());
        Assert.Equal(2, CountAlerts());
    }

    [Fact]
    [DisplayName("Check_未超过固定比例_不告警")]
    public void Check_BelowRatio_NoAlert()
    {
        var today = new DateTime(2021, 2, 10);
        SeedMax(today, 500);      // 阈值 600
        ClearAlerts();

        // 恰好在阈值（600）不算超过 → 不告警
        OnlineAlertService.Check(600, today.AddHours(10), BuildSet());
        Assert.Equal(0, CountAlerts());

        // 低于阈值
        OnlineAlertService.Check(400, today.AddHours(11), BuildSet());
        Assert.Equal(0, CountAlerts());
    }

    [Fact]
    [DisplayName("Check_无历史基线_不告警")]
    public void Check_NoBaseline_NoAlert()
    {
        var today = new DateTime(2022, 3, 10);
        ClearAlerts();

        // 近7天没有任何 UserStat 纪录（全新系统）
        OnlineAlertService.Check(700, today.AddHours(10), BuildSet());
        Assert.Equal(0, CountAlerts());
    }

    [Fact]
    [DisplayName("Check_在线统计未开启_不告警")]
    public void Check_StatDisabled_NoAlert()
    {
        var today = new DateTime(2023, 4, 10);
        SeedMax(today, 500);
        ClearAlerts();

        // 用户在线记录关闭（0）
        var set1 = new CubeSetting { EnableUserOnline = 0, EnableUserStat = true };
        OnlineAlertService.Check(700, today.AddHours(10), set1);
        Assert.Equal(0, CountAlerts());

        // 用户统计关闭
        var set2 = new CubeSetting { EnableUserOnline = 2, EnableUserStat = false };
        OnlineAlertService.Check(700, today.AddHours(11), set2);
        Assert.Equal(0, CountAlerts());
    }

    #region 辅助
    /// <summary>播种近7天（不含今天）的历史最大在线纪录</summary>
    private static void SeedMax(DateTime today, Int32 maxOnline)
    {
        for (var i = 1; i <= OnlineAlertService.Days; i++)
        {
            var st = new UserStat
            {
                Date = today.AddDays(-i),
                Total = 100,
                MaxOnline = maxOnline,
                Logins = 100,
                Actives = 80,
                News = 5,
            };
            st.Insert();
        }
    }

    /// <summary>清空历史告警记录，保证每个用例独立</summary>
    private static void ClearAlerts() => NotificationRecord.Delete(NotificationRecord._.Action == "OnlineAlert");

    private static CubeSetting BuildSet() => new() { EnableUserOnline = 2, EnableUserStat = true };

    private static Int32 CountAlerts() => (Int32)NotificationRecord.FindCount(NotificationRecord._.Action == "OnlineAlert");
    #endregion
}
