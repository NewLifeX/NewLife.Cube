using NewLife.Remoting.CloudProviders;

namespace NewLife.Cube.Services;

/// <summary>阿里云短信验证码</summary>
/// <remarks>
/// 阿里云短信服务实现，支持登录、重置、绑定三种验证码场景。
/// 开发者需要配置 SignName、AccessKeyId、AccessKeySecret 等参数。
/// </remarks>
public class AliyunSmsVerifyCode : ISmsVerifyCode
{
    #region 属性
    /// <summary>签名名称</summary>
    public String SignName { get; set; }

    /// <summary>方案名称</summary>
    public String SchemaName { get; set; }

    /// <summary>验证码长度</summary>
    public Int32 CodeLength { get; set; } = 4;

    /// <summary>阿里云客户端</summary>
    public AliyunClient Client { get; set; } = new() { Endpoint = "dypnsapi.aliyuncs.com" };
    #endregion

    #region 方法

    /// <summary>核心发送</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="templateCode">模版代码</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual Task<String> SendAsync(String mobile, String templateCode, String code, Int32 expire, SmsVerifyCodeOptions options = null)
    {
        var client = Client;
        /* Yann：手动设置服务器地址到 Services 列表，避免 InvokeAsync 中 Services.Count == 0 的问题;
        --这里应该使用Client的SendAsync方法AliyunClient.SendAsync，SendAsync方法内部处理了Services为空时设置服务器地址的问题。但由于SendAsync方法是protected的，无法直接调用，所以只能手动设置服务器地址。
        看看后续是否需要调整。
         */
        if (client.Services.Count == 0)
        {
            var url = client.Endpoint;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client.Endpoint));
            if (!url.StartsWithIgnoreCase("http://", "https://")) url = "https://" + url;
            client.SetServer(url);
        }
        //client.SetServer($"https://{client.Endpoint}");

        var expireMinutes = (expire + 59) / 60;

        if (!code.IsNullOrEmpty())
        {
            var args = new
            {
                SchemaName,
                SignName,
                TemplateCode = templateCode,
                PhoneNumber = mobile,
                TemplateParam = $"{{\"code\":\"{code}\",\"min\":\"{expireMinutes}\"}}",
                options?.OutId,
            };

            return client.PostAsync<String>("SendSmsVerifyCode", args);
        }
        else
        {
            var args = new
            {
                SchemaName,
                SignName,
                TemplateCode = templateCode,
                PhoneNumber = mobile,
                TemplateParam = $"{{\"code\":\"##code##\",\"min\":\"{expireMinutes}\"}}",
                CodeLength,
                ValidTime = expire,
                DuplicatePolicy = 2,
                Interval = 60,
                ReturnVerifyCode = true,
                options?.OutId,
            };

            return client.PostAsync<String>("SendSmsVerifyCode", args);
        }
    }

    /// <summary>发送登录验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    public Task<String> SendLogin(String mobile, String code = null, Int32 expire = 300, SmsVerifyCodeOptions options = null) => SendAsync(mobile, "100001", code, expire, options);

    /// <summary>发送重置验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    public Task<String> SendReset(String mobile, String code = null, Int32 expire = 300, SmsVerifyCodeOptions options = null) => SendAsync(mobile, "100003", code, expire, options);

    /// <summary>发送绑定验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    public Task<String> SendBind(String mobile, String code = null, Int32 expire = 300, SmsVerifyCodeOptions options = null) => SendAsync(mobile, "100004", code, expire, options);
    #endregion
}
