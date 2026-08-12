using System;
using NewLife.Cube;
using NewLife.Cube.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>覆盖 <see cref="MenuHelper.CheckVisible"/> 的多租户菜单模式隔离逻辑。</summary>
public class MenuHelperCheckVisibleTests
{
    // 仅管理后台可见
    [Menu(100, true, Mode = MenuModes.Admin)]
    private class AdminOnlyController { }

    // 仅租户可见
    [Menu(100, true, Mode = MenuModes.Tenant)]
    private class TenantOnlyController { }

    // 管理后台与租户均可见
    [Menu(100, true, Mode = MenuModes.Admin | MenuModes.Tenant)]
    private class BothController { }

    // 未声明模式（默认仅管理后台可见）
    [Menu(100, true)]
    private class NoModeController { }

    // 未标注 MenuAttribute
    private class NoAttributeController { }

    [Fact(DisplayName = "纯Admin菜单：管理后台可见，租户模式禁止")]
    public void AdminOnly_VisibleInAdmin_NotInTenant()
    {
        Assert.True(MenuHelper.CheckVisible(typeof(AdminOnlyController), false));
        Assert.False(MenuHelper.CheckVisible(typeof(AdminOnlyController), true));
    }

    [Fact(DisplayName = "纯Tenant菜单：租户模式可见，管理后台禁止")]
    public void TenantOnly_VisibleInTenant_NotInAdmin()
    {
        Assert.False(MenuHelper.CheckVisible(typeof(TenantOnlyController), false));
        Assert.True(MenuHelper.CheckVisible(typeof(TenantOnlyController), true));
    }

    [Fact(DisplayName = "双模式菜单：两种模式均可见")]
    public void Both_VisibleEverywhere()
    {
        Assert.True(MenuHelper.CheckVisible(typeof(BothController), false));
        Assert.True(MenuHelper.CheckVisible(typeof(BothController), true));
    }

    [Fact(DisplayName = "未声明模式：默认仅管理后台可见")]
    public void NoMode_DefaultAdminOnly()
    {
        Assert.True(MenuHelper.CheckVisible(typeof(NoModeController), false));
        Assert.False(MenuHelper.CheckVisible(typeof(NoModeController), true));
    }

    [Fact(DisplayName = "未标注MenuAttribute：两种模式均放行")]
    public void NoAttribute_VisibleEverywhere()
    {
        Assert.True(MenuHelper.CheckVisible(typeof(NoAttributeController), false));
        Assert.True(MenuHelper.CheckVisible(typeof(NoAttributeController), true));
    }

    [Fact(DisplayName = "null类型：放行（不拦截）")]
    public void NullType_Visible()
    {
        Assert.True(MenuHelper.CheckVisible(null, false));
        Assert.True(MenuHelper.CheckVisible(null, true));
    }
}
