using XCode;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>24h异常数。工作台 KPI 指标</summary>
[Widget("Error24h", "24h异常", Icon = "fa-exclamation-triangle", Cols = 2, Sort = 50, Category = "系统", AdminOnly = true, Color = "red")]
public class Error24hWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var now = DateTime.Now;
        // Log.Id 是雪花Id，带时间信息，用 Id 时间范围过滤
        var snow = XLog.Meta.Factory.Snow;
        var count = XLog.FindCount(XLog._.ID.Between(now.AddHours(-24), now, snow) & XLog._.Success == false);

        return new
        {
            Value = count.ToString(),
            Trend = "最近24小时异常",
            Url = "/Admin/Log",
        };
    }
}
