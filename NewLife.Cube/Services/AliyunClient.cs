using System.Security.Cryptography;
using System.Web;
using NewLife.Collections;
using NewLife.Remoting;

namespace NewLife.Cube.Services;

/// <summary>阿里云客户端</summary>
public class AliyunClient : ApiHttpClient
{
    #region 属性
    /// <summary>连接点</summary>
    public String Endpoint { get; set; }

    /// <summary>AK</summary>
    public String AccessKeyId { get; set; }

    /// <summary>密钥</summary>
    public String AccessKeySecret { get; set; }

    /// <summary>签名算法</summary>
    public String SignatureAlgorithm { get; set; } = "ACS3-HMAC-SHA256";

    /// <summary>签名版本</summary>
    public String SignatureVersion { get; set; } = "1.0";

    /// <summary>版本</summary>
    public String Version { get; set; } = "2017-05-25";
    #endregion

    #region 构造
    /// <summary>构造函数</summary>
    public AliyunClient()
    {
        CodeName = "Code";
        DataName = "Message";
    }
    #endregion

    #region 核心
    /// <summary>发送请求。添加服务器</summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Services.Count == 0)
        {
            var url = Endpoint;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Endpoint));

            if (!url.StartsWithIgnoreCase("http://", "https://")) url = "https://" + url;

            SetServer(url);
        }

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>生成请求</summary>
    /// <param name="method"></param>
    /// <param name="action"></param>
    /// <param name="args"></param>
    /// <param name="returnType"></param>
    /// <returns></returns>
    protected override HttpRequestMessage BuildRequest(HttpMethod method, String action, Object args, Type returnType)
    {
        var parameters = args?.ToDictionary();
        var request = base.BuildRequest(method, action, parameters, returnType);
        if (request.Headers.Host.IsNullOrEmpty())
            request.Headers.Host = Endpoint;

        // 1. 构造规范化请求
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var nonce = Guid.NewGuid().ToString("N");
        //var timestamp = "2025-12-25T09:27:50Z";
        //var nonce = "84daf241047949e4ba0d580faeb91363";

        var headers = new Dictionary<String, String>
        {
            //["x-acs-accesskey-id"] = AccessKeyId,
            ["x-acs-action"] = action,
            ["x-acs-date"] = timestamp,
            //["x-acs-signature-method"] = SignatureAlgorithm,
            ["x-acs-signature-nonce"] = nonce,
            //["x-acs-signature-version"] = SignatureVersion,
            ["x-acs-version"] = Version,
        };
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        // 2. 计算签名
        var signedHeaders = new List<String>();
        var canonicalRequest = CreateCanonicalRequest(request, parameters, signedHeaders);
        var hashedRequest = ComputeSha256(canonicalRequest);
        var stringToSign = $"{SignatureAlgorithm}\n{hashedRequest}";
        var signature = stringToSign.GetBytes().SHA256(AccessKeySecret.GetBytes()).ToHex("").ToLowerInvariant();

        // 3. 构造最终请求
        request.Headers.TryAddWithoutValidation("Authorization", $"{SignatureAlgorithm} Credential={AccessKeyId},SignedHeaders={signedHeaders.Join(";")},Signature={signature}");

        return request;
    }

    /// <summary>构造规范化请求</summary>
    /// <param name="request"></param>
    /// <param name="parameters"></param>
    /// <param name="signedHeaders"></param>
    /// <returns></returns>
    private String CreateCanonicalRequest(HttpRequestMessage request, IDictionary<String, Object> parameters, IList<String> signedHeaders)
    {
        // 规范化URI。URL的资源路径部分经过编码之后的结果。资源路径部分指URL中host与查询字符串之间的部分，包含host之后的/但不包含查询字符串前的?。
        var uri = request.RequestUri;
        var canonicalURI = uri.ToString().EnsureStart("/");
        var p = canonicalURI.IndexOf('?');
        if (p > 0) canonicalURI = canonicalURI[..p];

        // 规范化查询字符串。如果API的请求参数信息包含了"in":"query"时，需要将这些请求参数按照如下构造方法拼接起来
        var canonicalQuery = request.Method != HttpMethod.Get ? null : String.Join("&", parameters.OrderBy(p => p.Key)
            .Select(p => $"{HttpUtility.UrlEncode(p.Key)}={HttpUtility.UrlEncode(p.Value + "")}"));

        // HashedRequestPayload。RequestBody经过Hash摘要处理后再进行Base16编码得到HashedRequestPayload，并将RequestHeader中x-acs-content-sha256的值更新为HashedRequestPayload的值。
        var body = request.Method != HttpMethod.Post ? null : request.Content.ReadAsStream().ReadBytes(-1);
        var hashedRequestPayload = ComputeSha256(body);
        request.Headers.Add("x-acs-content-sha256", hashedRequestPayload);

        // 已签名消息头列表。用于说明此次请求中参与签名的公共请求头信息，与CanonicalHeaders中的参数名称一一对应。
        //var signedHeaders = new List<String>();

        // 规范化请求头。过滤出RequestHeader中包含以x-acs-为前缀、host、content-type的参数。
        var canonicalHeaders = Pool.StringBuilder.Get();
        foreach (var header in request.Headers.OrderBy(h => h.Key))
        {
            if (header.Key.StartsWith("x-acs-") || header.Key.EqualIgnoreCase("host", "content-type"))
            {
                var name = header.Key.ToLowerInvariant();
                canonicalHeaders.Append($"{name}:{header.Value?.FirstOrDefault().Trim()}\n");
                signedHeaders.Add(name);
            }
        }

        return $"{request.Method}\n{canonicalURI}\n{canonicalQuery}\n{canonicalHeaders.Return(true)}\n{signedHeaders.Join(";")}\n{hashedRequestPayload}";
    }

    private static String ComputeSha256(String input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(input.GetBytes());
        return bytes.ToHex("").ToLowerInvariant();
    }

    private static String ComputeSha256(Byte[] input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(input);
        return bytes.ToHex("").ToLowerInvariant();
    }
    #endregion
}
