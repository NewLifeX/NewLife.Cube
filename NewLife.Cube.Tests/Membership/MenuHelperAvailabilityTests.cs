using System;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>覆盖 <see cref="MenuHelper.IsMenuAvailable"/> 对方法级菜单与 Action 重载的处理。</summary>
public class MenuHelperAvailabilityTests
{
    private class OverloadActionController : Controller
    {
        public ActionResult Info(Int32 id) => Content(id.ToString());

        public ActionResult Info(String name) => Content(name);
    }

    [Fact(DisplayName = "方法级菜单：Action 存在重载时不应抛 AmbiguousMatchException")]
    public void MethodLevelMenu_WithOverloads_DoesNotThrow()
    {
        var fullName = typeof(OverloadActionController).FullName + ".Info";

        var type = MenuHelper.ResolveMenuType(fullName);

        Assert.NotNull(type);
        Assert.Equal(typeof(OverloadActionController), type);
        Assert.True(MenuHelper.IsMenuAvailable(fullName));
    }

    [Fact(DisplayName = "方法级菜单：Action 不存在时不可用")]
    public void MethodLevelMenu_MissingAction_NotAvailable()
    {
        var fullName = typeof(OverloadActionController).FullName + ".Missing";

        Assert.Null(MenuHelper.ResolveMenuType(fullName));
        Assert.False(MenuHelper.IsMenuAvailable(fullName));
    }
}
