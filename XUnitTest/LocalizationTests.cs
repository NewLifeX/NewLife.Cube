using System;
using System.Globalization;
using NewLife;
using Xunit;

namespace XUnitTest;

/// <summary>魔方多语言词表测试</summary>
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
}
