using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Models;
using NewLife.Model;
using XCode;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>用户页只读规则测试。非系统角色在用户页只读（仅可查看本人），资料编辑统一走用户中心</summary>
/// <remarks>
/// 规则：用户页（/Admin/User）对非系统角色只读——编辑入口重定向到用户中心（Info），
/// 所有写入（新增/修改/删除/批量启用禁用）经 Valid 一律拒绝，防止普通用户通过管理表单修改自己的角色、部门、启用等管理字段。
/// 系统角色不受影响（管理端仍可正常编辑）。
/// </remarks>
[Collection("SqliteDb")]
public class UserPageReadOnlyTests : IDisposable
{
    private readonly IManageProvider? _oldProvider;

    public UserPageReadOnlyTests()
    {
        SqliteDb.Ensure();

        _oldProvider = ManageProvider.Provider;
    }

    public void Dispose() => ManageProvider.Provider = _oldProvider;

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

    /// <summary>创建指定角色类型的测试用户。角色落库，用户对象未保存（Roles 按 RoleID 实时解析）</summary>
    /// <param name="isSystem">是否系统角色</param>
    /// <returns></returns>
    private static User CreateUser(Boolean isSystem)
    {
        var role = new Role { Name = $"测试角色_{(isSystem ? "系统" : "普通")}_{Guid.NewGuid():N}", Enable = true, IsSystem = isSystem };
        role.Insert();

        Role.Meta.Session.ClearCache("UserPageReadOnlyTests", true);

        return new User { Name = $"测试用户_{Guid.NewGuid():N}", RoleID = role.ID };
    }

    [Fact(DisplayName = "非系统角色在用户页写入被拒")]
    public void NonSystemRole_WriteBlocked()
    {
        var user = CreateUser(false);
        ManageProvider.Provider = new StubProvider { Mock = user };

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeValid(user));
        Assert.NotNull(ex.InnerException);
        Assert.Contains("只读", ex.InnerException!.Message);
        Assert.Contains("用户中心", ex.InnerException!.Message);
    }

    [Fact(DisplayName = "系统角色在用户页写入放行")]
    public void SystemRole_WriteAllowed()
    {
        var user = CreateUser(true);
        ManageProvider.Provider = new StubProvider { Mock = user };

        Assert.True((Boolean)InvokeValid(user)!);
    }

    private static Object? InvokeImport(IList<IEntity> list)
    {
        var controller = (UserController)RuntimeHelpers.GetUninitializedObject(typeof(UserController));

        var method = typeof(UserController).GetMethod("OnImport", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UserController 未找到 OnImport 方法");

        return method.Invoke(controller, [User.Meta.Factory, list, new ImportContext()]);
    }

    [Fact(DisplayName = "非系统角色禁止批量导入（旁路写入同样被拒）")]
    public void NonSystemRole_ImportBlocked()
    {
        var user = CreateUser(false);
        ManageProvider.Provider = new StubProvider { Mock = user };

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeImport(new List<IEntity>()));
        Assert.NotNull(ex.InnerException);
        Assert.Contains("只读", ex.InnerException!.Message);
    }

    [Fact(DisplayName = "系统角色批量导入不受只读规则影响")]
    public void SystemRole_ImportAllowed()
    {
        var user = CreateUser(true);
        ManageProvider.Provider = new StubProvider { Mock = user };

        // 空列表直接返回 0，验证系统角色不会命中只读拦截
        Assert.Equal(0, (Int32)InvokeImport(new List<IEntity>())!);
    }

    [Fact(DisplayName = "非系统角色禁止吊销令牌")]
    public void NonSystemRole_RevokeTokensBlocked()
    {
        var user = CreateUser(false);
        ManageProvider.Provider = new StubProvider { Mock = user };

        var controller = (UserController)RuntimeHelpers.GetUninitializedObject(typeof(UserController));
        var method = typeof(UserController).GetMethod("RevokeTokens", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("UserController 未找到 RevokeTokens 方法");

        var ex = Assert.Throws<TargetInvocationException>(() => method.Invoke(controller, [1]));
        Assert.NotNull(ex.InnerException);
        Assert.Contains("管理员权限", ex.InnerException!.Message);
    }

    [Fact(DisplayName = "用户控制器重写编辑入口，管理表单仅系统角色可用")]
    public void Edit_Overridden()
    {
        var method = typeof(UserController).GetMethod("Edit", BindingFlags.Public | BindingFlags.Instance, null, [typeof(String)], null);

        Assert.NotNull(method);
        Assert.Equal(typeof(UserController), method!.DeclaringType);
    }
}
