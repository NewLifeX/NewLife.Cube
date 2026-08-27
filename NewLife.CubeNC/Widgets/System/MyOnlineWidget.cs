using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>在线状态。普通用户工作台 KPI 指标</summary>
[Widget("MyOnline", "在线状态", Icon = "fa-user-circle-o", Cols = 3, Sort = 40, Category = "个人", Color = "cyan")]
public class MyOnlineWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var user = ManageProvider.User;

        return new
        {
            Value = user?.Online == true ? "在线" : "离线",
            Trend = "当前账号状态",
            Url = "",
        };
    }
}
