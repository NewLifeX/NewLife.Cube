using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Cube.Services;

/// <summary>列表型值集默认数据代理实现。
/// 以 HTTP 客户端（<see cref="IHttpClientFactory"/>）向外部数据源发起 GET/POST 请求，
/// 支持分页参数、固定参数，并按 DataPath/TotalPath 从响应 JSON 中抽取数据与总数。
/// 该实现通过 <see cref="LovServiceExtensions.AddCubeLov"/> 以 TryAddSingleton 注册，使用者可自定义实现覆盖。</summary>
public class DefaultLovListDataProxy : ILovListDataProxy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefaultLovListDataProxy>? _logger;

    /// <summary>实例化默认代理</summary>
    /// <param name="httpClientFactory">HTTP 客户端工厂</param>
    /// <param name="logger">日志（可选）</param>
    public DefaultLovListDataProxy(IHttpClientFactory httpClientFactory, ILogger<DefaultLovListDataProxy>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LovListDataProxyResult> FetchAsync(LovListConfig config, LovListDataRequest request, CancellationToken cancellationToken = default)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var url = config.RequestUrl;
        if (url.IsNullOrEmpty())
            throw new InvalidOperationException($"列表型值集未配置 RequestUrl，无法代理请求");

        var method = (config.Method ?? "GET").ToUpper();
        var httpClient = _httpClientFactory.CreateClient(nameof(DefaultLovListDataProxy));

        HttpResponseMessage httpResponse;
        if (method == "GET")
        {
            // GET：参数拼接到 URL（搜索参数 + 分页 + 固定参数）
            var queryParams = new List<String>();
            if (request.Params != null)
            {
                foreach (var kv in request.Params)
                {
                    if (kv.Value != null)
                        queryParams.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString()!)}");
                }
            }
            if (config.Pageable)
            {
                if (request.PageNum > 0 && !config.PageNumField.IsNullOrEmpty())
                    queryParams.Add($"{Uri.EscapeDataString(config.PageNumField)}={request.PageNum}");
                if (request.PageSize > 0 && !config.PageSizeField.IsNullOrEmpty())
                    queryParams.Add($"{Uri.EscapeDataString(config.PageSizeField)}={request.PageSize}");
            }
            if (!config.FixedParams.IsNullOrEmpty())
            {
                var fixedParams = JsonParser.Decode(config.FixedParams) as IDictionary<String, Object>;
                if (fixedParams != null)
                {
                    foreach (var kv in fixedParams)
                    {
                        if (kv.Value != null)
                            queryParams.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString()!)}");
                    }
                }
            }

            var queryString = String.Join("&", queryParams);
            if (!queryString.IsNullOrEmpty())
                url = url.Contains('?') ? $"{url}&{queryString}" : $"{url}?{queryString}";

            httpResponse = await httpClient.GetAsync(url, cancellationToken);
        }
        else
        {
            // POST：参数放 Body（搜索参数 + 分页 + 固定参数）
            var bodyParams = new Dictionary<String, Object>();
            if (request.Params != null)
            {
                foreach (var kv in request.Params)
                    bodyParams[kv.Key] = kv.Value!;
            }
            if (config.Pageable)
            {
                if (request.PageNum > 0 && !config.PageNumField.IsNullOrEmpty())
                    bodyParams[config.PageNumField] = request.PageNum;
                if (request.PageSize > 0 && !config.PageSizeField.IsNullOrEmpty())
                    bodyParams[config.PageSizeField] = request.PageSize;
            }
            if (!config.FixedParams.IsNullOrEmpty())
            {
                var fixedParams = JsonParser.Decode(config.FixedParams) as IDictionary<String, Object>;
                if (fixedParams != null)
                {
                    foreach (var kv in fixedParams)
                        bodyParams[kv.Key] = kv.Value!;
                }
            }

            var json = bodyParams.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            httpResponse = await httpClient.PostAsync(url, content, cancellationToken);
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"请求外部接口失败：{httpResponse.StatusCode} - {responseBody}");

        // 解析响应：按 DataPath 取数据列表，按 TotalPath 取总数
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        JsonElement? dataElement = !config.DataPath.IsNullOrEmpty()
            ? ResolveJsonPath(root, config.DataPath)
            : root;

        Int32 total = 0;
        if (config.Pageable && !config.TotalPath.IsNullOrEmpty())
        {
            var totalElement = ResolveJsonPath(root, config.TotalPath);
            if (totalElement.HasValue)
                total = totalElement.Value.GetInt32();
        }

        // 将数据节点转换为原生 CLR 对象图（List/Dictionary/标量），保证后续任意 JSON 写入器都能序列化为数组。
        // 注意：NewLife 的 JsonParser.Decode 仅支持对象，对 JSON 数组会返回 null，故此处不能用其解析列表数据。
        var dataObj = dataElement.HasValue ? JsonElementToObject(dataElement.Value) : null;

        return new LovListDataProxyResult
        {
            Data = dataObj,
            Total = total,
        };
    }

    /// <summary>将 <see cref="JsonElement"/> 递归转换为原生 CLR 对象（数组→List&lt;Object&gt;，对象→Dictionary&lt;String,Object&gt;，标量→对应基础类型）。</summary>
    private static Object? JsonElementToObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<String, Object?>();
                foreach (var prop in element.EnumerateObject())
                    dict[prop.Name] = JsonElementToObject(prop.Value);
                return dict;
            case JsonValueKind.Array:
                var list = new List<Object?>();
                foreach (var item in element.EnumerateArray())
                    list.Add(JsonElementToObject(item));
                return list;
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                // 优先保留整数精度，否则按双精度
                if (element.TryGetInt64(out var l)) return l;
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            default:
                return element.GetRawText();
        }
    }

    /// <summary>解析 JSON 路径表达式（如 data.records）。空路径返回根节点。</summary>
    /// <param name="root">JSON 根元素</param>
    /// <param name="path">点号分隔的路径</param>
    /// <returns>命中的子元素；路径不存在返回 null</returns>
    public static JsonElement? ResolveJsonPath(JsonElement root, String path)
    {
        if (path.IsNullOrEmpty()) return root;

        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonElement current = root;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            if (!current.TryGetProperty(part, out current)) return null;
        }

        return current;
    }
}
