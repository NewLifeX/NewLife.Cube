using System.Net;
using System.Net.Mail;

namespace NewLife.Cube.Services;

/// <summary>SMTP邮件验证码服务</summary>
/// <remarks>通过 SMTP 发送邮件验证码，支持登录、重置、绑定三种验证码场景</remarks>
public class SmtpMailVerifyCode : IMailVerifyCode
{
    #region 属性
    /// <summary>SMTP服务器地址</summary>
    public String Server { get; set; }

    /// <summary>SMTP端口。默认25</summary>
    public Int32 Port { get; set; } = 25;

    /// <summary>是否使用SSL</summary>
    public Boolean EnableSsl { get; set; }

    /// <summary>发件人邮箱</summary>
    public String From { get; set; }

    /// <summary>发件人显示名称</summary>
    public String DisplayName { get; set; }

    /// <summary>SMTP用户名</summary>
    public String Username { get; set; }

    /// <summary>SMTP密码</summary>
    public String Password { get; set; }

    /// <summary>邮件主题模板。{action} 会被替换为操作类型</summary>
    public String SubjectTemplate { get; set; } = "您的验证码";

    /// <summary>邮件正文模板。{code} 会被替换为验证码，{expire} 会被替换为有效期（分钟）</summary>
    public String BodyTemplate { get; set; } = "您的验证码是：{code}，有效期 {expire} 分钟。如非本人操作，请忽略此邮件。";
    #endregion

    #region 方法
    /// <summary>核心发送</summary>
    /// <param name="mail">邮箱地址</param>
    /// <param name="action">操作类型</param>
    /// <param name="code">验证码</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>发送结果</returns>
    protected virtual async Task<String> SendAsync(String mail, String action, String code, Int32 expire, MailVerifyCodeOptions options = null)
    {
        if (Server.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Server));
        if (From.IsNullOrEmpty()) throw new ArgumentNullException(nameof(From));

        var expireMinutes = (expire + 59) / 60;
        var subject = SubjectTemplate.Replace("{action}", action);
        var body = BodyTemplate.Replace("{code}", code).Replace("{expire}", expireMinutes.ToString());

        using var client = new SmtpClient(Server, Port)
        {
            EnableSsl = EnableSsl,
            Credentials = new NetworkCredential(Username ?? From, Password)
        };

        var from = new MailAddress(From, DisplayName);
        var to = new MailAddress(mail);
        using var message = new MailMessage(from, to)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        await client.SendMailAsync(message).ConfigureAwait(false);

        return "发送成功";
    }

    /// <summary>发送登录验证码</summary>
    /// <param name="mail">邮箱地址</param>
    /// <param name="code">验证码</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>发送结果</returns>
    public Task<String> SendLogin(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null) 
        => SendAsync(mail, "登录", code, expire, options);

    /// <summary>发送重置验证码</summary>
    /// <param name="mail">邮箱地址</param>
    /// <param name="code">验证码</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>发送结果</returns>
    public Task<String> SendReset(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null) 
        => SendAsync(mail, "重置密码", code, expire, options);

    /// <summary>发送绑定验证码</summary>
    /// <param name="mail">邮箱地址</param>
    /// <param name="code">验证码</param>
    /// <param name="expire">有效期。秒</param>
    /// <param name="options">可选项</param>
    /// <returns>发送结果</returns>
    public Task<String> SendBind(String mail, String code, Int32 expire, MailVerifyCodeOptions options = null) 
        => SendAsync(mail, "绑定邮箱", code, expire, options);
    #endregion
}
