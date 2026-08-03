using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 覆盖 <see cref="DefaultLovListDataProxy"/> —— 列表型值集的默认数据代理实现（HTTP 客户端转发）。
/// 通过自定义 HttpMessageHandler 与 IHttpClientFactory 在完全离线的情况下验证：
///   1) GET 请求正确拼接分页参数、固定参数与搜索参数；
///   2) 按 DataPath/TotalPath 从响应中抽取数据与总数；
///   3) POST 请求把参数放入请求体。
/// </summary>
public class DefaultLovListDataProxyTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly String _responseJson;
        public HttpRequestMessage? CapturedRequest { get; private set; }

        public CapturingHandler(String responseJson) => _responseJson = responseJson;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(resp);
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(String name) => new HttpClient(_handler);
    }

    [Fact]
    public async Task FetchAsync_Get_ParsesDataPathAndTotal_WithPagingAndFixedParams()
    {
        // 外部接口返回 { data:[...], total:N }
        const String externalJson = "{\"data\":[{\"id\":1,\"name\":\"管理员\"},{\"id\":2,\"name\":\"普通用户\"}],\"total\":2}";
        var handler = new CapturingHandler(externalJson);
        var proxy = new DefaultLovListDataProxy(new FakeHttpClientFactory(handler));

        var config = new LovListConfig
        {
            RequestUrl = "http://external/api/roles",
            Method = "GET",
            Pageable = true,
            PageNumField = "pageIndex",
            PageSizeField = "pageSize",
            DataPath = "data",
            TotalPath = "total",
            FixedParams = "{\"tenantId\":42}",
        };

        var request = new LovListDataRequest
        {
            LovCode = "List.X.Role",
            Params = new Dictionary<String, Object> { ["name"] = "管理员" },
            PageNum = 1,
            PageSize = 20,
        };

        // Act
        var result = await proxy.FetchAsync(config, request);

        // Assert: 总数解析
        Assert.Equal(2, result.Total);

        // Assert: 数据数组解析（JsonElementToObject 返回 IList）
        var dataList = result.Data as System.Collections.IList;
        Assert.NotNull(dataList);
        Assert.Equal(2, dataList!.Count);

        // Assert: 请求 URL 含分页与固定参数（GET 拼接到 query）
        var uri = handler.CapturedRequest!.RequestUri!.ToString();
        Assert.Contains("pageIndex=1", uri);
        Assert.Contains("pageSize=20", uri);
        Assert.Contains("tenantId=42", uri);
        Assert.Contains("name=", uri); // 值会被 URL 编码，仅校验键存在
    }

    [Fact]
    public async Task FetchAsync_Post_SendsBodyParams()
    {
        const String externalJson = "{\"data\":[],\"total\":0}";
        var handler = new CapturingHandler(externalJson);
        var proxy = new DefaultLovListDataProxy(new FakeHttpClientFactory(handler));

        var config = new LovListConfig
        {
            RequestUrl = "http://external/api/roles",
            Method = "POST",
            Pageable = false,
            DataPath = null,
            TotalPath = null,
        };
        var request = new LovListDataRequest
        {
            Params = new Dictionary<String, Object> { ["q"] = "x" },
        };

        // Act
        var result = await proxy.FetchAsync(config, request);

        // Assert: Data 非空（空数组也会被解析）
        Assert.NotNull(result.Data);

        // Assert: POST 且参数进入请求体
        var req = handler.CapturedRequest!;
        Assert.Equal(HttpMethod.Post, req.Method);
        var body = await req.Content!.ReadAsStringAsync();
        Assert.Contains("q", body);
    }
}
