using System;
using System.ComponentModel;
using NewLife.Cube.Widgets;
using Xunit;

namespace XUnitTest;

/// <summary>WidgetManager 工作台组件管理器单元测试</summary>
public class WidgetManagerTests
{
    private readonly WidgetManager _manager = new();

    [Fact]
    [DisplayName("扫描发现全部注册部件且元数据完整")]
    public void Scan_FindsAllWidgets()
    {
        var dic = _manager.Scan();

        // 核心部件应全部存在
        foreach (var name in new[] { "UserCount", "TodayLogin", "OnlineCount", "Log24h", "Error24h", "CpuRate", "MyLogins", "MyDays", "Monitor", "QuickLink", "Profile", "SysInfo", "LoginLog" })
        {
            Assert.True(dic.ContainsKey(name), $"未找到部件 {name}");
        }

        // 元数据完整性
        var info = dic["UserCount"];
        Assert.Equal("用户总数", info.Title);
        Assert.Equal(2, info.Cols);
        Assert.True(info.AdminOnly);
        Assert.Equal("系统", info.Category);
        Assert.NotNull(info.Type);
        Assert.True(typeof(IWidget).IsAssignableFrom(info.Type));
    }

    [Fact]
    [DisplayName("系统 KPI 部件宽度为 2（小卡）")]
    public void KpiWidgets_ColsLessThan3()
    {
        var dic = _manager.Scan();

        foreach (var name in new[] { "UserCount", "TodayLogin", "OnlineCount", "Log24h", "Error24h", "CpuRate" })
        {
            Assert.True(dic[name].Cols <= 3, $"KPI 部件 {name} 宽度应为 2");
        }
    }

    [Fact]
    [DisplayName("个人 KPI 部件对普通用户可见")]
    public void PersonalKpi_VisibleForNormalUser()
    {
        var dic = _manager.Scan();

        foreach (var name in new[] { "MyLogins", "MyDays" })
        {
            Assert.False(dic[name].AdminOnly, $"个人 KPI {name} 不应仅管理员可见");
            Assert.Equal("个人", dic[name].Category);
        }
    }

    [Fact]
    [DisplayName("可见性：AdminOnly 部件对普通用户隐藏")]
    public void IsVisible_AdminOnly_HiddenForNormalUser()
    {
        var dic = _manager.Scan();

        // 管理员部件：普通用户不可见
        Assert.False(_manager.IsVisible(dic["UserCount"], new[] { "普通用户" }, false));
        // 管理员：可见
        Assert.True(_manager.IsVisible(dic["UserCount"], new[] { "管理员" }, true));
        // 全用户部件：普通用户可见
        Assert.True(_manager.IsVisible(dic["Profile"], new[] { "普通用户" }, false));
    }

    [Fact]
    [DisplayName("可见性：无角色集合时权限过滤")]
    public void IsVisible_NoRoles_HiddenForPermissionWidget()
    {
        var dic = _manager.Scan();

        // 无 Permission 的部件，无角色集合也可见
        Assert.True(_manager.IsVisible(dic["Profile"], null, false));

        // 构造带权限的部件：无角色集合不可见，指定角色可见，其它角色不可见
        var info = new WidgetAttribute("Test", "测试") { Permission = "管理员" };
        Assert.False(_manager.IsVisible(info, null, false));
        Assert.True(_manager.IsVisible(info, new[] { "管理员" }, false));
        Assert.False(_manager.IsVisible(info, new[] { "普通用户" }, false));
    }
}
