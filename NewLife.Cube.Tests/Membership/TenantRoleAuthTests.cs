using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Areas.Admin.Models;
using XCode;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Membership;

/// <summary>
/// 多租户角色授权测试：验证 <see cref="RoleController"/> 在租户上下文中，
/// 1) 强制填充新建角色的租户标识（TenantId）；
/// 2) 禁止租户管理员越界授权（只能授予自己拥有的权限位）。
/// </summary>
[Collection("TenantAuth")]
public class TenantRoleAuthTests
{
    private readonly TenantAuthFixture _fx;

    public TenantRoleAuthTests(TenantAuthFixture fx) => _fx = fx;

    /// <summary>暴露受保护的 Valid 以便测试直接驱动</summary>
    private sealed class TestRoleController : RoleController
    {
        public Boolean Run(Role entity, DataObjectMethodType type) => Valid(entity, type, true);
    }

    /// <summary>创建一个带四种权限（查看/添加/修改/删除）的叶子菜单</summary>
    private static IMenu CreateMenu(String name)
    {
        var menu = Menu.Root.Add(name, name, "NewLife.Cube.Tests." + name, "/" + name);
        menu.Permissions[(Int32)PermissionFlags.Detail] = "查看";
        menu.Permissions[(Int32)PermissionFlags.Insert] = "添加";
        menu.Permissions[(Int32)PermissionFlags.Update] = "修改";
        menu.Permissions[(Int32)PermissionFlags.Delete] = "删除";
        ((IEntity)menu).Save();

        return menu;
    }

    [Fact]
    public void TenantAdmin_Insert_ForceFills_TenantId_And_Blocks_Escalation()
    {
        var tag = "TR" + DateTime.Now.Ticks;
        var menu = CreateMenu(tag + "Menu");

        // 客户管理员角色：只拥有 查看+添加 权限
        var adminRole = new Role { Name = tag + "客户管理员", Enable = true };
        adminRole.Insert();
        adminRole.Set(menu.ID, PermissionFlags.Detail | PermissionFlags.Insert);
        adminRole.Save();

        var user = new User
        {
            Name = tag + "cadmin",
            DisplayName = tag + "cadmin",
            RoleID = adminRole.ID,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        var ctx = _fx.CreateContext();
        ctx.Items["CurrentUser"] = user;
        // 表单勾选全部四种权限，尝试越界授权
        ctx.Request.QueryString = new QueryString(
            $"?p{menu.ID}=true&pf{menu.ID}_{(Int32)PermissionFlags.Detail}=true" +
            $"&pf{menu.ID}_{(Int32)PermissionFlags.Insert}=true" +
            $"&pf{menu.ID}_{(Int32)PermissionFlags.Update}=true" +
            $"&pf{menu.ID}_{(Int32)PermissionFlags.Delete}=true");

        var oldTenant = TenantContext.Current;
        TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };
        try
        {
            var role = new Role { Name = tag + "租户角色", Enable = true };
            var controller = new TestRoleController
            {
                ControllerContext = new ControllerContext { HttpContext = ctx }
            };

            controller.Run(role, DataObjectMethodType.Insert);

            // 需求2：强制填充租户标识
            Assert.Equal(_fx.Tenant1.Id, role.TenantId);

            // 需求3：只能授予自己拥有的权限，越界的修改/删除被拦截
            Assert.True(role.Has(menu.ID, PermissionFlags.Detail));
            Assert.True(role.Has(menu.ID, PermissionFlags.Insert));
            Assert.False(role.Has(menu.ID, PermissionFlags.Update));
            Assert.False(role.Has(menu.ID, PermissionFlags.Delete));
        }
        finally
        {
            TenantContext.Current = oldTenant!;
        }
    }

