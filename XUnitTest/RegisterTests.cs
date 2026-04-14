using System;
using NewLife.Cube.Areas.Admin.Models;
using Xunit;

namespace XUnitTest;

/// <summary>注册流程相关模型与规则测试</summary>
public class RegisterTests
{
    [Fact(DisplayName = "AuthRegisterModel 默认注册类型为用户名密码")]
    public void AuthRegisterModel_DefaultCategory_IsPassword()
    {
        var model = new AuthRegisterModel();

        Assert.Equal(RegisterCategory.Password, model.RegisterCategory);
    }

    [Fact(DisplayName = "AuthRegisterModel Password2 与 ConfirmPassword 双向兼容")]
    public void AuthRegisterModel_Password2_Compatibility()
    {
        var model = new AuthRegisterModel
        {
            Password2 = "P@ssw0rd#2026"
        };

        Assert.Equal("P@ssw0rd#2026", model.ConfirmPassword);

        model.ConfirmPassword = "NewPass#2026";
        Assert.Equal("NewPass#2026", model.Password2);
    }

    [Fact(DisplayName = "OAuthPendingInfoModel 可承载回跳预填信息")]
    public void OAuthPendingInfoModel_CanHoldPrefillData()
    {
        var model = new OAuthPendingInfoModel
        {
            Provider = "GitHub",
            Username = "octocat",
            Email = "octocat@github.com",
            Mobile = "13800138000",
            Avatar = "https://example.com/avatar.png"
        };

        Assert.Equal("GitHub", model.Provider);
        Assert.Equal("octocat", model.Username);
        Assert.Equal("octocat@github.com", model.Email);
        Assert.Equal("13800138000", model.Mobile);
        Assert.Equal("https://example.com/avatar.png", model.Avatar);
    }

    [Theory(DisplayName = "OAuth 回跳用户名回退策略")]
    [InlineData("name1", "mail@test.com", "13800138000", "name1")]
    [InlineData("", "mail@test.com", "13800138000", "mail")]
    [InlineData("", "", "13800138000", "P13800138000")]
    public void OAuthFallbackUsername_ShouldFollowRule(String username, String email, String mobile, String expectedPrefix)
    {
        var actual = BuildOAuthFallbackUsername(username, email, mobile);

        Assert.StartsWith(expectedPrefix, actual, StringComparison.OrdinalIgnoreCase);
    }

    private static String BuildOAuthFallbackUsername(String username, String email, String mobile)
    {
        var result = username?.Trim();
        if (!String.IsNullOrEmpty(result)) return result;

        if (!String.IsNullOrEmpty(email)) return email.Split('@')[0];
        if (!String.IsNullOrEmpty(mobile)) return $"P{mobile}";

        return $"OAuth_{Guid.NewGuid().ToString("N")[..8]}";
    }
}
