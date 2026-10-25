using System;
using System.Reflection;
using NewLife.Cube.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>覆盖菜单名迁移判定：旧版本按英文源码落库的自动菜单需回落中文源文。</summary>
/// <remarks>该判定只在存量未标记的菜单上执行一次，日常同步由 <see cref="MenuHelper.SyncDisplayName"/> 的标记分支决定。</remarks>
public class MenuHelperNameNormalizationTests
{
    private static Boolean LooksLikeTranslatedName(String displayName, String source)
    {
        var method = typeof(MenuHelper).GetMethod("LooksLikeTranslatedName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (Boolean)method.Invoke(null, [displayName, source]);
    }

    [Fact(DisplayName = "菜单名归一化 - 英文落库值回落中文源文")]
    public void EnglishValueFallsBackToChineseSource()
    {
        Assert.True(LooksLikeTranslatedName("Dictionary parameters", "字典参数"));
        Assert.True(LooksLikeTranslatedName("Basic settings", "基本设置"));
        Assert.True(LooksLikeTranslatedName("Parameter", "字典参数"));
    }

    [Fact(DisplayName = "菜单名归一化 - 中文或形态合法的落库值保持不变")]
    public void NormalValuesAreKept()
    {
        Assert.False(LooksLikeTranslatedName("字典参数", "字典参数"));
        Assert.False(LooksLikeTranslatedName("OAuth配置", "OAuth配置"));
        Assert.False(LooksLikeTranslatedName("", "字典参数"));
        Assert.False(LooksLikeTranslatedName(null, "字典参数"));
    }

    [Fact(DisplayName = "菜单名归一化 - 源文本身是英文时不处理")]
    public void EnglishSourceIsNotTouched()
    {
        Assert.False(LooksLikeTranslatedName("Access rules", "Access rules"));
        Assert.False(LooksLikeTranslatedName("Dictionary parameters", "Dictionary parameters"));
    }
}
