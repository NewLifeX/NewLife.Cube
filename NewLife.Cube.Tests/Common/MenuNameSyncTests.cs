using System;
using System.ComponentModel;
using NewLife.Cube.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>覆盖菜单源文快照契约：扫描只填空，刷新按代码源文覆盖</summary>
[Collection("ConfigLocalization")]
public class MenuNameSyncTests
{
    private static Menu CreateMenu(String? displayName, String? fullName = null) => new()
    {
        Name = "Sample",
        DisplayName = displayName,
        FullName = fullName ?? typeof(SampleController).FullName,
    };

    [Fact(DisplayName = "源文快照 - 显示名为空时写入开发者语言")]
    public void EmptyNameIsFilled()
    {
        var menu = CreateMenu(null);

        Assert.True(MenuHelper.FillDisplayName(menu, "用户"));
        Assert.Equal("用户", menu.DisplayName);
    }

    [Fact(DisplayName = "源文快照 - 已有显示名时一个字节都不改")]
    public void ExistingNameIsKept()
    {
        foreach (var name in new[] { "用户", "User", "我的用户" })
        {
            var menu = CreateMenu(name);

            Assert.False(MenuHelper.FillDisplayName(menu, "用户"));
            Assert.Equal(name, menu.DisplayName);
        }
    }

    [Fact(DisplayName = "源文快照 - 源文为空时不做任何处理")]
    public void EmptySourceDoesNothing()
    {
        var menu = CreateMenu(null);

        Assert.False(MenuHelper.FillDisplayName(menu, null));
        Assert.False(MenuHelper.FillDisplayName(menu, ""));
        Assert.Null(menu.DisplayName);
    }

    [Fact(DisplayName = "源文解析 - 控制器特性优先")]
    public void SourceFromControllerDisplayName()
    {
        var menu = CreateMenu(null, typeof(SampleController).FullName);

        Assert.Equal("用户", MenuHelper.GetSourceName(menu));
    }

    [Fact(DisplayName = "源文解析 - 实体类型取表显示名")]
    public void SourceFromEntity()
    {
        var menu = CreateMenu(null, typeof(User).FullName);

        Assert.Equal("用户", MenuHelper.GetSourceName(menu));
    }

    [Fact(DisplayName = "源文解析 - 无特性控制器回退类名短名")]
    public void SourceFromTypeName()
    {
        var menu = CreateMenu(null, typeof(PlainController).FullName);

        Assert.Equal("Plain", MenuHelper.GetSourceName(menu));
    }

    [Fact(DisplayName = "源文解析 - 无法解析类型时返回空")]
    public void SourceUnknownType()
    {
        var menu = CreateMenu(null, "No.Such.Type");

        Assert.Null(MenuHelper.GetSourceName(menu));
    }

    [Fact(DisplayName = "刷新菜单 - 用代码源文覆盖库值")]
    public void RefreshOverwritesStoredName()
    {
        var menu = CreateMenu("旧名字", typeof(SampleController).FullName);

        Assert.True(MenuHelper.RefreshDisplayName(menu));
        Assert.Equal("用户", menu.DisplayName);

        // 已经是源文时不再改动
        Assert.False(MenuHelper.RefreshDisplayName(menu));
    }

    [Fact(DisplayName = "刷新菜单 - 全名为空时按菜单名猜控制器并补全")]
    public void RefreshFillsFullName()
    {
        var menu = CreateMenu("旧名字", "");
        menu.Name = "UniqueSync";

        Assert.True(MenuHelper.RefreshDisplayName(menu));
        Assert.Equal("唯一同步", menu.DisplayName);
        Assert.Equal(typeof(UniqueSyncController).FullName, menu.FullName);
    }

    [Fact(DisplayName = "刷新菜单 - 找不到控制器时跳过")]
    public void RefreshSkipsUnknown()
    {
        var menu = CreateMenu("旧名字", "No.Such.Type");

        Assert.False(MenuHelper.RefreshDisplayName(menu));
        Assert.Equal("旧名字", menu.DisplayName);
    }

    [DisplayName("用户")]
    private class SampleController { }

    private class PlainController { }

    [DisplayName("唯一同步")]
    private class UniqueSyncController { }
}
