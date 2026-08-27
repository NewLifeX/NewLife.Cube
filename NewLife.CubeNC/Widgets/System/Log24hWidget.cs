using XCode;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>24h日志量。工作台 KPI 指标</summary>
[Widget("Log24h", "24h日志", Icon = "fa-file-text-o", Cols = 2, Sort = 40, Category = "系统", AdminOnly = true, Color = "grey")]
public class Log24hWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var now = DateTime.Now;
        // Log.Id 是雪花Id，带时间信息，用 Id 时间范围过滤
        var snow = XLog.Meta.Factory.Snow;
        var count = XLog.FindCount(XLog._.ID.Between(now.AddHours(-24), now, snow));

        return new
        {
            Value = count.ToString("n0"),
            Trend = "最近24小时",
            Url = "/Admin/Log",
        };
    }
}
