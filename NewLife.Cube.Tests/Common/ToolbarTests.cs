using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>工具栏按钮声明与序列化测试。涉及控制器全局上下文，禁止与其它集合并行</summary>
[CollectionDefinition("ToolbarTests", DisableParallelization = true)]
public class ToolbarTestsCollection { }

/// <summary>工具栏按钮声明与序列化测试</summary>
[Collection("ToolbarTests")]
public class ToolbarTests
{
    #region 辅助
    private sealed class TestActions
    {
        [ToolbarButton("批量刷新", PermissionFlags.Update, Method = "POST")]
        public void Refresh() { }

        [ToolbarButton("无参动作", Include = "")]
        public void NoArgs() { }

        [ToolbarButton("继承权限")]
        [EntityAuthorize(PermissionFlags.Delete)]
        public void WithAuthorize() { }

        [ToolbarButton("地图", Kind = "link", Url = "/Base/Org/Map")]
        public void Map() { }
    }

    private static IList<ToolbarItem> BuildToolbar(ToolbarCollection source = null)
        => ToolbarBuilder.Build(source, typeof(TestActions), t => t.AddLink("区域地图", "/Base/Org/Map", target: "_blank"), null, null);
    #endregion

    [Fact(DisplayName = "工具栏 - 菜单控制器声明批量刷新按钮")]
    public void MenuControllerDeclaresRefresh()
    {
        var list = new ToolbarCollection();
        ToolbarBuilder.Collect(typeof(NewLife.Cube.Areas.Admin.Controllers.MenuController), list);

        var item = list.GetItem("RefreshName");
        Assert.NotNull(item);
        Assert.Equal("Refresh menu names", item.DisplayName);
        Assert.Equal("POST", item.Method);
        Assert.Equal("fa-refresh", item.Icon);
        Assert.Contains("keys", item.Include ?? []);
    }

    [Fact(DisplayName = "ToDictionary 只输出有意义的属性")]
    public void ToDictionary_OutputsOnlyMeaningfulKeys()
    {
        var item = new ToolbarItem { Name = "refresh", DisplayName = "批量刷新", Kind = "request", Action = "Refresh", Include = ["keys"], RequiresSelection = true };
        var dic = item.ToDictionary();

        Assert.Equal("refresh", dic["name"]);
        Assert.Equal("request", dic["kind"]);
        Assert.Equal("批量刷新", dic["displayName"]);
        Assert.Equal("Refresh", dic["action"]);
        Assert.Equal(true, dic["requiresSelection"]);
        Assert.Equal(new[] { "keys" }, dic["include"]);
        Assert.False(dic.ContainsKey("url"));
        Assert.False(dic.ContainsKey("confirm"));
        Assert.False(dic.ContainsKey("target"));
    }

    [Fact(DisplayName = "未指定 Kind 时按 link 下发")]
    public void ToDictionary_DefaultsKindToLink()
    {
        var dic = new ToolbarItem { Name = "map", Url = "/Base/Center/Map" }.ToDictionary();

        Assert.Equal("link", dic["kind"]);
        Assert.Equal("/Base/Center/Map", dic["url"]);
    }

    [Fact(DisplayName = "AddAction 默认提交选中行主键，并自动要求选中")]
    public void AddAction_DefaultsToSelectedKeys()
    {
        var list = new ToolbarCollection();
        var item = list.AddAction("批量刷新", "Refresh");

        Assert.Equal(new[] { "keys" }, item.Include);
        Assert.True(item.RequiresSelection);
        Assert.Equal("request", item.Kind);
    }

    [Fact(DisplayName = "同名按钮后者覆盖前者")]
    public void AddItem_ReplacesByName()
    {
        var list = new ToolbarCollection();
        list.AddLink("地图", "/a");
        list.AddLink("地图", "/b");

        Assert.Single(list);
        Assert.Equal("/b", list[0].Url);
    }

    [Fact(DisplayName = "Clone 深拷贝，互不影响")]
    public void Clone_IsDeepCopy()
    {
        var list = new ToolbarCollection();
        list.AddAction("批量刷新", "Refresh");

        var clone = list.Clone();
        clone[0].Include[0] = "ids";
        clone[0].DisplayName = "改了";

        Assert.Equal("keys", list[0].Include[0]);
        Assert.Equal("批量刷新", list[0].DisplayName);
    }

    [Fact(DisplayName = "特性声明的动作进入工具栏，权限取授权特性")]
    public void OnGetToolbar_CollectsAttributes()
    {
        var list = BuildToolbar();

        var refresh = list.FirstOrDefault(e => e.Name == "Refresh");
        Assert.NotNull(refresh);
        Assert.Equal("批量刷新", refresh.DisplayName);
        Assert.Equal("POST", refresh.Method);
        Assert.Equal(new[] { "keys" }, refresh.Include);
        Assert.True(refresh.RequiresSelection);
        Assert.Equal(new[] { PermissionFlags.Update }, refresh.Permissions);

        var noArgs = list.FirstOrDefault(e => e.Name == "NoArgs");
        Assert.NotNull(noArgs);
        Assert.Null(noArgs.Include);
        Assert.False(noArgs.RequiresSelection);

        var withAuthorize = list.FirstOrDefault(e => e.Name == "WithAuthorize");
        Assert.NotNull(withAuthorize);
        Assert.Equal(new[] { PermissionFlags.Delete }, withAuthorize.Permissions);

        var map = list.FirstOrDefault(e => e.Name == "Map");
        Assert.NotNull(map);
        Assert.Null(map.Include);
        Assert.False(map.RequiresSelection);
        Assert.Equal("/Base/Org/Map", map.Url);
    }

    [Fact(DisplayName = "没有菜单时不做权限过滤")]
    public void OnGetToolbar_SkipsPermissionWhenNoMenu()
    {
        var list = BuildToolbar();

        Assert.Contains(list, e => e.Name == "Refresh");
    }

    [Fact(DisplayName = "OnBuildToolbar 在过滤前追加按钮")]
    public void OnGetToolbar_CollectsBuildHook()
    {
        var list = BuildToolbar();

        var map = list.FirstOrDefault(e => e.Name == "区域地图");
        Assert.NotNull(map);
        Assert.Equal("/Base/Org/Map", map.Url);
        Assert.False(map.RequiresSelection);
    }

    [Fact(DisplayName = "集合配置与特性同名时以集合为准")]
    public void OnGetToolbar_CollectionWins()
    {
        var source = new ToolbarCollection();
        source.AddLink("Refresh", "/custom/refresh");

        var list = BuildToolbar(source);
        var refresh = list.Single(e => e.Name == "Refresh");

        Assert.Equal("/custom/refresh", refresh.Url);
        Assert.Null(refresh.Action);
    }
}