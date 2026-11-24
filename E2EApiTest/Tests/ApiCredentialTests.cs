using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>凭据字段只写 E2E：应用密钥/第三方令牌永不下发，编辑留空不清空</summary>
public sealed class ApiCredentialTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-CRED-001 OAuth 应用密钥不下发，编辑留空保留原值、提交新值才覆盖")]
    [Trait("Category", "Credential")]
    public async Task TC_API_CRED_001_OAuthSecret()
    {
        var name = NewName("cred");
        var id = 0;
        try
        {
            var create = await Admin.PostJsonAsync("/api/Admin/OAuthConfig", Json(new { provider = "Sso", name, secret = "S3cr3t-Value", appId = "appid-x", enable = false }));
            ApiAssert.Ok(create, "TC-API-CRED-001 create");
            id = ApiResult.GetInt32(create.Data?["id"]) ?? 0;
            Assert.True(id > 0, $"新增未返回主键：{create.Content}");
            Assert.DoesNotContain("secret", create.Data?.ToJsonString() ?? "", StringComparison.OrdinalIgnoreCase);
            Assert.Equal("S3cr3t-Value", ApiDatabaseHelper.GetOAuthSecret(id));

            var list = await Admin.GetAsync($"/api/Admin/OAuthConfig?name={Uri.EscapeDataString(name)}");
            ApiAssert.Ok(list, "TC-API-CRED-001 list");
            foreach (var row in list.Rows!)
                Assert.Null(row?["secret"]);

            var detail = await Admin.GetAsync($"/api/Admin/OAuthConfig/{id}");
            ApiAssert.Ok(detail, "TC-API-CRED-001 detail");
            Assert.Null(detail.Data?["secret"]);

            // 编辑时不提交密钥，库中密钥保持原值
            var keep = await Admin.PutJsonAsync($"/api/Admin/OAuthConfig/{id}", Json(new { name = name + "-keep" }));
            ApiAssert.Ok(keep, "TC-API-CRED-001 keep");
            Assert.Equal("S3cr3t-Value", ApiDatabaseHelper.GetOAuthSecret(id));

            // 显式提交新密钥才覆盖
            var change = await Admin.PutJsonAsync($"/api/Admin/OAuthConfig/{id}", Json(new { name = name + "-new", secret = "NewS3cr3t" }));
            ApiAssert.Ok(change, "TC-API-CRED-001 change");
            Assert.Equal("NewS3cr3t", ApiDatabaseHelper.GetOAuthSecret(id));
        }
        finally
        {
            if (id > 0) await Admin.DeleteAsync($"/api/Admin/OAuthConfig/{id}");
        }
    }

    [Fact(DisplayName = "TC-API-CRED-002 短信渠道密钥不下发，更新后库中保留")]
    [Trait("Category", "Credential")]
    public async Task TC_API_CRED_002_SmsSecret()
    {
        var list = await Admin.GetAsync("/api/Admin/SmsConfig?pageSize=1");
        ApiAssert.Ok(list, "TC-API-CRED-002 list");
        var row = list.Rows?.FirstOrDefault();
        Assert.NotNull(row);

        var id = ApiResult.GetInt32(row!["id"]) ?? 0;
        Assert.True(id > 0);
        Assert.Null(row["appSecret"]);

        var original = ApiDatabaseHelper.GetSmsAppSecret(id);
        try
        {
            var update = await Admin.PutJsonAsync($"/api/Admin/SmsConfig/{id}", Json(new { appSecret = "SmsS3cr3t" }));
            ApiAssert.Ok(update, "TC-API-CRED-002 update");
            Assert.Equal("SmsS3cr3t", ApiDatabaseHelper.GetSmsAppSecret(id));

            var detail = await Admin.GetAsync($"/api/Admin/SmsConfig/{id}");
            ApiAssert.Ok(detail, "TC-API-CRED-002 detail");
            Assert.Null(detail.Data?["appSecret"]);
        }
        finally
        {
            await Admin.PutJsonAsync($"/api/Admin/SmsConfig/{id}", Json(new { appSecret = original }));
        }
    }

    [Fact(DisplayName = "TC-API-CRED-003 第三方令牌与密钥字段不出现在列表与详情字段集中")]
    [Trait("Category", "Credential")]
    public async Task TC_API_CRED_003_TokenFields()
    {
        var cases = new (String Path, String[] Fields)[]
        {
            ("/api/Admin/UserConnect", ["AccessToken", "RefreshToken"]),
            ("/api/Admin/OAuthLog", ["AccessToken", "RefreshToken"]),
            ("/api/Cube/AppLog", ["AccessToken", "RefreshToken"]),
            ("/api/Admin/OAuthConfig", ["Secret"]),
            ("/api/Cube/App", ["Secret"]),
            ("/api/Admin/SmsConfig", ["AppSecret"]),
        };

        foreach (var (path, fields) in cases)
        {
            var meta = await Admin.GetAsync($"{path}/meta");
            ApiAssert.Ok(meta, $"TC-API-CRED-003 {path}");

            var listNames = (meta.Data?["list"] as JsonArray)?.Select(e => ApiResult.GetString(e?["name"]) ?? "").ToArray() ?? [];
            var detailNames = (meta.Data?["detail"] as JsonArray)?.Select(e => ApiResult.GetString(e?["name"]) ?? "").ToArray() ?? [];
            foreach (var field in fields)
            {
                Assert.DoesNotContain(field, listNames, StringComparer.OrdinalIgnoreCase);
                Assert.DoesNotContain(field, detailNames, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}


