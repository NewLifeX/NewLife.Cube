using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>在线用户。工作台 KPI 指标，当前在线用户数（UserOnline 由定时任务清理 20 分钟不活跃会话）</summary>
[Widget("OnlineCount", "在线用户", Icon = "fa-user-circle-o", Cols = 2, Sort = 30, Category = "系统", AdminOnly = true, Color = "cyan")]
public class OnlineCountWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        // UserOnline 表内即当前在线会话，直接取总数
        var count = UserOnline.FindCount();

        return new
        {
            Value = count.ToString(),
            Trend = "当前在线",
            Url = "/Admin/UserOnline",
        };
    }
}
