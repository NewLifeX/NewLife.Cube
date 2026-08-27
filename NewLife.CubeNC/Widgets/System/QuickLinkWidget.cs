namespace NewLife.Cube.Widgets.System;

/// <summary>快捷入口。常用功能链接，管理员专属链接由视图按角色过滤</summary>
[Widget("QuickLink", "快捷入口", Icon = "fa-th-large", Cols = 4, Sort = 80, Category = "通用", WidgetType = WidgetTypes.Content)]
public class QuickLinkWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>快捷链接列表匿名对象</returns>
    public Object GetData() => new
    {
        Links = new[]
        {
            new { Name = "个人中心", Url = "/Admin/User/Info", Icon = "fa-user", AdminOnly = false },
            new { Name = "系统设置", Url = "/Admin/Cube", Icon = "fa-cog", AdminOnly = true },
            new { Name = "用户管理", Url = "/Admin/User", Icon = "fa-users", AdminOnly = true },
            new { Name = "审计日志", Url = "/Admin/Log", Icon = "fa-history", AdminOnly = true },
            new { Name = "在线用户", Url = "/Admin/UserOnline", Icon = "fa-user-circle", AdminOnly = true },
            new { Name = "定时作业", Url = "/Cube/CronJob", Icon = "fa-clock-o", AdminOnly = true },
        },
    };
}
