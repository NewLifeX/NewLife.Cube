using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>覆盖菜单命名契约：自动项跟随代码源文，用户自定义名称永不覆盖</summary>
[Collection("ConfigLocalization")]
public class MenuNameSyncTests
{
    private static Menu CreateMenu(String? displayName, String? marker = null) => new()
    {
        Name = "User",
        DisplayName = displayName,
        Ex6 = marker,
    };

    [Fact(DisplayName = "命名同步 - 存量空名识别为自动项并写入源文")]
    public void EmptyNameBecomesAuto()
    {
        var menu = CreateMenu(null);

        Assert.True(MenuHelper.SyncDisplayName(menu, "用户", "User", "UserController"));
        Assert.Equal("用户", menu.DisplayName);
        Assert.Equal("用户", menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 存量等于菜单名或类型名识别为自动项")]
    public void FallbackNameBecomesAuto()
    {
        var menu = CreateMenu("User");
        Assert.True(MenuHelper.SyncDisplayName(menu, "用户", "User", "UserController"));
        Assert.Equal("用户", menu.DisplayName);

        var menu2 = CreateMenu("UserController");
        Assert.True(MenuHelper.SyncDisplayName(menu2, "用户", "User", "UserController"));
        Assert.Equal("用户", menu2.DisplayName);
    }

    [Fact(DisplayName = "命名同步 - 存量等于当前语言译文识别为自动项")]
    public void TranslatedNameBecomesAuto()
    {
        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { ["用户"] = "User" }, overwrite: true);
            Localization.Culture = culture;

            var menu = CreateMenu("User");
            Assert.True(MenuHelper.SyncDisplayName(menu, "用户"));
            Assert.Equal("用户", menu.DisplayName);
            Assert.Equal("用户", menu.Ex6);
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "命名同步 - 存量等于旧版本英文译文识别为自动项")]
    public void OtherLanguageNameBecomesAuto()
    {
        var menu = CreateMenu("Dictionary parameters");

        Assert.True(MenuHelper.SyncDisplayName(menu, "字典参数"));
        Assert.Equal("字典参数", menu.DisplayName);
        Assert.Equal("字典参数", menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 用户自定义名称只标记不覆盖，后续扫描不再修改")]
    public void CustomNameIsProtected()
    {
        var menu = CreateMenu("我的用户");

        Assert.True(MenuHelper.SyncDisplayName(menu, "用户"));
        Assert.Equal("我的用户", menu.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, menu.Ex6);

        Assert.False(MenuHelper.SyncDisplayName(menu, "Users"));
        Assert.Equal("我的用户", menu.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 自动项跟随代码源文改名")]
    public void AutoNameFollowsSourceChange()
    {
        var menu = CreateMenu("用户", "用户");

        Assert.True(MenuHelper.SyncDisplayName(menu, "Users"));
        Assert.Equal("Users", menu.DisplayName);
        Assert.Equal("Users", menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 自动项被人工改名后转为自定义并保留文本")]
    public void EditedAutoNameBecomesCustom()
    {
        var menu = CreateMenu("Users", "用户");

        Assert.True(MenuHelper.SyncDisplayName(menu, "用户"));
        Assert.Equal("Users", menu.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, menu.Ex6);

        Assert.False(MenuHelper.SyncDisplayName(menu, "用户"));
        Assert.Equal("Users", menu.DisplayName);
    }

    [Fact(DisplayName = "命名同步 - 源文为空时不处理")]
    public void EmptySourceDoesNothing()
    {
        var menu = CreateMenu("Users");

        Assert.False(MenuHelper.SyncDisplayName(menu, null));
        Assert.False(MenuHelper.SyncDisplayName(menu, ""));
        Assert.Equal("Users", menu.DisplayName);
        Assert.Null(menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 新建自动项写入标记")]
    public void MarkAutoNameWritesMarker()
    {
        var menu = CreateMenu("用户");

        MenuHelper.MarkAutoName(menu, "用户");
        Assert.Equal("用户", menu.Ex6);

        MenuHelper.MarkAutoName(menu, null);
        Assert.Equal("用户", menu.Ex6);
    }
}
