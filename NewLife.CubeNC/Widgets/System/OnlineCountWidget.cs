using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>在线用户。工作台 KPI 指标，统计最近30分钟活跃用户</summary>
[Widget("OnlineCount", "在线用户", Icon = "fa-user-circle-o", Cols = 2, Sort = 30, Category = "系统", AdminOnly = true, Color = "cyan")]
public class OnlineCountWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var start = DateTime.Now.AddMinutes(-30);
        var count = UserOnline.FindCount(UserOnline._.CreateTime >= start);

        return new
        {
            Value = count.ToString(),
            Trend = "最近30分钟活跃",
            Url = "/Admin/UserOnline",
        };
    }
}
