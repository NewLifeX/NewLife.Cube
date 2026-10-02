using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.Membership;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>覆盖 <see cref="MenuHelper.FilterByTenant"/> 空树容错与租户/管理后台隔离。</summary>
public class MenuHelperFilterByTenantTests
{
    [Menu(100, true, Mode = MenuModes.Admin)]
    private class AdminOnlyController { }

    [Menu(100, true, Mode = MenuModes.Tenant)]
    private class TenantOnlyController { }

    [Menu(100, true, Mode = MenuModes.Admin | MenuModes.Tenant)]
    private class BothController { }

    private static MenuTree Node(String fullName, String name = null) => new()
    {
        Name = name ?? fullName?.Split('.').LastOrDefault() ?? "node",
        DisplayName = name ?? fullName,
        FullName = fullName,
        Visible = true,
    };

    [Fact(DisplayName = "FilterByTenant：menus 为 null 时返回空列表（不抛 NRE）")]
    public void FilterByTenant_NullMenus_ReturnsEmpty()
    {
        var admin = MenuHelper.FilterByTenant(null, false);
        var tenant = MenuHelper.FilterByTenant(null, true);

        Assert.NotNull(admin);
        Assert.Empty(admin);
        Assert.NotNull(tenant);
        Assert.Empty(tenant);
    }

    [Fact(DisplayName = "FilterByTenant：空列表返回空列表")]
    public void FilterByTenant_EmptyMenus_ReturnsEmpty()
    {
        var result = MenuHelper.FilterByTenant([], false);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "FilterByTenant：管理后台隐藏纯 Tenant 菜单，保留 Admin/双模式")]
    public void FilterByTenant_AdminMode_HidesTenantOnly()
    {
        var menus = new List<MenuTree>
        {
            Node(typeof(AdminOnlyController).FullName),
            Node(typeof(TenantOnlyController).FullName),
            Node(typeof(BothController).FullName),
            Node(null), // FullName 为空应跳过
        };

        var result = MenuHelper.FilterByTenant(menus, false);

        Assert.Contains(result, e => e.FullName == typeof(AdminOnlyController).FullName);
        Assert.Contains(result, e => e.FullName == typeof(BothController).FullName);
        Assert.DoesNotContain(result, e => e.FullName == typeof(TenantOnlyController).FullName);
    }

    [Fact(DisplayName = "FilterByTenant：租户模式隐藏纯 Admin 菜单，保留 Tenant/双模式")]
    public void FilterByTenant_TenantMode_HidesAdminOnly()
    {
        var menus = new List<MenuTree>
        {
            Node(typeof(AdminOnlyController).FullName),
            Node(typeof(TenantOnlyController).FullName),
            Node(typeof(BothController).FullName),
        };

        var result = MenuHelper.FilterByTenant(menus, true);

        Assert.DoesNotContain(result, e => e.FullName == typeof(AdminOnlyController).FullName);
        Assert.Contains(result, e => e.FullName == typeof(TenantOnlyController).FullName);
        Assert.Contains(result, e => e.FullName == typeof(BothController).FullName);
    }

    [Fact(DisplayName = "FilterByTenant：列表含 null 元素时跳过，不抛 NRE")]
    public void FilterByTenant_NullItem_Skipped()
    {
        var menus = new List<MenuTree>
        {
            null,
            Node(typeof(AdminOnlyController).FullName),
        };

        var result = MenuHelper.FilterByTenant(menus, false);

        Assert.Single(result);
        Assert.Equal(typeof(AdminOnlyController).FullName, result[0].FullName);
    }
}
