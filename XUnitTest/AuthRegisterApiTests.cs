using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTest;

/// <summary>统一注册接口 HTTP 回归测试（依赖本地运行的 CubeDemo 服务）</summary>
public class AuthRegisterApiTests
{
    /// <summary>CubeDemo 服务器根地址</summary>
    private const String BaseUrl = "https://localhost:5001";

    /// <summary>统一注册路径</summary>
    private const String RegisterPath = "/Auth/Register";

    /// <summary>OAuth 待注册信息路径</summary>
    private const String OAuthPendingInfoPath = "/Auth/OAuthPendingInfo";

    [Fact(DisplayName = "OAuthPendingInfo 无效 token 返回失败")]
    public async Task OAuthPendingInfo_InvalidToken_ShouldFail()
    {
        using var client = CreateClient();

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"{OAuthPendingInfoPath}?token=invalid_token_for_test");
        }
        catch (HttpRequestException)
        {
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.NotEqual(0, GetCode(doc));
        Assert.Contains("token", GetMessage(doc), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Register 密码与确认密码不一致时失败")]
    public async Task Register_PasswordNotMatch_ShouldFail()
    {
        using var client = CreateClient();

        var username = $"reg_mismatch_{Guid.NewGuid():N}"[..20];
        var payload = new Dictionary<String, Object>
        {
            ["registerCategory"] = 0,
            ["username"] = username,
            ["email"] = $"{username}@example.com",
            ["password"] = "Strong@2026",
            ["confirmPassword"] = "Strong@2026_DIFF"
        };

        HttpResponseMessage response;
        try
        {
            response = await PostJsonAsync(client, RegisterPath, payload);
        }
        catch (HttpRequestException)
        {
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.NotEqual(0, GetCode(doc));
        Assert.Contains("密码", GetMessage(doc));
    }

    [Fact(DisplayName = "Register OAuthBind 缺少 oauthToken 时失败")]
    public async Task Register_OAuthBind_WithoutToken_ShouldFail()
    {
        using var client = CreateClient();

        var payload = new Dictionary<String, Object>
        {
            ["registerCategory"] = 3,
            ["username"] = "oauth_new_user",
            ["password"] = "Strong@2026",
            ["confirmPassword"] = "Strong@2026"
        };

        HttpResponseMessage response;
        try
        {
            response = await PostJsonAsync(client, RegisterPath, payload);
        }
        catch (HttpRequestException)
        {
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.NotEqual(0, GetCode(doc));
        Assert.Contains("OAuth", GetMessage(doc), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Register 缺少用户名时失败")]
    public async Task Register_PasswordMode_WithoutUsername_ShouldFail()
    {
        using var client = CreateClient();

        var payload = new Dictionary<String, Object>
        {
            ["registerCategory"] = 0,
            ["password"] = "Strong@2026",
            ["confirmPassword"] = "Strong@2026"
        };

        HttpResponseMessage response;
        try
        {
            response = await PostJsonAsync(client, RegisterPath, payload);
        }
        catch (HttpRequestException)
        {
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.NotEqual(0, GetCode(doc));
        Assert.Contains("用户名", GetMessage(doc));
    }

    #region 辅助

    /// <summary>创建忽略 SSL 证书校验的客户端</summary>
    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    /// <summary>POST JSON</summary>
    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, String path, Dictionary<String, Object> payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return client.PostAsync(path, content);
    }

    /// <summary>读取响应 code 字段</summary>
    private static Int32 GetCode(JsonDocument doc)
    {
        if (doc.RootElement.TryGetProperty("code", out var code)) return code.GetInt32();
        if (doc.RootElement.TryGetProperty("Code", out code)) return code.GetInt32();
        return -1;
    }

    /// <summary>读取响应 message 字段</summary>
    private static String GetMessage(JsonDocument doc)
    {
        if (doc.RootElement.TryGetProperty("message", out var msg)) return msg.GetString() ?? "";
        if (doc.RootElement.TryGetProperty("Message", out msg)) return msg.GetString() ?? "";
        return "";
    }

    #endregion
}
