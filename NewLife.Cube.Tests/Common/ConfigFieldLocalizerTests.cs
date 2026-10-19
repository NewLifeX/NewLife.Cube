using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife.Cube.ViewModels;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>配置字段本地化单元测试</summary>
/// <remarks>配置页面按请求语言翻译元数据，并从中英文描述中拆出显示名。</remarks>
[CollectionDefinition("ConfigLocalization", DisableParallelization = true)]
public class ConfigLocalizationCollection { }

[Collection("ConfigLocalization")]
public class ConfigFieldLocalizerTests
{
    [Fact(DisplayName = "SplitFirstSentence_英文描述_按句点拆分")]
    public void SplitFirstSentence_English()
    {
        var (display, description) = ConfigFieldLocalizer.SplitFirstSentence("Log level. Only logs at or above this level are output.");

        Assert.Equal("Log level", display);
        Assert.Equal("Only logs at or above this level are output.", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_中文描述_按句号拆分")]
    public void SplitFirstSentence_Chinese()
    {
        var (display, description) = ConfigFieldLocalizer.SplitFirstSentence("日志等级。只输出大于等于该级别的日志。");

        Assert.Equal("日志等级", display);
        Assert.Equal("只输出大于等于该级别的日志。", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_印地语_按Danda拆分")]
    public void SplitFirstSentence_Hindi()
    {
        var (display, description) = ConfigFieldLocalizer.SplitFirstSentence("सेवा का नाम। विवरण यहाँ है।");

        Assert.Equal("सेवा का नाम", display);
        Assert.Equal("विवरण यहाँ है।", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_泰语无标点_取首个空格前短语")]
    public void SplitFirstSentence_Thai()
    {
        var text = "ช่วงเวลาตรวจสอบ ช่วงเวลาการใช้ทรัพยากรเป็นวินาที ค่าเริ่มต้น 10";
        var (display, description) = ConfigFieldLocalizer.SplitFirstSentence(text);

        Assert.Equal("ช่วงเวลาตรวจสอบ", display);
        Assert.StartsWith("ช่วงเวลาการใช้ทรัพยากร", description);
    }

    [Fact(DisplayName = "SplitFirstSentence_短标签_不拆分")]
    public void SplitFirstSentence_ShortLabel()
    {
        var (display, description) = ConfigFieldLocalizer.SplitFirstSentence("Enabled");

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

            var fields = ConfigFieldLocalizer.Localize([
                new DataField { Name = "LogLevel", DisplayName = key },
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

            var fields = ConfigFieldLocalizer.Localize([
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
}
