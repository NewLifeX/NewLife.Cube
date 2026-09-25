using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Controllers;
using NewLife.Cube.Workflow.Entity;
using NewLife.Model;
using XCode;
using XCode.DataAccessLayer;
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
    [Fact(DisplayName = "EnsureMenus：播种一级流程审批及五叶子，设计器不单独占菜单")]
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
        AssertLeaf(top, "Todo", "我的待办", "/Cube/Workflow/Todo");
        AssertLeaf(top, "Started", "我发起的", "/Cube/Workflow/Started");
        AssertLeaf(top, "Done", "已办", "/Cube/Workflow/Done");
        AssertLeaf(top, "Efficiency", "效率", "/Cube/Workflow/Efficiency");

        var def = top.FindByPath("Definition");
        Assert.Equal(typeof(NewLife.Cube.Areas.Cube.Controllers.WorkflowDefinitionController).FullName, def.FullName);
        var designer = top.Childs.FirstOrDefault(e => e.Name.EqualIgnoreCase("Designer"));
        if (designer != null) Assert.False(designer.Visible);

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

    [Fact(DisplayName = "Efficiency：未登录 401；无权限 403；系统管理员参数校验 400；合法请求 0")]
    public void Efficiency_Auth_And_Validation()
    {
        WorkflowHost.EnsureMenus();
        DAL.AddConnStr("Workflow", "Data Source=Osc201aEffWf;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureWorkflowTables();

        var c = new WorkflowController();

        // 未登录 → 401
        using (UserContext(null))
        {
            Assert.Contains("\"code\":401", ContentOf(c.Efficiency()));
        }

        // 无角色普通用户 → 403（效率菜单存在但无 Detail 权；系统角色外默认拒绝）
        var plain = new User { Name = "plain-" + Guid.NewGuid().ToString("N")[..6], Enable = true };
        using (UserContext(plain))
        {
            Assert.Contains("\"code\":403", ContentOf(c.Efficiency()));
        }

        // 系统管理员（Roles 缓存注入）→ 参数校验 400；合法请求 0
        var admin = new MockUser { Name = "adm-" + Guid.NewGuid().ToString("N")[..6], Enable = true };
        admin.MockRoles([new Role { Name = "sys", Enable = true, IsSystem = true }]);
        using (UserContext(admin))
        {
            Assert.Contains("\"code\":400", ContentOf(c.Efficiency(groupBy: "ghost")));
            Assert.Contains("\"code\":400", ContentOf(c.Efficiency(days: 7, year: "2024")));
            Assert.Contains("\"code\":400", ContentOf(c.Efficiency(groupBy: "node")));
            Assert.Contains("\"code\":0", ContentOf(c.Efficiency()));
        }
    }

    [Fact(DisplayName = "BatchReject：办理任务跳过并返回条数，审批任务正常驳回")]
    public void BatchReject_SkipsHandleTask()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc201aBrCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc201aBrWf;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureWorkflowTables();
        WorkflowTask.FindAll().Delete();
        WorkflowSubject.FindAll().Delete();
        WorkflowInstance.FindAll().Delete();
        WorkflowDefinition.FindAll().Delete();
        EntityPageRegistry.Register(typeof(NotificationRecord), "Cube/NotificationRecord", "Id");

        // 实例1：审批节点任务（可驳回）
        var d1 = NewDef(ChainGraph("oa.approve"));
        var rec1 = NewRec("br1");
        var i1 = WorkflowEngine.Start(d1, [rec1.ToString()], 101, "甲", "");
        var t1 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == i1.Id).First();

        // 实例2：办理节点任务（批量驳回应跳过）
        var d2 = NewDef(ChainGraph("oa.handle"));
        var rec2 = NewRec("br2");
        var i2 = WorkflowEngine.Start(d2, [rec2.ToString()], 101, "甲", "");
        var t2 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == i2.Id).First();

        // 以候选人 101 身份批量驳回（驳回走候选人校验，非管理员通道）
        var actor = new MockUser { ID = 101, Name = "u101", Enable = true };
        using (UserContext(actor))
        {
            var c = new WorkflowController();
            var content = ContentOf(c.BatchReject(new WorkflowController.BatchModel { Ids = [t1.Id, t2.Id], Comment = "批量" }));
            Assert.Contains("\"code\":0", content);
            Assert.Contains("\"skipped\":true", content);
        }

        Assert.Equal(WorkflowStatuses.Rejected, WorkflowInstance.FindById(i1.Id).Status);
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(i2.Id).Status);
    }

    static String ChainGraph(String nodeType) =>
        "{\"version\":1,\"nodes\":[" +
        "{\"id\":\"s\",\"type\":\"oa.start\",\"data\":{}}," +
        "{\"id\":\"n1\",\"type\":\"" + nodeType + "\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}}}," +
        "{\"id\":\"e\",\"type\":\"oa.end\",\"data\":{}}]," +
        "\"edges\":[{\"source\":\"s\",\"target\":\"n1\"},{\"source\":\"n1\",\"target\":\"e\"}]}";

    static WorkflowDefinition NewDef(String graph)
    {
        var def = new WorkflowDefinition
        {
            TenantId = 0,
            TypePath = "Cube/NotificationRecord",
            Name = "br" + Guid.NewGuid().ToString("N")[..6],
            Enable = true,
            Published = true,
            Version = 1,
            LockPolicy = WorkflowStatuses.LockFull,
            StartFilter = "{}",
            GraphJson = graph,
        };
        def.PublishedGraphJson = graph;
        def.Insert();
        return def;
    }

    static Int64 NewRec(String title)
    {
        var rec = new NotificationRecord { Action = "Notify", Channel = "InApp", UserId = 1, Title = title, Content = "c", Success = true };
        rec.Insert();
        return rec.Id;
    }

    static String ContentOf(Object rs) => (rs as ContentResult)?.Content ?? rs?.ToString() ?? "";

    static IDisposable UserContext(IManageUser user) => new UserScope(user);

    /// <summary>当前登录用户桩。ManageProvider.User 静态属性经此返回指定用户</summary>
    sealed class StubProvider : ManageProvider
    {
        public IManageUser? Mock { get; set; }

        public override IManageUser? GetCurrent(IServiceProvider? context = null) => Mock;

        public override void SetCurrent(IManageUser? user, IServiceProvider? context = null) => Mock = user;
    }

    /// <summary>可注入角色缓存的测试用户（Extends 为 protected）</summary>
    sealed class MockUser : User
    {
        public void MockRoles(IRole[] roles) => Extends.Get(nameof(Roles), _ => roles);
    }

    sealed class UserScope : IDisposable
    {
        readonly IManageProvider _old;

        public UserScope(IManageUser user)
        {
            _old = ManageProvider.Provider;
            ManageProvider.Provider = new StubProvider { Mock = user };
        }

        public void Dispose() => ManageProvider.Provider = _old;
    }
}