    [Fact]
    public void SystemAdmin_Is_Not_Restricted_In_Tenant_Context()
    {
        var tag = "TS" + DateTime.Now.Ticks;
        var menu = CreateMenu(tag + "Menu");

        var ctx = _fx.CreateContext();
        // 系统管理员用户（角色 IsSystem=true）
        ctx.Items["CurrentUser"] = _fx.AdminUser;
        ctx.Request.QueryString = new QueryString(
            $"?p{menu.ID}=true&pf{menu.ID}_{(Int32)PermissionFlags.Detail}=true" +
            $"&pf{menu.ID}_{(Int32)PermissionFlags.Insert}=true" +
            $"&pf{menu.ID}_{(Int32)PermissionFlags.Update}=true" +
            $"&pf{menu.ID}_{(Int32)PermissionFlags.Delete}=true");

        var oldTenant = TenantContext.Current;
        TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };
        try
        {
            var role = new Role { Name = tag + "角色", Enable = true };
            var controller = new TestRoleController
            {
                ControllerContext = new ControllerContext { HttpContext = ctx }
            };

            controller.Run(role, DataObjectMethodType.Insert);

            // 系统管理员即使在租户上下文中也不受限，可授予全部权限
            Assert.True(role.Has(menu.ID, PermissionFlags.Detail));
            Assert.True(role.Has(menu.ID, PermissionFlags.Insert));
            Assert.True(role.Has(menu.ID, PermissionFlags.Update));
            Assert.True(role.Has(menu.ID, PermissionFlags.Delete));
        }
        finally
        {
            TenantContext.Current = oldTenant!;
        }
    }

    [Fact]
    public void TenantAdmin_Insert_Json_Blocks_Escalation()
    {
        var tag = "TRJ" + DateTime.Now.Ticks;
        var menu = CreateMenu(tag + "Menu");

        // 客户管理员角色：只拥有 查看+添加 权限
        var adminRole = new Role { Name = tag + "客户管理员", Enable = true };
        adminRole.Insert();
        adminRole.Set(menu.ID, PermissionFlags.Detail | PermissionFlags.Insert);
        adminRole.Save();

        var user = new User
        {
            Name = tag + "cadmin",
            DisplayName = tag + "cadmin",
            RoleID = adminRole.ID,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        var ctx = _fx.CreateContext();
        ctx.Items["CurrentUser"] = user;
        // JSON 请求：通过权限字符串尝试授予全部权限
        ctx.Request.ContentType = "application/json";

        var oldTenant = TenantContext.Current;
        TenantContext.Current = new TenantContext { TenantId = _fx.Tenant1.Id };
        try
        {
            // 全部权限位组合（15），模拟尝试授予 查看+添加+修改+删除
            var allFlags = (Int32)(PermissionFlags.Detail | PermissionFlags.Insert | PermissionFlags.Update | PermissionFlags.Delete);
            var role = new Role { Name = tag + "租户角色", Enable = true, Permission = $"{menu.ID}#{allFlags}" };
            var controller = new TestRoleController
            {
                ControllerContext = new ControllerContext { HttpContext = ctx }
            };

            controller.Run(role, DataObjectMethodType.Insert);

            // 强制填充租户标识
            Assert.Equal(_fx.Tenant1.Id, role.TenantId);

            // 越界的 修改/删除 被拦截
            Assert.True(role.Has(menu.ID, PermissionFlags.Detail));
            Assert.True(role.Has(menu.ID, PermissionFlags.Insert));
            Assert.False(role.Has(menu.ID, PermissionFlags.Update));
            Assert.False(role.Has(menu.ID, PermissionFlags.Delete));
        }
        finally
        {
            TenantContext.Current = oldTenant!;
        }
    }

    [Fact]
    public void TenantAdmin_Update_KeepsUnownedPermissions()
    {
        var tag = "TRU" + DateTime.Now.Ticks;
        var menu = CreateMenu(tag + "Menu");

        // 客户管理员角色：只拥有 查看 权限
        var adminRole = new Role { Name = tag + "客户管理员", Enable = true };
        adminRole.Insert();
        adminRole.Set(menu.ID, PermissionFlags.Detail);
        adminRole.Save();

        var user = new User
        {
            Name = tag + "cadmin",
            DisplayName = tag + "cadmin",
            RoleID = adminRole.ID,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        // 目标角色已有 查看+添加（添加由系统管理员授予）
        var role = new Role { Name = tag + "租户角色", Enable = true };
        role.Insert();
        role.Set(menu.ID, PermissionFlags.Detail | PermissionFlags.Insert);
        role.Save();

        // 租户管理员以空权限字符串提交：受限模式只回收自己拥有的 查看，系统授予的 添加 保留
        RolePermissionHelper.Apply(role, "", user, true);

        Assert.False(role.Has(menu.ID, PermissionFlags.Detail));
        Assert.True(role.Has(menu.ID, PermissionFlags.Insert));
    }
}
