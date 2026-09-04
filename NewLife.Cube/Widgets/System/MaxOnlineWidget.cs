using NewLife.Cube.Entity;

namespace NewLife.Cube.Widgets.System;

/// <summary>最大在线。工作台 KPI 指标，今日最大同时在线用户数（取自 UserStat 每日统计）</summary>
[Widget("MaxOnline", "最大在线", Icon = "fa-signal", Cols = 2, Sort = 40, Category = "系统", AdminOnly = true, Color = "purple", WidgetType = WidgetTypes.Kpi)]
public class MaxOnlineWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        // 今日最大在线取自 UserStat 每日统计（ClearExpire 检测在线数变化时刷新峰值），比扫日志高效
        var stat = UserStat.FindByDate(DateTime.Today);
        var count = stat?.MaxOnline ?? 0;

        return new
        {
            Value = count.ToString("n0"),
            Trend = "今日峰值",
            // 跳转每日统计页：携带今日时间范围
            Url = $"/Admin/UserStat?dtStart={DateTime.Today:yyyy-MM-dd}&dtEnd={DateTime.Today:yyyy-MM-dd}",
        };
    }
}
