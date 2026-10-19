using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewLife;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>魔方多语言词表测试</summary>
[CollectionDefinition("Localization", DisableParallelization = true)]
public class LocalizationCollection { }

[Collection("Localization")]
public class LocalizationTests
{
    private static String Translate(String culture, String template, params Object[] args)
    {
        var old = Localization.Culture;
        try
        {
            Localization.Culture = new CultureInfo(culture);
            return Localization.Translate(template, args);
        }
        finally
        {
            Localization.Culture = old;
        }
    }

    [Fact(DisplayName = "多语言 - 简体中文查表")]
    public void TranslateZhHans()
    {
        Assert.Equal("操作成功", Translate("zh-CN", "Operation succeeded"));
        Assert.Equal("删除成功！", Translate("zh-CN", "{0} succeeded!", "删除"));
    }

    [Fact(DisplayName = "多语言 - 繁体中文查表")]
    public void TranslateZhHant()
    {
        Assert.Equal("操作成功", Translate("zh-TW", "Operation succeeded"));
        Assert.Equal("刪除", Translate("zh-TW", "Delete"));
    }

    [Fact(DisplayName = "多语言 - 英文为源文与兜底")]
    public void TranslateEnglish()
    {
        Assert.Equal("Operation succeeded", Translate("en-US", "Operation succeeded"));
        Assert.Equal("Not translated", Translate("en-US", "Not translated"));
    }

    [Fact(DisplayName = "多语言 - 未提供语言回退英文")]
    public void TranslateUnknownCulture()
    {
        Assert.Equal("Operation succeeded", Translate("sw-KE", "Operation succeeded"));
    }

    [Fact(DisplayName = "多语言 - 占位符格式化")]
    public void TranslatePlaceholder()
    {
        Assert.Equal("用户名不可以为空！", Translate("zh-CN", "{0} is required.", "用户名"));
        Assert.Equal("用户名长度不能超过20个字符！", Translate("zh-CN", "{0} must not exceed {1} characters.", "用户名", 20));
    }

    [Fact(DisplayName = "多语言 - 运行时支持词表中的全部语言")]
    public void SupportedCultures()
    {
        Assert.Equal(32, CubeLocalization.SupportedCultures.Length);
        Assert.Equal(new[] { "zh-CN", "zh-TW", "en-US" }, CubeLocalization.SupportedCultures[..3]);

        foreach (var item in CubeLocalization.SupportedCultures)
        {
            var culture = CultureInfo.GetCultureInfo(item);
            Assert.True(Localization.IsSupported(culture), item);
        }
    }

    [Fact(DisplayName = "多语言 - 请求本地化注册全部区域与语言来源")]
    public void RequestLocalizationOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCubeLocalization();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        Assert.Equal(32, options.SupportedUICultures.Count);
        Assert.Equal(3, options.RequestCultureProviders.Count);
        Assert.IsType<QueryStringRequestCultureProvider>(options.RequestCultureProviders[0]);
        Assert.IsType<CookieRequestCultureProvider>(options.RequestCultureProviders[1]);
        Assert.IsType<AcceptLanguageHeaderRequestCultureProvider>(options.RequestCultureProviders[2]);
    }

    [Fact(DisplayName = "多语言 - 新增公共消息覆盖全部语言")]
    public void NewPublicMessages()
    {
        const String key = "MFA is not enabled on this system.";
        var expected = new Dictionary<String, String>
        {
            ["zh-CN"] = "系统未开启MFA功能",
            ["zh-TW"] = "系統未開啟MFA功能",
            ["de-DE"] = "MFA ist in diesem System nicht aktiviert.",
            ["ja-JP"] = "このシステムではMFAが有効になっていません。",
            ["ar-SA"] = "MFA غير ممكّن على هذا النظام.",
            ["hi-IN"] = "इस सिस्टम पर MFA सक्षम नहीं है।",
        };

        foreach (var item in expected)
        {
            var text = Translate(item.Key, key);
            Assert.Equal(item.Value, text);
            Assert.NotEqual(key, text);
        }

        // 没有词条的语言回退英文源文
        Assert.Equal(key, Translate("sw-KE", key));
    }

    [Fact(DisplayName = "多语言 - 新增公共消息在全部译文语言中均有译文")]
    public void AllLanguagesTranslated()
    {
        var keys = new[]
        {
            "MFA is not enabled on this system.",
            "The activation link is invalid.",
            "The new password must be different from the current password.",
            "Authentication failed.",
            "Invalid application [{0}].",
            "User [{0}] does not exist.",
            "The token has expired.",
            "OAuth service is not enabled.",
            "The operation timed out. Please try again.",
            "User [{0}] is disabled.",
            "The OAuth server is not configured.",
            "Unsupported verification channel.",
            "The request body is empty.",
            "Unknown error",
        };

        foreach (var culture in CubeLocalization.SupportedCultures)
        {
            if (culture.StartsWith("en", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var key in keys)
            {
                var text = Translate(culture, key);
                Assert.False(String.IsNullOrEmpty(text), culture);
                Assert.NotEqual(key, text);
            }
        }
    }

    [Fact(DisplayName = "多语言 - 密码提示覆盖全部语言")]
    public void PasswordMessages()
    {
        Assert.Equal("密码不能为空", Translate("zh-CN", "Password is required."));
        Assert.Equal("新密码不能为空", Translate("zh-CN", "New password is required."));
        Assert.Equal("The new password must be different from the current password.",
            Translate("en-US", "The new password must be different from the current password."));
        Assert.Equal("修改密码不能与原密码一致",
            Translate("zh-CN", "The new password must be different from the current password."));
    }
}
