using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>用户总数。工作台 KPI 指标</summary>
[Widget("UserCount", "Total users", Icon = "fa-users", Cols = 2, Sort = 10, Category = "System", AdminOnly = true, Color = "blue", WidgetType = WidgetTypes.Kpi)]
public class UserCountWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData() => new
    {
        Value = User.Meta.Count.ToString("n0"),
        Trend = L.T("Registered users"),
        Url = "/Admin/User",
    };
}
