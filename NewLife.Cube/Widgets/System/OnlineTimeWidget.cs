using NewLife.Cube.Entity;

namespace NewLife.Cube.Widgets.System;

/// <summary>今日在线时长。工作台 KPI 指标，今日累计在线时长（秒，取自 UserStat 每日统计）</summary>
[Widget("OnlineTime", "今日在线时长", Icon = "fa-clock-o", Cols = 2, Sort = 60, Category = "系统", AdminOnly = true, Color = "orange", WidgetType = WidgetTypes.Kpi)]
public class OnlineTimeWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        // 今日累计在线时长取自 UserStat 每日统计（会话退出/超时结算时累加秒数）
        var stat = UserStat.FindByDate(DateTime.Today);
        var value = FormatSeconds(stat?.OnlineTime ?? 0);

        return new
        {
            Value = value,
            Trend = "今日累计",
            // 跳转每日统计页：携带今日时间范围
            Url = $"/Admin/UserStat?dtStart={DateTime.Today:yyyy-MM-dd}&dtEnd={DateTime.Today:yyyy-MM-dd}",
        };
    }

    /// <summary>秒数格式化为可读时长。不足1分钟显示"不足1分钟"，1小时内显示分钟数，超过则 X小时Y分</summary>
    /// <param name="seconds">累计秒数</param>
    /// <returns>可读时长字符串</returns>
    private static String FormatSeconds(Int32 seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalMinutes < 1) return "不足1分钟";

        var hours = (Int32)ts.TotalHours;
        if (hours > 0) return $"{hours}小时{ts.Minutes}分";

        return $"{Math.Max(1, ts.Minutes)}分钟";
    }
}
