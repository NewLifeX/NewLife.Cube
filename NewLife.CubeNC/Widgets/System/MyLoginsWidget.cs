using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>我的登录次数。普通用户工作台 KPI 指标</summary>
[Widget("MyLogins", "我的登录", Icon = "fa-sign-in", Cols = 3, Sort = 10, Category = "个人", Color = "green")]
public class MyLoginsWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var user = ManageProvider.User;

        return new
        {
            Value = (user?.Logins ?? 0).ToString("n0"),
            Trend = "累计登录次数",
            Url = "/Admin/User/Info",
        };
    }
}
