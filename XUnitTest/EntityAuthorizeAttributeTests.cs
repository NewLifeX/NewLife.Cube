using System;
using NewLife.Cube;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>实体授权特性构造与权限位测试（MVC 版）</summary>
public class EntityAuthorizeAttributeTests
{
    [Fact(DisplayName = "无参构造表示只需登录")]
    public void Default_Means_LoginOnly()
    {
        var attribute = new EntityAuthorizeAttribute();

        Assert.Single(attribute.Permissions);
        Assert.Equal(PermissionFlags.None, attribute.Permissions[0]);
    }

    [Fact(DisplayName = "None 权限位表示只需登录，构造不抛异常")]
    public void None_Is_Valid()
    {
        var attribute = new EntityAuthorizeAttribute(PermissionFlags.None);

        Assert.Equal(PermissionFlags.None, attribute.Permission);
        Assert.Single(attribute.Permissions);
        Assert.Equal(PermissionFlags.None, attribute.Permissions[0]);
    }

    [Fact(DisplayName = "All 权限位为合法的全权限组合，构造不抛异常")]
    public void All_Is_Valid()
    {
        var attribute = new EntityAuthorizeAttribute(PermissionFlags.All);

        Assert.Equal(PermissionFlags.All, attribute.Permission);
        Assert.Single(attribute.Permissions);
        Assert.Equal(PermissionFlags.All, attribute.Permissions[0]);
    }

    [Fact(DisplayName = "多个授权项按或语义完整保存")]
    public void Multiple_Permissions_Are_Kept()
    {
        var attribute = new EntityAuthorizeAttribute(PermissionFlags.Insert, PermissionFlags.Update);

        Assert.Equal(2, attribute.Permissions.Length);
        Assert.Equal(PermissionFlags.Insert, attribute.Permissions[0]);
        Assert.Equal(PermissionFlags.Update, attribute.Permissions[1]);
        Assert.Equal(PermissionFlags.Insert, attribute.Permission);
    }

    [Fact(DisplayName = "空授权项数组视为参数错误")]
    public void Empty_Permissions_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new EntityAuthorizeAttribute([]));
    }
}
