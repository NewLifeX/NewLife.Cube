using System.Net;
using System.Security.Cryptography;
using System.Text;
using NewLife;
using NewLife.Collections;
using NewLife.Data;
using NewLife.IO;
using NewLife.Log;

namespace NewLife.Cube.Services;

/// <summary>S3兼容对象存储。基于AWS SigV4签名，路径风格访问，对接阿里云OSS、腾讯云COS、七牛Kodo</summary>
/// <remarks>
/// 三家云厂商均支持S3兼容协议，通过端点与区域配置切换，无需引入各家SDK。
/// <code>
/// // 阿里云OSS
/// Server = "oss-cn-beijing.aliyuncs.com", Bucket = "mybucket", Region = "cn-beijing"
/// // 腾讯云COS
/// Server = "cos.ap-beijing.myqcloud.com", Bucket = "mybucket-1250000000", Region = "ap-beijing"
/// // 七牛Kodo
/// Server = "s3-cn-east-1.qiniucs.com", Bucket = "mybucket", Region = "cn-east-1"
/// </code>
/// 上传使用UNSIGNED-PAYLOAD流式传输，避免大文件整体缓冲。
/// </remarks>
public class S3ObjectStorage : IObjectStorage, ILogFeature, ITracerFeature, IDisposable
{
    #region 属性
    /// <summary>服务端地址。S3兼容端点，如 oss-cn-beijing.aliyuncs.com，可带http(s)前缀</summary>
    public String Server { get; set; }

    /// <summary>应用标识。AccessKeyId</summary>
    public String AppId { get; set; }

    /// <summary>应用密钥。AccessKeySecret</summary>
    public String Secret { get; set; }

    /// <summary>存储桶</summary>
    public String Bucket { get; set; }

    /// <summary>区域。用于签名作用域，如 cn-beijing / ap-beijing / cn-east-1</summary>
    public String Region { get; set; } = "cn-north-1";

    /// <summary>预签名Url有效期。默认3600秒</summary>
    public Int32 ExpireSeconds { get; set; } = 3600;

    /// <summary>是否支持获取文件直接访问Url</summary>
    public Boolean CanGetUrl => true;

    /// <summary>是否支持删除</summary>
    public Boolean CanDelete => true;

    /// <summary>是否支持搜索</summary>
    public Boolean CanSearch => false;

    /// <summary>是否支持复制</summary>
    public Boolean CanCopy => false;

    /// <summary>追踪器</summary>
    public ITracer Tracer { get; set; }

    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    private HttpClient _client;
    private String _emptyHash = Sha256Hex("");
    #endregion

    #region 基础方法
    /// <summary>获取HttpClient</summary>
    /// <returns></returns>
    protected virtual HttpClient GetClient() => _client ??= new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

    /// <summary>获取对象</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>文件对象信息，不存在时返回null</returns>
    public async Task<IObjectInfo?> GetAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        using var span = Tracer?.NewSpan("S3-Get", id);

        var (url, host, amzDate, auth) = BuildRequest("GET", id, null);
        using var req = CreateRequest(HttpMethod.Get, url, host, amzDate, _emptyHash, auth);

        var rs = await GetClient().SendAsync(req);
        if (!rs.IsSuccessStatusCode) return null;

