using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

[Collection("ConfigLocalization")]
public class MenuLocalizationTests
{
    [Fact(DisplayName = "菜单显示名按当前语言翻译")]
    public void MenuDisplayName()
    {
        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { ["用户"] = "User" }, overwrite: true);
            Localization.Culture = culture;

            var menu = new Menu { Name = "User", DisplayName = "用户" };
            Assert.Equal("User", MenuHelper.GetDisplayName(menu));

            menu.DisplayName = null;
            Assert.Equal("User", MenuHelper.GetDisplayName(menu));
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "权限子项 - 标准权限位与自定义模板均按语言翻译")]
    public void PermissionNames()
    {
        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        try
        {
            Localization.Register(culture, new Dictionary<String, String>
            {
                ["用户"] = "User",
                ["查看"] = "View",
                ["删除{type}"] = "Delete {type}",
            }, overwrite: true);
            Localization.Culture = culture;

            var menu = new Menu { Name = "User", DisplayName = "用户" };
            menu.Permissions[(Int32)PermissionFlags.Detail] = "查看";
            menu.Permissions[16] = "删除{type}";

            Assert.Equal("View", MenuHelper.GetPermissionName(menu, (Int32)PermissionFlags.Detail, "查看"));
            Assert.Equal("Delete User", MenuHelper.GetPermissionName(menu, 16, "删除{type}"));

            var dic = MenuHelper.GetPermissions(menu);
            Assert.Equal("View", dic[(Int32)PermissionFlags.Detail]);
            Assert.Equal("Delete User", dic[16]);
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "枚举标签按当前语言翻译")]
    public void EnumLabels()
    {
        var prev = Localization.Culture;
        var culture = CultureInfo.GetCultureInfo("sw");
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { ["查看"] = "View" }, overwrite: true);
            Localization.Culture = culture;

            Assert.Equal("View", EnumLocalization.GetDescription(PermissionFlags.Detail));
            Assert.Equal("View", EnumLocalization.GetDescriptions(typeof(PermissionFlags))[(Int32)PermissionFlags.Detail]);
        }
        finally
        {
            Localization.Culture = prev;
            Localization.Unregister(culture);
        }
    }
}
