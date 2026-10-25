using System;
using System.Collections.Generic;
using System.Globalization;
using NewLife;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>覆盖菜单命名契约：自动项落库类型全名并跟随代码，用户自定义名称永不覆盖</summary>
[Collection("ConfigLocalization")]
public class MenuNameSyncTests
{
    private const String UserKey = "XCode.Membership.User";

    private static Menu CreateMenu(String? displayName, String? marker = null) => new()
    {
        Name = "User",
        DisplayName = displayName,
        Ex6 = marker,
    };

    [Fact(DisplayName = "命名同步 - 存量空名识别为自动项并写入规范键")]
    public void EmptyNameBecomesAuto()
    {
        var menu = CreateMenu(null);

        Assert.True(MenuHelper.SyncDisplayName(menu, UserKey, "用户", "User", "UserController"));
        Assert.Equal(UserKey, menu.DisplayName);
        Assert.Equal(UserKey, menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 存量等于菜单名或类型名识别为自动项")]
    public void FallbackNameBecomesAuto()
    {
        var menu = CreateMenu("User");
        Assert.True(MenuHelper.SyncDisplayName(menu, UserKey, "用户", "User", "UserController"));
        Assert.Equal(UserKey, menu.DisplayName);

        var menu2 = CreateMenu("UserController");
        Assert.True(MenuHelper.SyncDisplayName(menu2, UserKey, "用户", "User", "UserController"));
        Assert.Equal(UserKey, menu2.DisplayName);
    }

    [Fact(DisplayName = "命名同步 - 存量等于代码源文识别为自动项")]
    public void SourceNameBecomesAuto()
    {
        var menu = CreateMenu("用户");

        Assert.True(MenuHelper.SyncDisplayName(menu, UserKey, "用户"));
        Assert.Equal(UserKey, menu.DisplayName);
        Assert.Equal(UserKey, menu.Ex6);
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
            Assert.True(MenuHelper.SyncDisplayName(menu, UserKey, "用户"));
            Assert.Equal(UserKey, menu.DisplayName);
            Assert.Equal(UserKey, menu.Ex6);
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

        Assert.True(MenuHelper.SyncDisplayName(menu, "XCode.Membership.Parameter", "字典参数"));
        Assert.Equal("XCode.Membership.Parameter", menu.DisplayName);
        Assert.Equal("XCode.Membership.Parameter", menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 用户自定义名称只标记不覆盖，后续扫描不再修改")]
    public void CustomNameIsProtected()
    {
        var menu = CreateMenu("我的用户");

        Assert.True(MenuHelper.SyncDisplayName(menu, UserKey, "用户"));
        Assert.Equal("我的用户", menu.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, menu.Ex6);

        Assert.False(MenuHelper.SyncDisplayName(menu, "XCode.Membership.Role", "角色"));
        Assert.Equal("我的用户", menu.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 自动项跟随代码改名后的规范键")]
    public void AutoNameFollowsSourceChange()
    {
        var menu = CreateMenu(UserKey, UserKey);

        Assert.True(MenuHelper.SyncDisplayName(menu, "XCode.Membership.Member", "会员"));
        Assert.Equal("XCode.Membership.Member", menu.DisplayName);
        Assert.Equal("XCode.Membership.Member", menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 自动项被人工改名后转为自定义并保留文本")]
    public void EditedAutoNameBecomesCustom()
    {
        var menu = CreateMenu("Users", UserKey);

        Assert.True(MenuHelper.SyncDisplayName(menu, UserKey, "用户"));
        Assert.Equal("Users", menu.DisplayName);
        Assert.Equal(MenuHelper.CustomNameMarker, menu.Ex6);

        Assert.False(MenuHelper.SyncDisplayName(menu, UserKey, "用户"));
        Assert.Equal("Users", menu.DisplayName);
    }

    [Fact(DisplayName = "命名同步 - 规范键为空时不处理")]
    public void EmptyKeyDoesNothing()
    {
        var menu = CreateMenu("Users");

        Assert.False(MenuHelper.SyncDisplayName(menu, null, "用户"));
        Assert.False(MenuHelper.SyncDisplayName(menu, "", "用户"));
        Assert.Equal("Users", menu.DisplayName);
        Assert.Null(menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 新建自动项同时写入规范键与标记")]
    public void MarkAutoNameWritesMarker()
    {
        var menu = CreateMenu("用户");

        MenuHelper.MarkAutoName(menu, UserKey);
        Assert.Equal(UserKey, menu.DisplayName);
        Assert.Equal(UserKey, menu.Ex6);

        menu.DisplayName = "用户";
        MenuHelper.MarkAutoName(menu, null);
        Assert.Equal("用户", menu.DisplayName);
        Assert.Equal(UserKey, menu.Ex6);
    }

    [Fact(DisplayName = "命名同步 - 规范键实体全名优先，其次控制器全名")]
    public void BuildMenuKeyUsesFullName()
    {
        Assert.Equal(UserKey, MenuHelper.BuildMenuKey(typeof(TestUserController), typeof(User)));
        Assert.Equal(typeof(TestUserController).FullName, MenuHelper.BuildMenuKey(typeof(TestUserController)));
    }

    private sealed class TestUserController { }
}
