using System;
using System.Reflection;
using NewLife.Cube.Entity;
using NewLife.Cube.WebMiddleware;
using XCode;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>数据权限架构测试（API 版）。数据权限统一由接口层承担，实体层以系统身份运行</summary>
/// <remarks>
/// 架构约定（OSC-2608273d95 合并后）：
/// 1. 页面行级数据权限 = 接口层 DataScopeHelper.GetFilter/CanAccess，按当前用户上下文在查询/详情/写入管道显式执行；
/// 2. XCode 实体层不承担数据权限，魔方宿主注入系统态上下文使数据权限拦截器休眠；
/// 3. 业务代码（SSO/服务/任务）查询实体不受数据权限影响；
/// 4. 敏感字段脱敏上下文必须取真实用户，宿主系统态 ViewSensitive 为 false 会把所有人的敏感字段一起脱敏。
/// </remarks>
public class DataPermissionArchitectureTests
{
    [Theory(DisplayName = "控制器_行权已改由 DataScope 承担_不再声明仅本人特性")]
    [InlineData(typeof(Areas.Admin.Controllers.UserController))]
    [InlineData(typeof(Areas.Admin.Controllers.LogController))]
    [InlineData(typeof(Areas.Admin.Controllers.UserOnlineController))]
    [InlineData(typeof(Areas.Admin.Controllers.UserConnectController))]
    [InlineData(typeof(Areas.Admin.Controllers.UserTokenController))]
    [InlineData(typeof(Areas.Admin.Controllers.OAuthLogController))]
    public void Controllers_DoNotDeclareSelfOnlyDataPermission(Type controllerType)
    {
        // 这6个控制器的实体都实现了 IUserScope（User 为 IDataScope），
        // 保留 [DataPermission(null, "UserID={#userId}")] 会把「本部门/下级/自定义」压成「仅本人」
        Assert.Null(controllerType.GetCustomAttribute<DataPermissionAttribute>());
    }

    [Theory(DisplayName = "控制器_数据权限特性_表达式符合页面行语义")]
    [InlineData(typeof(Areas.Admin.Controllers.DepartmentController), "ManagerID={#userId}")]
    [InlineData(typeof(Areas.Admin.Controllers.ParameterController), "UserID={#userId}")]
    [InlineData(typeof(Areas.Cube.Controllers.AttachmentController), "CreateUserID={#userId}")]
    [InlineData(typeof(Areas.Cube.Controllers.PrincipalAgentController), "PrincipalId={#userId} or AgentId={#userId}")]
    public void Controllers_HaveExpectedDataPermission(Type controllerType, String expression)
    {
        var att = controllerType.GetCustomAttribute<DataPermissionAttribute>();

        Assert.NotNull(att);
        Assert.Equal(expression, att!.Expression);
    }

    [Theory(DisplayName = "实体_实现归属接口_供 DataScope 解析归属列")]
    [InlineData(typeof(UserToken))]
    [InlineData(typeof(UserOnline))]
    [InlineData(typeof(UserConnect))]
    [InlineData(typeof(OAuthLog))]
    [InlineData(typeof(NotificationRecord))]
    public void Entities_ImplementUserScope(Type entityType)
    {
        Assert.True(typeof(IUserScope).IsAssignableFrom(entityType));
        Assert.True(typeof(IDataScopeFieldProvider).IsAssignableFrom(entityType));
    }

    [Fact(DisplayName = "行权_仅本人_按归属用户列过滤")]
    public void GetFilter_SelfScope_UsesOwnerColumn()
    {
        var ctx = new DataScopeContext { UserId = 7, DataScope = DataScopes.仅本人 };

        AssertFilterContains(Entity<UserToken>.Meta.Factory, ctx, "UserID");
        AssertFilterContains(Entity<UserOnline>.Meta.Factory, ctx, "UserID");
        AssertFilterContains(Entity<UserConnect>.Meta.Factory, ctx, "UserID");
        AssertFilterContains(Entity<OAuthLog>.Meta.Factory, ctx, "UserId");
        AssertFilterContains(Entity<NotificationRecord>.Meta.Factory, ctx, "UserId");
    }

    [Fact(DisplayName = "行权_宿主系统态_不过滤_且 ViewSensitive 为假")]
    public void GetFilter_HostScope_ReturnsNull()
    {
        var scope = DataScopeMiddleware.CreateHostScope(new User { ID = 1 });

        Assert.Null(DataScopeHelper.GetFilter(Entity<UserToken>.Meta.Factory, scope));
        // 宿主态并非「有权限看敏感字段」，不能拿它当脱敏判定上下文
        Assert.False(scope.ViewSensitive);
    }

    [Fact(DisplayName = "脱敏_他人数据_未授 ViewSensitive_置掩码")]
    public void MaskSensitiveFields_Others_WithoutViewSensitive_Masked()
    {
        var ctx = new DataScopeContext { UserId = 1, DataScope = DataScopes.仅本人 };
        var other = new User { ID = 2, Password = "secret" };

        FieldScopeHelper.MaskSensitiveFields(other, ctx);

        Assert.Equal("***", other.Password);
    }

    [Fact(DisplayName = "脱敏_本人数据_不遮蔽")]
    public void MaskSensitiveFields_Self_NotMasked()
    {
        var ctx = new DataScopeContext { UserId = 2, DataScope = DataScopes.仅本人 };
        var self = new User { ID = 2, Password = "secret" };

        FieldScopeHelper.MaskSensitiveFields(self, ctx);

        Assert.Equal("secret", self.Password);
    }

    [Fact(DisplayName = "脱敏_已授 ViewSensitive_不遮蔽")]
    public void MaskSensitiveFields_ViewSensitive_NotMasked()
    {
        var ctx = new DataScopeContext { UserId = 1, ViewSensitive = true };
        var other = new User { ID = 2, Password = "secret" };

        FieldScopeHelper.MaskSensitiveFields(other, ctx);

        Assert.Equal("secret", other.Password);
    }

    static void AssertFilterContains(IEntityFactory factory, DataScopeContext ctx, String column)
    {
        var exp = DataScopeHelper.GetFilter(factory, ctx);

        Assert.NotNull(exp);
        Assert.Contains(column, exp!.ToString());
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
