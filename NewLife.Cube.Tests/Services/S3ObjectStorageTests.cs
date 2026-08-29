using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Services;
using NewLife.Data;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 覆盖 <see cref="S3ObjectStorage"/> 的SigV4签名与S3操作，验证Authorization头、路径风格Url与预签名Url。
/// </summary>
public class S3ObjectStorageTests
{
    /// <summary>捕获请求的HttpMessageHandler，返回固定响应</summary>
    private class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public Byte[] Body { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            Body = request.Content == null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);

            var rs = new HttpResponseMessage(HttpStatusCode.OK);
            if (request.Method == HttpMethod.Get)
                rs.Content = new ByteArrayContent("hello"u8.ToArray());
            return rs;
        }
    }

    /// <summary>测试用S3客户端，注入捕获Handler</summary>
    private class TestS3 : S3ObjectStorage
    {
        public CaptureHandler Handler { get; } = new CaptureHandler();

        protected override HttpClient GetClient() => new HttpClient(Handler);
    }

    private static TestS3 CreateClient() => new()
    {
        Server = "oss-cn-beijing.aliyuncs.com",
        AppId = "testkey",
        Secret = "testsecret",
        Bucket = "mybucket",
        Region = "cn-beijing",
    };

    [Fact]
    [System.ComponentModel.DisplayName("PutAsync_发送Authorization签名头与内容")]
    public async Task PutAsync_UploadsWithSignature()
    {
        var client = CreateClient();
        var data = Encoding.UTF8.GetBytes("hello cube");

        var info = await client.PutAsync("dir/test.txt", new ArrayPacket(data));

        var req = client.Handler.LastRequest!;
        Assert.Equal(HttpMethod.Put, req.Method);
        Assert.Equal("https://oss-cn-beijing.aliyuncs.com/mybucket/dir/test.txt", req.RequestUri!.AbsoluteUri);

        var auth = req.Headers.GetValues("Authorization").First();
        Assert.StartsWith("AWS4-HMAC-SHA256 Credential=testkey/", auth);
        Assert.Contains("SignedHeaders=host;x-amz-content-sha256;x-amz-date", auth);
        // 签名值64位十六进制
        var sig = auth.Split("Signature=").Last();
        Assert.Equal(64, sig.Length);
        Assert.True(sig.All(Uri.IsHexDigit));

        Assert.Equal("UNSIGNED-PAYLOAD", req.Headers.GetValues("x-amz-content-sha256").First());
        Assert.Equal(data, client.Handler.Body);
        Assert.NotNull(info);
        Assert.Equal(10, info!.Length);
    }

    [Fact]
    [System.ComponentModel.DisplayName("GetAsync_下载对象内容")]
    public async Task GetAsync_DownloadsContent()
    {
        var client = CreateClient();

        var info = await client.GetAsync("dir/test.txt");

        Assert.NotNull(info);
        Assert.Equal("hello", Encoding.UTF8.GetString(info!.Data!.ToArray()));

        var req = client.Handler.LastRequest!;
        Assert.Equal(HttpMethod.Get, req.Method);
        Assert.Equal("https://oss-cn-beijing.aliyuncs.com/mybucket/dir/test.txt", req.RequestUri!.AbsoluteUri);
        Assert.Contains("AWS4-HMAC-SHA256", req.Headers.GetValues("Authorization").First());
    }

    [Fact]
    [System.ComponentModel.DisplayName("ExistsAsync_发送HEAD请求")]
    public async Task ExistsAsync_SendsHead()
    {
        var client = CreateClient();

        var rs = await client.ExistsAsync("dir/test.txt");

        Assert.True(rs);
        Assert.Equal(HttpMethod.Head, client.Handler.LastRequest!.Method);
    }

    [Fact]
    [System.ComponentModel.DisplayName("DeleteAsync_发送DELETE请求")]
    public async Task DeleteAsync_SendsDelete()
    {
        var client = CreateClient();

        var count = await client.DeleteAsync("dir/test.txt");

        Assert.Equal(1, count);
        Assert.Equal(HttpMethod.Delete, client.Handler.LastRequest!.Method);
        Assert.Equal("https://oss-cn-beijing.aliyuncs.com/mybucket/dir/test.txt", client.Handler.LastRequest!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    [System.ComponentModel.DisplayName("GetUrlAsync_生成预签名Url")]
    public async Task GetUrlAsync_PresignedUrl()
    {
        var client = CreateClient();

        var url = await client.GetUrlAsync("dir/test.txt");

        Assert.NotNull(url);
        Assert.StartsWith("https://oss-cn-beijing.aliyuncs.com/mybucket/dir/test.txt?", url);
        Assert.Contains("X-Amz-Algorithm=AWS4-HMAC-SHA256", url);
        Assert.Contains("X-Amz-Credential=testkey", url);
        Assert.Contains("X-Amz-Expires=3600", url);
        Assert.Contains("X-Amz-Signature=", url);
    }

    [Fact]
    [System.ComponentModel.DisplayName("UriEncode_中文文件名正确编码")]
    public async Task PutAsync_ChineseFileNameEncoded()
    {
        var client = CreateClient();

        await client.PutAsync("附件/中文 文件.txt", new ArrayPacket("x"u8.ToArray()));

        var url = client.Handler.LastRequest!.RequestUri!.AbsoluteUri;
        // 中文与空格按RFC3986百分号编码
        Assert.Contains("%E9%99%84%E4%BB%B6", url);
        Assert.Contains("%20", url);
    }
}
