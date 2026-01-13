namespace NewLife.Cube.Services;

/// <summary>短信验证码选项</summary>
public class SmsVerifyCodeOptions
{
    /// <summary>外部流水号</summary>
    public String OutId { get; set; }
}

/// <summary>短信验证码接口</summary>
public interface ISmsVerifyCode
{
    /// <summary>发送登录验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expireMinutes">有效期。分钟</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    Task<String> SendLogin(String mobile, String code, Int32 expireMinutes, SmsVerifyCodeOptions options = null);

    /// <summary>发送重置验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expireMinutes">有效期。分钟</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    Task<String> SendReset(String mobile, String code, Int32 expireMinutes, SmsVerifyCodeOptions options = null);

    /// <summary>发送绑定验证码</summary>
    /// <param name="mobile">手机号</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expireMinutes">有效期。分钟</param>
    /// <param name="options">可选项</param>
    /// <returns>内部生成的验证码</returns>
    Task<String> SendBind(String mobile, String code, Int32 expireMinutes, SmsVerifyCodeOptions options = null);
}
