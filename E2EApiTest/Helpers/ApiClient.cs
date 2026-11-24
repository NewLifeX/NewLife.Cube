using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using E2EApiTest.Fixtures;

namespace E2EApiTest.Helpers;

/// <summary>API 响应封装，提供信封字段与 JSON 节点的便捷访问</summary>
public sealed class ApiResult
{
    /// <summary>HTTP 状态码</summary>
    public Int32 StatusCode { get; init; }

    /// <summary>原始响应文本</summary>
    public String Content { get; init; } = "";

    /// <summary>响应 Content-Type</summary>
    public String? ContentType { get; init; }

    /// <summary>响应头</summary>
    public IDictionary<String, String> Headers { get; init; } = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

    private JsonNode? _json;
    private Boolean _parsed;

    /// <summary>解析后的 JSON 根节点，非 JSON 响应返回 null</summary>
    public JsonNode? Json
    {
        get
        {
            if (!_parsed)
            {
                _parsed = true;
                try { _json = String.IsNullOrWhiteSpace(Content) ? null : JsonNode.Parse(Content); }
                catch { _json = null; }
            }
            return _json;
        }
    }

    /// <summary>业务码。响应体无 code 字段时返回 Int32.MinValue</summary>
    public Int32 Code => GetInt32(Json?["code"]) ?? Int32.MinValue;

    /// <summary>提示文本</summary>
    public String? Message => Json?["message"]?.GetValue<String>();

    /// <summary>业务数据</summary>
    public JsonNode? Data => Json?["data"];

    /// <summary>分页信息</summary>
    public JsonNode? Page => Json?["page"];

    /// <summary>字段级校验错误</summary>
    public JsonArray? FieldErrors => Json?["fieldErrors"] as JsonArray;

    /// <summary>把 data 当作数组返回</summary>
    public JsonArray? Rows => Data as JsonArray;

    /// <summary>读取字符串属性，缺失返回 null</summary>
    public static String? GetString(JsonNode? node) =>
        node is JsonValue v && v.TryGetValue<String>(out var s) ? s : null;

    /// <summary>读取 Int32 属性，缺失或非数字返回 null</summary>
    public static Int32? GetInt32(JsonNode? node)
    {
        if (node is not JsonValue v) return null;
        if (v.TryGetValue<Int32>(out var i)) return i;
        if (v.TryGetValue<String>(out var s) && Int32.TryParse(s, out i)) return i;
        return null;
    }

    /// <summary>读取 Int64 属性（允许字符串或数字），缺失返回 null</summary>
    public static Int64? GetInt64(JsonNode? node)
    {
        if (node is not JsonValue v) return null;
        if (v.TryGetValue<Int64>(out var i)) return i;
        if (v.TryGetValue<String>(out var s) && Int64.TryParse(s, out i)) return i;
        return null;
    }

    /// <summary>读取布尔属性，缺失返回 null</summary>
    public static Boolean? GetBoolean(JsonNode? node)
    {
        if (node is not JsonValue v) return null;
        if (v.TryGetValue<Boolean>(out var b)) return b;
        if (v.TryGetValue<String>(out var s) && Boolean.TryParse(s, out b)) return b;
        return null;
    }

    /// <summary>取 data 中指定键的值</summary>
    public JsonNode? this[String name] => Data?[name];

    /// <summary>调试输出。断言失败时附带原始响应</summary>
    public override String ToString() => $"HTTP {StatusCode} {ContentType}\n{Content}";
}

