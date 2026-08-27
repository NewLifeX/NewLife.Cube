using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;
using static XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>系统日志统计。24h日志量、最近7天趋势、最近异常记录</summary>
[Widget("Log", "系统日志", Icon = "fa-list-alt", Cols = 7, Sort = 90, Category = "系统", AdminOnly = true)]
public class LogWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>日志统计匿名对象</returns>
    public Object GetData()
    {
        var now = DateTime.Now;
        var start = now.AddHours(-24);
        // Log.Id 是雪花Id，带时间信息，用 Id 时间范围过滤
        var snow = XLog.Meta.Factory.Snow;

        // 24h 统计
        var total = XLog.FindCount(_.ID.Between(start, now, snow));
        var error = XLog.FindCount(_.ID.Between(start, now, snow) & _.Success == false);

        // 最近7天日志趋势
        var trend = new List<Object>();
        for (var i = 6; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            var count = XLog.FindCount(_.ID.Between(day, day.AddDays(1), snow));
            trend.Add(new { day = day.ToString("MM-dd"), count });
        }

        // 最近异常。雪花Id，按 Id 降序
        var recent = XLog.FindAll(_.Success == false & _.ID.Between(start, now, snow), "ID desc", null, 0, 10);

        return new
        {
            Total24h = total,
            Error24h = error,
            Trend = trend.ToArray(),
            RecentErrors = recent.Select(e => new { e.CreateTime, e.Action, e.Remark }).ToArray(),
        };
    }
}
