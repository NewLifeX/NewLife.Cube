using XCode;
using XLog = XCode.Membership.Log;
using static XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>今日登录。工作台 KPI 指标，统计今日登录成功次数</summary>
[Widget("TodayLogin", "今日登录", Icon = "fa-sign-in", Cols = 2, Sort = 20, Category = "系统", AdminOnly = true, Color = "green")]
public class TodayLoginWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var now = DateTime.Now;
        // Log.Id 是雪花Id，带时间信息，直接用时间范围过滤，避免依赖 CreateTime 列
        var snow = XLog.Meta.Factory.Snow;
        var count = XLog.FindCount(_.Action == "登录" & _.ID.Between(DateTime.Today, now, snow));

        return new
        {
            Value = count.ToString("n0"),
            Trend = "今日登录成功",
            // 跳转日志页带参数：今日登录记录
            Url = $"/Admin/Log?dtStart={DateTime.Today:yyyy-MM-dd}&dtEnd={now:yyyy-MM-dd HH:mm:ss}&act=登录",
        };
    }
}
