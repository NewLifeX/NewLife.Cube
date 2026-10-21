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

    [Fact(DisplayName = "多语言 - 共享视图文案 31 种语言齐备")]
    public void SharedViewChrome()
    {
        // 共享视图（导航、通知铃铛、AI 助手、省市区选择、错误页）的界面文案以英文为源文，
        // 每种受支持语言都必须有译文，避免非中文用户看到英文按钮。同形词（Send/ID 等）不在此列。
        String[] keys =
        [
            "Notifications", "Mark all as read", "View all notifications", "No unread notifications",
            "AI assistant", "AI diagnosis", "Collapse", "Clear session",
            "Select province", "Select province/city/district/town", "Search department",
            "Back", "Close", "No data", "Searching...", "Only image files are supported",
        ];

        foreach (var culture in CubeLocalization.SupportedCultures)
        {
            if (culture == "en-US") continue;

            foreach (var key in keys)
            {
                var text = Translate(culture, key);
                Assert.True(text != key, $"{culture} 缺少共享视图文案译文：{key}");
            }
        }
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
        Assert.IsAssignableFrom<QueryStringRequestCultureProvider>(options.RequestCultureProviders[0]);
        Assert.IsAssignableFrom<CookieRequestCultureProvider>(options.RequestCultureProviders[1]);
        Assert.IsAssignableFrom<RequestCultureProvider>(options.RequestCultureProviders[2]);
    }

    [Fact(DisplayName = "多语言 - 语言名称归一化")]
    public void NormalizeCulture()
    {
        // 精确名称原样返回
        Assert.Equal("de-DE", CubeLocalization.NormalizeCulture("de-DE"));
        Assert.Equal("en-US", CubeLocalization.NormalizeCulture("en-us"));

        // 裸语言码与非支持区域映射到受支持的区域语言
        Assert.Equal("en-US", CubeLocalization.NormalizeCulture("en"));
        Assert.Equal("ja-JP", CubeLocalization.NormalizeCulture("ja"));
        Assert.Equal("de-DE", CubeLocalization.NormalizeCulture("de-AT"));
        Assert.Equal("zh-CN", CubeLocalization.NormalizeCulture("zh"));
        Assert.Equal("pt-BR", CubeLocalization.NormalizeCulture("pt-PT"));

        // 带权重的请求头
        Assert.Equal("fr-FR", CubeLocalization.NormalizeCulture("fr-CA;q=0.9"));

        // 不支持的语言与空值
        Assert.Null(CubeLocalization.NormalizeCulture("sw-KE"));
        Assert.Null(CubeLocalization.NormalizeCulture(""));
        Assert.Null(CubeLocalization.NormalizeCulture(null));
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

    [Fact(DisplayName = "多语言 - 日志模板按 LogCulture 翻译")]
    public void TranslateForLogUsesLogCulture()
    {
        const String key = "Compressed database {0} in {1}";
        var oldLog = Localization.LogCulture;
        var oldCulture = Localization.Culture;
        try
        {
            // 日志按 LogCulture 固定语言，不受请求级 Culture 影响
            Localization.Culture = new CultureInfo("de-DE");
            Localization.LogCulture = new CultureInfo("zh-CN");
            Assert.Equal("压缩数据库 Order 完成，耗时 3s", Localization.TranslateForLog(key, "Order", "3s"));

            Localization.LogCulture = new CultureInfo("zh-TW");
            Assert.Equal("壓縮資料庫 Order 完成，耗時 3s", Localization.TranslateForLog(key, "Order", "3s"));

            // 未配置 LogCulture 时回退 Culture
            Localization.LogCulture = null;
            Localization.Culture = new CultureInfo("en-US");
            Assert.Equal("Compressed database Order in 3s", Localization.TranslateForLog(key, "Order", "3s"));
        }
        finally
        {
            Localization.LogCulture = oldLog;
            Localization.Culture = oldCulture;
        }
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
            "Value set code is required.",
            "Category is required.",
            "The user page is read-only for non-system roles; edit your profile in the user center.",
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

    [Fact(DisplayName = "多语言 - 实体元数据与视图文案全语言覆盖")]
    public void MetadataAndViewKeysCoverAllLanguages()
    {
        // 实体元数据（字段名/设置项描述/控制器标题）与视图文案以英文为源文，
        // 每种受支持语言都必须有译文，否则非中文用户会看到英文元数据。
        String[] keys =
        [
            "Third-party authorization", "Tenant administrator", "Account closure failed",
            "Password strength", "Share count", "Permission items", "Operation permissions",
            "Security center", "Remember me", "No online users", "Move up", "Move down",
            "Basic information", "Notification records", "System info", "Quick links",
            "Attachments not downloaded for this many days are cleaned up",
            "Gender", "Birthday", "Company name", "Login tip",
        ];

        // 含占位符的键：译文必须保留占位符，且不得等于源文
        String[] placeholderKeys = ["{0} cores", "{0} assemblies", "{0} items in clipboard", "Bound to {0} platforms"];

        foreach (var culture in CubeLocalization.SupportedCultures)
        {
            if (culture.StartsWith("en", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var key in keys)
            {
                var text = Translate(culture, key);
                Assert.True(text != key, $"{culture} 缺少元数据/视图译文：{key}");
            }

            foreach (var key in placeholderKeys)
            {
                // 取值为品牌/技术借词时可能与源文同形（如 pt 的 {0} assemblies），此处只要求占位符保留
                var text = Translate(culture, key);
                Assert.False(String.IsNullOrEmpty(text), $"{culture} 缺少元数据/视图译文：{key}");
                Assert.Contains("{0}", text);
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
    [Fact(DisplayName = "多语言 - 日志模板可本地化且英文为兜底")]
    public void LogMessagesAreLocalizable()
    {
        // 日志模板按 L.TLog + LogCulture 翻译：中文与已翻译语言使用译文，缺译文的语言回退英文源文
        var key = "Start importing file [{0}], size [{1:n0}] bytes, type [{2}].";
        Assert.Equal("开始导入文件[{0}]，大小[{1:n0}]字节，类型[{2}]", Translate("zh-CN", key));

        var de = Translate("de-DE", key);
        Assert.NotEqual(key, de);
        Assert.Contains("{0}", de);

        // 未提供译文的模板回退英文源文
        var missing = "Cube localization fallback probe [{0}].";
        Assert.Equal(missing, Translate("ja-JP", missing));
    }
}
