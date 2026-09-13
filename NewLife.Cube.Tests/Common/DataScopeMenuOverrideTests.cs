using System;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>菜单级数据权限覆盖测试（OSC-2608273d95 · 实现审计 N6）</summary>
/// <remarks>
/// 覆盖 <see cref="DataScopeContext.SetMenu(IMenu)"/> 的覆盖/继承语义：
/// <c>menu.DataScope &gt;= 0</c> 覆盖角色数据范围；<c>&lt; 0</c>（默认 -1）表示沿用角色默认值。
/// 不访问数据库：只构造 Menu 实例并断言上下文取值。
/// </remarks>
public class DataScopeMenuOverrideTests
{
    [Fact(DisplayName = "菜单数据范围_>=0_覆盖角色范围")]
    public void SetMenu_ExplicitScope_Overrides()
    {
        var ctx = new DataScopeContext { UserId = 1, DataScope = DataScopes.本部门 };
        var menu = new Menu { ID = 101, DataScope = DataScopes.仅本人 };

        ctx.SetMenu(menu);

        Assert.Equal(101, ctx.MenuId);
        Assert.Equal(DataScopes.仅本人, ctx.DataScope);
    }

    [Fact(DisplayName = "菜单数据范围_<0_继承角色范围")]
    public void SetMenu_NegativeScope_Inherits()
    {
        var ctx = new DataScopeContext { UserId = 1, DataScope = DataScopes.本部门 };
        // DataScopes.默认 = -1：表示菜单不覆盖，沿用角色数据范围
        var menu = new Menu { ID = 102, DataScope = DataScopes.默认 };

        ctx.SetMenu(menu);

        Assert.Equal(102, ctx.MenuId);
        Assert.Equal(DataScopes.本部门, ctx.DataScope);
    }
}
