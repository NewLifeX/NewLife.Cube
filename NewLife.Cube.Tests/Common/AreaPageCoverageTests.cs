using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube.Areas.Admin;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Areas.Cube;
using NewLife.Cube.Areas.Cube.Controllers;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// 系统管理与魔方管理页面清单回归
///
/// 这两个区域是 SPA 的页面来源：实体/配置控制器由 `/meta` 动态页承接，自定义控制器由前端静态页承接。
/// 控制器缺失会让菜单与页面一起消失（MVC 侧有、API 侧没有的页面就是这种情况），因此把清单固化成测试。
/// </summary>
public class AreaPageCoverageTests
{
    /// <summary>系统管理（Admin）区域的控制器页面清单，与 MVC 版对齐</summary>
    private static readonly String[] AdminPages =
    [
        "AccessRule", "Core", "Cube", "Db", "Department", "File", "Index", "Log", "Lov",
        "MailConfig", "Menu", "NotificationRecord", "OAuthConfig", "OAuthLog", "Parameter",
        "Role", "SmsConfig", "Star", "Sys", "Tenant", "TenantUser", "User", "UserConnect",
        "UserOnline", "UserStat", "UserToken", "XCode",
    ];

    /// <summary>魔方管理（Cube）区域的控制器页面清单，与 MVC 版对齐</summary>
    private static readonly String[] CubePages =
    [
        "App", "AppLog", "AppModule", "Area", "Attachment", "CronJob", "PrincipalAgent", "Widget",
    ];

    private static String[] ControllersIn(Type areaType)
        => areaType.Assembly.GetTypes()
            .Where(e => e.Name.EndsWith("Controller") && e.Namespace == areaType.Namespace + ".Controllers")
            .Select(e => e.Name.TrimSuffix("Controller"))
            .OrderBy(e => e)
            .ToArray();

    [Fact(DisplayName = "页面清单 - 系统管理区域控制器齐备")]
    public void AdminArea_HasAllPages()
    {
        var actual = ControllersIn(typeof(AdminArea));

        foreach (var name in AdminPages)
        {
            Assert.True(actual.Contains(name), $"系统管理区域缺少控制器 {name}Controller");
        }
    }

    [Fact(DisplayName = "页面清单 - 魔方管理区域控制器齐备")]
    public void CubeArea_HasAllPages()
    {
        var actual = ControllersIn(typeof(CubeArea));

        foreach (var name in CubePages)
        {
            Assert.True(actual.Contains(name), $"魔方管理区域缺少控制器 {name}Controller");
        }
    }

    [Fact(DisplayName = "通知记录页提供行内标记已读与全部已读动作")]
    public void NotificationRecord_HasMarkReadActions()
    {
        var type = typeof(NotificationRecordController);

        var markRead = type.GetMethod(nameof(NotificationRecordController.NotifyMarkRead));
        Assert.NotNull(markRead);
        Assert.Contains(
            markRead!.GetCustomAttributes<HttpPostAttribute>(),
            e => e.Template == "/api/[area]/[controller]/NotifyMarkRead");

        var markAll = type.GetMethod(nameof(NotificationRecordController.NotifyMarkAllRead));
        Assert.NotNull(markAll);
        Assert.Contains(
            markAll!.GetCustomAttributes<HttpPostAttribute>(),
            e => e.Template == "/api/[area]/[controller]/NotifyMarkAllRead");
    }

    [Fact(DisplayName = "应用插件页在魔方管理菜单中可见")]
    public void AppModule_HasVisibleMenu()
    {
        var att = typeof(AppModuleController).GetCustomAttribute<MenuAttribute>();

        Assert.NotNull(att);
        Assert.True(att!.Visible);
        Assert.Equal(36, att.Order);
    }

    [Fact(DisplayName = "通知记录页为隐藏菜单页面")]
    public void NotificationRecord_IsHiddenMenu()
    {
        var att = typeof(NotificationRecordController).GetCustomAttribute<MenuAttribute>();

        Assert.NotNull(att);
        Assert.False(att!.Visible);
    }
}
