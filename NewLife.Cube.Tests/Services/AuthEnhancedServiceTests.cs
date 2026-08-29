using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>增强认证服务测试：验证码重置密码等（API版/用户中心能力）</summary>
public class AuthEnhancedServiceTests
{
    private readonly AuthEnhancedService _svc;

    public AuthEnhancedServiceTests()
    {
        var provider = new TestCacheProvider();
        var userService = new UserService(new PasswordService(), provider, null!, null!, null!, new TenantContextService());
        var verifyCode = new VerifyCodeService(null!, null!, provider, null!, userService, new TenantContextService());
        var accountActivate = new AccountActivateService(userService, verifyCode, provider, null!, new TenantContextService());
        _svc = new AuthEnhancedService(userService, verifyCode, provider, new PasswordService(), null!, new TenantContextService(), accountActivate);
    }

    [Fact(DisplayName = "ResetPassword：challengeId 无效（过期/伪造）时返回明确错误，不把密文当明文新密码")]
    public void ResetPassword_InvalidChallenge_ReturnsExplicitError()
    {
        var result = _svc.ResetPassword(
            "13800138000",          // account
            "123456",               // code
            "NewPass123",           // newPassword（假设为密文）
            "NewPass123",           // confirmPassword
            "expired-challenge-id", // challengeId 无效（缓存中不存在）
            "127.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal("登录挑战已过期或无效，请重新获取公钥后重试", result.Message);
    }
}
