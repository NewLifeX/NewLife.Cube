using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>多因素认证 E2E：开关门禁、TOTP 开通激活、二步登录与解绑闭环</summary>
public sealed class ApiMfaTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-MFA-001 系统未开启 MFA 时开通接口明确拒绝且状态标记不可用")]
    [Trait("Category", "Mfa")]
    public async Task TC_API_MFA_001_DisabledBySetting()
    {
        await SetMfaSwitchAsync(false);
        try
        {
            var status = await Admin.GetAsync("/Mfa/Status");
            ApiAssert.Ok(status, "TC-API-MFA-001 status");
            Assert.False(ApiResult.GetBoolean(status.Data?["available"]), "未开启时应标记 available=false");

            var setup = await Admin.GetAsync("/Mfa/Setup");
            Assert.Equal(-1, setup.Code);
            Assert.False(String.IsNullOrWhiteSpace(setup.Message), "应提示系统未开启 MFA");
            Assert.Null(setup.Data);
        }
        finally
        {
            await SetMfaSwitchAsync(false);
        }
    }

    [Fact(DisplayName = "TC-API-MFA-002 开通 TOTP 后二步登录生效，解绑后恢复单步登录")]
    [Trait("Category", "Mfa")]
    public async Task TC_API_MFA_002_TotpLifecycle()
    {
        var userName = NewName("mfa");
        await SetMfaSwitchAsync(true);
        try
        {
            using var user = CreateAnonymous();
            await RegisterAndLogin(user, userName);

            var status = await user.GetAsync("/Mfa/Status");
            ApiAssert.Ok(status, "TC-API-MFA-002 status");
            Assert.True(ApiResult.GetBoolean(status.Data?["available"]), "开启后应标记 available=true");
            Assert.False(ApiResult.GetBoolean(status.Data?["enabled"]), "新用户默认未启用 MFA");

            var setup = await user.GetAsync("/Mfa/Setup");
            ApiAssert.Ok(setup, "TC-API-MFA-002 setup");
            var secret = ApiResult.GetString(setup.Data?["secret"]);
            var uri = ApiResult.GetString(setup.Data?["totpUri"]);
            Assert.False(String.IsNullOrWhiteSpace(secret), "缺少密钥");
            Assert.StartsWith("otpauth://totp/", uri);
            Assert.Contains(secret!, uri);

            // 错误验证码不得激活
            var badActivate = await user.PostJsonAsync($"/Mfa/Activate?code={TotpHelper.WrongCode(secret!)}", "{}");
            Assert.Equal(-1, badActivate.Code);
            Assert.False(String.IsNullOrWhiteSpace(badActivate.Message), "应提示验证码错误");

            var activate = await user.PostJsonAsync($"/Mfa/Activate?code={TotpHelper.Compute(secret!)}", "{}");
            ApiAssert.Ok(activate, "TC-API-MFA-002 activate");
            var backupCodes = activate.Data?["backupCodes"] as JsonArray;
            Assert.Equal(10, backupCodes?.Count);

            var enabled = await user.GetAsync("/Mfa/Status");
            Assert.True(ApiResult.GetBoolean(enabled.Data?["enabled"]), "激活后应标记 enabled=true");

            // 二步登录：账密通过后返回挂起令牌，补验证码才下发正式令牌
            using var relogin = CreateAnonymous();
            var login = await relogin.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Test@123" }));
            Assert.Equal(-7, login.Code);
            var mfaToken = ApiResult.GetString(login.Data?["mfaToken"]);
            Assert.False(String.IsNullOrWhiteSpace(mfaToken), "二步登录应返回 mfaToken");

            var badVerify = await relogin.PostJsonAsync("/Mfa/Verify", Json(new { mfaToken, code = TotpHelper.WrongCode(secret!) }));
            Assert.Equal(-1, badVerify.Code);

            // 输错一次不应作废挂起令牌，用户可以直接重试
            var verify = await relogin.PostJsonAsync("/Mfa/Verify", Json(new { mfaToken, code = TotpHelper.Compute(secret!) }));
            ApiAssert.Ok(verify, "TC-API-MFA-002 verify");
            Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(verify.Data?["accessToken"])), "MFA 通过后应下发访问令牌");

            // 通过后令牌一次性：重复使用必须被拒绝
            var replay = await relogin.PostJsonAsync("/Mfa/Verify", Json(new { mfaToken, code = TotpHelper.Compute(secret!) }));
            Assert.Equal(-1, replay.Code);

            // 连续输错达到上限后挂起令牌作废，必须重新登录
            var locked = await relogin.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Test@123" }));
            Assert.Equal(-7, locked.Code);
            var lockedToken = ApiResult.GetString(locked.Data?["mfaToken"]);
            String? lockMessage = null;
            for (var i = 0; i < 5; i++)
            {
                var wrong = await relogin.PostJsonAsync("/Mfa/Verify", Json(new { mfaToken = lockedToken, code = TotpHelper.WrongCode(secret!) }));
                Assert.Equal(-1, wrong.Code);
                lockMessage = wrong.Message;
            }

            var afterLock = await relogin.PostJsonAsync("/Mfa/Verify", Json(new { mfaToken = lockedToken, code = TotpHelper.Compute(secret!) }));
            Assert.Equal(-1, afterLock.Code);
            Assert.NotEqual(lockMessage, afterLock.Message);

            var disable = await user.PostJsonAsync($"/Mfa/Disable?code={TotpHelper.Compute(secret!)}", "{}");
            ApiAssert.Ok(disable, "TC-API-MFA-002 disable");

            var disabled = await user.GetAsync("/Mfa/Status");
            Assert.False(ApiResult.GetBoolean(disabled.Data?["enabled"]), "解绑后应标记 enabled=false");

            using var single = CreateAnonymous();
            var plain = await single.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Test@123" }));
            ApiAssert.Ok(plain, "TC-API-MFA-002 single factor");
        }
        finally
        {
            await SetMfaSwitchAsync(false);
            ApiDatabaseHelper.DeleteUserByName(userName);
        }
    }

    /// <summary>注册并登录，供 MFA 用例使用固定密码</summary>
    private static async Task RegisterAndLogin(ApiClient client, String userName)
    {
        var register = await client.PostJsonAsync("/Auth/Register", Json(new { username = userName, password = "Test@123", confirmPassword = "Test@123" }));
        ApiAssert.Ok(register, "MFA register");

        var login = await client.PostJsonAsync("/Auth/Login", Json(new { username = userName, password = "Test@123" }));
        ApiAssert.Ok(login, "MFA login");
    }

    /// <summary>切换系统级 MFA 开关（走配置页保存接口，与后台设置页同一路径）</summary>
    private async Task SetMfaSwitchAsync(Boolean enabled)
    {
        var get = await Admin.GetAsync("/api/Admin/Cube");
        ApiAssert.Ok(get, "读取魔方设置");

        var settings = get.Data as JsonObject ?? throw new InvalidOperationException("魔方设置响应不是对象：" + get.Content);
        settings["enableMfa"] = enabled;

        var put = await Admin.PutJsonAsync("/api/Admin/Cube", settings.ToJsonString());
        ApiAssert.Ok(put, "保存魔方设置");
        Assert.Equal(enabled, ApiResult.GetBoolean(put.Data?["enableMfa"]));
    }
}
