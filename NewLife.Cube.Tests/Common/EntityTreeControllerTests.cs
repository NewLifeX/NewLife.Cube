using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Tests.Membership;
using NewLife.Serialization;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// DATA-3 树形控制器测试。覆盖节点排序上移/下移、末节点不再下移、校验不通过时不动、节点不存在返回 404，
/// 以及按父级检索返回该节点及其子节点。
/// </summary>
[Collection("TenantAuth")]
public class EntityTreeControllerTests
{
    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public EntityTreeControllerTests(TenantAuthFixture fixture) { }

    #region 测试控制器
    /// <summary>菜单树控制器。跳过权限校验以便单测直接驱动排序动作</summary>
    private class MenuTreeController : EntityTreeApiController<Menu>
    {
        /// <summary>校验开关</summary>
        public Boolean AllowUpdate { get; set; } = true;

        protected override Boolean Valid(Menu entity, DataObjectMethodType type, Boolean post) => AllowUpdate;

        /// <summary>测试环境无 HttpContext，重写分页获取避免空引用</summary>
        protected override Pager GetCachePager() => null!;

        /// <summary>测试环境无日志上下文，重写日志为空实现</summary>
        protected override void WriteLog(String action, Boolean success, String remark) { }

        /// <summary>暴露树形标记</summary>
        public Boolean TestEnableTree => EnableTree;

        /// <summary>暴露列表检索</summary>
        public IEnumerable<Menu> TestSearch(Pager p) => Search(p);
    }
    #endregion

    #region 辅助
    private static MenuTreeController CreateController(Boolean allowUpdate = true)
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new MenuTreeController { AllowUpdate = allowUpdate };
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static String Unique(String prefix) => prefix + "_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>造一棵同级三兄弟树</summary>
    private static Menu[] BuildSiblings(out Menu parent)
    {
        parent = new Menu { Name = Unique("目录"), DisplayName = "目录", Type = MenuTypes.目录, ParentID = 0, Sort = 1, Visible = true };
        parent.Insert();

        var list = new Menu[3];
        for (var i = 0; i < list.Length; i++)
        {
            var child = new Menu
            {
                Name = Unique("菜单" + (i + 1)),
                DisplayName = "菜单" + (i + 1),
                Type = MenuTypes.菜单,
                ParentID = parent.ID,
                Sort = i + 1,
                Visible = true,
            };
            child.Insert();
            list[i] = child;
        }

        ClearCache();

        return list;
    }

    private static void ClearCache()
    {
        Menu.Meta.Session.Cache.Clear("tree-test", true);
        Menu.Meta.Cache.Clear("tree-test", true);
    }

    /// <summary>同级节点的展示顺序（与控制器/前端一致的排序口径）</summary>
    private static Int32[] DisplayOrder(Int32 parentId) => Menu.FindAllByParent(parentId).Select(e => e.ID).ToArray();

    private static Dictionary<String, Object> ReadJson(IActionResult rs)
    {
        var result = Assert.IsType<ContentResult>(rs);
        var json = new JsonParser(result.Content!).Decode() as IDictionary<String, Object>;

        return json == null ? [] : new Dictionary<String, Object>(json);
    }
    #endregion

    [Fact(DisplayName = "树形控制器_列表按树形渲染")]
    public void TreeController_EnableTree()
    {
        var ctrl = CreateController();

        Assert.True(ctrl.TestEnableTree);
    }

    [Fact(DisplayName = "树形控制器_按父级检索返回该节点及其子节点")]
    public void TreeController_SearchByParent_ReturnsNodeAndChildren()
    {
        var siblings = BuildSiblings(out var parent);
        var other = new Menu { Name = Unique("无关"), DisplayName = "无关", Type = MenuTypes.目录, ParentID = 0, Sort = 9, Visible = true };
        other.Insert();
        ClearCache();

        var ctrl = CreateController();
        var p = new Pager { PageSize = 1000 };
        p.Params[Menu.Setting?.Parent ?? "ParentID"] = parent.ID + "";

        var list = ctrl.TestSearch(p).ToList();

        // 以父节点为根的子树，含父节点自身
        Assert.Contains(list, e => e.ID == parent.ID);
        foreach (var child in siblings)
        {
            Assert.Contains(list, e => e.ID == child.ID);
        }
        Assert.DoesNotContain(list, e => e.ID == other.ID);
    }

    [Fact(DisplayName = "树形控制器_上移后排在兄弟节点之前")]
    public void TreeController_Up_MovesBeforePrevious()
    {
        var siblings = BuildSiblings(out var parent);
        var before = DisplayOrder(parent.ID);
        Assert.Equal(3, before.Length);

        var ctrl = CreateController();
        var rs = ctrl.Up(before[1]);

        Assert.Equal(0, ReadJson(rs)["code"].ToInt());

        var after = DisplayOrder(parent.ID);
        Assert.Equal(before[1], after[0]);
        Assert.Equal(before[0], after[1]);
        Assert.Equal(before[2], after[2]);
    }

    [Fact(DisplayName = "树形控制器_下移后排在兄弟节点之后")]
    public void TreeController_Down_MovesAfterNext()
    {
        var siblings = BuildSiblings(out var parent);
        var before = DisplayOrder(parent.ID);

        var ctrl = CreateController();
        var rs = ctrl.Down(before[0]);

        Assert.Equal(0, ReadJson(rs)["code"].ToInt());

        var after = DisplayOrder(parent.ID);
        Assert.Equal(before[1], after[0]);
        Assert.Equal(before[0], after[1]);
        Assert.Equal(before[2], after[2]);
    }

    [Fact(DisplayName = "树形控制器_末位节点下移不变")]
    public void TreeController_Down_LastSibling_NoChange()
    {
        var siblings = BuildSiblings(out var parent);
        var before = DisplayOrder(parent.ID);

        var ctrl = CreateController();
        ctrl.Down(before[2]);

        Assert.Equal(before, DisplayOrder(parent.ID));
    }

    [Fact(DisplayName = "树形控制器_校验不通过时不移动")]
    public void TreeController_Up_ValidRejects_NoChange()
    {
        var siblings = BuildSiblings(out var parent);
        var before = DisplayOrder(parent.ID);

        var ctrl = CreateController(false);
        ctrl.Up(before[1]);

        Assert.Equal(before, DisplayOrder(parent.ID));
    }

    [Fact(DisplayName = "树形控制器_节点不存在时返回404")]
    public void TreeController_Up_MissingNode_Returns404()
    {
        var ctrl = CreateController();

        var rs = ctrl.Up(999_999);

        Assert.Equal(404, ReadJson(rs)["code"].ToInt());
    }
}
