using System;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>邮箱/手机验证（账号激活）单元测试。覆盖联系方式脱敏与模型默认值（API 版）；激活邮件/短信模板测试见 XUnitTest</summary>
public class AccountActivateTests
{
    #region 脱敏
    [Theory(DisplayName = "联系方式脱敏_邮箱")]
    [InlineData("zhangsan@example.com", "@example.com")]
    [InlineData("a@ab.com", "@ab.com")] // 短本地部分（单字符）也须脱敏
    public void MaskTarget_Email(String mail, String expectTail)
    {
        var masked = AccountActivateService.MaskTarget(mail);

        Assert.Contains("***", masked);
        Assert.EndsWith(expectTail, masked);
        Assert.DoesNotContain(mail, masked);
    }

    [Theory(DisplayName = "联系方式脱敏_手机")]
    [InlineData("13800138000", "138****8000")]
    [InlineData("13912345678", "139****5678")]
    public void MaskTarget_Mobile(String mobile, String expect)
    {
        var masked = AccountActivateService.MaskTarget(mobile);

        Assert.Equal(expect, masked);
    }

    [Fact(DisplayName = "联系方式脱敏_空与短值不报错")]
    public void MaskTarget_EmptyAndShort()
    {
        Assert.Null(AccountActivateService.MaskTarget(null));
        Assert.Equal("", AccountActivateService.MaskTarget(""));
        Assert.Equal("a****", AccountActivateService.MaskTarget("ab")); // 超短值统一脱敏
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
