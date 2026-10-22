using System;
using System.Reflection;
using NewLife.Cube.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>覆盖菜单名归一化判定：旧版本按英文源码落库的自动菜单需回落中文源文。</summary>
public class MenuHelperNameNormalizationTests
{
    private static Boolean IsOtherLanguageName(String displayName, String source)
    {
        var method = typeof(MenuHelper).GetMethod("IsOtherLanguageName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (Boolean)method.Invoke(null, [displayName, source]);
    }

    [Fact(DisplayName = "菜单名归一化 - 英文落库值回落中文源文")]
    public void EnglishValueFallsBackToChineseSource()
    {
        Assert.True(IsOtherLanguageName("Dictionary parameters", "字典参数"));
        Assert.True(IsOtherLanguageName("Basic settings", "基本设置"));
        Assert.True(IsOtherLanguageName("Parameter", "字典参数"));
    }

    [Fact(DisplayName = "菜单名归一化 - 中文或形态合法的落库值保持不变")]
    public void NormalValuesAreKept()
    {
        Assert.False(IsOtherLanguageName("字典参数", "字典参数"));
        Assert.False(IsOtherLanguageName("OAuth配置", "OAuth配置"));
        Assert.False(IsOtherLanguageName("", "字典参数"));
        Assert.False(IsOtherLanguageName(null, "字典参数"));
    }

    [Fact(DisplayName = "菜单名归一化 - 源文本身是英文时不处理")]
    public void EnglishSourceIsNotTouched()
    {
        Assert.False(IsOtherLanguageName("Access rules", "Access rules"));
        Assert.False(IsOtherLanguageName("Dictionary parameters", "Dictionary parameters"));
    }
}
