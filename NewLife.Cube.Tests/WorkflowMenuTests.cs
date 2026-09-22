using System;
using System.Linq;
using NewLife;
using NewLife.Cube.Workflow;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

[CollectionDefinition("Osc47f1Menu", DisableParallelization = true)]
public class Osc47f1MenuCollection
{
}

/// <summary>
/// 流程审批菜单播种。不切换 Menu.ConnName：连接是进程全局量，并行用例一旦先访问 Menu，
/// 再改连接名也不会换库，断言会打到空文件上。
/// </summary>
[Collection("Osc47f1Menu")]
public class WorkflowMenuTests
{
    [Fact(DisplayName = "EnsureMenus：播种一级流程审批及五叶子，父节点无 URL，Definition 指向实体控制器")]
    public void EnsureMenus_Seeds_TopLevel()
    {
        WorkflowHost.EnsureMenus();

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

        var def = top.FindByPath("Definition");
        Assert.Equal(typeof(NewLife.Cube.Areas.Cube.Controllers.WorkflowDefinitionController).FullName, def.FullName);

        WorkflowHost.EnsureMenus();
        AssertLeaf(top, "Definition", "流程定义", "/Cube/WorkflowDefinition");
        AssertLeaf(top, "Done", "已办", "/Cube/Workflow/Done");
        Assert.Equal(1, Menu.Root.Childs.Count(e => e.Name.EqualIgnoreCase("Workflow")));
    }

    [Fact(DisplayName = "EnsureMenus：旧版 Cube/Workflow 提升为一级，叶子保留")]
    public void EnsureMenus_Migrate_From_Cube()
    {
        IMenu cube = Menu.Root.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase("Cube"));
        if (cube == null)
            cube = Menu.Root.Add("Cube", "魔方管理", "NewLife.Cube.Areas.Cube.Controllers", "/Cube");
        var oldParent = cube.FindByPath("Workflow");
        if (oldParent == null || oldParent.ParentID == 0)
        {
            oldParent = cube.Add("Workflow", "流程审批", typeof(NewLife.Cube.Workflow.Controllers.WorkflowController).FullName, null);
            oldParent.Add("Todo", "我的待办", "x.Todo", "/Cube/Workflow/Todo");
        }

        WorkflowHost.EnsureMenus();

        var top = Menu.Root.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase("Workflow"));
        Assert.NotNull(top);
        Assert.Equal(0, top.ParentID);
        Assert.Equal("流程审批", top.DisplayName);
        AssertLeaf(top, "Todo", "我的待办", "/Cube/Workflow/Todo");

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
