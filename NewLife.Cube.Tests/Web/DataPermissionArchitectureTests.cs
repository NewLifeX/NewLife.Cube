using System;
using System.Reflection;
using NewLife.Cube.WebMiddleware;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>数据权限架构测试（API 版）。数据权限统一由接口层控制器特性承担，实体层以系统身份运行</summary>
/// <remarks>
/// 架构约定：
/// 1. 页面行级数据权限 = 控制器 [DataPermission] 特性，经 SearchData/FindData 管道显式执行；
/// 2. XCode 实体层不承担数据权限，魔方宿主注入系统态上下文使数据权限拦截器休眠；
/// 3. 业务代码（SSO/服务/任务）查询实体不受数据权限影响。
/// </remarks>
public class DataPermissionArchitectureTests
{
    [Theory(DisplayName = "控制器_数据权限特性_表达式符合页面行语义")]
    [InlineData(typeof(Areas.Admin.Controllers.DepartmentController), "ManagerID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.ParameterController), "UserID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.UserOnlineController), "UserID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.LogController), "CreateUserID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.UserController), "ID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.UserConnectController), "UserID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.UserTokenController), "UserID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.OAuthLogController), "UserId={#userId}")]
    [InlineData(typeof(Areas.Cube.Controllers.AttachmentController), "CreateUserID={#userId}")]
    [InlineData(typeof(Areas.Cube.Controllers.PrincipalAgentController), "PrincipalId={#userId} or AgentId={#userId}")]
    public void Controllers_HaveExpectedDataPermission(Type controllerType, String expression)
    {
        var att = controllerType.GetCustomAttribute<DataPermissionAttribute>();

        Assert.NotNull(att);
        Assert.Equal(expression, att!.Expression);
    }

    [Fact(DisplayName = "宿主上下文_系统态_保留用户身份用于审计填充")]
    public void CreateHostScope_WithUser_IsSystemAndKeepsIdentity()
    {
        var user = new User { ID = 5, DepartmentID = 3 };

        var scope = DataScopeMiddleware.CreateHostScope(user);

        Assert.True(scope.IsSystem);
        Assert.Equal(5, scope.UserId);
        Assert.Equal(3, scope.DepartmentId);
    }

    [Fact(DisplayName = "宿主上下文_匿名_同样为系统态")]
    public void CreateHostScope_Anonymous_IsSystem()
    {
        var scope = DataScopeMiddleware.CreateHostScope(null!);

        Assert.True(scope.IsSystem);
        Assert.Equal(0, scope.UserId);
        Assert.Equal(0, scope.DepartmentId);
    }
}
