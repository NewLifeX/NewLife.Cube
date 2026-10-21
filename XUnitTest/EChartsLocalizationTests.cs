using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife;
using NewLife.Cube.Charts;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>ECharts 图表文案跟随界面语言测试</summary>
/// <remarks>字段元数据保存中文源文，建系列时必须按当前界面语言本地化，否则英文界面下图例与系列名仍是中文。</remarks>
[Collection("Localization")]
public class EChartsLocalizationTests
{
    private static String SeriesName(String culture, DataField field)
    {
        var old = Localization.Culture;
        try
        {
            Localization.Culture = new CultureInfo(culture);

            var list = new List<UserStat>
            {
                new() { Logins = 3 },
                new() { Logins = 5 },
            };
            var chart = new ECharts();
            chart.SetX(list, "ID");
            chart.Add(list, [field], SeriesTypes.Line);

            return chart.Series[0].Name;
        }
        finally
        {
            Localization.Culture = old;
        }
    }

    [Fact(DisplayName = "图表 - 英文界面系列名使用英文")]
    public void SeriesNameEnglish()
    {
        var field = new DataField { Name = "Logins", DisplayName = "登录数" };

        Assert.Equal("Sign-ins", SeriesName("en-US", field));
    }

    [Fact(DisplayName = "图表 - 中文界面系列名保持中文")]
    public void SeriesNameChinese()
    {
        var field = new DataField { Name = "Logins", DisplayName = "登录数" };

        Assert.Equal("登录数", SeriesName("zh-CN", field));
    }

    [Fact(DisplayName = "图表 - 未翻译的显示名回退源文")]
    public void SeriesNameFallback()
    {
        var field = new DataField { Name = "CustomColumn", DisplayName = "自定义列名" };

        Assert.Equal("自定义列名", SeriesName("en-US", field));
    }

    [Fact(DisplayName = "图表 - 无显示名时使用字段名")]
    public void SeriesNameFromName()
    {
        var field = new DataField { Name = "Logins" };

        Assert.Equal("Logins", SeriesName("en-US", field));
    }
}
