using System;
using System.Threading.Tasks;
using E2EApiTest.Fixtures;
using E2EApiTest.Helpers;
using Xunit;

namespace E2EApiTest.Tests;

/// <summary>认证与会话接口 E2E：健康检查、登录、令牌来源、刷新、登出</summary>
public sealed class ApiAuthTests : ApiTestBase
{
    [Fact(DisplayName = "TC-API-AUTH-001 健康检查匿名可访问且只暴露最小信息")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_001_Health()
    {
        using var client = CreateAnonymous();
        var rs = await client.GetAsync("/Cube/Health?state=e2e-probe");

        ApiAssert.Ok(rs, "TC-API-AUTH-001");
        Assert.Equal("ok", ApiResult.GetString(rs.Data?["status"]));
        Assert.Equal("e2e-probe", ApiResult.GetString(rs.Data?["state"]));
        Assert.True(DateTime.TryParse(ApiResult.GetString(rs.Data?["time"]), out _), "time 不是合法时间");
    }

    [Fact(DisplayName = "TC-API-AUTH-002 错误密码登录返回 -1 且不下发令牌")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_002_LoginWrongPassword()
    {
        using var client = CreateAnonymous();
        var rs = await client.PostJsonAsync("/Auth/Login", Json(new { username = ApiAppFixture.AdminUser, password = "wrong-password" }));

        ApiAssert.Fail(rs, -1, "TC-API-AUTH-002");
        Assert.Null(ApiResult.GetString(rs.Data?["access_token"]));
        Assert.Null(ApiResult.GetString(rs.Data?["refresh_token"]));
    }

    [Fact(DisplayName = "TC-API-AUTH-003 正确密码登录下发三段式 JWT 与刷新令牌")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_003_LoginSuccess()
    {
        using var client = CreateAnonymous();
        var rs = await client.PostJsonAsync("/Auth/Login", Json(new { username = ApiAppFixture.AdminUser, password = ApiAppFixture.AdminPass }));

        ApiAssert.Ok(rs, "TC-API-AUTH-003");
        var token = ApiResult.GetString(rs.Data?["access_token"]);
        Assert.False(String.IsNullOrWhiteSpace(token), "access_token 为空");
        Assert.Equal(3, token!.Split('.').Length);
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["refresh_token"])), "refresh_token 为空");
    }

    [Fact(DisplayName = "TC-API-AUTH-004 未登录访问实体接口返回 HTTP 401 且响应体只有 code/message")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_004_AnonymousEntity()
    {
        using var client = CreateAnonymous();
        var rs = await client.GetAsync("/api/School/Student");

        ApiAssert.HttpStatus(rs, 401, "TC-API-AUTH-004");
        ApiAssert.Fail(rs, 401, "TC-API-AUTH-004");
        Assert.False(String.IsNullOrWhiteSpace(rs.Message), "401 响应缺少 message");
        Assert.Null(rs.Json?["traceId"]);
        Assert.Null(rs.Data);
    }

    [Fact(DisplayName = "TC-API-AUTH-005 Bearer 令牌可访问 /Auth/Info 且不回传密码")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_005_BearerInfo()
    {
        using var client = await CreateTokenOnlyClientAsync("Bearer");

        var rs = await client.GetAsync("/Auth/Info");

        ApiAssert.Ok(rs, "TC-API-AUTH-005");
        Assert.Equal(ApiAppFixture.AdminUser, ApiResult.GetString(rs.Data?["name"]));
        var password = ApiResult.GetString(rs.Data?["password"]);
        Assert.True(String.IsNullOrEmpty(password), $"用户信息泄露密码：{password}");
    }

    [Fact(DisplayName = "TC-API-AUTH-006 X-Token 兼容头可作为登录凭据")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_006_XToken()
    {
        using var client = await CreateTokenOnlyClientAsync("XToken");

        var rs = await client.GetAsync("/api/School/Student");

        ApiAssert.Ok(rs, "TC-API-AUTH-006");
        Assert.NotNull(rs.Page);
    }

    [Fact(DisplayName = "TC-API-AUTH-007 查询串 token 可作为登录凭据")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_007_QueryToken()
    {
        using var client = await CreateTokenOnlyClientAsync("Query");

        var rs = await client.GetAsync("/api/School/Student");

        ApiAssert.Ok(rs, "TC-API-AUTH-007");
        Assert.NotNull(rs.Page);
    }

    [Fact(DisplayName = "TC-API-AUTH-008 刷新令牌换发新令牌且旧刷新令牌失效")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_008_Refresh()
    {
        using var client = CreateAnonymous();
        await client.LoginAsync();
        var refresh = client.RefreshToken;
        Assert.False(String.IsNullOrWhiteSpace(refresh), "登录未返回 refresh_token");

        var rs = await client.PostJsonAsync("/Auth/Refresh", Json(new { userName = ApiAppFixture.AdminUser, refreshToken = refresh }));
        ApiAssert.Ok(rs, "TC-API-AUTH-008");
        var newToken = ApiResult.GetString(rs.Data?["accessToken"]);
        Assert.False(String.IsNullOrWhiteSpace(newToken), "刷新未返回 accessToken");
        Assert.Equal(3, newToken!.Split('.').Length);

        // 旧刷新令牌黑名单：重复使用应返回 -4（需要重新登录）
        var replay = await client.PostJsonAsync("/Auth/Refresh", Json(new { userName = ApiAppFixture.AdminUser, refreshToken = refresh }));
        ApiAssert.Fail(replay, -4, "TC-API-AUTH-008");
    }

    [Fact(DisplayName = "TC-API-AUTH-009 Challenge 返回可解析的 RSA 公钥")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_009_Challenge()
    {
        using var client = CreateAnonymous();
        var rs = await client.GetAsync("/Auth/Challenge");

        ApiAssert.Ok(rs, "TC-API-AUTH-009");
        Assert.False(String.IsNullOrWhiteSpace(ApiResult.GetString(rs.Data?["challengeId"])), "challengeId 为空");

        var pem = ApiResult.GetString(rs.Data?["publicKey"]);
        Assert.Contains("BEGIN PUBLIC KEY", pem);
        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportFromPem(pem);
        Assert.True(rsa.KeySize >= 2048, $"公钥位数过小：{rsa.KeySize}");
    }

    [Fact(DisplayName = "TC-API-AUTH-010 登出返回成功")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_010_Logout()
    {
        var rs = await Admin.PostJsonAsync("/Auth/Logout", "{}");

        ApiAssert.Ok(rs, "TC-API-AUTH-010");
    }

    [Fact(DisplayName = "TC-API-AUTH-011 Bearer 令牌可访问 /Cube 服务接口")]
    [Trait("Category", "Auth")]
    public async Task TC_API_AUTH_011_BearerCubeService()
    {
        using var client = await CreateTokenOnlyClientAsync("Bearer");

        var rs = await client.GetAsync("/Cube/MenuTree");

        ApiAssert.Ok(rs, "TC-API-AUTH-011");
        Assert.NotEmpty(ApiAssert.Rows(rs, "TC-API-AUTH-011"));
    }
}
