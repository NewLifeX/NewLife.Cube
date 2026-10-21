using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>设置页多语言测试</summary>
/// <remarks>字段描述以英文为源文，翻译后按首句拆分显示名与描述；语言下拉框取自 Core 语言清单。</remarks>
[Collection("Localization")]
public class CubeLocalizationTests
{
    [Fact(DisplayName = "字段填充 - 英文句点拆分显示名与描述")]
    public void FillSplitsEnglishDescription()
    {
        var field = new FormField();
        field.Fill(typeof(EnglishSetting).GetProperty(nameof(EnglishSetting.LogLevel)));

        Assert.Equal("Log level", field.DisplayName);
        Assert.Equal("Only logs at or above this level are output.", field.Description);
        Assert.Equal("Log level. Only logs at or above this level are output.", field.SourceDescription);
    }

    [Fact(DisplayName = "字段本地化 - 整段描述先翻译再拆分")]
    public void LocalizeTranslatesThenSplits()
    {
        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        const String key = "Log level. Only logs at or above this level are output.";
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { [key] = "日志等级。只输出大于等于该级别的日志。" }, overwrite: true);
            Localization.Culture = culture;

            var source = new DataField { Name = "LogLevel", DisplayName = key, SourceDescription = key };
            var field = Assert.Single(DataField.Localize([source]));
            Assert.Equal("日志等级", field.DisplayName);
            Assert.Equal("只输出大于等于该级别的日志。", field.Description);

            // 源字段保留源文，未被就地修改，可继续按其它语言输出
            Assert.Equal(key, source.DisplayName);
            Assert.Equal(key, source.SourceDescription);
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "语言清单 - 覆盖全部受支持语言并带自动选项")]
    public void Languages()
    {
        var prev = Localization.Culture;
        try
        {
            Localization.Culture = new CultureInfo("zh-CN");

            var dic = CubeLocalization.GetLanguageOptions();

            Assert.Equal("自动（跟随系统）", dic[""] as String);
            Assert.Equal(CubeLocalization.SupportedCultures.Length + 1, dic.Count);
            Assert.Equal("简体中文", dic["zh-CN"] as String);
            Assert.StartsWith("英语", dic["en-US"] as String);
            Assert.Contains("English", dic["en-US"] as String);
            Assert.StartsWith("德语", dic["de-DE"] as String);
            Assert.Contains("Deutsch", dic["de-DE"] as String);
        }
        finally
        {
            Localization.Culture = prev;
        }
    }

    [Fact(DisplayName = "请求本地化注册全部语言来源")]
    public void BuildOptions()
    {
        var services = new ServiceCollection();
        services.AddCubeLocalization();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

        Assert.Equal(CubeLocalization.SupportedCultures.Length, options.SupportedUICultures.Count);
        Assert.Equal(3, options.RequestCultureProviders.Count);
    }

    [Theory(DisplayName = "语言归一化 - 裸语言码、区域与旧版别名")]
    [InlineData("en", "en-US")]
    [InlineData("zh-Hant", "zh-TW")]
    [InlineData("zh-HK", "zh-TW")]
    [InlineData("de-AT", "de-DE")]
    [InlineData("in", "id-ID")]
    [InlineData("iw", "he-IL")]
    [InlineData("no", "nb-NO")]
    [InlineData("mo", "ro-RO")]
    [InlineData("sw-KE", null)]
    public void NormalizeCulture(String name, String? expected) => Assert.Equal(expected, CubeLocalization.NormalizeCulture(name));

    private class EnglishSetting
    {
        [Description("Log level. Only logs at or above this level are output.")]
        public Int32 LogLevel { get; set; }
    }

    [Theory(DisplayName = "数据库页面文案 - 按请求语言输出词表译文")]
    [InlineData("fr-FR", "Connection string", "Chaîne de connexion")]
    [InlineData("ja-JP", "Model differences", "モデルの差分")]
    [InlineData("ar-SA", "Backup", "نسخ احتياطي")]
    [InlineData("de-DE", "Backup and compress", "Sichern und komprimieren")]
    [InlineData("en-US", "Model differences", "Model differences")]
    [InlineData("zh-CN", "Name", "名称")]
    public void DbPageMessages(String culture, String key, String expected)
    {
        var prev = Localization.Culture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal(expected, CubeLocalization.T(key));
        }
        finally
        {
            Localization.Culture = prev;
        }
    }

    [Fact(DisplayName = "数据库页面文案 - 占位符参数替换")]
    public void DbPageFormatting()
    {
        var prev = Localization.Culture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal(", 3 au total", CubeLocalization.T(", {0} total", 3));
            Assert.Equal("Écarts de modèle de la base de données Cube", CubeLocalization.T("Model differences of database {0}", "Cube"));
        }
        finally
        {
            Localization.Culture = prev;
        }
    }

    [Fact(DisplayName = "魔方设置字段 - 英文源文且中文译文齐全")]
    public void CubeSettingFields()
    {
        var prev = Localization.Culture;
        try
        {
            var descs = typeof(CubeSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(e => e.GetCustomAttribute<DescriptionAttribute>()?.Description)
                .Where(e => !e.IsNullOrEmpty())
                .ToArray();
            Assert.NotEmpty(descs);

            // 设置描述必须以英文为源文，避免中文源文在其他语言下无法翻译
            Localization.Culture = CultureInfo.GetCultureInfo("en-US");
            foreach (var item in descs) Assert.False(ContainsCjk(item), item);

            // 每条描述都要有简体中文译文，缺译文时界面会回退英文
            Localization.Culture = CultureInfo.GetCultureInfo("zh-CN");
            foreach (var item in descs) Assert.True(ContainsCjk(CubeLocalization.T(item)), item);
        }
        finally
        {
            Localization.Culture = prev;
        }
    }

    private static Boolean ContainsCjk(String text)
    {
        if (text.IsNullOrEmpty()) return false;
        foreach (var ch in text)
        {
            if (ch >= '\u4e00' && ch <= '\u9fff') return true;
        }

        return false;
    }
}
