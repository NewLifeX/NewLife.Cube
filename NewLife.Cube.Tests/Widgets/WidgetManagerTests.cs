using System;
using System.Collections.Generic;
using NewLife.Cube.Widgets;
using Xunit;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>覆盖 <see cref="WidgetManager"/> 的扫描与权限过滤逻辑（不依赖数据库）</summary>
public class WidgetManagerTests
{
    private readonly WidgetManager _manager = new();

    [Fact(DisplayName = "扫描发现内置系统组件")]
    public void Scan_FindsBuiltinWidgets()
    {
        var widgets = _manager.Scan();

        // 内置组件随 NewLife.Cube（API版）编译；Monitor 依赖 Charts、KPI 部件属 MVC 工作台，仅 MVC 版存在，这里不校验
        // LogWidget 已删除（与 KPI 24h 数字重复），不再断言
        Assert.Contains("LoginLog", widgets.Keys);
        Assert.Contains("SysInfo", widgets.Keys);
        Assert.Contains("Profile", widgets.Keys);
        Assert.Contains("QuickLink", widgets.Keys);
    }

    [Fact(DisplayName = "IsVisible_无权限要求_任何角色可见")]
    public void IsVisible_NoPermission_VisibleToAll()
    {
        var info = new WidgetAttribute("Test", "测试") { Permission = "" };

        Assert.True(_manager.IsVisible(info, new List<String>()));
        Assert.True(_manager.IsVisible(info, null));
        Assert.True(_manager.IsVisible(info, new List<String> { "普通用户" }));
    }

    [Fact(DisplayName = "IsVisible_角色匹配_可见")]
    public void IsVisible_MatchingRole_Visible()
    {
        var info = new WidgetAttribute("Test", "测试") { Permission = "管理员,系统管理员" };

        Assert.True(_manager.IsVisible(info, new List<String> { "系统管理员" }));
        Assert.True(_manager.IsVisible(info, new List<String> { "普通用户", "管理员" }));
    }

    [Fact(DisplayName = "IsVisible_角色不匹配_不可见")]
    public void IsVisible_NoMatchingRole_Invisible()
    {
        var info = new WidgetAttribute("Test", "测试") { Permission = "管理员" };

        Assert.False(_manager.IsVisible(info, new List<String> { "普通用户" }));
        Assert.False(_manager.IsVisible(info, new List<String>()));
        Assert.False(_manager.IsVisible(info, null));
    }

    [Fact(DisplayName = "普通用户看不到AdminOnly系统组件")]
    public void NormalUser_HidesAdminOnly()
    {
        var dic = _manager.Scan();

        Assert.True(_manager.IsVisible(dic["Profile"], new List<String> { "普通用户" }, false));
        Assert.True(_manager.IsVisible(dic["QuickLink"], new List<String> { "普通用户" }, false));
        Assert.False(_manager.IsVisible(dic["LoginLog"], new List<String> { "普通用户" }, false));
        Assert.False(_manager.IsVisible(dic["SysInfo"], new List<String> { "普通用户" }, false));
    }

    [Fact(DisplayName = "系统角色可见全部组件")]
    public void Admin_SeesAll()
    {
        var dic = _manager.Scan();

        Assert.True(_manager.IsVisible(dic["LoginLog"], new List<String> { "系统管理员" }, true));
        Assert.True(_manager.IsVisible(dic["SysInfo"], new List<String> { "系统管理员" }, true));
        Assert.True(_manager.IsVisible(dic["Profile"], new List<String> { "系统管理员" }, true));
        Assert.True(_manager.IsVisible(dic["QuickLink"], new List<String> { "系统管理员" }, true));
    }

    [Fact(DisplayName = "IsVisible_AdminOnly组件_普通用户不可见管理员可见")]
    public void IsVisible_AdminOnly_HiddenForNormal()
    {
        var info = new WidgetAttribute("Test", "测试") { AdminOnly = true };

        Assert.False(_manager.IsVisible(info, new List<String> { "普通用户" }, false));
        Assert.True(_manager.IsVisible(info, new List<String> { "普通用户" }, true));
    }

    [Fact(DisplayName = "GetData_QuickLink未登录返回空链接")]
    public void GetData_QuickLink_ReturnsLinks()
    {
        var info = _manager.Scan()["QuickLink"];
        var data = _manager.GetData(info);
        Assert.NotNull(data);

        dynamic d = data!;
        var links = (IEnumerable<Object>)d.Links;
        Assert.Empty(links);
    }

    [Fact(DisplayName = "GetData_Profile无登录用户返回空安全结构")]
    public void GetData_Profile_NoUser_ReturnsStructure()
    {
        // 测试环境无登录用户，Profile 组件应返回空安全的数据结构而非异常
        var info = _manager.Scan()["Profile"];
        var data = _manager.GetData(info);
        Assert.NotNull(data);

        dynamic d = data!;
        Assert.Equal(0, (Int32)d.Logins);
    }
}
