using System;
using System.Reflection;
using System.Threading.Tasks;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Services;
using Xunit;

namespace XUnitTest;

/// <summary>邮箱/手机验证（账号激活）单元测试。覆盖激活邮件/短信模板渲染、脱敏与模型默认值</summary>
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

    #region 脱敏
    [Theory(DisplayName = "联系方式脱敏_邮箱")]
    [InlineData("zhangsan@example.com", "@example.com")]
    [InlineData("a@b.com", "@b.com")]
    public void MaskTarget_Email(String mail, String expectTail)
    {
        var masked = InvokeMask(mail);

        Assert.Contains("***", masked);
        Assert.EndsWith(expectTail, masked);
        Assert.DoesNotContain(mail, masked);
    }

    [Theory(DisplayName = "联系方式脱敏_手机")]
    [InlineData("13800138000", "138****8000")]
    [InlineData("13912345678", "139****5678")]
    public void MaskTarget_Mobile(String mobile, String expect)
    {
        var masked = InvokeMask(mobile);

        Assert.Equal(expect, masked);
    }

    [Fact(DisplayName = "联系方式脱敏_空与短值不报错")]
    public void MaskTarget_EmptyAndShort()
    {
        Assert.Equal(null, InvokeMask(null));
        Assert.Equal("", InvokeMask(""));
        Assert.Equal("a****", InvokeMask("ab")); // 超短值统一脱敏
    }

    /// <summary>通过反射调用 internal 静态脱敏方法</summary>
    private static String InvokeMask(String target)
    {
        var method = typeof(AccountActivateService).GetMethod("MaskTarget", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (String)method.Invoke(null, [target]);
    }
    #endregion

    #region 模型
    [Fact(DisplayName = "RegisterResult_默认非待激活且无令牌")]
    public void RegisterResult_Defaults()
    {
        var model = new RegisterResult();

        Assert.False(model.PendingActivation);
        Assert.Null(model.AccessToken);
        Assert.Null(model.RefreshToken);
        Assert.Null(model.Channels);
        Assert.Null(model.Targets);
        Assert.Equal(0, model.ExpireIn);
    }

    [Fact(DisplayName = "ActivatePendingModel_默认有效期 1 小时")]
    public void ActivatePendingModel_DefaultExpire()
    {
        var model = new ActivatePendingModel();

        Assert.Equal(3600, model.ExpireIn);
    }

    [Fact(DisplayName = "VerifyStatusModel_默认均未验证")]
    public void VerifyStatusModel_Defaults()
    {
        var model = new VerifyStatusModel();

        Assert.False(model.MailVerified);
        Assert.False(model.MobileVerified);
    }

    [Fact(DisplayName = "ActivateModel/VerifyContactModel 承载渠道与账号")]
    public void ActivateAndVerifyContact_Models()
    {
        var activate = new ActivateModel { Channel = "mail", Account = "a@b.com", Code = "1234" };
        Assert.Equal("mail", activate.Channel);
        Assert.Equal("a@b.com", activate.Account);
        Assert.Equal("1234", activate.Code);

        var verify = new VerifyContactModel { Channel = "sms", Account = "13800138000", Code = "5678" };
        Assert.Equal("sms", verify.Channel);
        Assert.Equal("13800138000", verify.Account);
        Assert.Equal("5678", verify.Code);
    }
    #endregion
}
