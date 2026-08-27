using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.Widgets;
using Xunit;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>覆盖 <see cref="WidgetManager"/> 的扫描、权限过滤与排序逻辑</summary>
public class WidgetManagerTests
{
    [Fact(DisplayName = "扫描发现内置系统组件")]
    public void Scan_FindsBuiltinWidgets()
    {
        var widgets = WidgetManager.Instance.Scan();

        // 内置组件随 NewLife.Cube（API版）编译；Monitor 依赖 Charts，仅 MVC 版存在，这里不校验
        Assert.Contains("StatCards", widgets.Keys);
        Assert.Contains("Log", widgets.Keys);
        Assert.Contains("LoginLog", widgets.Keys);
        Assert.Contains("SysInfo", widgets.Keys);
        Assert.Contains("Profile", widgets.Keys);
        Assert.Contains("QuickLink", widgets.Keys);
    }

    [Fact(DisplayName = "GetWidgets按Sort升序返回")]
    public void GetWidgets_SortedBySort()
    {
        var list = WidgetManager.Instance.GetWidgets();
        Assert.NotEmpty(list);

        var sorts = list.Select(e => e.Sort).ToList();
        Assert.Equal(sorts.OrderBy(e => e).ToList(), sorts);
    }

    [Fact(DisplayName = "IsVisible_无权限要求_任何角色可见")]
    public void IsVisible_NoPermission_VisibleToAll()
    {
        var info = new WidgetInfo { Permission = "" };

        Assert.True(WidgetManager.IsVisible(info, new List<String>()));
        Assert.True(WidgetManager.IsVisible(info, null));
        Assert.True(WidgetManager.IsVisible(info, new List<String> { "普通用户" }));
    }

    [Fact(DisplayName = "IsVisible_角色匹配_可见")]
    public void IsVisible_MatchingRole_Visible()
    {
        var info = new WidgetInfo { Permission = "管理员,系统管理员" };

        Assert.True(WidgetManager.IsVisible(info, new List<String> { "系统管理员" }));
        Assert.True(WidgetManager.IsVisible(info, new List<String> { "普通用户", "管理员" }));
    }

    [Fact(DisplayName = "IsVisible_角色不匹配_不可见")]
    public void IsVisible_NoMatchingRole_Invisible()
    {
        var info = new WidgetInfo { Permission = "管理员" };

        Assert.False(WidgetManager.IsVisible(info, new List<String> { "普通用户" }));
        Assert.False(WidgetManager.IsVisible(info, new List<String>()));
        Assert.False(WidgetManager.IsVisible(info, null));
    }

    [Fact(DisplayName = "普通用户看不到AdminOnly系统组件")]
    public void GetWidgets_NormalUser_HidesAdminOnly()
    {
        // 普通用户（非系统角色）：仅见个人组件，系统监控组件全部隐藏
        var list = WidgetManager.Instance.GetWidgets(new List<String> { "普通用户" }, false);
        var names = list.Select(e => e.Name).ToList();

        Assert.Contains("Profile", names);
        Assert.Contains("QuickLink", names);
        Assert.DoesNotContain("StatCards", names);
        Assert.DoesNotContain("Monitor", names);
        Assert.DoesNotContain("Log", names);
        Assert.DoesNotContain("LoginLog", names);
        Assert.DoesNotContain("SysInfo", names);
    }

    [Fact(DisplayName = "系统角色可见全部组件")]
    public void GetWidgets_Admin_SeesAll()
    {
        var list = WidgetManager.Instance.GetWidgets(new List<String> { "系统管理员" }, true);
        var names = list.Select(e => e.Name).ToList();

        // Monitor 依赖 Charts，仅 MVC 版编译，API 测试程序集中不存在
        Assert.Contains("StatCards", names);
        Assert.Contains("Log", names);
        Assert.Contains("LoginLog", names);
        Assert.Contains("SysInfo", names);
        Assert.Contains("Profile", names);
        Assert.Contains("QuickLink", names);
    }

    [Fact(DisplayName = "IsVisible_AdminOnly组件_普通用户不可见管理员可见")]
    public void IsVisible_AdminOnly_HiddenForNormal()
    {
        var info = new WidgetInfo { AdminOnly = true };

        Assert.False(WidgetManager.IsVisible(info, new List<String> { "普通用户" }, false));
        Assert.True(WidgetManager.IsVisible(info, new List<String> { "普通用户" }, true));
    }

    [Fact(DisplayName = "GetData_StatCards返回统计指标")]
    public void GetData_StatCards_ReturnsStructure()
    {
        var info = WidgetManager.Instance.Scan()["StatCards"];
        var data = WidgetManager.Instance.GetData(info);
        Assert.NotNull(data);

        dynamic d = data!;
        Assert.True((Double)d.CpuRate >= 0);
        Assert.False(String.IsNullOrEmpty((String)d.Uptime));
    }

    [Fact(DisplayName = "GetData_QuickLink返回快捷链接")]
    public void GetData_QuickLink_ReturnsLinks()
    {
        var info = WidgetManager.Instance.Scan()["QuickLink"];
        var data = WidgetManager.Instance.GetData(info);
        Assert.NotNull(data);

        dynamic d = data!;
        var links = (Object[])d.Links;
        Assert.NotEmpty(links);
    }

    [Fact(DisplayName = "GetData_Profile无登录用户返回空安全结构")]
    public void GetData_Profile_NoUser_ReturnsStructure()
    {
        // 测试环境无登录用户，Profile 组件应返回空安全的数据结构而非异常
        var info = WidgetManager.Instance.Scan()["Profile"];
        var data = WidgetManager.Instance.GetData(info);
        Assert.NotNull(data);

        dynamic d = data!;
        Assert.Equal(0, (Int32)d.Logins);
    }
}
