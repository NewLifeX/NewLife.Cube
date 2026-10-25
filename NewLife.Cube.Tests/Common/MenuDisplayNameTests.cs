using System;
using System.ComponentModel;
using System.Globalization;
using NewLife;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>覆盖菜单显示名的语言判定与分层回退</summary>
/// <remarks>只使用内置语言（de/zh-Hant），不注册临时词表，避免影响同进程内其它测试的语言环境</remarks>
[Collection("ConfigLocalization")]
public class MenuDisplayNameTests
{
    private const String LogControllerName = "NewLife.Cube.Areas.Admin.Controllers.LogController";

    private static Menu CreateMenu(String? displayName, String fullName) => new()
    {
        Name = "Sample",
        DisplayName = displayName,
        FullName = fullName,
    };

    [Fact(DisplayName = "菜单显示 - 中文源文在简体中文下直接用库值")]
    public void ChineseSourceUsesStoredInSimplifiedChinese()
    {
        var menu = CreateMenu("我的用户", LogControllerName);

        using var _ = new CultureScope("zh-Hans");

        Assert.Equal("我的用户", MenuHelper.GetDisplayName(menu));
    }

    [Fact(DisplayName = "菜单显示 - 中文源文在其它语言下查 menu 词条")]
    public void ChineseSourceTranslatesByMenuKey()
    {
        var menu = CreateMenu("我的用户", LogControllerName);

        using var _ = new CultureScope("de");

        Assert.Equal("Audit-Protokoll", MenuHelper.GetDisplayName(menu));
    }

    [Fact(DisplayName = "菜单显示 - 繁体中文不算源文语言，走词表翻译")]
    public void TraditionalChineseTranslates()
    {
        var menu = CreateMenu("我的用户", LogControllerName);

        using var _ = new CultureScope("zh-Hant");

        Assert.Equal("稽核記錄", MenuHelper.GetDisplayName(menu));
    }

    [Fact(DisplayName = "菜单显示 - menu 词条缺译时回退 entity 词条")]
    public void FallsBackToEntityKey()
    {
        var menu = CreateMenu("我的用户", typeof(User).FullName);

        using var _ = new CultureScope("de");

        Assert.Equal("Benutzer", MenuHelper.GetDisplayName(menu));
    }

    [Fact(DisplayName = "菜单显示 - 库值为空时即时计算，不显示空白")]
    public void EmptyStoredIsComputed()
    {
        var menu = CreateMenu(null, LogControllerName);

        using var _ = new CultureScope("de");

        Assert.Equal("Audit-Protokoll", MenuHelper.GetDisplayName(menu));
    }

    [Fact(DisplayName = "菜单显示 - 手工菜单命中普通词条时翻译")]
    public void ManualMenuTranslatesByPlainKey()
    {
        var menu = CreateMenu("用户", "");

        using var _ = new CultureScope("de");

        Assert.Equal("Benutzer", MenuHelper.GetDisplayName(menu));
    }

    [Fact(DisplayName = "菜单显示 - 类型无法解析且库值为空时取菜单名")]
    public void FallsBackToMenuName()
    {
        var menu = CreateMenu(null, "No.Such.Type");

        using var _ = new CultureScope("de");

        Assert.Equal("Sample", MenuHelper.GetDisplayName(menu));
    }

    [Fact(DisplayName = "菜单显示 - 源文语言判定：中文源只认简体")]
    public void SourceLanguageDetection()
    {
        Assert.True(MenuHelper.IsSourceLanguage(CultureInfo.GetCultureInfo("zh-Hans"), "用户"));
        Assert.True(MenuHelper.IsSourceLanguage(CultureInfo.GetCultureInfo("zh-CN"), "用户"));
        Assert.False(MenuHelper.IsSourceLanguage(CultureInfo.GetCultureInfo("zh-Hant"), "用户"));
        Assert.False(MenuHelper.IsSourceLanguage(CultureInfo.GetCultureInfo("de"), "用户"));
        Assert.True(MenuHelper.IsSourceLanguage(CultureInfo.GetCultureInfo("en"), "User"));
        Assert.False(MenuHelper.IsSourceLanguage(CultureInfo.GetCultureInfo("zh-Hans"), "User"));
    }

    /// <summary>临时切换全局语言，释放时还原</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _prev;

        public CultureScope(String name)
        {
            _prev = Localization.Culture;
            Localization.Culture = CultureInfo.GetCultureInfo(name);
        }

        public void Dispose() => Localization.Culture = _prev;
    }

    private class UnusedController { }
}
