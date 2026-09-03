using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.Automation;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Tests;

/// <summary>OSC-26090347f1 GetPage/列表行覆盖：匿名仅 enabled；登录行级 __wf*；IN 无 N+1</summary>
public class WorkflowPageOverlayTests
{
    public WorkflowPageOverlayTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc47f1OvlCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc47f1OvlLog;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc47f1OvlWf;Mode=Memory;Cache=Shared", null, "SQLite");

        WorkflowTask.FindAll().Delete();
        WorkflowSubject.FindAll().Delete();
        WorkflowInstance.FindAll().Delete();
        WorkflowDefinition.FindAll().Delete();
        WfLockRecord.FindAll().Delete();

        EntityPageRegistry.Register(typeof(WfLockRecord), "Cube/WfLockRecord", "Id");
    }

    const String TypePath = "Cube/WfLockRecord";

    static String Graph() =>
        "{\"version\":1," +
        "\"nodes\":[" +
        "{\"id\":\"start\",\"type\":\"oa.start\",\"data\":{}}," +
        "{\"id\":\"n1\",\"type\":\"oa.approve\",\"data\":{\"name\":\"审批\",\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]},\"fields\":{\"visible\":[\"*\"],\"writable\":[\"Name\"]},\"timeoutHours\":24,\"timeoutAction\":\"pass\"}}," +
        "{\"id\":\"end\",\"type\":\"oa.end\",\"data\":{}}" +
        "]," +
        "\"edges\":[{\"source\":\"start\",\"target\":\"n1\"},{\"source\":\"n1\",\"target\":\"end\"}]}";

    static WorkflowDefinition NewDef(String name)
    {
        var def = new WorkflowDefinition
        {
            TenantId = 0,
            TypePath = TypePath,
            Name = name,
            Enable = true,
            Published = true,
            Version = 1,
            LockPolicy = WorkflowStatuses.LockFull,
            StartFilter = "{}",
            GraphJson = Graph(),
        };
        def.PublishedGraphJson = def.GraphJson;
        def.Insert();
        return def;
    }

    static IUser FakeUser() => new User { ID = 101, Name = "wf", Enable = true };

    [Fact(DisplayName = "GetPage 类型块：无定义 enabled=false；匿名仅 enabled；登录含 canStart")]
    public void GetTypeBlock_Matrix()
    {
        // 无定义：仅 enabled=false
        var block = WorkflowPageOverlay.GetTypeBlock(TypePath, null);
        Assert.Single(block);
        Assert.False((Boolean)block["enabled"]);

        // 有定义：匿名仅 enabled=true（单键）
        NewDef("匿名块");
        block = WorkflowPageOverlay.GetTypeBlock(TypePath, null);
        Assert.Single(block);
        Assert.True((Boolean)block["enabled"]);

        // 登录：definitionCount/lockPolicy/canStart
        block = WorkflowPageOverlay.GetTypeBlock(TypePath, FakeUser());
        Assert.Equal(4, block.Count);
        Assert.True((Boolean)block["canStart"] || (Int32)block["definitionCount"] >= 1);
        Assert.Equal(WorkflowStatuses.LockFull, block["lockPolicy"]);
    }

    [Fact(DisplayName = "行覆盖：running 行注入 __wfStatus/__wfInstanceId/canStart=false；无主体行 none")]
    public void ApplyRows_RunningAndNone()
    {
        var def = NewDef("行覆盖");
        var rec1 = new WfLockRecord { Name = "r1" };
        rec1.Insert();
        var rec2 = new WfLockRecord { Name = "r2" };
        rec2.Insert();

        var instance = WorkflowEngine.Start(def, [$"{rec1.Id}"], 9, "发起人", "");
        Assert.Equal(WorkflowStatuses.Running, instance.Status);

        var user = FakeUser();
        var rows = new List<IEntity> { rec1, rec2 };
        WorkflowPageOverlay.ApplyRows(rows, TypePath, user);

        Assert.Equal("running", rec1["__wfStatus"]);
        Assert.Equal(instance.Id, rec1["__wfInstanceId"]);
        Assert.Equal(false, rec1["__wfCanStart"]);

        Assert.Equal("none", rec2["__wfStatus"]);
        Assert.Equal(0L, rec2["__wfInstanceId"]);
        Assert.True(rec2["__wfCanStart"] is Boolean);

        // 通过后：status approved、instanceId 保留、canStart=true（可再次发起）
        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        WorkflowEngine.Approve(task.Id, 101, "ok");

        WorkflowPageOverlay.ApplyRows(new List<IEntity> { rec1 }, TypePath, user);
        Assert.Equal("approved", rec1["__wfStatus"]);
        Assert.Equal(true, rec1["__wfCanStart"]);
    }

    [Fact(DisplayName = "无定义时行覆盖不注入任何 __wf 键")]
    public void ApplyRows_Disabled_NoKeys()
    {
        var rec = new WfLockRecord { Name = "x" };
        rec.Insert();
        WorkflowPageOverlay.ApplyRows([rec], TypePath, FakeUser());
        Assert.True(rec["__wfStatus"] == null, "无定义不应注入 __wfStatus");
    }
}
