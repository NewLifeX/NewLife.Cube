using System;
using NewLife.Cube.Areas.Cube.Controllers;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>定时作业 Cron 表达式解析测试。覆盖单表达式、多表达式取最早与非法表达式</summary>
public class CronScheduleTests
{
    private static readonly DateTime _now = new(2026, 10, 24, 10, 30, 0, DateTimeKind.Local);

    [Fact(DisplayName = "Cron：单表达式返回晚于基准时间的下次执行")]
    public void GetNextRunTime_SingleExpression()
    {
        var next = CronJobController.GetNextRunTime("0 0 8 * * *", _now);

        Assert.True(next > _now, $"next={next:yyyy-MM-dd HH:mm:ss}");
        Assert.Equal(8, next.Hour);
        Assert.Equal(0, next.Minute);
        Assert.Equal(0, next.Second);
    }

    [Fact(DisplayName = "Cron：分号分隔多个表达式时取最近一次")]
    public void GetNextRunTime_MultipleExpressions_TakesEarliest()
    {
        // 基准 10:30：每天 8 点已过（下次为明天），每天 12 点未到，应取当天 12 点
        var next = CronJobController.GetNextRunTime("0 0 8 * * *;0 0 12 * * *", _now);

        Assert.True(next > _now);
        Assert.Equal(12, next.Hour);
        Assert.Equal(_now.Date.AddHours(12), next);
    }

    [Fact(DisplayName = "Cron：非法表达式抛参数异常")]
    public void GetNextRunTime_InvalidExpression_Throws()
    {
        // 步进值为 0 会导致永不推进，解析失败
        Assert.Throws<ArgumentException>(() => CronJobController.GetNextRunTime("*/0 * * * * *", _now));
        Assert.Throws<ArgumentException>(() => CronJobController.GetNextRunTime("0 0 8 * * *;*/0 * * * * *", _now));
    }
}
