using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>账号自助接口 E2E：个人资料、修改密码闭环与管理员专属操作的越权拦截</summary>
public sealed class ApiAccountTests : ApiTestBase
{
    private const String Password1 = "Test@123";
    private const String Password2 = "Pass@456";

    [Fact(DisplayName = "TC-API-ACCT-001 注册用户可查看本人资料并修改密码，改后旧密码失效")]
    [Trait("Category", "Account")]
    public async Task TC_API_ACCT_001_ChangePasswordRoundtrip()
    {
        var userName = NewName("acct");
        try
        {
            using var client = CreateAnonymous();
            await RegisterAndLogin(client, userName, Password1);

            var info = await client.GetAsync("/api/Admin/User/Info");
            ApiAssert.Ok(info, "TC-API-ACCT-001 info");
            Assert.Equal(userName, ApiResult.GetString(info.Data?["name"]));
            Assert.DoesNotContain("password", info.Content, StringComparison.OrdinalIgnoreCase);

            var change = await client.PostJsonAsync("/api/Admin/User/ChangePassword", Json(new
            {
                oldPassword = Password1,
                newPassword = Password2,
                newPassword2 = Password2,
            }));
            ApiAssert.Ok(change, "TC-API-ACCT-001 change");

            using var relogin = CreateAnonymous();
            var old = await relogin.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = Password1 }));
            Assert.NotEqual(0, old.Code);

            var fresh = await relogin.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = Password2 }));
            ApiAssert.Ok(fresh, "TC-API-ACCT-001 login new password");
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserByName(userName);
        }
    }

    [Fact(DisplayName = "TC-API-ACCT-002 修改密码的新密码不一致、与旧密码相同或被拒绝时给出提示")]
    [Trait("Category", "Account")]
    public async Task TC_API_ACCT_002_ChangePasswordValidation()
    {
        var userName = NewName("acctv");
        try
        {
            using var client = CreateAnonymous();
            await RegisterAndLogin(client, userName, Password1);

            var mismatch = await client.PostJsonAsync("/api/Admin/User/ChangePassword", Json(new
            {
                oldPassword = Password1,
                newPassword = Password2,
                newPassword2 = Password2 + "x",
            }));
            Assert.NotEqual(0, mismatch.Code);
            Assert.False(String.IsNullOrWhiteSpace(mismatch.Message), "两次密码不一致应给出提示");

            var same = await client.PostJsonAsync("/api/Admin/User/ChangePassword", Json(new
            {
                oldPassword = Password1,
                newPassword = Password1,
                newPassword2 = Password1,
            }));
            Assert.NotEqual(0, same.Code);
            Assert.False(String.IsNullOrWhiteSpace(same.Message), "新旧密码相同应给出提示");

            var wrongOld = await client.PostJsonAsync("/api/Admin/User/ChangePassword", Json(new
            {
                oldPassword = Password1 + "x",
                newPassword = Password2,
                newPassword2 = Password2,
            }));
            Assert.NotEqual(0, wrongOld.Code);
            Assert.False(String.IsNullOrWhiteSpace(wrongOld.Message), "原密码错误应给出提示");
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserByName(userName);
        }
    }

    [Fact(DisplayName = "TC-API-ACCT-003 普通用户不能吊销他人令牌或清空密码")]
    [Trait("Category", "Account")]
    public async Task TC_API_ACCT_003_AdminOnlyActions()
    {
        var userName = NewName("acctn");
        try
        {
            using var client = CreateAnonymous();
            var login = await RegisterAndLogin(client, userName, Password1);
            var info = await client.GetAsync("/api/Admin/User/Info");
            ApiAssert.Ok(info, "TC-API-ACCT-003 info");
            var id = ApiResult.GetInt32(info.Data?["id"]);
            Assert.True(id > 0, $"资料接口未返回用户编号：{info.Content}");

            var revoke = await client.PostJsonAsync("/api/Admin/User/RevokeTokens", Json(new { id = 1 }));
            Assert.NotEqual(0, revoke.Code);
            Assert.False(String.IsNullOrWhiteSpace(revoke.Message), "越权吊销令牌应给出提示");

            var clear = await client.PostJsonAsync("/api/Admin/User/ClearPassword", Json(new { id = id }));
            Assert.NotEqual(0, clear.Code);
            Assert.False(String.IsNullOrWhiteSpace(clear.Message), "越权清空密码应给出提示");
        }
        finally
        {
            ApiDatabaseHelper.DeleteUserByName(userName);
        }
    }

    [Fact(DisplayName = "TC-API-ACCT-004 用户绑定列表不回传令牌与密钥")]
    [Trait("Category", "Account")]
    public async Task TC_API_ACCT_004_Binds()
    {
        var rs = await Admin.GetAsync("/api/Admin/User/Binds");

        ApiAssert.Ok(rs, "TC-API-ACCT-004");
        Assert.Equal("admin", ApiResult.GetString(rs.Data?["name"]));

        var content = rs.Content.ToLowerInvariant();
        foreach (var key in new[] { "accesstoken", "access_token", "secret", "password" })
            Assert.DoesNotContain(key, content);
    }

    /// <summary>注册新用户并完成登录，返回登录响应</summary>
    private static async Task<ApiResult> RegisterAndLogin(ApiClient client, String userName, String password)
    {
        var register = await client.PostJsonAsync("/Auth/Register", Json(new { username = userName, password, confirmPassword = password }));
        ApiAssert.Ok(register, "Register " + userName);

        var login = await client.PostJsonAsync("/Auth/Login", Json(new { username = userName, password }));
        ApiAssert.Ok(login, "Login " + userName);
        return login;
    }
}
