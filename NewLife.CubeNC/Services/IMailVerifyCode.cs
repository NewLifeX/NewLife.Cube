namespace NewLife.Cube.Services;

/// <summary>邮件验证码选项</summary>
public class MailVerifyCodeOptions
{
    /// <summary>外部流水号</summary>
    public String OutId { get; set; }
}

/// <summary>邮件验证码接口</summary>
public interface IMailVerifyCode
{
    /// <summary>发送登录验证码</summary>
    /// <param name="mail">邮箱地址</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>发送结果</returns>
    Task<String> SendLogin(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null);

    /// <summary>发送重置验证码</summary>
    /// <param name="mail">邮箱地址</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>发送结果</returns>
    Task<String> SendReset(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null);

    /// <summary>发送绑定验证码</summary>
    /// <param name="mail">邮箱地址</param>
    /// <param name="code">验证码。未指定时内部生成</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>发送结果</returns>
    Task<String> SendBind(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null);
}
