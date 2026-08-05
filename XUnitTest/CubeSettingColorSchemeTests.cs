using System;
using System.ComponentModel;
using NewLife.Cube;
using Xunit;

namespace XUnitTest;

/// <summary>CubeSetting AI 主题色方案联动单元测试</summary>
/// <remarks>验证 ApplyColorScheme：方案变化时主色/辅色采用新方案颜色，方案未变或未知方案时保留原值</remarks>
public class CubeSettingColorSchemeTests
{
    [Fact]
    [DisplayName("ColorSchemes 字典每项恰好主色、辅色两色")]
    public void ColorSchemes_HaveTwoColors()
    {
        foreach (var kv in CubeSetting.ColorSchemes)
        {
            Assert.False(String.IsNullOrEmpty(kv.Key), $"方案名[{kv.Key}]为空");
            var colors = kv.Value.Split(',');
            Assert.Equal(2, colors.Length);
            Assert.StartsWith("#", colors[0].Trim());
            Assert.StartsWith("#", colors[1].Trim());
        }
    }

    [Fact]
    [DisplayName("方案变化时采用新方案颜色")]
    public void ApplyColorScheme_SchemeChanged_UsesNewColors()
    {
        var set = new CubeSetting
        {
            AIColorScheme = "翠绿",
            AIPrimaryColor = "#2ecc71",
            AISecondaryColor = "#1e8e3e",
        };

        set.ApplyColorScheme("新生命绿");

        Assert.Equal("#10b981", set.AIPrimaryColor);
        Assert.Equal("#059669", set.AISecondaryColor);
    }

    [Fact]
    [DisplayName("方案未变时保留手动微调")]
    public void ApplyColorScheme_SchemeUnchanged_KeepsManualColors()
    {
        var set = new CubeSetting
        {
            AIColorScheme = "翠绿",
            AIPrimaryColor = "#123456",
            AISecondaryColor = "#654321",
        };

        set.ApplyColorScheme("翠绿");

        Assert.Equal("#123456", set.AIPrimaryColor);
        Assert.Equal("#654321", set.AISecondaryColor);
    }

    [Fact]
    [DisplayName("未知方案时不崩溃且保留原值")]
    public void ApplyColorScheme_UnknownScheme_KeepsColors()
    {
        var set = new CubeSetting
        {
            AIColorScheme = "不存在的方案",
            AIPrimaryColor = "#2ecc71",
            AISecondaryColor = "#1e8e3e",
        };

        set.ApplyColorScheme("新生命绿");

        Assert.Equal("#2ecc71", set.AIPrimaryColor);
        Assert.Equal("#1e8e3e", set.AISecondaryColor);
    }

    [Fact]
    [DisplayName("方案为空时不崩溃")]
    public void ApplyColorScheme_EmptyScheme_KeepsColors()
    {
        var set = new CubeSetting
        {
            AIColorScheme = null,
            AIPrimaryColor = "#2ecc71",
            AISecondaryColor = "#1e8e3e",
        };

        set.ApplyColorScheme("新生命绿");

        Assert.Equal("#2ecc71", set.AIPrimaryColor);
        Assert.Equal("#1e8e3e", set.AISecondaryColor);
    }
}
