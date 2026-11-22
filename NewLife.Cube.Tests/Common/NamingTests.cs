using System;
using System.Reflection;
using NewLife.Cube.Entity;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>内置实体命名规范测试</summary>
/// <remarks>
/// Issue #177：主键 Id、外键 XxxId、审计 CreateUserId/UpdateUserId。
/// Cube 自有实体不再保留旧名兼容别名，下游升级时以编译错误暴露，避免编译通过但运行时字段名不匹配。
/// </remarks>
public class NamingTests
{
    [Theory(DisplayName = "Cube 内置实体使用 Id 且不保留旧名别名")]
    [InlineData(typeof(OAuthConfig))]
    [InlineData(typeof(UserToken))]
    [InlineData(typeof(UserOnline))]
    [InlineData(typeof(UserStat))]
    [InlineData(typeof(UserConnect))]
    public void CubeEntitiesUseIdOnly(Type type)
    {
        Assert.NotNull(type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(type.GetProperty("ID", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(type.GetProperty("UserID", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(type.GetProperty("CreateUserID", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(type.GetProperty("UpdateUserID", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact(DisplayName = "Membership 内置实体字段使用 Id 命名")]
    public void MembershipEntitiesUseId()
    {
        Assert.Equal("Id", User.__.Id);
        Assert.Equal("Id", Department.__.Id);
        Assert.Equal("ParentId", Department.__.ParentId);
        Assert.Equal("RoleId", User.__.RoleId);
        Assert.Equal("DepartmentId", User.__.DepartmentId);
        Assert.Equal("UpdateUserId", User.__.UpdateUserId);
    }

    [Fact(DisplayName = "Membership 接口使用 Id 成员")]
    public void MembershipInterfacesUseId()
    {
        Assert.NotNull(typeof(IUser).GetProperty("Id"));
        Assert.NotNull(typeof(IMenu).GetProperty("Id"));
        Assert.NotNull(typeof(IMenu).GetProperty("ParentId"));
    }
}