/// <summary>轻量 HTTP 客户端，封装认证头、JSON 提交与常用断言</summary>
public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;

    /// <summary>实例化客户端</summary>
    /// <param name="baseUrl">站点根地址，默认使用测试夹具的 BaseUrl</param>
    /// <remarks>启用 Cookie 容器：/Cube/* 服务接口按浏览器会话（.Cube.Session）鉴权，Bearer 令牌只覆盖 /api/* 实体接口</remarks>
    public ApiClient(String? baseUrl = null)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer(),
        };
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl ?? ApiAppFixture.BaseUrl),
            Timeout = TimeSpan.FromSeconds(90),
        };
    }

    /// <summary>访问令牌</summary>
    public String? Token { get; set; }

    /// <summary>刷新令牌</summary>
    public String? RefreshToken { get; set; }

    /// <summary>令牌放置方式：Bearer=Authorization 头（默认），XToken=X-Token 头，Query=查询串</summary>
    public String TokenMode { get; set; } = "Bearer";

    /// <summary>使用管理员账号登录并记录令牌</summary>
    public async Task LoginAsync(String? user = null, String? pass = null)
    {
        var body = JsonSerializer.Serialize(new { username = user ?? ApiAppFixture.AdminUser, password = pass ?? ApiAppFixture.AdminPass });
        var rs = await PostJsonAsync("/Auth/Login", body);
        ApiAssert.Ok(rs, "Login");

        Token = ApiResult.GetString(rs.Data?["access_token"]);
        RefreshToken = ApiResult.GetString(rs.Data?["refresh_token"]);
    }

    /// <summary>发送请求</summary>
    public async Task<ApiResult> SendAsync(HttpMethod method, String path, String? json = null, String? contentType = null, IDictionary<String, String>? headers = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (json != null)
            request.Content = new StringContent(json, Encoding.UTF8, contentType ?? "application/json");

        ApplyAuth(request, path);

        if (headers != null)
        {
            foreach (var item in headers)
                request.Headers.TryAddWithoutValidation(item.Key, item.Value);
        }

        using var response = await _http.SendAsync(request);
        return await BuildResultAsync(response);
    }

    /// <summary>提交 multipart 表单（文件上传、导入）</summary>
    /// <param name="path">请求路径</param>
    /// <param name="fileField">文件字段名</param>
    /// <param name="fileName">文件名（扩展名决定导入格式）</param>
    /// <param name="content">文件内容</param>
    /// <param name="contentType">文件 Content-Type</param>
    /// <param name="fields">附加表单字段</param>
    public async Task<ApiResult> PostMultipartAsync(String path, String fileField, String fileName, Byte[] content, String contentType, IDictionary<String, String>? fields = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, fileField, fileName);

        if (fields != null)
        {
            foreach (var item in fields)
                form.Add(new StringContent(item.Value), item.Key);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        ApplyAuth(request, path);

        using var response = await _http.SendAsync(request);
        return await BuildResultAsync(response);
    }

    private void ApplyAuth(HttpRequestMessage request, String path)
    {
        if (String.IsNullOrEmpty(Token)) return;

        switch (TokenMode)
        {
            case "XToken":
                request.Headers.TryAddWithoutValidation("X-Token", Token);
                break;
            case "Query":
                var sep = path.Contains('?') ? '&' : '?';
                request.RequestUri = new Uri(_http.BaseAddress + path.TrimStart('/') + sep + "token=" + Uri.EscapeDataString(Token));
                break;
            default:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                break;
        }
    }

    private static async Task<ApiResult> BuildResultAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var result = new ApiResult
        {
            StatusCode = (Int32)response.StatusCode,
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString(),
        };
        foreach (var header in response.Headers)
            result.Headers[header.Key] = String.Join(",", header.Value);
        foreach (var header in response.Content.Headers)
            result.Headers[header.Key] = String.Join(",", header.Value);

        return result;
    }

    /// <summary>GET 请求</summary>
    public Task<ApiResult> GetAsync(String path) => SendAsync(HttpMethod.Get, path);

    /// <summary>POST JSON 请求</summary>
    public Task<ApiResult> PostJsonAsync(String path, String json) => SendAsync(HttpMethod.Post, path, json);

    /// <summary>POST JSON 请求（对象序列化）</summary>
    public Task<ApiResult> PostJsonAsync(String path, Object value) => PostJsonAsync(path, JsonSerializer.Serialize(value));

    /// <summary>PUT JSON 请求</summary>
    public Task<ApiResult> PutJsonAsync(String path, String json) => SendAsync(HttpMethod.Put, path, json);

    /// <summary>PUT JSON 请求（对象序列化）</summary>
    public Task<ApiResult> PutJsonAsync(String path, Object value) => PutJsonAsync(path, JsonSerializer.Serialize(value));

    /// <summary>DELETE 请求</summary>
    public Task<ApiResult> DeleteAsync(String path) => SendAsync(HttpMethod.Delete, path);

    /// <summary>释放 HTTP 客户端</summary>
    public void Dispose() => _http.Dispose();
}
