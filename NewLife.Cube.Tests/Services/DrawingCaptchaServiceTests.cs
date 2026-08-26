using System;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>图片验证码服务测试：生成、错误输入拒绝、一次性防重放（验证码被消耗）</summary>
/// <remarks>
/// 验证码答案由服务端随机生成且不对外暴露，无法确定性获取正确答案；
/// "正确输入校验通过"由浏览器 E2E（CaptchaScene=1 错误码被拒 + 默认配置免码登录）与
/// UserService.RequireCaptcha 链路反向保证。本类只测确定性行为。
/// </remarks>
public class DrawingCaptchaServiceTests
{
    [Fact(DisplayName = "Generate 返回验证码ID和PNG图片")]
    public void Generate_ReturnsIdAndPng()
    {
        var svc = new DrawingCaptchaService(new TestCacheProvider());
        var r = svc.Generate();

        Assert.False(String.IsNullOrEmpty(r.CaptchaId));
        Assert.StartsWith("data:image/png;base64,", r.Image);
    }

    [Fact(DisplayName = "Validate 空值或非数字 拒绝")]
    public void Validate_InvalidInput_Rejected()
    {
        var svc = new DrawingCaptchaService(new TestCacheProvider());
        var r = svc.Generate();

        Assert.False(svc.Validate(r.CaptchaId, ""));
        Assert.False(svc.Validate(r.CaptchaId, "abc"));
        Assert.False(svc.Validate(r.CaptchaId, "999"));
        Assert.False(svc.Validate(r.CaptchaId, null!));
    }

    [Fact(DisplayName = "Validate 校验即消耗 防重放（失败也消耗）")]
    public void Validate_Consumed_AntiReplay()
    {
        var svc = new DrawingCaptchaService(new TestCacheProvider());
        var r = svc.Generate();

        // 首次校验（无论成败）后验证码即被删除，同 captchaId 再次校验必然失败
        svc.Validate(r.CaptchaId, "999");
        Assert.False(svc.Validate(r.CaptchaId, "999"));
    }
}
