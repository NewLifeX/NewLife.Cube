using NewLife.Remoting.CloudProviders;

namespace NewLife.Cube.Services;

/// <summary>阿里云短信验证码</summary>
public class AliyunSmsVerifyCode
{
    #region 属性
    /// <summary>签名名称</summary>
    public String SignName { get; set; }

    /// <summary>方案名称</summary>
    public String? SchemaName { get; set; }

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
    /// <param name="expireMinutes">有效期。分钟</param>
    /// <param name="options">可选项</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual Task<String> SendAsync(String mobile, String templateCode, String? code, Int32 expireMinutes, SmsVerifyCodeOptions? options = null)
    {
        var client = Client;

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
                ValidTime = expireMinutes * 60,
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
    /// <param name="expireMinutes">有效期。分钟</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    public Task<String> SendLogin(String mobile, String? code = null, Int32 expireMinutes = 5, SmsVerifyCodeOptions? options = null) => SendAsync(mobile, "100001", code, expireMinutes, options);

    /// <summary>发送重置验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expireMinutes">有效期。分钟</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    public Task<String> SendReset(String mobile, String? code = null, Int32 expireMinutes = 5, SmsVerifyCodeOptions? options = null) => SendAsync(mobile, "100003", code, expireMinutes, options);

    /// <summary>发送绑定验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expireMinutes">有效期。分钟</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    public Task<String> SendBind(String mobile, String? code = null, Int32 expireMinutes = 5, SmsVerifyCodeOptions? options = null) => SendAsync(mobile, "100004", code, expireMinutes, options);
    #endregion
}
