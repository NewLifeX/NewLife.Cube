using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>系统日志统计。24h日志量、最近7天趋势、最近异常记录</summary>
[Widget("Log", "系统日志", Icon = "fa-list-alt", Cols = 6, Sort = 30, Category = "系统", AdminOnly = true)]
public class LogWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>日志统计匿名对象</returns>
    public Object GetData()
    {
        var now = DateTime.Now;
        var start = now.AddHours(-24);

        // 24h 统计
        var total = XLog.FindCount(XLog._.CreateTime >= start & XLog._.CreateTime <= now);
        var error = XLog.FindCount(XLog._.CreateTime >= start & XLog._.CreateTime <= now & XLog._.Success == false);

        // 最近7天日志趋势
        var trend = new List<Object>();
        for (var i = 6; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            var count = XLog.FindCount(XLog._.CreateTime >= day & XLog._.CreateTime < day.AddDays(1));
            trend.Add(new { day = day.ToString("MM-dd"), count });
        }

        // 最近异常
        var recent = XLog.FindAll(XLog._.Success == false & XLog._.CreateTime >= start, "CreateTime desc", null, 0, 10);

        return new
        {
            Total24h = total,
            Error24h = error,
            Trend = trend.ToArray(),
            RecentErrors = recent.Select(e => new { e.CreateTime, e.Action, e.Remark }).ToArray(),
        };
    }
}
