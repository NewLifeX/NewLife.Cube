using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>用户总数。工作台 KPI 指标</summary>
[Widget("UserCount", "用户总数", Icon = "fa-users", Cols = 2, Sort = 10, Category = "系统", AdminOnly = true, Color = "blue", WidgetType = WidgetTypes.Kpi)]
public class UserCountWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        // 行权（OSC-2608273d95）：部件是页外出口，口径必须与 Admin/User 列表一致，
        // 否则「系统角色但 DataScope 非全部」的用户会看到全表总数。无限制时仍走实体缓存，保持性能
        var scope = CubeDataScope.GetFilter(User.Meta.Factory);

        return new
        {
            Value = (scope == null ? User.Meta.Count : User.FindCount(scope)).ToString("n0"),
            Trend = "注册用户",
            Url = "/Admin/User",
        };
    }
}
