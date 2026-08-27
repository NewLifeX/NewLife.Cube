using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>注册天数。普通用户工作台 KPI 指标</summary>
[Widget("MyDays", "注册天数", Icon = "fa-calendar", Cols = 3, Sort = 30, Category = "个人", Color = "blue")]
public class MyDaysWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var user = ManageProvider.User;
        var days = user != null && user.RegisterTime.Year > 2000 ? (DateTime.Now - user.RegisterTime).Days : 0;

        return new
        {
            Value = days.ToString("n0"),
            Trend = "加入魔方以来",
            Url = "",
        };
    }
}
