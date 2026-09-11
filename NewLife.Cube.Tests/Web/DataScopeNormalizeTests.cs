using System;
using System.Reflection;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>数据权限上下文规范化单元测试。覆盖未分配部门用户退化为"仅本人"，避免生成 DepartmentID=-1 恒假条件</summary>
public class DataScopeNormalizeTests
{
    [Fact(DisplayName = "Normalize_无部门加本部门范围_退化为仅本人")]
    public void Normalize_NoDepartment_DepartmentScope_DegradesToSelf()
    {
        var scope = new DataScopeContext
        {
            UserId = 112,
            DepartmentId = -1,
            DataScope = DataScopes.本部门,
            AccessibleDepartmentIds = [],
        };

        WebMiddleware.DataScopeMiddleware.Normalize(scope);

        Assert.Equal(DataScopes.仅本人, scope.DataScope);
        Assert.Empty(scope.AccessibleDepartmentIds!);
    }

    [Fact(DisplayName = "Normalize_无部门加本部门及下级范围_退化为仅本人")]
    public void Normalize_NoDepartment_DeptAndChildrenScope_DegradesToSelf()
    {
        var scope = new DataScopeContext
        {
            UserId = 112,
            DepartmentId = 0,
            DataScope = DataScopes.本部门及下级,
            AccessibleDepartmentIds = [],
        };

        WebMiddleware.DataScopeMiddleware.Normalize(scope);

        Assert.Equal(DataScopes.仅本人, scope.DataScope);
    }

    [Fact(DisplayName = "Normalize_有部门加本部门范围_保持不变")]
    public void Normalize_HasDepartment_KeepsScope()
    {
        var scope = new DataScopeContext
        {
            UserId = 112,
            DepartmentId = 8,
            DataScope = DataScopes.本部门,
            AccessibleDepartmentIds = [8],
        };

        WebMiddleware.DataScopeMiddleware.Normalize(scope);

        Assert.Equal(DataScopes.本部门, scope.DataScope);
        Assert.Equal(new Int32[] { 8 }, scope.AccessibleDepartmentIds!);
    }

    [Fact(DisplayName = "Normalize_无部门加自定义且已配置数据部门_保持不变")]
    public void Normalize_NoDepartment_CustomWithDepartments_KeepsScope()
    {
        var scope = new DataScopeContext
        {
            UserId = 112,
            DepartmentId = -1,
            DataScope = DataScopes.自定义,
            AccessibleDepartmentIds = [3, 5],
        };

        WebMiddleware.DataScopeMiddleware.Normalize(scope);

        Assert.Equal(DataScopes.自定义, scope.DataScope);
        Assert.Equal(new Int32[] { 3, 5 }, scope.AccessibleDepartmentIds!);
    }

    [Fact(DisplayName = "Normalize_系统角色全部范围_保持不变")]
    public void Normalize_SystemRole_KeepsAllScope()
    {
        var scope = new DataScopeContext
        {
            UserId = 1,
            DepartmentId = -1,
            DataScope = DataScopes.全部,
            AccessibleDepartmentIds = null,
        };

        WebMiddleware.DataScopeMiddleware.Normalize(scope);

        Assert.Equal(DataScopes.全部, scope.DataScope);
        Assert.Null(scope.AccessibleDepartmentIds);
    }

    [Fact(DisplayName = "Normalize_本就仅本人_保持不变")]
    public void Normalize_SelfScope_Unchanged()
    {
        var scope = new DataScopeContext
        {
            UserId = 112,
            DepartmentId = -1,
            DataScope = DataScopes.仅本人,
            AccessibleDepartmentIds = [],
        };

        WebMiddleware.DataScopeMiddleware.Normalize(scope);

        Assert.Equal(DataScopes.仅本人, scope.DataScope);
    }

    [Fact(DisplayName = "Normalize_空上下文_不抛异常")]
    public void Normalize_NullScope_NoThrow()
    {
        WebMiddleware.DataScopeMiddleware.Normalize(null!);
    }

    [Fact(DisplayName = "AttachmentController_标注数据权限_仅限本人附件")]
    public void AttachmentController_HasSelfOnlyDataPermission()
    {
        var att = typeof(Areas.Cube.Controllers.AttachmentController)
            .GetCustomAttribute<DataPermissionAttribute>();

        Assert.NotNull(att);
        Assert.Equal("CreateUserID={#userId}", att!.Expression);
    }
}
