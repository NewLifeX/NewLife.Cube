using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NewLife;
using NewLife.Cube.ViewModels;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>配置字段本地化单元测试</summary>
/// <remarks>字段元数据按请求语言翻译，并从中英文描述中拆出显示名。</remarks>
[CollectionDefinition("ConfigLocalization", DisableParallelization = true)]
public class ConfigLocalizationCollection { }

[Collection("ConfigLocalization")]
public class DataFieldLocalizationTests
{
    [Fact(DisplayName = "SplitFirstSentence_英文描述_按句点拆分")]
    public void SplitFirstSentence_English()
    {
        var (display, description) = DataField.SplitFirstSentence("Log level. Only logs at or above this level are output.");

        Assert.Equal("Log level", display);
        Assert.Equal("Only logs at or above this level are output.", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_中文描述_按句号拆分")]
    public void SplitFirstSentence_Chinese()
    {
        var (display, description) = DataField.SplitFirstSentence("日志等级。只输出大于等于该级别的日志。");

        Assert.Equal("日志等级", display);
        Assert.Equal("只输出大于等于该级别的日志。", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_印地语_按Danda拆分")]
    public void SplitFirstSentence_Hindi()
    {
        var (display, description) = DataField.SplitFirstSentence("सेवा का नाम। विवरण यहाँ है।");

        Assert.Equal("सेवा का नाम", display);
        Assert.Equal("विवरण यहाँ है।", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_泰语无标点_取首个空格前短语")]
    public void SplitFirstSentence_Thai()
    {
        var text = "ช่วงเวลาตรวจสอบ ช่วงเวลาการใช้ทรัพยากรเป็นวินาที ค่าเริ่มต้น 10";
        var (display, description) = DataField.SplitFirstSentence(text);

        Assert.Equal("ช่วงเวลาตรวจสอบ", display);
        Assert.StartsWith("ช่วงเวลาการใช้ทรัพยากร", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_短标签_不拆分")]
    public void SplitFirstSentence_ShortLabel()
    {
        var (display, description) = DataField.SplitFirstSentence("Enabled");

        Assert.Equal("Enabled", display);
        Assert.Null(description);
    }

    [Fact(DisplayName = "Localize_只有描述_翻译后拆分显示名")]
    public void Localize_DescriptionOnly()
    {
        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        const String key = "Log level. Only logs at or above this level are output.";
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { [key] = "日志等级。只输出大于等于该级别的日志。" }, overwrite: true);
            Localization.Culture = culture;

            var fields = DataField.Localize([
                new DataField { Name = "LogLevel", DisplayName = key, SourceDescription = key },
            ]);

            var field = Assert.Single(fields);
            Assert.Equal("日志等级", field.DisplayName);
            Assert.Equal("只输出大于等于该级别的日志。", field.Description);
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "Localize_英文源文_未翻译时保持英文")]
    public void Localize_EnglishSource()
    {
        var prev = Localization.Culture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo("en-US");

            var fields = DataField.Localize([
                new DataField { Name = "Debug", DisplayName = "Debug switch. Default false", SourceDescription = "Debug switch. Default false" },
            ]);

            var field = Assert.Single(fields);
            Assert.Equal("Debug switch", field.DisplayName);
            Assert.Equal("Default false", field.Description);
        }
        finally
        {
            Localization.Culture = prev;
        }
    }

    [Fact(DisplayName = "Localize_同时有显示名和描述_分别翻译")]
    public void Localize_DisplayNameAndDescription()
    {
        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        try
        {
            Localization.Register(culture, new Dictionary<String, String>
            {
                ["System name"] = "系统名称",
                ["English name used to identify the system."] = "用于标识系统的英文名。",
            }, overwrite: true);
            Localization.Culture = culture;

            var fields = DataField.Localize([
                new DataField { Name = "Name", DisplayName = "System name", Description = "English name used to identify the system." },
            ]);

            var field = Assert.Single(fields);
            Assert.Equal("系统名称", field.DisplayName);
            Assert.Equal("用于标识系统的英文名。", field.Description);
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
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
