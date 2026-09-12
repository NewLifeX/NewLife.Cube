using System;
using System.Reflection;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Entity;
using NewLife.Cube.WebMiddleware;
using XCode;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>数据权限去耦测试（MVC 版）。实体层不承担数据权限，页面行权限由控制器特性表达</summary>
/// <remarks>
/// 架构约定：
/// 1. 页面行级数据权限 = 控制器 [DataPermission] 特性，经 SearchData/FindData 管道显式执行；
/// 2. 实体不注册 DataScopeInterceptor，魔方宿主注入系统态上下文使拦截器休眠；
/// 3. 跨用户业务查询（SSO openid 查重）在任意数据权限上下文下均命中，实体层不得过滤。
/// </remarks>
[Collection("SqliteDb")]
public class DataScopeDecouplingTests
{
    public DataScopeDecouplingTests() => SqliteDb.Ensure();

    [Theory(DisplayName = "控制器_数据权限特性_表达式符合页面行语义")]
    [InlineData(typeof(DepartmentController), "ManagerID={#userId}")]
    [InlineData(typeof(ParameterController), "UserID={#userId}")]
    [InlineData(typeof(UserOnlineController), "UserID={#userId}")]
    [InlineData(typeof(LogController), "CreateUserID={#userId}")]
    [InlineData(typeof(UserController), "ID={#userId}")]
    [InlineData(typeof(UserConnectController), "UserID={#userId}")]
    [InlineData(typeof(UserTokenController), "UserID={#userId}")]
    [InlineData(typeof(OAuthLogController), "UserId={#userId}")]
    [InlineData(typeof(NotificationRecordController), "UserId={#userId}")]
    [InlineData(typeof(NewLife.Cube.Areas.Cube.Controllers.AttachmentController), "CreateUserID={#userId}")]
    [InlineData(typeof(NewLife.Cube.Areas.Cube.Controllers.PrincipalAgentController), "PrincipalId={#userId} or AgentId={#userId}")]
    public void Controllers_HaveExpectedDataPermission(Type controllerType, String expression)
    {
        var att = controllerType.GetCustomAttribute<DataPermissionAttribute>();

        Assert.NotNull(att);
        Assert.Equal(expression, att!.Expression);
    }

    [Fact(DisplayName = "实体层_不注册数据权限拦截器_实体保持纯数据访问")]
    public void Entities_DoNotRegisterDataScopeInterceptor()
    {
        // 触发实体静态构造与拦截器异步注册（注册内部走 Task.Run + 短等待），留出静默期后统一断言不存在
        _ = new UserConnect();
        _ = new UserToken();
        _ = new OAuthLog();
        _ = new NotificationRecord();

        System.Threading.Thread.Sleep(300);

        Assert.DoesNotContain(Entity<UserConnect>.Meta.Interceptors.Interceptors, e => e is DataScopeInterceptor);
        Assert.DoesNotContain(Entity<UserToken>.Meta.Interceptors.Interceptors, e => e is DataScopeInterceptor);
        Assert.DoesNotContain(Entity<OAuthLog>.Meta.Interceptors.Interceptors, e => e is DataScopeInterceptor);
        Assert.DoesNotContain(Entity<NotificationRecord>.Meta.Interceptors.Interceptors, e => e is DataScopeInterceptor);
    }

    [Fact(DisplayName = "跨用户查询_任意数据权限上下文_实体层均不过滤")]
    public void CrossUserQuery_NotFilteredByEntityLayer()
    {
        var provider = $"E2E_{Guid.NewGuid():N}";
        var uc111 = new UserConnect { Provider = provider, OpenID = "openid_111", UserID = 111, Enable = true };
        var uc222 = new UserConnect { Provider = provider, OpenID = "openid_222", UserID = 222, Enable = true };

        var old = DataScopeContext.Current;
        try
        {
            uc111.Insert();
            uc222.Insert();

            // (a) 无上下文
            DataScopeContext.Current = null;
            var found = UserConnect.FindByProviderAndOpenID(provider, "openid_222");
            Assert.NotNull(found);
            Assert.Equal(222, found!.UserID);

            // (b) 宿主系统态
            DataScopeContext.Current = DataScopeMiddleware.CreateHostScope(null!);
            Assert.NotNull(UserConnect.FindByProviderAndOpenID(provider, "openid_222"));

            // (c) 窄上下文（仅本人=111）：实体层不参与过滤，跨用户查询仍命中（严禁在此加数据范围）
            DataScopeContext.Current = new DataScopeContext { UserId = 111, DataScope = DataScopes.仅本人 };
            Assert.NotNull(UserConnect.FindByProviderAndOpenID(provider, "openid_222"));
        }
        finally
        {
            DataScopeContext.Current = old;

            foreach (var item in UserConnect.FindAll(UserConnect._.Provider == provider))
            {
                item.Delete();
            }
        }
    }
}
