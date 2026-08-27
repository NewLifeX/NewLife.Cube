using XCode;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>今日登录。工作台 KPI 指标，统计今日登录成功次数</summary>
[Widget("TodayLogin", "今日登录", Icon = "fa-sign-in", Cols = 2, Sort = 20, Category = "系统", AdminOnly = true, Color = "green")]
public class TodayLoginWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        // Log.Id 是雪花Id，带时间信息，直接用时间范围过滤，避免依赖 CreateTime 列
        var snow = XLog.Meta.Factory.Snow;
        var count = XLog.FindCount(XLog._.Action == "登录" & XLog._.ID.Between(DateTime.Today, DateTime.Now, snow));

        return new
        {
            Value = count.ToString("n0"),
            Trend = "今日登录成功",
            Url = "/Admin/Log",
        };
    }
}
