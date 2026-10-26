using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using XCode;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// PERM-1 / PERM-2 控制器验证规则测试。覆盖用户页写入权限、密码哈希与角色串清理，
/// 以及角色新增时租户归属填充与 JSON 权限串解析。
/// </summary>
[Collection("TenantAuth")]
public class PermissionControllerTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public PermissionControllerTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 测试宿主
    private sealed class UserControllerHost : UserController
    {
        public UserControllerHost(PasswordService passwordService, ITenantContext tenantContext)
            : base(null!, null!, passwordService, tenantContext) { }

        public Boolean InvokeValid(User entity, DataObjectMethodType type, Boolean post) => base.Valid(entity, type, post);
    }

    private sealed class RoleControllerHost : RoleController
    {
        public Boolean InvokeValid(Role entity, DataObjectMethodType type, Boolean post) => base.Valid(entity, type, post);
    }
    #endregion

    #region 辅助
    private HttpContext Setup(User current, String contentType = "application/json")
    {
        var ctx = _fx.CreateContext();
        ctx.Request.ContentType = contentType;
        ctx.Items["CurrentUser"] = current;
        ctx.RequestServices = _fx.Services;

        return ctx;
    }

    private UserControllerHost CreateUserController(HttpContext ctx) => new(_fx.Provider.PasswordProvider as PasswordService ?? new PasswordService(), new TenantContextService())
    {
        ControllerContext = new ControllerContext { HttpContext = ctx },
    };

    private RoleControllerHost CreateRoleController(HttpContext ctx) => new()
    {
        ControllerContext = new ControllerContext { HttpContext = ctx },
    };
    #endregion

    #region PERM-1 用户管理
    [Fact(DisplayName = "用户页_非系统角色写入被拒绝")]
    public void UserPage_NonSystemRole_Post_Throws()
    {
        var ctx = Setup(_fx.LegacyUser);
        var controller = CreateUserController(ctx);
        var entity = new User { Name = "x", RoleID = _fx.LegacyUser.RoleID, Enable = true };

        Assert.Throws<Exception>(() => controller.InvokeValid(entity, DataObjectMethodType.Update, true));
    }

    [Fact(DisplayName = "用户页_系统角色保存时哈希密码并清理角色串")]
    public void UserPage_AdminUpdate_HashesPasswordAndCleansRoles()
    {
        var ctx = Setup(_fx.AdminUser);
        var controller = CreateUserController(ctx);
        var entity = _fx.LegacyUser2;
        entity.Password = TenantAuthFixture.Password;
        entity.RoleIds = ",1,-1,";

        controller.InvokeValid(entity, DataObjectMethodType.Update, true);

        Assert.NotEqual(TenantAuthFixture.Password, entity.Password);
        Assert.True(entity.Password.Length > 20, $"密码应被哈希，实际[{entity.Password}]");
        Assert.Equal(",1,", entity.RoleIds);
    }

    [Fact(DisplayName = "用户页_读取时清空密码不回传浏览器")]
    public void UserPage_NonPostUpdate_ReturnsNoPassword()
    {
        var ctx = Setup(_fx.AdminUser);
        var controller = CreateUserController(ctx);
        var entity = _fx.LegacyUser2;
        entity.Password = "hashed-value";

        controller.InvokeValid(entity, DataObjectMethodType.Update, false);

        Assert.True(entity.Password.IsNullOrEmpty());
    }
    #endregion

    #region PERM-2 角色管理
    [Fact(DisplayName = "角色新增_租户上下文下自动归属当前租户")]
    public void Role_Insert_FillsTenantId()
    {
        var previous = TenantContext.Current;
        TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };
        try
        {
            var ctx = Setup(_fx.AdminUser);
            var controller = CreateRoleController(ctx);
            var role = new Role { Name = Guid.NewGuid().ToString("N")[..8], Enable = true, TenantId = 0 };

            controller.InvokeValid(role, DataObjectMethodType.Insert, true);

            Assert.Equal(_fx.Tenant1.Id, role.TenantId);
        }
        finally
        {
            TenantContext.Current = previous;
        }
    }

    [Fact(DisplayName = "角色新增_JSON提交解析权限串到权限字典")]
    public void Role_Insert_ParsesPermissionJson()
    {
        var ctx = Setup(_fx.AdminUser);
        var controller = CreateRoleController(ctx);
        var role = new Role
        {
            Name = Guid.NewGuid().ToString("N")[..8],
            Enable = true,
            IsSystem = false,
            Permission = "10#3,11#1",
        };

        controller.InvokeValid(role, DataObjectMethodType.Insert, true);

        Assert.Equal(PermissionFlags.Detail | PermissionFlags.Insert, role.Get(10));
        Assert.Equal(PermissionFlags.Detail, role.Get(11));
    }
    #endregion
}



