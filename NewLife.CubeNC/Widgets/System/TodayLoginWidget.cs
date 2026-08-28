using NewLife.Cube.Entity;

namespace NewLife.Cube.Widgets.System;

/// <summary>今日登录。工作台 KPI 指标，统计今日登录成功次数（取自 UserStat 每日统计）</summary>
[Widget("TodayLogin", "今日登录", Icon = "fa-sign-in", Cols = 2, Sort = 20, Category = "系统", AdminOnly = true, Color = "green", WidgetType = WidgetTypes.Kpi)]
public class TodayLoginWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var now = DateTime.Now;
        // 今日登录次数直接取自 UserStat 每日统计（本地/SSO 登录时累加 Logins），比扫日志高效
        var stat = UserStat.FindByDate(DateTime.Today);
        var count = stat?.Logins ?? 0;

        return new
        {
            Value = count.ToString("n0"),
            Trend = "今日登录成功",
            // 跳转日志页带参数：今日登录记录
            Url = $"/Admin/Log?dtStart={DateTime.Today:yyyy-MM-dd}&dtEnd={now:yyyy-MM-dd HH:mm:ss}&act=登录",
        };
    }
}
