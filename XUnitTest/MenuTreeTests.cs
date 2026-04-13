using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>MenuTree 视图模型单元测试</summary>
public class MenuTreeTests
{
    #region 属性测试

    [Fact]
    [DisplayName("所有属性可正确设置和读取")]
    public void Properties_SetAndGet_Correctly()
    {
        var perms = new Dictionary<Int32, String> { { 1, "查看" }, { 2, "编辑" } };
        var tree = new MenuTree
        {
            ID = 2,
            Name = "Admin",
            DisplayName = "系统管理",
            FullName = "NewLife.Cube.Areas.Admin.Controllers",
            ParentID = 0,
            Url = "/Admin",
            Icon = "fa-desktop",
            Visible = true,
            NewWindow = false,
            Permissions = perms
        };

        Assert.Equal(2, tree.ID);
        Assert.Equal("Admin", tree.Name);
        Assert.Equal("系统管理", tree.DisplayName);
        Assert.Equal("NewLife.Cube.Areas.Admin.Controllers", tree.FullName);
        Assert.Equal(0, tree.ParentID);
        Assert.Equal("/Admin", tree.Url);
        Assert.Equal("fa-desktop", tree.Icon);
        Assert.True(tree.Visible);
        Assert.False(tree.NewWindow);
        Assert.Same(perms, tree.Permissions);
    }

    [Fact]
    [DisplayName("ParentID 可为 null")]
    public void ParentID_CanBeNull()
    {
        var tree = new MenuTree();
        Assert.Null(tree.ParentID);

        tree.ParentID = null;
        Assert.Null(tree.ParentID);
    }

    [Fact]
    [DisplayName("ParentID 可设置为零")]
    public void ParentID_CanBeZero()
    {
        var tree = new MenuTree { ParentID = 0 };
        Assert.Equal(0, tree.ParentID);
    }

    [Fact]
    [DisplayName("ToString 返回 Name 属性值")]
    public void ToString_Returns_Name()
    {
        var tree = new MenuTree { Name = "Admin" };
        Assert.Equal("Admin", tree.ToString());
    }

    [Fact]
    [DisplayName("ToString 当 Name 为 null 时返回 null")]
    public void ToString_NullName_ReturnsNull()
    {
        var tree = new MenuTree { Name = null };
        Assert.Null(tree.ToString());
    }

    [Fact]
    [DisplayName("Permissions 可为 null")]
    public void Permissions_CanBeNull()
    {
        var tree = new MenuTree { Permissions = null };
        Assert.Null(tree.Permissions);
    }

    [Fact]
    [DisplayName("Permissions 可包含多个权限项")]
    public void Permissions_MultipleItems()
    {
        var perms = new Dictionary<Int32, String>
        {
            { 1, "查看" },
            { 2, "新增" },
            { 4, "修改" },
            { 8, "删除" }
        };
        var tree = new MenuTree { Permissions = perms };

        Assert.Equal(4, tree.Permissions.Count);
        Assert.Equal("查看", tree.Permissions[1]);
        Assert.Equal("新增", tree.Permissions[2]);
        Assert.Equal("修改", tree.Permissions[4]);
        Assert.Equal("删除", tree.Permissions[8]);
    }

    [Fact]
    [DisplayName("Visible 默认为 false")]
    public void Visible_DefaultIsFalse()
    {
        var tree = new MenuTree();
        Assert.False(tree.Visible);
    }

    [Fact]
    [DisplayName("NewWindow 默认为 false")]
    public void NewWindow_DefaultIsFalse()
    {
        var tree = new MenuTree();
        Assert.False(tree.NewWindow);
    }

    [Fact]
    [DisplayName("Children setter 不影响 getter 返回值")]
    public void Children_Setter_DoesNotAffectGetter()
    {
        // 先构建一个能懒加载子菜单的树
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin" }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => list == null ? null : list.Select(x => new MenuTree { ID = x.ID, Name = x.Name }).ToList() is { Count: > 0 } l ? l : null,
            menus);

        Assert.NotNull(result);
        var item = result[0];

        // Children getter 通过静态委托返回 null（因为 getChildrenSrc 返回 null）
        var before = item.Children;
        // setter 是空实现，不应影响 getter
        item.Children = new List<MenuTree> { new MenuTree { ID = 99 } };
        var after = item.Children;

