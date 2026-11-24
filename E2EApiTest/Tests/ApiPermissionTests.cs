using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>权限与授权 E2E：未授权拦截、角色权限位、菜单过滤</summary>
public sealed class ApiPermissionTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-PERM-001 无查看权限的用户访问列表返回 HTTP 403")]
    [Trait("Category", "Permission")]
    public async Task TC_API_PERM_001_NoPermission()
    {
        var userName = NewName("noperm");
        var roleName = NewName("nopermrole");
        try
        {
            var roleId = await CreateRoleAsync(roleName, "16#1");
            await CreateUserAsync(userName, roleId);

            using var client = new ApiClient();
            await client.LoginAsync(userName, "Test@123");

            var rs = await client.GetAsync("/api/School/Student");
            ApiAssert.HttpStatus(rs, 403, "TC-API-PERM-001");
            ApiAssert.Fail(rs, 403, "TC-API-PERM-001");

            var obj = rs.Json as JsonObject;
            Assert.NotNull(obj);
            Assert.Equal(2, obj!.Count);
            Assert.NotNull(obj["code"]);
            Assert.NotNull(obj["message"]);
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserByName(userName);
            ApiDatabaseHelper.DeleteRoleByName(roleName);
        }
    }

    [Fact(DisplayName = "TC-API-PERM-002 只授予查看权限时可查询但不能新增")]
    [Trait("Category", "Permission")]
    public async Task TC_API_PERM_002_DetailOnly()
    {
        var userName = NewName("detailonly");
        var roleName = NewName("detailrole");
        try
        {
            var meta = await Admin.GetAsync("/api/School/Student/meta");
            ApiAssert.Ok(meta, "TC-API-PERM-002");
            var menuId = ApiResult.GetInt32(meta.Data?["setting"]?["menuId"]);
            Assert.True(menuId > 0, "meta 未下发 menuId");

            var roleId = await CreateRoleAsync(roleName, $"{menuId}#1");
            await CreateUserAsync(userName, roleId);

            using var client = new ApiClient();
            await client.LoginAsync(userName, "Test@123");

            var list = await client.GetAsync("/api/School/Student");
            ApiAssert.Ok(list, "TC-API-PERM-002");

            var insert = await client.PostJsonAsync("/api/School/Student", Json(new { name = NewName("nope") }));
            ApiAssert.HttpStatus(insert, 403, "TC-API-PERM-002");
            ApiAssert.Fail(insert, 403, "TC-API-PERM-002");
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserByName(userName);
            ApiDatabaseHelper.DeleteRoleByName(roleName);
        }
    }

    [Fact(DisplayName = "TC-API-PERM-003 菜单树按权限过滤，不暴露未授权模块")]
    [Trait("Category", "Permission")]
    public async Task TC_API_PERM_003_MenuTreeFiltered()
    {
        var userName = NewName("menufilter");
        var roleName = NewName("menurole");
        try
        {
            var roleId = await CreateRoleAsync(roleName, "16#1");
            await CreateUserAsync(userName, roleId);

            using var client = new ApiClient();
            await client.LoginAsync(userName, "Test@123");

            var rs = await client.GetAsync("/Cube/MenuTree");
            ApiAssert.Ok(rs, "TC-API-PERM-003");

            Assert.DoesNotContain("/School/Student", rs.Content);
            Assert.DoesNotContain("/Admin/User", rs.Content);
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserByName(userName);
            ApiDatabaseHelper.DeleteRoleByName(roleName);
        }
    }

    [Fact(DisplayName = "TC-API-PERM-004 角色权限树可读取且保存权限生效")]
    [Trait("Category", "Permission")]
    public async Task TC_API_PERM_004_PermissionTree()
    {
        var roleName = NewName("permrole");
        var roleId = 0;
        try
        {
            var role = await Admin.PostJsonAsync("/api/Admin/Role", Json(new { name = roleName, type = 2, enable = true }));
            ApiAssert.Ok(role, "TC-API-PERM-004");
            roleId = ApiResult.GetInt32(role.Data?["id"]) ?? 0;
            Assert.True(roleId > 0, "新增角色未返回主键");

            var tree = await Admin.GetAsync($"/api/Admin/Role/PermissionTree?roleId={roleId}");
            ApiAssert.Ok(tree, "TC-API-PERM-004");
            Assert.Equal(roleId, ApiResult.GetInt32(tree.Data?["role"]?["id"]));
            Assert.True((tree.Data?["tree"] as JsonArray)?.Count > 0, "权限树为空");

            var meta = await Admin.GetAsync("/api/School/Student/meta");
            ApiAssert.Ok(meta, "TC-API-PERM-004");
            var menuId = ApiResult.GetInt32(meta.Data?["setting"]?["menuId"]);
            Assert.True(menuId > 0);

            var save = await Admin.PostJsonAsync("/api/Admin/Role/SavePermission", Json(new { roleId, permission = $"{menuId}#1" }));
            ApiAssert.Ok(save, "TC-API-PERM-004");
            Assert.True(ApiResult.GetBoolean(save.Data?["success"]) == true, "保存权限未返回成功");

            var detail = await Admin.GetAsync($"/api/Admin/Role/{roleId}");
            ApiAssert.Ok(detail, "TC-API-PERM-004");
            Assert.Contains($"{menuId}#1", ApiResult.GetString(detail.Data?["permission"]));
        }
        finally
        {
            if (roleId > 0) await Admin.DeleteAsync($"/api/Admin/Role/{roleId}");
            ApiDatabaseHelper.DeleteRoleByName(roleName);
        }
    }

    private async Task<Int32> CreateRoleAsync(String name, String permission)
    {
        var rs = await Admin.PostJsonAsync("/api/Admin/Role", Json(new { name, type = 2, enable = true, permission }));
        ApiAssert.Ok(rs, "CreateRole");

        var id = ApiResult.GetInt32(rs.Data?["id"]) ?? 0;
        if (id > 0) return id;

        id = ApiDatabaseHelper.GetRoleIdByName(name);
        Assert.True(id > 0, $"角色 {name} 未落库");
        return id;
    }

    private async Task CreateUserAsync(String name, Int32 roleId)
    {
        var rs = await Admin.PostJsonAsync("/api/Admin/User", Json(new { name, password = "Test@123", roleId, enable = true }));
        ApiAssert.Ok(rs, "CreateUser");
        Assert.True(ApiDatabaseHelper.CountUsersByName(name) == 1, $"用户 {name} 未落库。响应：{rs.Content}");
    }
}
