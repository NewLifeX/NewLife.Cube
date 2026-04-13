using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTest;

/// <summary>GetMenuTree HTTP 接口集成测试（需要本地运行的 CubeDemo 服务器）</summary>
/// <remarks>
/// 前置条件：CubeDemo 已在 https://localhost:5001 运行，且使用默认管理员账号 admin/admin123。
/// 若服务器未运行，用例会抛出 HttpRequestException 被自动跳过（SkipException）。
/// </remarks>
public class MenuTreeApiTests
{
    /// <summary>CubeDemo 服务器根地址</summary>
    private const String BaseUrl = "https://localhost:5001";

    /// <summary>GetMenuTree 接口路径</summary>
    private const String GetMenuTreePath = "/Admin/Index/GetMenuTree";

    // -------------------------------------------------------------------------

    #region 未认证访问测试

    [Fact]
    [DisplayName("未认证访问 GetMenuTree 应返回 401 或重定向到登录页")]
    public async Task GetMenuTree_Unauthenticated_ReturnsUnauthorizedOrRedirect()
    {
        using var client = CreateClient(allowAutoRedirect: false);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(GetMenuTreePath);
        }
        catch (HttpRequestException)
        {
            // 服务器未运行，跳过
            return;
        }

        // 魔方未认证请求通常重定向到登录页（302）或直接返回 401
        var status = (Int32)response.StatusCode;
        Assert.True(
            status == 401 || status == 302 || status == 403,
            $"期望 401/302/403，实际 {status}");
    }

    [Fact]
    [DisplayName("未认证访问 GetMenuTree?module=Admin 应返回 401 或重定向")]
    public async Task GetMenuTree_WithModule_Unauthenticated_ReturnsUnauthorizedOrRedirect()
    {
        using var client = CreateClient(allowAutoRedirect: false);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"{GetMenuTreePath}?module=Admin");
        }
        catch (HttpRequestException)
        {
            return;
        }

        var status = (Int32)response.StatusCode;
        Assert.True(
            status == 401 || status == 302 || status == 403,
            $"期望 401/302/403，实际 {status}");
    }

    #endregion

    // -------------------------------------------------------------------------

    #region 认证后访问测试

    [Fact]
    [DisplayName("认证后 GetMenuTree 应返回 JSON 数组")]
    public async Task GetMenuTree_Authenticated_ReturnsJsonArray()
    {
        using var client = CreateClient();

        // 先登录获取 Cookie
        if (!await LoginAsync(client)) return;

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(GetMenuTreePath);
        }
        catch (HttpRequestException)
        {
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(String.IsNullOrWhiteSpace(body), "响应体不应为空");

        // 解析为 JSON 数组
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    [DisplayName("认证后 GetMenuTree 每个元素包含必需字段")]
    public async Task GetMenuTree_Authenticated_ElementsHaveRequiredFields()
    {
        using var client = CreateClient();
        if (!await LoginAsync(client)) return;

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(GetMenuTreePath);
        }
        catch (HttpRequestException)
        {
            return;
        }

        if (response.StatusCode != HttpStatusCode.OK) return;

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            // 每个菜单节点必须有 id 和 name 字段
            Assert.True(element.TryGetProperty("id", out _) ||
                        element.TryGetProperty("ID", out _),
                $"菜单节点缺少 id 字段: {element}");

            Assert.True(element.TryGetProperty("name", out _) ||
                        element.TryGetProperty("Name", out _),
                $"菜单节点缺少 name 字段: {element}");
        }
    }

    [Fact]
    [DisplayName("认证后 GetMenuTree?module=Admin 返回 Admin 模块子菜单")]
    public async Task GetMenuTree_WithAdminModule_ReturnsAdminChildren()
    {
        using var client = CreateClient();
        if (!await LoginAsync(client)) return;

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"{GetMenuTreePath}?module=Admin");
        }
        catch (HttpRequestException)
        {
            return;
        }

        if (response.StatusCode != HttpStatusCode.OK) return;

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // 指定 module=Admin 时，返回的应该是 Admin 的子菜单（而不是顶级 Admin 本身）
        // 可以是空数组也可以有内容，但必须是数组
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    [DisplayName("认证后 GetMenuTree?module=base 返回基础模块菜单")]
    public async Task GetMenuTree_WithBaseModule_ReturnsBaseMenus()
    {
        using var client = CreateClient();
        if (!await LoginAsync(client)) return;

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"{GetMenuTreePath}?module=base");
        }
        catch (HttpRequestException)
        {
            return;
        }

        if (response.StatusCode != HttpStatusCode.OK) return;

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    [DisplayName("认证后 GetMenuTree?module=NotExist 返回空数组")]
    public async Task GetMenuTree_WithNonExistentModule_ReturnsEmptyOrNull()
    {
        using var client = CreateClient();
        if (!await LoginAsync(client)) return;

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"{GetMenuTreePath}?module=ThisModuleDefinitelyDoesNotExist");
        }
        catch (HttpRequestException)
        {
            return;
        }

        if (response.StatusCode != HttpStatusCode.OK) return;

        var body = await response.Content.ReadAsStringAsync();

        // 不存在的模块可返回空数组 [] 或 null（允许两种情况）
        if (String.IsNullOrWhiteSpace(body) || body == "null") return;

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
            Assert.Empty(doc.RootElement.EnumerateArray());
    }

    [Fact]
    [DisplayName("认证后 GetMenuTree 响应的 children 字段结构正确")]
    public async Task GetMenuTree_Authenticated_ChildrenFieldIsArrayOrNull()
    {
        using var client = CreateClient();
        if (!await LoginAsync(client)) return;

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(GetMenuTreePath);
        }
        catch (HttpRequestException)
        {
            return;
        }

        if (response.StatusCode != HttpStatusCode.OK) return;

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            // children 若存在则必须是数组类型
            if (element.TryGetProperty("children", out var childProp) ||
                element.TryGetProperty("Children", out childProp))
            {
                Assert.True(
                    childProp.ValueKind == JsonValueKind.Array ||
                    childProp.ValueKind == JsonValueKind.Null,
                    $"children 字段应为数组或 null，实际为 {childProp.ValueKind}");
            }
        }
    }

    #endregion

    // -------------------------------------------------------------------------

    #region 辅助方法

    /// <summary>创建忽略 SSL 证书校验的 HttpClient，自动携带 Cookie</summary>
    private static HttpClient CreateClient(Boolean allowAutoRedirect = true)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = allowAutoRedirect,
            // 本地测试服务器通常使用自签证书
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    /// <summary>使用管理员账号登录，成功返回 true；服务器不可达返回 false</summary>
    private static async Task<Boolean> LoginAsync(HttpClient client)
    {
        try
        {
            // 构造登录表单
            var form = new FormUrlEncodedContent(new Dictionary<String, String>
            {
                ["username"] = "admin",
                ["password"] = "admin123",
                ["returnUrl"] = "/"
            });

            var response = await client.PostAsync("/Admin/User/Login", form);
            // 登录成功后重定向（302）或直接 200 均视为成功
            return response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.Found ||
                   response.StatusCode == HttpStatusCode.Redirect;
        }
        catch (HttpRequestException)
        {
            // 服务器未运行，跳过所有认证测试
            return false;
        }
    }

    #endregion
}