        Assert.Equal(before?.Count, after?.Count);
    }

    #endregion

    #region GetMenuTree 构建测试

    [Fact]
    [DisplayName("getMenuList 为 null 时 GetMenuTree 返回 null")]
    public void GetMenuTree_NullGetMenuList_ReturnsNull()
    {
        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => [],
            null,
            [new MockMenu { ID = 1, Name = "Test" }]);

        Assert.Null(result);
    }

    [Fact]
    [DisplayName("src 为 null 时 getMenuList 接收 null，返回 null")]
    public void GetMenuTree_NullSrc_ReturnsNull()
    {
        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => [],
            list => list == null ? null : [],
            null);

        Assert.Null(result);
    }

    [Fact]
    [DisplayName("空菜单列表时 getMenuList 返回 null")]
    public void GetMenuTree_EmptySource_ReturnsNull()
    {
        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => [],
            list => list == null || list.Count == 0 ? null : ProjectMenus(list),
            []);

        Assert.Null(result);
    }

    [Fact]
    [DisplayName("单菜单项正确映射所有字段")]
    public void GetMenuTree_SingleMenu_MapsAllFields()
    {
        var perms = new Dictionary<Int32, String> { { 1, "查看" } };
        var menus = new List<MockMenu>
        {
            new MockMenu
            {
                ID = 2,
                Name = "Admin",
                DisplayName = "系统管理",
                FullName = "NewLife.Cube.Areas.Admin.Controllers",
                ParentID = 0,
                Url = "/Admin",
                Icon = "fa-desktop",
                Visible = true,
                NewWindow = false,
                Permissions = perms
            }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.Single(result);

        var item = result[0];
        Assert.Equal(2, item.ID);
        Assert.Equal("Admin", item.Name);
        Assert.Equal("系统管理", item.DisplayName);
        Assert.Equal("NewLife.Cube.Areas.Admin.Controllers", item.FullName);
        Assert.Equal(0, item.ParentID);
        Assert.Equal("/Admin", item.Url);
        Assert.Equal("fa-desktop", item.Icon);
        Assert.True(item.Visible);
        Assert.False(item.NewWindow);
        Assert.Same(perms, item.Permissions);
    }

    [Fact]
    [DisplayName("多菜单项全部映射，顺序保持一致")]
    public void GetMenuTree_MultipleMenus_AllMappedInOrder()
    {
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin", DisplayName = "系统管理" },
            new MockMenu { ID = 2, Name = "School", DisplayName = "教务系统" },
            new MockMenu { ID = 3, Name = "Log", DisplayName = "日志" }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Admin", result[0].Name);
        Assert.Equal("School", result[1].Name);
        Assert.Equal("Log", result[2].Name);
    }

    [Fact]
    [DisplayName("子菜单通过静态委托懒加载")]
    public void GetMenuTree_Children_LazilyLoaded()
    {
        var childMap = new Dictionary<Int32, List<MockMenu>>
        {
            [1] = [new MockMenu { ID = 11, Name = "User", ParentID = 1, Url = "/Admin/User" }],
            [2] = [new MockMenu { ID = 21, Name = "Student", ParentID = 2, Url = "/School/Student" }]
        };

        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin" },
            new MockMenu { ID = 2, Name = "School" }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => childMap.TryGetValue(m.ID, out var c) ? c : null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var adminChildren = result[0].Children;
        Assert.NotNull(adminChildren);
        Assert.Single(adminChildren);
        Assert.Equal(11, adminChildren[0].ID);
        Assert.Equal("User", adminChildren[0].Name);

        var schoolChildren = result[1].Children;
        Assert.NotNull(schoolChildren);
        Assert.Single(schoolChildren);
        Assert.Equal(21, schoolChildren[0].ID);
        Assert.Equal("Student", schoolChildren[0].Name);
    }

    [Fact]
    [DisplayName("getChildrenSrc 返回 null 时 Children 为 null")]
    public void GetMenuTree_NullChildrenSrc_ChildrenIsNull()
    {
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin" }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result[0].Children);
    }

    [Fact]
    [DisplayName("子菜单为空列表时 Children 为 null")]
    public void GetMenuTree_EmptyChildrenList_ChildrenIsNull()
    {
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin" }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => (IList<MockMenu>)[],
            list => list == null || list.Count == 0 ? null : ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result[0].Children);
    }

    [Fact]
    [DisplayName("DisplayName 为 null 时 getMenuList 中回退到 Name（模拟控制器逻辑）")]
    public void GetMenuTree_DisplayNameNull_FallsBackToName()
    {
        // 控制器代码：DisplayName = menu.DisplayName ?? menu.Name
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Abc", DisplayName = null }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => list?.Select(m => new MenuTree
            {
                ID = m.ID,
                Name = m.Name,
                DisplayName = m.DisplayName ?? m.Name
            }).ToList() is { Count: > 0 } l ? l : null,
            menus);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Abc", result[0].DisplayName);
    }

    [Fact]
    [DisplayName("不可见菜单也被包含（控制器不过滤 Visible=false）")]
    public void GetMenuTree_InvisibleMenus_AreIncluded()
    {
        // 控制器原代码注释掉了 where m.Visible 过滤
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin", Visible = true },
            new MockMenu { ID = 2, Name = "Abc", Visible = false }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].Visible);
        Assert.False(result[1].Visible);
    }

    [Fact]
    [DisplayName("三级嵌套菜单正确构建（Admin → User → 子级）")]
    public void GetMenuTree_ThreeLevelHierarchy_BuiltCorrectly()
    {
        var level3 = new List<MockMenu>
        {
            new MockMenu { ID = 111, Name = "Add", ParentID = 11, Url = "/Admin/User/Add" }
        };
        var level2 = new List<MockMenu>
        {
            new MockMenu { ID = 11, Name = "User", ParentID = 1, Url = "/Admin/User" }
        };
        var level1 = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin", ParentID = 0, Url = "/Admin" }
        };

        var childMap = new Dictionary<Int32, List<MockMenu>>
        {
            [1] = level2,
            [11] = level3
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => childMap.TryGetValue(m.ID, out var c) ? c : null,
            list => ProjectMenus(list),
            level1);

        Assert.NotNull(result);
        Assert.Single(result);

        var admin = result[0];
        Assert.Equal("Admin", admin.Name);

        var userChildren = admin.Children;
        Assert.NotNull(userChildren);
        Assert.Single(userChildren);
        Assert.Equal("User", userChildren[0].Name);

        var addChildren = userChildren[0].Children;
        Assert.NotNull(addChildren);
        Assert.Single(addChildren);
        Assert.Equal("Add", addChildren[0].Name);
    }

    [Fact]
    [DisplayName("GetMenuTree 多次调用，后一次覆盖静态委托")]
    public void GetMenuTree_MultipleInvocations_LastWins()
    {
        var menus1 = new List<MockMenu> { new MockMenu { ID = 1, Name = "First" } };
        var menus2 = new List<MockMenu> { new MockMenu { ID = 2, Name = "Second" } };

        var result1 = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null, list => ProjectMenus(list), menus1);

        var result2 = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null, list => ProjectMenus(list), menus2);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        // 第二次调用结果正确
        Assert.Equal("Second", result2[0].Name);
    }

    #endregion

    #region 菜单过滤逻辑测试（镜像 IndexController.GetMenu 业务规则）

    [Fact]
    [DisplayName("提升规则：只有一个顶级菜单且所有子项有更深层时提升一级")]
    public void FilterLogic_SingleTopLevelAllChildrenHaveSubChildren_Promotes()
    {
        // 对应控制器代码：
        // if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0)) menus = menus[0].Childs;
        var child1 = new MockMenu { ID = 11, Name = "User", Childs = [new MockMenu { ID = 111 }] };
        var child2 = new MockMenu { ID = 12, Name = "Role", Childs = [new MockMenu { ID = 121 }] };
        var topLevel = new MockMenu
        {
            ID = 1,
            Name = "Root",
            Childs = new List<IMenu> { child1, child2 }
        };

        IList<IMenu> menus = new List<IMenu> { topLevel };

        // 提升逻辑
        if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0))
            menus = menus[0].Childs;

        Assert.Equal(2, menus.Count);
        Assert.Equal("User", menus[0].Name);
        Assert.Equal("Role", menus[1].Name);
    }

    [Fact]
    [DisplayName("提升规则：多个顶级菜单时不触发提升")]
    public void FilterLogic_MultipleTopLevel_NoPromotion()
    {
        var top1 = new MockMenu { ID = 1, Name = "Admin", Childs = [new MockMenu { ID = 11, Childs = [new MockMenu { ID = 111 }] }] };
        var top2 = new MockMenu { ID = 2, Name = "School", Childs = [new MockMenu { ID = 21, Childs = [new MockMenu { ID = 211 }] }] };
        IList<IMenu> menus = new List<IMenu> { top1, top2 };

        if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0))
            menus = menus[0].Childs;

        Assert.Equal(2, menus.Count);
        Assert.Equal("Admin", menus[0].Name);
        Assert.Equal("School", menus[1].Name);
    }

    [Fact]
    [DisplayName("提升规则：单顶级但包含叶子节点时不提升")]
    public void FilterLogic_SingleTopLevelWithLeafChild_NoPromotion()
    {
        // child2 没有子节点（叶子），不满足 All(m => m.Childs.Count > 0)
        var child1 = new MockMenu { ID = 11, Name = "User", Childs = [new MockMenu { ID = 111 }] };
        var child2 = new MockMenu { ID = 12, Name = "Role", Childs = [] };  // 叶子节点
        var topLevel = new MockMenu { ID = 1, Name = "Root", Childs = new List<IMenu> { child1, child2 } };

        IList<IMenu> menus = new List<IMenu> { topLevel };

        if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0))
            menus = menus[0].Childs;

        Assert.Single(menus);
        Assert.Equal("Root", menus[0].Name);  // 未提升
    }

    [Fact]
    [DisplayName("默认模式（module 为空）：只保留所有子项均为叶子的模块")]
    public void FilterLogic_DefaultModule_KeepsOnlyTwoLevelMenus()
    {
        // 控制器逻辑：var ms = menus.Where(e => e.Childs.All(x => x.Childs.Count == 0)).ToList()
        var adminChild = new MockMenu { ID = 21, Name = "User", Childs = [] };        // 叶子
        var schoolChild = new MockMenu { ID = 31, Name = "Student", Childs = [new MockMenu { ID = 311 }] };  // 有子级

        var adminMenu = new MockMenu { ID = 2, Name = "Admin", Childs = new List<IMenu> { adminChild } };
        var schoolMenu = new MockMenu { ID = 3, Name = "School", Childs = new List<IMenu> { schoolChild } };

        var menus = new List<IMenu> { adminMenu, schoolMenu };

        // 只保留子菜单全部为叶子的模块（二级菜单结构）
        var ms = menus.Where(e => e.Childs.All(x => x.Childs.Count == 0)).ToList();

        Assert.Single(ms);
        Assert.Equal("Admin", ms[0].Name);
    }

    [Fact]
    [DisplayName("默认模式：所有模块都有三级菜单时，回退取第一个有可访问子菜单的模块")]
    public void FilterLogic_DefaultModule_FallbackToFirstNonEmpty()
    {
        // 所有顶级菜单的子菜单都有更深层，导致 ms 初始为空
        var deepChild = new MockMenu { ID = 111, Childs = [] };
        var midChild = new MockMenu { ID = 11, Name = "User", Childs = new List<IMenu> { deepChild } };
        var adminMenu = new MockMenu { ID = 1, Name = "Admin", Childs = new List<IMenu> { midChild } };

        var menus = new List<IMenu> { adminMenu };

        var ms = menus.Where(e => e.Childs.All(x => x.Childs.Count == 0)).ToList();
        Assert.Empty(ms);  // 前提条件：初始为空

        // 回退逻辑：取第一个有可访问子菜单的模块
        if (ms.Count == 0)
        {
            foreach (var item in menus)
            {
                var subMenus = item.Childs;  // 模拟 GetMySubMenus
                if (subMenus.Count > 0)
                {
                    ms = subMenus.ToList();
                    break;
                }
            }
        }

        Assert.Single(ms);
        Assert.Equal("User", ms[0].Name);
    }

    [Fact]
    [DisplayName("默认模式：无任何菜单时返回空列表")]
    public void FilterLogic_DefaultModule_EmptyMenus_ReturnsEmpty()
    {
        var menus = new List<IMenu>();

        var ms = menus.Where(e => e.Childs.All(x => x.Childs.Count == 0)).ToList();

        Assert.Empty(ms);
    }

    [Fact]
    [DisplayName("base 模块过滤：取 base 的子菜单，并追加其他仅有二级的模块")]
    public void FilterLogic_BaseModule_BaseChildrenPlusFlatModules()
    {
        // 控制器逻辑：
        // var ms = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("base"))?.Childs ?? [];
        // foreach (var item in menus) { if (!base && item.Childs.All(e => e.Childs.Count == 0)) ms.Add(item); }
        var baseChild = new MockMenu { ID = 11, Name = "Config" };
        var baseMenu = new MockMenu { ID = 1, Name = "Base", Childs = new List<IMenu> { baseChild } };

        var adminLeafChild = new MockMenu { ID = 21, Name = "Cfg", Childs = [] };  // 叶子
        var adminMenu = new MockMenu { ID = 2, Name = "Admin", Childs = new List<IMenu> { adminLeafChild } };

        // School 有三级菜单，不符合条件
        var schoolDeep = new MockMenu { ID = 311, Childs = [] };
        var schoolChild = new MockMenu { ID = 31, Childs = new List<IMenu> { schoolDeep } };
        var schoolMenu = new MockMenu { ID = 3, Name = "School", Childs = new List<IMenu> { schoolChild } };

        var menus = new List<IMenu> { baseMenu, adminMenu, schoolMenu };

        var msList = (menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("base"))?.Childs?.ToList() ?? []).Cast<IMenu>().ToList();
        foreach (var item in menus)
        {
            if (!item.Name.EqualIgnoreCase("base") && item.Childs.All(e => e.Childs.Count == 0))
                msList.Add(item);
        }

        Assert.Equal(2, msList.Count);
        Assert.Equal("Config", msList[0].Name);   // base 的子菜单
        Assert.Equal("Admin", msList[1].Name);    // 符合条件的平级模块
        // School 不符合（三级菜单），不包含
    }

    [Fact]
    [DisplayName("base 模块过滤：没有 base 菜单时仅返回符合条件的其他模块")]
    public void FilterLogic_BaseModule_NoBaseMenuExists()
    {
        var adminLeafChild = new MockMenu { ID = 21, Name = "User", Childs = [] };
        var adminMenu = new MockMenu { ID = 2, Name = "Admin", Childs = new List<IMenu> { adminLeafChild } };
        var menus = new List<IMenu> { adminMenu };

        var msList = (menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("base"))?.Childs?.ToList() ?? []).Cast<IMenu>().ToList();
        foreach (var item in menus)
        {
            if (!item.Name.EqualIgnoreCase("base") && item.Childs.All(e => e.Childs.Count == 0))
                msList.Add(item);
        }

        Assert.Single(msList);
        Assert.Equal("Admin", msList[0].Name);
    }

    [Fact]
    [DisplayName("指定 module 名称：返回该模块的子菜单列表")]
    public void FilterLogic_SpecificModule_ReturnsModuleChildren()
    {
        var adminChild1 = new MockMenu { ID = 21, Name = "User", ParentID = 2 };
        var adminChild2 = new MockMenu { ID = 22, Name = "Role", ParentID = 2 };
        var adminMenu = new MockMenu { ID = 2, Name = "Admin", Childs = new List<IMenu> { adminChild1, adminChild2 } };
        var schoolMenu = new MockMenu { ID = 3, Name = "School" };

        var menus = new List<IMenu> { adminMenu, schoolMenu };
        String module = "admin";

        var filtered = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase(module))?.Childs ?? [];

        Assert.Equal(2, filtered.Count);
        Assert.Equal("User", filtered[0].Name);
        Assert.Equal("Role", filtered[1].Name);
    }

    [Fact]
    [DisplayName("指定 module 名称大小写不敏感")]
    public void FilterLogic_SpecificModule_CaseInsensitive()
    {
        var child = new MockMenu { ID = 11, Name = "Index", ParentID = 1 };
        var adminMenu = new MockMenu { ID = 1, Name = "Admin", Childs = new List<IMenu> { child } };
        var menus = new List<IMenu> { adminMenu };

        // 大写
        var result1 = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("ADMIN"))?.Childs ?? [];
        // 小写
        var result2 = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("admin"))?.Childs ?? [];
        // 混合
        var result3 = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("Admin"))?.Childs ?? [];

        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Single(result3);
    }

    [Fact]
    [DisplayName("指定不存在的 module 时返回空列表")]
    public void FilterLogic_NonExistentModule_ReturnsEmptyList()
    {
        var adminMenu = new MockMenu { ID = 2, Name = "Admin" };
        var menus = new List<IMenu> { adminMenu };
        String module = "NonExistent";

        var filtered = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase(module))?.Childs ?? [];

        Assert.Empty(filtered);
    }

    [Fact]
    [DisplayName("指定模块子菜单为空时返回空列表")]
    public void FilterLogic_SpecificModule_EmptyChildren_ReturnsEmpty()
    {
        var adminMenu = new MockMenu { ID = 2, Name = "Admin", Childs = [] };
        var menus = new List<IMenu> { adminMenu };
        String module = "Admin";

        var filtered = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase(module))?.Childs ?? [];

        Assert.Empty(filtered);
    }

    #endregion

    #region 完整业务场景测试

    [Fact]
    [DisplayName("GetMenuTree 正确产出 Admin 菜单（复现 JSON 响应结构）")]
    public void Scenario_AdminModuleOnly_SingleTopLevelResult()
    {
        // 模拟仅有 Admin 菜单权限的场景（对应用户反馈的只返回一个菜单）
        var perms = new Dictionary<Int32, String>();
        var menus = new List<MockMenu>
        {
            new MockMenu
            {
                ID = 2,
                Name = "Admin",
                DisplayName = "系统管理",
                FullName = "NewLife.Cube.Areas.Admin.Controllers",
                ParentID = 0,
                Url = "/Admin",
                Icon = "fa-desktop",
                Visible = true,
                NewWindow = false,
                Permissions = perms
            }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => list?.Select(x => new MenuTree
            {
                ID = x.ID,
                Name = x.Name,
                DisplayName = x.DisplayName ?? x.Name,
                FullName = x.FullName,
                Url = x.Url,
                Icon = x.Icon,
                Visible = x.Visible,
                NewWindow = x.NewWindow,
                ParentID = x.ParentID,
                Permissions = x.Permissions
            }).ToList() is { Count: > 0 } l ? l : null,
            menus);

        // 验证 JSON 响应中的结构
        Assert.NotNull(result);
        Assert.Single(result);

        var admin = result[0];
        Assert.Equal(2, admin.ID);
        Assert.Equal("Admin", admin.Name);
        Assert.Equal("系统管理", admin.DisplayName);
        Assert.Equal("NewLife.Cube.Areas.Admin.Controllers", admin.FullName);
        Assert.Equal(0, admin.ParentID);
        Assert.Equal("/Admin", admin.Url);
        Assert.Equal("fa-desktop", admin.Icon);
        Assert.True(admin.Visible);
        Assert.False(admin.NewWindow);
        Assert.NotNull(admin.Permissions);
        Assert.Null(admin.Children);  // 无子菜单
    }

    [Fact]
    [DisplayName("GetMenuTree 多个顶级模块全部映射（Admin/School 等）")]
    public void Scenario_MultipleTopLevelModules_AllMapped()
    {
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 2, Name = "Admin", DisplayName = "系统管理", Url = "/Admin", ParentID = 0, Visible = true },
            new MockMenu { ID = 34, Name = "School", DisplayName = "教务系统", Url = "/School", ParentID = 0, Visible = true }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Admin", result[0].Name);
        Assert.Equal("School", result[1].Name);
        Assert.Equal(0, result[0].ParentID);
        Assert.Equal(0, result[1].ParentID);
    }

    [Fact]
    [DisplayName("GetMenuTree 子菜单 ParentID 正确映射")]
    public void Scenario_ChildMenus_ParentIDMappedCorrectly()
    {
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 2, Name = "Admin", ParentID = 0 }
        };

        var adminChildren = new List<MockMenu>
        {
            new MockMenu { ID = 21, Name = "User", ParentID = 2 },
            new MockMenu { ID = 22, Name = "Role", ParentID = 2 }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => m.ID == 2 ? adminChildren : null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        var children = result[0].Children;
        Assert.NotNull(children);
        Assert.Equal(2, children.Count);
        Assert.Equal(2, children[0].ParentID);
        Assert.Equal(2, children[1].ParentID);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    [DisplayName("新建 MenuTree 实例时所有属性为默认值")]
    public void MenuTree_DefaultValues_AreExpected()
    {
        var tree = new MenuTree();

        Assert.Equal(0, tree.ID);
        Assert.Null(tree.Name);
        Assert.Null(tree.DisplayName);
        Assert.Null(tree.FullName);
        Assert.Null(tree.ParentID);
        Assert.Null(tree.Url);
        Assert.Null(tree.Icon);
        Assert.False(tree.Visible);
        Assert.False(tree.NewWindow);
        Assert.Null(tree.Permissions);
        Assert.Null(tree.Children);
    }

    [Fact]
    [DisplayName("空字符串字段与 null 不同，可区分")]
    public void MenuTree_EmptyString_IsNotNull()
    {
        var tree = new MenuTree { Name = "", Url = "", Icon = "", FullName = "" };

        Assert.NotNull(tree.Name);
        Assert.Equal(String.Empty, tree.Name);
        Assert.NotNull(tree.Url);
        Assert.Equal(String.Empty, tree.Url);
    }

    [Fact]
    [DisplayName("极大 ID 值可正确存储和读取")]
    public void MenuTree_LargeIdValue_Stored()
    {
        var tree = new MenuTree { ID = Int32.MaxValue, ParentID = Int32.MaxValue - 1 };

        Assert.Equal(Int32.MaxValue, tree.ID);
        Assert.Equal(Int32.MaxValue - 1, tree.ParentID);
    }

    [Fact]
    [DisplayName("负数 ParentID 可正确存储（保留原始值）")]
    public void MenuTree_NegativeParentID_Stored()
    {
        var tree = new MenuTree { ParentID = -1 };
        Assert.Equal(-1, tree.ParentID);
    }

    [Fact]
    [DisplayName("GetMenuTree 来源权限字典为空时可正常映射")]
    public void GetMenuTree_EmptyPermissions_MappedAsEmptyDict()
    {
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin", Permissions = [] }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => ProjectMenus(list),
            menus);

        Assert.NotNull(result);
        Assert.NotNull(result[0].Permissions);
        Assert.Empty(result[0].Permissions);
    }

    [Fact]
    [DisplayName("默认模式：所有模块子菜单均有三级嵌套时过滤后结果为空")]
    public void FilterLogic_DefaultModule_AllHaveDeepChildren_EmptyResult()
    {
        // 所有模块的子都不是叶子（都有自己的子菜单），不满足 All(x => x.Childs.Count == 0)
        var deep = new MockMenu { ID = 111, Childs = [] };
        var mid1 = new MockMenu { ID = 11, Childs = new List<IMenu> { deep } };
        var mid2 = new MockMenu { ID = 21, Childs = new List<IMenu> { deep } };
        var admin = new MockMenu { ID = 1, Name = "Admin", Childs = new List<IMenu> { mid1 } };
        var school = new MockMenu { ID = 2, Name = "School", Childs = new List<IMenu> { mid2 } };
        var menus = new List<IMenu> { admin, school };

        var ms = menus.Where(e => e.Childs.All(x => x.Childs.Count == 0)).ToList();

        Assert.Empty(ms);
    }

    [Fact]
    [DisplayName("指定模块其 Childs 初始为空集合时返回空")]
    public void FilterLogic_SpecificModule_EmptyChildsList_ReturnsEmpty()
    {
        var adminMenu = new MockMenu { ID = 1, Name = "Admin", Childs = new List<IMenu>() };
        var menus = new List<IMenu> { adminMenu };

        var filtered = menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("Admin"))?.Childs ?? [];

        Assert.NotNull(filtered);
        Assert.Empty(filtered);
    }

    [Fact]
    [DisplayName("base 模块过滤：所有非 base 模块都有深层子节点，只返回 base 子菜单")]
    public void FilterLogic_BaseModule_OnlyBaseChildrenWhenOthersAreDeep()
    {
        var baseChild = new MockMenu { ID = 11, Name = "Config" };
        var baseMenu = new MockMenu { ID = 1, Name = "base", Childs = new List<IMenu> { baseChild } };

        // Admin 和 School 都有二级嵌套（不符合"所有子项为叶子"条件）
        var deep = new MockMenu { ID = 211, Childs = [] };
        var mid = new MockMenu { ID = 21, Childs = new List<IMenu> { deep } };
        var adminMenu = new MockMenu { ID = 2, Name = "Admin", Childs = new List<IMenu> { mid } };
        var schoolMenu = new MockMenu { ID = 3, Name = "School", Childs = new List<IMenu> { mid } };

        var menus = new List<IMenu> { baseMenu, adminMenu, schoolMenu };

        var msList = (menus.FirstOrDefault(e => e.Name.EqualIgnoreCase("base"))?.Childs?.ToList() ?? []).Cast<IMenu>().ToList();
        foreach (var item in menus)
        {
            if (!item.Name.EqualIgnoreCase("base") && item.Childs.All(e => e.Childs.Count == 0))
                msList.Add(item);
        }

        Assert.Single(msList);
        Assert.Equal("Config", msList[0].Name);
    }

    [Fact]
    [DisplayName("GetMenuTree 当 getMenuList 委托返回 null 时，整体结果为 null")]
    public void GetMenuTree_GetMenuListReturnsNull_ChildrenIsNull()
    {
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin" }
        };

        // getMenuList 返回 null，意味着顶级映射失败，整体结果应为 null
        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => menus,     // 有子数据来源（会被调用，但顶级已是 null）
            list => null,   // 映射委托返回 null
            menus);

        Assert.Null(result);   // 顶级映射为 null → 整体结果为 null
    }

    [Fact]
    [DisplayName("GetMenuTree 多次调用替换静态委托，以最后一次设置为准")]
    public void GetMenuTree_MultipleInvocations_LastDelegateWins()
    {
        var menus1 = new List<MockMenu> { new MockMenu { ID = 1, Name = "First" } };
        var menus2 = new List<MockMenu> { new MockMenu { ID = 2, Name = "Second" } };

        // 第一次调用
        var result1 = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            ProjectMenus,
            menus1);

        // 第二次调用（会替换静态 GetChildren 委托）
        var result2 = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            ProjectMenus,
            menus2);

        Assert.Single(result1);
        Assert.Equal("First", result1[0].Name);
        Assert.Single(result2);
        Assert.Equal("Second", result2[0].Name);
    }

    [Fact]
    [DisplayName("GetMenuTree 权限列表包含多个权限项时全部映射")]
    public void GetMenuTree_MultiplePermissions_AllMapped()
    {
        var perms = new Dictionary<Int32, String>
        {
            [1] = "查看",
            [2] = "新增",
            [4] = "编辑",
            [8] = "删除"
        };
        var menus = new List<MockMenu>
        {
            new MockMenu { ID = 1, Name = "Admin", Permissions = perms }
        };

        var result = MenuTree.GetMenuTree<IList<MockMenu>>(
            m => null,
            list => list?.Select(m => new MenuTree
            {
                ID = m.ID,
                Name = m.Name,
                DisplayName = m.DisplayName ?? m.Name,
                Permissions = m.Permissions
            }).ToList(),
            menus);

        Assert.NotNull(result);
        Assert.Equal(4, result[0].Permissions.Count);
        Assert.Equal("查看", result[0].Permissions[1]);
        Assert.Equal("删除", result[0].Permissions[8]);
    }

    [Fact]
    [DisplayName("提升规则：单顶级且所有子都有子集时正确提升，再次检验提升后 Count")]
    public void FilterLogic_Promotion_SingleTopLevel_AllChildrenHaveKids_PromotedCount()
    {
        // 构建 3 个二级子菜单，每个都有自己的子菜单
        var grandchildren = new[] { 0, 1, 2 }.Select(i =>
            new MockMenu { ID = 100 + i, Name = $"GC{i}", Childs = [new MockMenu { ID = 200 + i }] } as IMenu).ToList();

        var topLevel = new MockMenu { ID = 1, Name = "Root", Childs = grandchildren };
        IList<IMenu> menus = new List<IMenu> { topLevel };

        if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0))
            menus = menus[0].Childs;

        Assert.Equal(3, menus.Count);
        Assert.Equal("GC0", menus[0].Name);
        Assert.Equal("GC1", menus[1].Name);
        Assert.Equal("GC2", menus[2].Name);
    }

    #endregion

    #region 辅助方法

    /// <summary>将 MockMenu 列表投影为 MenuTree 列表（模拟控制器的 LINQ 映射）</summary>
    private static IList<MenuTree> ProjectMenus(IList<MockMenu> list)
    {
        if (list == null || list.Count == 0) return null;
        return list.Select(m => new MenuTree
        {
            ID = m.ID,
            Name = m.Name,
            DisplayName = m.DisplayName ?? m.Name,
            FullName = m.FullName,
            Url = m.Url,
            Icon = m.Icon,
            Visible = m.Visible,
            NewWindow = m.NewWindow,
            ParentID = m.ParentID,
            Permissions = m.Permissions
        }).ToList();
    }

    #endregion
}

