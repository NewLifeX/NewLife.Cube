using System;
using System.Collections.Generic;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>覆盖 <see cref="MenuHelper.IsMenuAvailable"/> 的跨宿主菜单可用性判断</summary>
public class MenuAvailabilityTests
{
    static readonly String _controller = typeof(NewLife.Cube.Areas.Admin.Controllers.UserController).FullName;

    [Fact(DisplayName = "空全名：不做校验，可用")]
    public void EmptyFullName_Available()
    {
        Assert.True(MenuHelper.IsMenuAvailable((String)null));
        Assert.True(MenuHelper.IsMenuAvailable(""));
    }

    [Fact(DisplayName = "非类路径全名：不做校验，可用")]
    public void NonClassFullName_Available() => Assert.True(MenuHelper.IsMenuAvailable("custom-menu"));

    [Fact(DisplayName = "区域根命名空间：可用")]
    public void AreaNamespace_Available() => Assert.True(MenuHelper.IsMenuAvailable("NewLife.Cube.Areas.Admin.Controllers"));

    [Fact(DisplayName = "当前宿主存在的控制器：可用")]
    public void ExistingController_Available() => Assert.True(MenuHelper.IsMenuAvailable(_controller));

    [Fact(DisplayName = "当前宿主存在的控制器方法：可用")]
    public void ExistingControllerAction_Available() => Assert.True(MenuHelper.IsMenuAvailable($"{_controller}.Index"));

    [Fact(DisplayName = "当前宿主不存在的控制器（API 版值集管理）：不可用")]
    public void MissingController_Unavailable() => Assert.False(MenuHelper.IsMenuAvailable("NewLife.Cube.Areas.Admin.Controllers.LovController"));

    [Fact(DisplayName = "当前宿主不存在的控制器方法：不可用")]
    public void MissingControllerAction_Unavailable() => Assert.False(MenuHelper.IsMenuAvailable("NewLife.Cube.Areas.Admin.Controllers.LovController.Index"));

    [Fact(DisplayName = "重复判断命中缓存且结果一致")]
    public void Repeat_ResultStable()
    {
        for (var i = 0; i < 3; i++) Assert.True(MenuHelper.IsMenuAvailable(_controller));
        for (var i = 0; i < 3; i++) Assert.False(MenuHelper.IsMenuAvailable("NewLife.Cube.Areas.Admin.Controllers.LovController"));
    }

    [Fact(DisplayName = "空集合过滤返回空列表")]
    public void Filter_Null_ReturnsEmpty()
    {
        Assert.Empty(MenuHelper.FilterAvailable(null));
        Assert.Empty(MenuHelper.FilterAvailable(new List<IMenu>()));
    }

    [Fact(DisplayName = "过滤列表只保留当前宿主可用菜单")]
    public void Filter_KeepsAvailableOnly()
    {
        var list = new List<IMenu>
        {
            new Menu { Name = "User", FullName = _controller },
            new Menu { Name = "Lov", FullName = "NewLife.Cube.Areas.Admin.Controllers.LovController" },
            new Menu { Name = "Custom" },
        };

        var rs = MenuHelper.FilterAvailable(list);

        Assert.Equal(2, rs.Count);
        Assert.Contains(rs, e => e.Name == "User");
        Assert.Contains(rs, e => e.Name == "Custom");
    }
}