        var buf = await rs.Content.ReadAsByteArrayAsync();
        return new ObjectInfo { Name = id, Length = buf.Length, Data = new ArrayPacket(buf) };
    }

    /// <summary>获取对象直接访问Url（预签名）</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>可直接访问的Url地址</returns>
    public Task<String?> GetUrlAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var (url, _, _, _) = BuildRequest("GET", id, null, ExpireSeconds);
        return Task.FromResult<String?>(url);
    }

    /// <summary>检查对象是否存在</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>存在返回true，不存在返回false</returns>
    public async Task<Boolean> ExistsAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        using var span = Tracer?.NewSpan("S3-Exists", id);

        var (url, host, amzDate, auth) = BuildRequest("HEAD", id, null);
        using var req = CreateRequest(HttpMethod.Head, url, host, amzDate, _emptyHash, auth);

        var rs = await GetClient().SendAsync(req);
        return rs.IsSuccessStatusCode;
    }

    /// <summary>上传对象</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <param name="data">数据内容</param>
    /// <returns>文件对象信息</returns>
    public async Task<IObjectInfo?> PutAsync(String id, IPacket data)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var span = Tracer?.NewSpan("S3-Put", id);

        var (url, host, amzDate, auth) = BuildRequest("PUT", id, "UNSIGNED-PAYLOAD");
        using var req = CreateRequest(HttpMethod.Put, url, host, amzDate, "UNSIGNED-PAYLOAD", auth);

        // 流式上传，避免大文件整体缓冲
        var stream = data.GetStream();
        req.Content = new StreamContent(stream);
        if (data.Length > 0) req.Content.Headers.ContentLength = data.Length;

        var rs = await GetClient().SendAsync(req);
        if (!rs.IsSuccessStatusCode)
        {
            var msg = await rs.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"上传对象[{id}]失败：{(Int32)rs.StatusCode} {rs.ReasonPhrase} {msg}");
        }

        return new ObjectInfo { Name = id, Length = data.Length, Data = data };
    }

    /// <summary>删除对象</summary>
    /// <param name="id">对象文件名，支持斜杠目录结构</param>
    /// <returns>删除成功的数量</returns>
    public async Task<Int32> DeleteAsync(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        using var span = Tracer?.NewSpan("S3-Delete", id);

        var (url, host, amzDate, auth) = BuildRequest("DELETE", id, null);
        using var req = CreateRequest(HttpMethod.Delete, url, host, amzDate, _emptyHash, auth);

        var rs = await GetClient().SendAsync(req);
        return rs.IsSuccessStatusCode ? 1 : 0;
    }

    /// <summary>批量删除对象</summary>
    /// <param name="ids">对象文件名列表</param>
    /// <returns>删除成功的数量</returns>
    public async Task<Int32> DeleteAsync(String[] ids)
    {
        if (ids == null || ids.Length == 0) return 0;

        var count = 0;
        foreach (var id in ids)
            count += await DeleteAsync(id);

        return count;
    }

    /// <summary>复制对象。S3兼容存储暂不支持</summary>
    /// <param name="sourceId">源对象文件名</param>
    /// <param name="destId">目标对象文件名</param>
    /// <returns></returns>
    public Task<IObjectInfo?> CopyAsync(String sourceId, String destId) => throw new NotSupportedException("S3兼容对象存储暂不支持复制");

    /// <summary>搜索对象。S3兼容存储暂不支持</summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="start">开始序号</param>
    /// <param name="count">最大个数</param>
    /// <returns></returns>
    public Task<IList<IObjectInfo>?> SearchAsync(String? pattern = null, Int32 start = 0, Int32 count = 100) => throw new NotSupportedException("S3兼容对象存储暂不支持搜索");

    #region 兼容旧版
    /// <summary>获取对象</summary>
    /// <param name="id">对象文件名</param>
    /// <returns></returns>
    [Obsolete("请使用GetAsync")]
    public Task<IObjectInfo?> Get(String id) => GetAsync(id);

    /// <summary>获取对象下载Url</summary>
    /// <param name="id">对象文件名</param>
    /// <returns></returns>
    [Obsolete("请使用GetUrlAsync")]
    public Task<String?> GetUrl(String id) => GetUrlAsync(id);

    /// <summary>上传对象</summary>
    /// <param name="id">对象文件名</param>
    /// <param name="data">数据内容</param>
    /// <returns></returns>
    [Obsolete("请使用PutAsync")]
    public Task<IObjectInfo?> Put(String id, IPacket data) => PutAsync(id, data);

    /// <summary>删除对象</summary>
    /// <param name="id">对象文件名</param>
    /// <returns></returns>
    [Obsolete("请使用DeleteAsync")]
    public Task<Int32> Delete(String id) => DeleteAsync(id);

    /// <summary>搜索对象</summary>
    /// <param name="pattern">匹配模式</param>
    /// <param name="start">开始序号</param>
    /// <param name="count">最大个数</param>
    /// <returns></returns>
    [Obsolete("请使用SearchAsync")]
    public Task<IList<IObjectInfo>?> Search(String? pattern = null, Int32 start = 0, Int32 count = 100) => SearchAsync(pattern, start, count);
    #endregion

    private HttpRequestMessage CreateRequest(HttpMethod method, String url, String host, String amzDate, String payloadHash, String auth)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Host = host;
        req.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        req.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        req.Headers.TryAddWithoutValidation("Authorization", auth);
        return req;
    }
    #endregion

    #region SigV4签名
    /// <summary>构建请求Url与签名头。expires大于0时生成预签名Url</summary>
    /// <param name="method">HTTP方法</param>
    /// <param name="id">对象文件名</param>
    /// <param name="payloadHash">负载哈希。null时使用空负载哈希</param>
    /// <param name="expires">预签名有效期（秒），大于0时生成预签名Url</param>
    /// <returns>Url、Host、时间戳、Authorization头或签名值</returns>
    private (String url, String host, String amzDate, String auth) BuildRequest(String method, String id, String payloadHash, Int32 expires = 0)
    {
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var dateStamp = now.ToString("yyyyMMdd");
        var scope = $"{dateStamp}/{Region}/s3/aws4_request";

        if (AppId.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AppId), "缺少对象存储应用标识AccessKeyId");
        if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret), "缺少对象存储应用密钥AccessKeySecret");

        var server = Server;
        if (server.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server), "缺少对象存储服务器地址");
        if (!server.StartsWithIgnoreCase("http://", "https://")) server = "https://" + server;
        server = server.TrimEnd('/');

        // 路径风格：/Bucket/Key
        var canonicalUri = EncodePath("/" + Bucket + "/" + id);

        // 预签名：查询参数包含X-Amz-*，签名后追加X-Amz-Signature
        String canonicalQuery;
        var presign = expires > 0;
        if (presign)
        {
            var query = new SortedDictionary<String, String>(StringComparer.Ordinal)
            {
                ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
                ["X-Amz-Credential"] = $"{AppId}/{scope}",
                ["X-Amz-Date"] = amzDate,
                ["X-Amz-Expires"] = expires + "",
                ["X-Amz-SignedHeaders"] = "host",
            };
            canonicalQuery = String.Join("&", query.Select(e => UriEncode(e.Key, true) + "=" + UriEncode(e.Value, true)));
        }
        else
            canonicalQuery = "";

        var uri = new Uri(server + canonicalUri);
        var host = uri.Authority;
        var payload = presign ? "UNSIGNED-PAYLOAD" : (payloadHash ?? _emptyHash);

        var canonicalRequest = $"{method}\n{canonicalUri}\n{canonicalQuery}\n" +
            $"host:{host}\nx-amz-content-sha256:{payload}\nx-amz-date:{amzDate}\n" +
            $"host;x-amz-content-sha256;x-amz-date\n{payload}";

        var sts = $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{Sha256Hex(canonicalRequest)}";

        var key = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + Secret), dateStamp);
        key = HmacSha256(key, Region);
        key = HmacSha256(key, "s3");
        key = HmacSha256(key, "aws4_request");
        var signature = HmacSha256(key, sts).ToHex();

        var url = uri.AbsoluteUri;
        if (presign)
            return (url + "?" + canonicalQuery + "&X-Amz-Signature=" + signature, host, amzDate, signature);

        var auth = $"AWS4-HMAC-SHA256 Credential={AppId}/{scope}, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature={signature}";
        return (url, host, amzDate, auth);
    }

    private static String Sha256Hex(String data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return hash.ToHex();
    }

    private static Byte[] HmacSha256(Byte[] key, String data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>URI编码。按RFC3986编码，保留安全字符</summary>
    /// <param name="value">待编码字符串</param>
    /// <param name="encodeSlash">是否编码斜杠</param>
    /// <returns></returns>
    private static String UriEncode(String value, Boolean encodeSlash)
    {
        var sb = Pool.StringBuilder.Get();
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            var c = (Char)b;
            if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' ||
                c == '-' || c == '_' || c == '.' || c == '~' || c == '/' && encodeSlash)
                sb.Append(c);
            else
                sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.Return(true);
    }

    /// <summary>编码路径。逐段编码，保留斜杠分隔符</summary>
    /// <param name="path">路径</param>
    /// <returns></returns>
    private static String EncodePath(String path)
    {
        var parts = path.Split('/');
        for (var i = 0; i < parts.Length; i++)
            parts[i] = UriEncode(parts[i], false);

        return String.Join("/", parts);
    }
    #endregion

    #region 日志
    /// <summary>写日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info(format, args);

    /// <summary>销毁</summary>
    public void Dispose() => _client?.Dispose();
    #endregion
}
