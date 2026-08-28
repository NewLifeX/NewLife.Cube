using System;
using System.Threading.Tasks;
using NewLife.Cube.Services;
using Xunit;

namespace XUnitTest;

/// <summary>邮箱/手机验证（账号激活）模板单元测试。覆盖激活邮件/短信模板渲染；服务与模型测试见 NewLife.Cube.Tests（API 版）</summary>
public class AccountActivateTests
{
    #region 激活邮件模板
    private class TestSmtpMail : SmtpMailVerifyCode
    {
        public String CapturedSubject;
        public String CapturedBody;

        protected override Task<String> SendCore(String mail, String subject, String body, MailVerifyCodeOptions options = null)
        {
            CapturedSubject = subject;
            CapturedBody = body;

            return Task.FromResult("发送成功");
        }
    }

    [Fact(DisplayName = "激活邮件_正文包含激活链接与验证码")]
    public async Task SendActivate_ContainsLinkAndCode()
    {
        var svc = new TestSmtpMail();
        var link = "https://cube.example.com/activate?token=abc123&account=zhangsan%40example.com";

        await svc.SendActivate("zhangsan@example.com", link, "123456", 3600);

        Assert.Equal("请激活您的账号", svc.CapturedSubject);
        Assert.Contains(link, svc.CapturedBody);
        Assert.Contains("123456", svc.CapturedBody);
        Assert.Contains("60", svc.CapturedBody); // 3600 秒 = 60 分钟
    }

    [Fact(DisplayName = "激活邮件_有效期不足一分钟按一分钟展示")]
    public async Task SendActivate_ExpireMinutes_RoundUp()
    {
        var svc = new TestSmtpMail();

        await svc.SendActivate("zhangsan@example.com", "https://x/activate?token=t", "1234", 30);

        Assert.Contains("1", svc.CapturedBody); // 30 秒向上取整为 1 分钟
    }

    [Fact(DisplayName = "激活邮件_模板缺省链接时不影响普通验证码邮件")]
    public async Task SendBind_NoLinkInBody()
    {
        var svc = new TestSmtpMail();

        await svc.SendBind("zhangsan@example.com", "1234", 300);

        Assert.DoesNotContain("activate", svc.CapturedBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{link}", svc.CapturedBody);
    }
    #endregion

    #region 激活短信模板
    private class TestAliyunSms : AliyunSmsVerifyCode
    {
        public String CapturedTemplate;
        public String CapturedMobile;
        public String CapturedCode;

        protected override Task<String> SendAsync(String mobile, String templateCode, String code, Int32 expire, SmsVerifyCodeOptions options = null)
        {
            CapturedTemplate = templateCode;
            CapturedMobile = mobile;
            CapturedCode = code;

            return Task.FromResult("发送成功");
        }
    }

    [Fact(DisplayName = "激活短信_使用激活模板 100005")]
    public async Task SendActivate_UsesActivateTemplate()
    {
        var svc = new TestAliyunSms();

        await svc.SendActivate("13800138000", "123456", 300);

        Assert.Equal("100005", svc.CapturedTemplate);
        Assert.Equal("13800138000", svc.CapturedMobile);
        Assert.Equal("123456", svc.CapturedCode);
    }

    [Fact(DisplayName = "绑定短信_仍使用绑定模板 100004（不受激活影响）")]
    public async Task SendBind_UsesBindTemplate()
    {
        var svc = new TestAliyunSms();

        await svc.SendBind("13800138000", "123456", 300);

        Assert.Equal("100004", svc.CapturedTemplate);
    }
    #endregion
}