/// <summary>IMenu 的测试用模拟实现，用于单元测试场景构建</summary>
/// <remarks>
/// 此类属性使用 C# 关键字别名（int/string/bool 等）而非正式 .NET 名称，
/// 原因：XCode.dll 编译目标为 netstandard2.0，其接口泛型参数（如 Dictionary&lt;int,string&gt;）
/// 绑定到 mscorlib.dll，在 net10.0 项目中实现时需用关键字别名才能正确匹配类型标识。
/// </remarks>
internal class MockMenu : IMenu
{
    // ReSharper disable BuiltInTypeReferenceStyle
    public int ID { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public MenuTypes Type { get; set; }
    public string FullName { get; set; }
    public int ParentID { get; set; }
    public string Url { get; set; }
    public int Sort { get; set; }
    public string Icon { get; set; }
    public bool Visible { get; set; } = true;
    public bool Necessary { get; set; }
    public bool NewWindow { get; set; }
    public DataScopes DataScope { get; set; }
    public string DataDepartmentIds { get; set; }
    public string Permission { get; set; }
    public int Ex1 { get; set; }
    public int Ex2 { get; set; }
    public double Ex3 { get; set; }
    public string Ex4 { get; set; }
    public string Ex5 { get; set; }
    public string Ex6 { get; set; }
    public string CreateUser { get; set; }
    public int CreateUserID { get; set; }
    public string CreateIP { get; set; }
    public DateTime CreateTime { get; set; }
    public string UpdateUser { get; set; }
    public int UpdateUserID { get; set; }
    public string UpdateIP { get; set; }
    public DateTime UpdateTime { get; set; }
    public string Remark { get; set; }

    // 以下 4 个属性在 IMenu 接口中为只读，此处添加 setter 以便测试数据构建
    // 公共属性隐式实现接口 getter 要求，setter 仅供测试使用
    public IMenu Parent { get; set; }
    public IList<IMenu> Childs { get; set; } = [];
    public IList<IMenu> AllChilds { get; set; } = [];
    public Dictionary<int, string> Permissions { get; set; } = [];

    // IMenu 方法在测试中不涉及，以公共方法形式实现接口，避免跨目标框架显式实现时的类型标识问题
    public IMenu Add(string name, string displayName, string fullName, string url)
        => throw new NotSupportedException("MockMenu.Add 未实现");

    public IMenu FindByPath(string path)
        => throw new NotSupportedException("MockMenu.FindByPath 未实现");

    public void Up() => throw new NotSupportedException("MockMenu.Up 未实现");

    public void Down() => throw new NotSupportedException("MockMenu.Down 未实现");

    public IList<IMenu> GetSubMenus(int[] pids, bool visible)
        => throw new NotSupportedException("MockMenu.GetSubMenus 未实现");

    public string GetFullPath(bool includeSelf, string separator, Func<IMenu, string> func)
        => throw new NotSupportedException("MockMenu.GetFullPath 未实现");

    public override String ToString() => Name;
}
