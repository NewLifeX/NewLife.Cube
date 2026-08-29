using System;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>角色权限助手单元测试。覆盖权限字符串与权限字典的转换，防止 JSON/API 保存路径清空权限（Issue #80）</summary>
public class RolePermissionHelperTests
{
    [Fact(DisplayName = "Apply_有效权限字符串_权限完整保留")]
    public void Apply_ValidPermissionString_Preserved()
    {
        var role = new Role { Permission = "52#1,53#4" };

        RolePermissionHelper.Apply(role, role.Permission);

        Assert.Equal(2, role.Permissions.Count);
        Assert.Equal(PermissionFlags.Detail, role.Get(52));
        Assert.Equal(PermissionFlags.Update, role.Get(53));
    }

    [Fact(DisplayName = "Apply_权限字符串为空_清空全部权限")]
    public void Apply_EmptyPermissionString_ClearAll()
    {
        var role = new Role { Permission = "52#1" };

        RolePermissionHelper.Apply(role, "");

        Assert.Empty(role.Permissions);
    }

    [Fact(DisplayName = "Apply_新字符串不含旧菜单_移除旧权限")]
    public void Apply_WithoutOldMenu_RemovesOldPermission()
    {
        var role = new Role { Permission = "52#1,53#4" };

        RolePermissionHelper.Apply(role, "52#1");

        Assert.Single(role.Permissions);
        Assert.True(role.Has(52));
        Assert.False(role.Has(53));
    }

    [Fact(DisplayName = "Apply_含非法片段_忽略并保留有效权限")]
    public void Apply_InvalidPart_IgnoreAndKeepValid()
    {
        var role = new Role { Permission = "52#1" };

        RolePermissionHelper.Apply(role, "52#1,abc,53#,54#0");

        Assert.Single(role.Permissions);
        Assert.True(role.Has(52));
        Assert.False(role.Has(53));
    }
}
