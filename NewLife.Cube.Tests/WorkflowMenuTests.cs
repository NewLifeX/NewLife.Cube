using System;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Cube.Workflow;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

[CollectionDefinition("Osc47f1Menu", DisableParallelization = true)]
public class Osc47f1MenuCollection
{
}

/// <summary>OSC-26090347f1 流程审批菜单播种：一级菜单（Root 下），父节点无 URL，五叶子对齐页面/实体路由</summary>
[Collection("Osc47f1Menu")]
public class WorkflowMenuTests
{
    private readonly String _db = Path.Combine(AppContext.BaseDirectory, "Data", "Osc47f1Menu.db");

    public WorkflowMenuTests()
    {
        var dir = Path.GetDirectoryName(_db);
        Directory.CreateDirectory(dir!);
        if (File.Exists(_db)) File.Delete(_db);

        DAL.AddConnStr("Osc47f1Menu", $"Data Source={_db}", null, "SQLite");
        Menu.Meta.ConnName = "Osc47f1Menu";
        TryCreate(Menu.Meta.Factory);
    }

    static void TryCreate(IEntityFactory fact)
    {
        try
        {
            fact.Session.Dal.Db.CreateMetaData().SetSchema(DDLSchema.CreateTable, fact.Table.DataTable);
        }
        catch
        {
            // 已建表
        }
    }

    [Fact(DisplayName = "EnsureMenus：播种一级流程审批及五叶子，父节点无 URL，Definition 指向实体控制器")]
    public void EnsureMenus_Seeds_TopLevel()
    {
        WorkflowHost.EnsureMenus();

        // 一级目录：Root 直接子级，与 Cube 平级
        var top = Menu.Root.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase("Workflow"));
        Assert.NotNull(top);
        Assert.Equal(0, top.ParentID);
        Assert.Equal("流程审批", top.DisplayName);
        Assert.True(top.Visible);
        Assert.True(top.Url.IsNullOrEmpty());

        AssertLeaf(top, "Definition", "流程定义", "/Cube/WorkflowDefinition");
        AssertLeaf(top, "Designer", "流程设计", "/Cube/Workflow/Designer");
        AssertLeaf(top, "Todo", "我的待办", "/Cube/Workflow/Todo");
        AssertLeaf(top, "Started", "我发起的", "/Cube/Workflow/Started");
        AssertLeaf(top, "Done", "已办", "/Cube/Workflow/Done");

        // Definition 叶子 FullName 指向实体控制器，供 ScanController 幂等复用/授权
        var def = top.FindByPath("Definition");
        Assert.Equal(typeof(NewLife.Cube.Areas.Cube.Controllers.WorkflowDefinitionController).FullName, def.FullName);

        // 幂等：重复播种不翻倍
        WorkflowHost.EnsureMenus();
        Assert.Equal(5, top.Childs.Count);
    }

    [Fact(DisplayName = "EnsureMenus：旧版 Cube/Workflow 提升为一级，叶子保留")]
    public void EnsureMenus_Migrate_From_Cube()
    {
        // 模拟上一版播种：Cube 下 Workflow + Todo 叶子
        var cube = Menu.Root.Add("Cube", "魔方管理", "NewLife.Cube.Areas.Cube.Controllers", "/Cube");
        var oldParent = cube.Add("Workflow", "流程审批", typeof(NewLife.Cube.Workflow.Controllers.WorkflowController).FullName, null);
        oldParent.Add("Todo", "我的待办", "x.Todo", "/Cube/Workflow/Todo");

        WorkflowHost.EnsureMenus();

        var top = Menu.Root.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase("Workflow"));
        Assert.NotNull(top);
        Assert.Equal(0, top.ParentID);
        Assert.Equal("流程审批", top.DisplayName);
        // 旧叶子 Todo 随之保留
        AssertLeaf(top, "Todo", "我的待办", "/Cube/Workflow/Todo");
        // 幂等后可再次迁移不再新增
        WorkflowHost.EnsureMenus();
        Assert.Equal(1, Menu.Root.Childs.Count(e => e.Name.EqualIgnoreCase("Workflow")));
    }

    static void AssertLeaf(IMenu parent, String name, String display, String url)
    {
        var node = parent.FindByPath(name);
        Assert.NotNull(node);
        Assert.Equal(display, node.DisplayName);
        Assert.Equal(url, node.Url);
        Assert.True(node.Visible);
    }
}
