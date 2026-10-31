using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube.Membership;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>菜单搜索数据构建测试</summary>
public class MenuSearchTests
{
    #region 辅助

    /// <summary>用固定树结构构造搜索数据</summary>
    private static IList<MenuSearchItem> Build(IList<IMenu> menus, IDictionary<Int32, IList<IMenu>> children, IDictionary<String, String> modules)
    {
        return MenuHelper.BuildSearchMenus(
            menus,
            m => children != null && children.TryGetValue(m.ID, out var list) ? list : [],
            modules,
            url => url.Replace("~/", "/"));
    }

    private static IMenu Menu(Int32 id, String name, String displayName, String url = null, Boolean visible = true, Boolean newWindow = false)
    {
        return new Menu
        {
            ID = id,
            Name = name,
            DisplayName = displayName,
            Url = url,
            Visible = visible,
            NewWindow = newWindow,
        };
    }

    #endregion

    [Fact]
    [DisplayName("空菜单返回空列表")]
    public void BuildSearchMenus_Empty_ReturnsEmpty()
    {
        Assert.Empty(MenuHelper.BuildSearchMenus(null, null, null, null));
        Assert.Empty(MenuHelper.BuildSearchMenus([], null, null, null));
    }

    [Fact]
    [DisplayName("收集可见且有链接的菜单，保留层级路径与模块")]
    public void BuildSearchMenus_CollectsVisibleUrls_WithModuleAndPath()
    {
        var system = Menu(1, "ZxSystem", "ZxSystem");
        var user = Menu(2, "ZxUser", "ZxUsers", "~/Admin/User");
        var detail = Menu(3, "ZxDetail", "ZxUserDetail", "/Admin/User/Detail");
        var hidden = Menu(4, "ZxSecret", "ZxSecret", "/Admin/Secret", visible: false);
        var category = Menu(5, "ZxGroup", "ZxGroup");
        var child = Menu(6, "ZxChild", "ZxChildPage", "/Admin/Child");
        var tools = Menu(7, "ZxTools", "ZxTools");
        var log = Menu(8, "ZxLog", "ZxAuditLog", "/Admin/Log", newWindow: true);

        var children = new Dictionary<Int32, IList<IMenu>>
        {
            [1] = [user, category],
            [2] = [detail, hidden],
            [5] = [child],
            [7] = [log],
        };
        var modules = new Dictionary<String, String> { ["ZxSystem"] = "ZxSystem" };

        var list = Build([system, tools], children, modules);

        Assert.Equal(4, list.Count);
        Assert.DoesNotContain(list, e => e.ID == 4);
        Assert.DoesNotContain(list, e => e.ID == 5);

        var userItem = list.First(e => e.ID == 2);
        Assert.Equal("ZxSystem", userItem.Module);
        Assert.Equal("ZxSystem / ZxUsers", userItem.Path);
        Assert.Equal("/Admin/User", userItem.Url);

        var detailItem = list.First(e => e.ID == 3);
        Assert.Equal("ZxSystem / ZxUsers / ZxUserDetail", detailItem.Path);

        var childItem = list.First(e => e.ID == 6);
        Assert.Equal("ZxSystem / ZxGroup / ZxChildPage", childItem.Path);

        var logItem = list.First(e => e.ID == 8);
        Assert.Equal("base", logItem.Module);
        Assert.True(logItem.NewWindow);
    }

    [Fact]
    [DisplayName("拼音全拼与首字母按库中源文显示名计算")]
    public void BuildSearchMenus_ComputesPinyinFromStoredName()
    {
        var system = Menu(1, "System", "系统管理");
        var user = Menu(2, "User", "用户管理", "/Admin/User");

        var children = new Dictionary<Int32, IList<IMenu>>
        {
            [1] = [user],
        };

        var list = Build([system], children, null);

        var item = list.First(e => e.ID == 2);
        Assert.Equal("YongHuGuanLi", item.PinYin);
        Assert.Equal("YHGL", item.Initials);
        Assert.Contains("YongHuGuanLi", item.PathPinYin);
        Assert.Contains("YHGL", item.PathInitials);
    }

    [Fact]
    [DisplayName("忽略无链接、井号链接与重复菜单")]
    public void BuildSearchMenus_SkipsInvalidUrls_AndDuplicates()
    {
        var root = Menu(1, "Root", "Root");
        var noUrl = Menu(2, "NoUrl", "Group");
        var hash = Menu(3, "Hash", "Hash", "#");
        var ok = Menu(4, "Ok", "Ok page", "/Admin/Ok");
        var duplicate = Menu(4, "Ok", "Ok page", "/Admin/Ok");

        var children = new Dictionary<Int32, IList<IMenu>>
        {
            [1] = [noUrl, hash, ok, duplicate],
        };

        var list = Build([root], children, null);

        Assert.Single(list);
        Assert.Equal(4, list[0].ID);
    }

    [Fact]
    [DisplayName("空显示名回退菜单名")]
    public void BuildSearchMenus_EmptyDisplayName_FallsBackToName()
    {
        var root = Menu(1, "Root", null, "/Admin/Root");

        var list = Build([root], null, null);

        Assert.Single(list);
        Assert.Equal("Root", list[0].DisplayName);
        Assert.Equal("Root", list[0].Path);
    }
}
