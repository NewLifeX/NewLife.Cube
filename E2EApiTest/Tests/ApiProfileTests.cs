using System;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>个人资料自助维护 E2E：可改昵称、不可改角色/状态、不可改他人、不可借资料接口改密码</summary>
public sealed class ApiProfileTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-PROF-001 自助修改昵称落库并可回读")]
    [Trait("Category", "Account")]
    public async Task TC_API_PROF_001_UpdateDisplayName()
    {
        var rs = await Admin.PostJsonAsync("/api/Admin/User/Info", Json(new { id = 1, displayName = "E2E昵称" }));
        ApiAssert.Ok(rs, "TC-API-PROF-001 update");

        var info = await Admin.GetAsync("/api/Admin/User/Info");
        ApiAssert.Ok(info, "TC-API-PROF-001 info");
        Assert.Equal("E2E昵称", ApiResult.GetString(info.Data?["displayName"]));

        var restore = await Admin.PostJsonAsync("/api/Admin/User/Info", Json(new { id = 1, displayName = "管理员" }));
        ApiAssert.Ok(restore, "TC-API-PROF-001 restore");
    }

    [Fact(DisplayName = "TC-API-PROF-002 资料接口拒绝改角色、改状态与改他人资料")]
    [Trait("Category", "Account")]
    public async Task TC_API_PROF_002_ForbiddenFields()
    {
        var role = await Admin.PostJsonAsync("/api/Admin/User/Info", Json(new { id = 1, roleId = 2 }));
        Assert.NotEqual(0, role.Code);
        Assert.False(String.IsNullOrWhiteSpace(role.Message), "改角色应被拒绝并提示");

        var enable = await Admin.PostJsonAsync("/api/Admin/User/Info", Json(new { id = 1, enable = false }));
        Assert.NotEqual(0, enable.Code);
        Assert.False(String.IsNullOrWhiteSpace(enable.Message), "改启用状态应被拒绝并提示");

        var other = await Admin.PostJsonAsync("/api/Admin/User/Info", Json(new { id = 2, displayName = "越权" }));
        Assert.NotEqual(0, other.Code);
        Assert.False(String.IsNullOrWhiteSpace(other.Message), "改他人资料应被拒绝并提示");
    }

    [Fact(DisplayName = "TC-API-PROF-003 资料接口不接受密码字段，密码保持原样")]
    [Trait("Category", "Account")]
    public async Task TC_API_PROF_003_PasswordIgnored()
    {
        var userName = NewName("prof");
        try
        {
            using var client = CreateAnonymous();
            var register = await client.PostJsonAsync("/Auth/Register", Json(new { username = userName, password = "Test@123", confirmPassword = "Test@123" }));
            ApiAssert.Ok(register, "TC-API-PROF-003 register");

            var login = await client.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Test@123" }));
            ApiAssert.Ok(login, "TC-API-PROF-003 login");

            var info = await client.GetAsync("/api/Admin/User/Info");
            var id = ApiResult.GetInt32(info.Data?["id"]);
            Assert.True(id > 0, $"资料接口未返回编号：{info.Content}");

            // 借资料接口明文写入密码必须无效
            var update = await client.PostJsonAsync("/api/Admin/User/Info", Json(new { id, displayName = "改名测试", password = "Hacked@123" }));
            ApiAssert.Ok(update, "TC-API-PROF-003 update");

            using var check = CreateAnonymous();
            var hacked = await check.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Hacked@123" }));
            Assert.NotEqual(0, hacked.Code);

            var original = await check.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Test@123" }));
            ApiAssert.Ok(original, "TC-API-PROF-003 original password");
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserByName(userName);
        }
    }
}
