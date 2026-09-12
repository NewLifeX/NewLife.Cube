using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Models;
using NewLife.Cube.Tests.Membership;
using NewLife.Model;
using XCode;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>用户页只读规则测试（API 版）。非系统角色在用户页只读，资料编辑统一走用户中心</summary>
/// <remarks>
/// 规则：用户页（/Admin/User）对非系统角色只读——通用更新/新增接口经 Valid 拒绝，
/// 防止普通用户通过管理接口修改自己的角色、部门、启用等管理字段；系统角色不受影响。
/// 复用 <see cref="TenantAuthFixture"/> 的 SQLite 数据（普通用户与系统管理员角色各一）。
/// </remarks>
[Collection("TenantAuth")]
public class UserPageReadOnlyTests
{
    private readonly TenantAuthFixture _fixture;

    public UserPageReadOnlyTests(TenantAuthFixture fixture) => _fixture = fixture;

    /// <summary>当前登录用户桩。ManageProvider.User 静态属性经此返回指定用户</summary>
    private sealed class StubProvider : ManageProvider
    {
        public IManageUser? Mock { get; set; }

        public override IManageUser? GetCurrent(IServiceProvider? context = null) => Mock;

        public override void SetCurrent(IManageUser? user, IServiceProvider? context = null) => Mock = user;
    }

    private static Object? InvokeValid(User user)
    {
        // 不执行构造函数（依赖 DI），Valid 只读守卫仅访问静态当前用户与实体参数
        var controller = (UserController)RuntimeHelpers.GetUninitializedObject(typeof(UserController));

        var method = typeof(UserController).GetMethod("Valid", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UserController 未找到 Valid 方法");

        return method.Invoke(controller, [user, DataObjectMethodType.Update, true]);
    }

    private T RunAs<T>(User user, Func<T> action)
    {
        var old = ManageProvider.Provider;
        try
        {
            ManageProvider.Provider = new StubProvider { Mock = user };
            return action();
        }
        finally
        {
            ManageProvider.Provider = old;
        }
    }

    [Fact(DisplayName = "非系统角色在用户页写入被拒（API版）")]
    public void NonSystemRole_WriteBlocked()
    {
        var ex = RunAs(_fixture.LegacyUser, () => Assert.Throws<TargetInvocationException>(() => InvokeValid(_fixture.LegacyUser)));

        Assert.NotNull(ex.InnerException);
        Assert.Contains("只读", ex.InnerException!.Message);
        Assert.Contains("用户中心", ex.InnerException!.Message);
    }

    [Fact(DisplayName = "系统角色在用户页写入放行（API版）")]
    public void SystemRole_WriteAllowed()
    {
        var rs = RunAs(_fixture.AdminUser, () => InvokeValid(_fixture.AdminUser));

        Assert.True((Boolean)rs!);
    }

    [Fact(DisplayName = "用户控制器重写Valid守卫（API版）")]
    public void Valid_Overridden()
    {
        var method = typeof(UserController).GetMethod("Valid", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.Equal(typeof(UserController), method!.DeclaringType);
    }

    private static Object? InvokeImport(IList<IEntity> list)
    {
        var controller = (UserController)RuntimeHelpers.GetUninitializedObject(typeof(UserController));

        var method = typeof(UserController).GetMethod("OnImport", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UserController 未找到 OnImport 方法");

        return method.Invoke(controller, [User.Meta.Factory, list, new ImportContext()]);
    }

    [Fact(DisplayName = "非系统角色禁止批量导入（API版）")]
    public void NonSystemRole_ImportBlocked()
    {
        var ex = RunAs(_fixture.LegacyUser, () => Assert.Throws<TargetInvocationException>(() => InvokeImport(new List<IEntity>())));

        Assert.NotNull(ex.InnerException);
        Assert.Contains("只读", ex.InnerException!.Message);
    }

    [Fact(DisplayName = "系统角色批量导入不受只读规则影响（API版）")]
    public void SystemRole_ImportAllowed()
    {
        // 空列表直接返回 0，验证系统角色不会命中只读拦截
        var rs = RunAs(_fixture.AdminUser, () => InvokeImport(new List<IEntity>()));

        Assert.Equal(0, (Int32)rs!);
    }

    [Fact(DisplayName = "非系统角色禁止吊销令牌（API版）")]
    public void NonSystemRole_RevokeTokensBlocked()
    {
        var controller = (UserController)RuntimeHelpers.GetUninitializedObject(typeof(UserController));
        var method = typeof(UserController).GetMethod("RevokeTokens", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("UserController 未找到 RevokeTokens 方法");

        var ex = RunAs(_fixture.LegacyUser, () => Assert.Throws<TargetInvocationException>(() => method.Invoke(controller, [1])));

        Assert.NotNull(ex.InnerException);
        Assert.Contains("管理员权限", ex.InnerException!.Message);
    }
}
