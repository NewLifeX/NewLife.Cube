using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.DataAccessLayer;
using Xunit;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Tests;

/// <summary>OSC-26090347f1 引擎矩阵补测（design §8/§11）：会签 quorum、驳回整单、回退清下游、加签前恢复、XOR 无默认发布失败、空候选人</summary>
public class WorkflowMatrixTests
{
    public WorkflowMatrixTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc47f1MxCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc47f1MxLog;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc47f1MxWf;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureInstanceSummaryColumn();

        // 意见表为只写日志型，禁止删除，跳过；其余清理保证独立
        WorkflowTask.FindAll().Delete();
        WorkflowSubject.FindAll().Delete();
        WorkflowInstance.FindAll().Delete();
        WorkflowDefinition.FindAll().Delete();
        NotificationRecord.FindAll().Delete();

        EntityPageRegistry.Register(typeof(NotificationRecord), "Cube/NotificationRecord", "Id");
    }

    static Int64 InsertRecord(String title)
    {
        var rec = new NotificationRecord
        {
            Action = "Notify",
            Channel = "InApp",
            UserId = 1,
            Title = title,
            Content = "c",
            Success = true,
        };
        rec.Insert();
        return rec.Id;
    }

    static WorkflowDefinition NewDefinition(String graphJson)
    {
        var def = new WorkflowDefinition
        {
            TenantId = 0,
            TypePath = "Cube/NotificationRecord",
            Name = "矩阵" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
            Published = true,
            Version = 1,
            LockPolicy = WorkflowStatuses.LockFull,
            StartFilter = "{}",
            GraphJson = graphJson,
        };
        def.PublishedGraphJson = def.GraphJson;
        def.Insert();
        return def;
    }

    /// <summary>start → n1（mode/quorum/users）→ [n2] → end 链</summary>
    static String ChainGraph(String n1, String n2 = null)
    {
        var nodes = "[{\"id\":\"start\",\"type\":\"oa.start\",\"data\":{}}," +
            "{\"id\":\"n1\",\"type\":\"oa.approve\",\"data\":{" + n1 + "}},";
        var edges = "[{\"source\":\"start\",\"target\":\"n1\"},";
        if (n2 != null)
        {
            nodes += "{\"id\":\"n2\",\"type\":\"oa.approve\",\"data\":{" + n2 + "}},";
            edges += "{\"source\":\"n1\",\"target\":\"n2\"},{\"source\":\"n2\",\"target\":\"end\"}";
        }
        else
        {
            edges += "{\"source\":\"n1\",\"target\":\"end\"}";
        }
        nodes += "{\"id\":\"end\",\"type\":\"oa.end\",\"data\":{}}]";
        edges += "]";
        return "{\"version\":1,\"nodes\":" + nodes + ",\"edges\":" + edges + "}";
    }

    static String ToJson(String kind, String users, String extra = "")
    {
        var mode = $"\"mode\":\"{kind}\"";
        var to = $"\"to\":{{\"kind\":\"users\",\"users\":[{users}]}}";
        return mode + "," + to + (extra.IsNullOrEmpty() ? "" : "," + extra);
    }

    static IList<WorkflowTask> Tasks(Int64 instanceId) =>
        WorkflowTask.FindAll(WorkflowTask._.InstanceId == instanceId).OrderBy(e => e.Id).ToList();

    [Fact(DisplayName = "会签 and + quorum=0.6：3 人需 2 票通过（§8.1）")]
    public void AndQuorum_Partial()
    {
        var def = NewDefinition(ChainGraph(ToJson("and", "101,102,103", "\"quorum\":0.6")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var tasks = Tasks(instance.Id);
        Assert.Equal(3, tasks.Count);

        WorkflowEngine.Approve(tasks[0].Id, 101, "1/2");
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
        WorkflowEngine.Approve(tasks[1].Id, 102, "2/2");
        Assert.Equal(WorkflowStatuses.Approved, WorkflowInstance.FindById(instance.Id).Status);
        // 第三人任务被取消（节点通过）
        Assert.Contains(WorkflowTask.FindById(tasks[2].Id).Status, new[] { "Cancelled", "Done" });
    }

    [Fact(DisplayName = "或签 or 一人驳回 → 整单 Rejected，任务 Cancelled（§8.1）")]
    public void OrReject_AllCancelled()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101,102")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        // 或签为单任务共享候选人（AssigneeId=0）
        var task = Assert.Single(Tasks(instance.Id));

        WorkflowEngine.Reject(task.Id, 102, "不同意");
        Assert.Equal(WorkflowStatuses.Rejected, WorkflowInstance.FindById(instance.Id).Status);
        Assert.Equal("Rejected", WorkflowTask.FindById(task.Id).Status);
    }

    [Fact(DisplayName = "依次签 sequence 首人驳回 → 整单 Rejected（§8.1）")]
    public void SequenceReject_AllCancelled()
    {
        var def = NewDefinition(ChainGraph(ToJson("sequence", "101,102,103")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var first = Tasks(instance.Id)[0];
        WorkflowEngine.Reject(first.Id, 101, "否");
        Assert.Equal(WorkflowStatuses.Rejected, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "回退：n1 已办后到 n2；n2 回退 n1 → 下游取消并重开 n1（§8.2）")]
    public void Rollback_ClearDownstreamAndRegenerate()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101"), ToJson("or", "102")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var n1Task = Tasks(instance.Id).Single(t => t.NodeId == "n1");
        WorkflowEngine.Approve(n1Task.Id, 101, "过");
        var n2Task = Tasks(instance.Id).Single(t => t.NodeId == "n2");
        Assert.Equal("Pending", n2Task.Status);

        WorkflowEngine.Rollback(n2Task.Id, 102, "n1", "请重填");
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
        Assert.Equal("Cancelled", WorkflowTask.FindById(n2Task.Id).Status);
        // 目标节点重新生成（新任务可操作）
        var reopened = Tasks(instance.Id).Where(t => t.NodeId == "n1" && t.Status is "Pending" or "Active").ToList();
        Assert.NotEmpty(reopened);
        // 重开后 101 再同意可推进
        WorkflowEngine.Approve(reopened[0].Id, 101, "改好了");
        var next = Tasks(instance.Id).SingleOrDefault(t => t.NodeId == "n2" && t.Status is "Pending" or "Active");
        Assert.NotNull(next);
    }

    [Fact(DisplayName = "前加签：原任务挂起、加签先行；加签通过后恢复原任务（§4.3）")]
    public void AddSign_Before_Resume()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101", "\"allowAddSign\":true")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Tasks(instance.Id).Single();
        var to = new System.Text.Json.Nodes.JsonObject
        {
            ["kind"] = "users",
            ["users"] = new System.Text.Json.Nodes.JsonArray(202),
        };

        WorkflowEngine.AddSign(task.Id, 101, true, to, "先让主管签");
        var all = Tasks(instance.Id);
        var sign = all.Single(t => t.NodeId.Contains("#addsign#"));
        Assert.True(sign.Visible);
        Assert.Contains("202", sign.CandidateJson);
        Assert.False(WorkflowTask.FindById(task.Id).Visible); // 原任务挂起

        WorkflowEngine.Approve(sign.Id, 202, "同意");
        var resumed = WorkflowTask.FindById(task.Id);
        Assert.True(resumed.Visible);
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "后加签：原任务同意后激活加签人，加签完成再进下游（§4.3）")]
    public void AddSign_After_ActivateAndContinue()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101", "\"allowAddSign\":true"), ToJson("or", "102")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Tasks(instance.Id).Single(t => t.NodeId == "n1");
        var to = new System.Text.Json.Nodes.JsonObject
        {
            ["kind"] = "users",
            ["users"] = new System.Text.Json.Nodes.JsonArray(202),
        };

        WorkflowEngine.AddSign(task.Id, 101, false, to, "还要主管确认");
        var sign = Tasks(instance.Id).Single(t => t.NodeId.Contains("#after#"));
        Assert.False(sign.Visible); // 后加签先不可见，原任务完成后才轮到

        WorkflowEngine.Approve(task.Id, 101, "过");
        var fresh = WorkflowTask.FindById(sign.Id);
        Assert.True(fresh.Visible); // 原任务 Done → 激活后加签
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
        Assert.DoesNotContain(Tasks(instance.Id), t => t.NodeId == "n2" && t.Status == "Pending"); // 未提前推进下游

        WorkflowEngine.Approve(fresh.Id, 202, "确认");
        // 后加签完成 → 常规收尾推进到 n2
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
        Assert.Contains(Tasks(instance.Id), t => t.NodeId == "n2" && t.Status == "Pending");
        var n2 = Tasks(instance.Id).Single(t => t.NodeId == "n2");
        WorkflowEngine.Approve(n2.Id, 102, "ok");
        Assert.Equal(WorkflowStatuses.Approved, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "多前加签：全部加签完成才恢复原任务，避免残留加签人复活已办任务（§4.3）")]
    public void MultiAddSign_Before_WaitAll()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101", "\"allowAddSign\":true")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Tasks(instance.Id).Single();
        var to = new System.Text.Json.Nodes.JsonObject
        {
            ["kind"] = "users",
            ["users"] = new System.Text.Json.Nodes.JsonArray(202, 203),
        };

        WorkflowEngine.AddSign(task.Id, 101, true, to, "双人复核");
        var signs = Tasks(instance.Id).Where(t => t.NodeId.Contains("#addsign#")).ToList();
        Assert.Equal(2, signs.Count);
        Assert.False(WorkflowTask.FindById(task.Id).Visible);

        WorkflowEngine.Approve(signs[0].Id, 202, "1/2");
        Assert.False(WorkflowTask.FindById(task.Id).Visible); // 等全部，未提前恢复

        WorkflowEngine.Approve(signs[1].Id, 203, "2/2");
        var resumed = WorkflowTask.FindById(task.Id);
        Assert.True(resumed.Visible);
        Assert.Equal(WorkflowStatuses.Active, resumed.Status);

        WorkflowEngine.Approve(resumed.Id, 101, "最终同意");
        Assert.Equal(WorkflowStatuses.Approved, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "回退权限：节点 allowRollback=false → 403（§7 权限矩阵）")]
    public void Rollback_NoAllowRollback_403()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101"), ToJson("or", "102", "\"allowRollback\":false")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var n1Task = Tasks(instance.Id).Single(t => t.NodeId == "n1");
        WorkflowEngine.Approve(n1Task.Id, 101, "过");
        var n2Task = Tasks(instance.Id).Single(t => t.NodeId == "n2");

        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Rollback(n2Task.Id, 102, "n1", "退回"));
        Assert.Equal(403, ex.Code);
        // 未发生回退：n2 任务仍在办
        Assert.Equal("Pending", WorkflowTask.FindById(n2Task.Id).Status);
    }

    [Fact(DisplayName = "超时仅作用于可见任务：依次签逐轮放行后各自到期 pass（§4.3/§8.3）")]
    public void TimeoutSequence_OnlyVisibleAdvance()
    {
        var n1 = "\"mode\":\"sequence\",\"to\":{\"kind\":\"users\",\"users\":[101,102,103]},\"timeoutHours\":1,\"timeoutAction\":\"pass\"";
        var def = NewDefinition(ChainGraph(n1));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var tasks = Tasks(instance.Id);
        Assert.Equal(3, tasks.Count);
        Assert.True(WorkflowTask.FindById(tasks[0].Id).Visible);
        Assert.False(WorkflowTask.FindById(tasks[1].Id).Visible);

        // 全部置为到期；未轮到（!Visible）的任务不应被 TimeoutTick 处理
        var past = DateTime.Now.AddMinutes(-1);
        foreach (var t in tasks)
        {
            var e = WorkflowTask.FindById(t.Id);
            e.DueTime = past;
            e.Update();
        }
        Assert.True(WorkflowTask.FindById(tasks[0].Id).DueTime <= DateTime.Now, "DueTime 未落库");
        WorkflowEngine.TimeoutTick();
        Assert.Equal("Done", WorkflowTask.FindById(tasks[0].Id).Status);
        Assert.Equal("Pending", WorkflowTask.FindById(tasks[1].Id).Status); // 未轮到不被超时
        Assert.True(WorkflowTask.FindById(tasks[1].Id).Visible); // 101 超时通过后放行

        // 逐轮放行并到期 → 最终办结
        WorkflowEngine.TimeoutTick();
        Assert.Equal("Done", WorkflowTask.FindById(tasks[1].Id).Status);
        WorkflowEngine.TimeoutTick();
        Assert.Equal(WorkflowStatuses.Approved, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "XOR 缺 defaultTarget：发布校验失败（§5/§11）")]
    public void Xor_NoDefault_PublishFail()
    {
        var json = "{\"version\":1," +
            "\"nodes\":[" +
            "{\"id\":\"start\",\"type\":\"oa.start\",\"data\":{}}," +
            "{\"id\":\"x\",\"type\":\"oa.xor\",\"data\":{\"cases\":[],\"defaultTarget\":\"\"}}," +
            "{\"id\":\"end\",\"type\":\"oa.end\",\"data\":{}}" +
            "]," +
            "\"edges\":[" +
            "{\"source\":\"start\",\"target\":\"x\"}," +
            "{\"source\":\"x\",\"target\":\"end\"}" +
            "]}";
        var graph = WorkflowGraph.Parse(json);
        Assert.NotNull(graph);
        var errors = graph.Validate();
        Assert.Contains(errors, e => e.Contains("defaultTarget"));
    }

    [Fact(DisplayName = "空候选人：不自动通过；实例保持 Running 并记 error 意见（§4.1/§12）")]
    public void EmptyCandidate_NoAutoPass()
    {
        // 图校验要求 to 非空，故用不存在的角色（展开为空）触发运行时空候选人
        var n1 = "\"mode\":\"or\",\"to\":{\"kind\":\"roles\",\"roles\":[999]},\"fields\":{\"visible\":[\"*\"],\"writable\":[]}";
        var def = NewDefinition(ChainGraph(n1));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");

        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
        Assert.Empty(Tasks(instance.Id)); // 无待办任务
        var comments = WorkflowComment.FindAll(WorkflowComment._.InstanceId == instance.Id).ToList();
        Assert.Contains(comments, c => c.Action == "error");
    }

    [Fact(DisplayName = "G-04 或签未认领：CandidateJson 双方均进待办/角标；认领后仅认领人")]
    public void OrUnclaimed_TodoVisibleForCandidates()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101,102")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Assert.Single(Tasks(instance.Id));
        Assert.Equal(0, task.AssigneeId);

        Assert.Contains(WorkflowTask.FindTodoByUser(101), t => t.Id == task.Id);
        Assert.Contains(WorkflowTask.FindTodoByUser(102), t => t.Id == task.Id);
        Assert.Equal(1, WorkflowTask.CountTodoByUser(101));
        Assert.Equal(1, WorkflowTask.CountTodoByUser(102));
        Assert.Equal(0, WorkflowTask.CountTodoByUser(999));

        WorkflowEngine.Claim(task.Id, 101);
        Assert.Contains(WorkflowTask.FindTodoByUser(101), t => t.Id == task.Id);
        Assert.DoesNotContain(WorkflowTask.FindTodoByUser(102), t => t.Id == task.Id);
        Assert.Equal(1, WorkflowTask.CountTodoByUser(101));
        Assert.Equal(0, WorkflowTask.CountTodoByUser(102));
    }

    [Fact(DisplayName = "G-11 Claim：候选人认领后 AssigneeId/Active；非候选人 403")]
    public void Claim_CandidateOk_NonCandidate403()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101,102")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Assert.Single(Tasks(instance.Id));

        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Claim(task.Id, 999));
        Assert.Equal(403, ex.Code);

        WorkflowEngine.Claim(task.Id, 102);
        var got = WorkflowTask.FindById(task.Id);
        Assert.Equal(102, got.AssigneeId);
        Assert.Equal(WorkflowStatuses.Active, got.Status);
        Assert.True(got.ClaimTime > DateTime.MinValue);
    }

    [Fact(DisplayName = "G-11 Transfer：原任务 Transferred，目标得新 Pending 任务")]
    public void Transfer_CreatesTargetTask()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Assert.Single(Tasks(instance.Id));

        var to = System.Text.Json.Nodes.JsonNode.Parse("{\"kind\":\"users\",\"users\":[202]}") as System.Text.Json.Nodes.JsonObject;
        WorkflowEngine.Transfer(task.Id, 101, to, "请代办");

        Assert.Equal(WorkflowStatuses.Transferred, WorkflowTask.FindById(task.Id).Status);
        var nt = Tasks(instance.Id).Single(t => t.Id != task.Id);
        Assert.Equal(202, nt.AssigneeId);
        Assert.Equal(WorkflowStatuses.Pending, nt.Status);
        Assert.True(nt.Visible);
    }

    [Fact(DisplayName = "G-11 Cc：只写意见+通知，不增任务")]
    public void Cc_NotifyOnly_NoExtraTask()
    {
        var def = NewDefinition(ChainGraph(ToJson("or", "101")));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Assert.Single(Tasks(instance.Id));
        var before = Tasks(instance.Id).Count;

        var to = System.Text.Json.Nodes.JsonNode.Parse("{\"kind\":\"users\",\"users\":[303]}") as System.Text.Json.Nodes.JsonObject;
        WorkflowEngine.Cc(task.Id, 101, to, "请知悉");

        Assert.Equal(before, Tasks(instance.Id).Count);
        Assert.Contains(WorkflowComment.FindAll(WorkflowComment._.InstanceId == instance.Id), c => c.Action == "cc");
        Assert.Contains(NotificationRecord.FindAll(), n => n.UserId == 303 && n.Action == "Workflow");
    }

    [Fact(DisplayName = "G-11 超时 reject：到期 Visible 任务整单 Rejected")]
    public void TimeoutReject_FinishesRejected()
    {
        var n1 = "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]},\"timeoutHours\":1,\"timeoutAction\":\"reject\"";
        var def = NewDefinition(ChainGraph(n1));
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Assert.Single(Tasks(instance.Id));
        var e = WorkflowTask.FindById(task.Id);
        e.DueTime = DateTime.Now.AddMinutes(-1);
        e.Update();

        WorkflowEngine.TimeoutTick();
        Assert.Equal(WorkflowStatuses.Rejected, WorkflowInstance.FindById(instance.Id).Status);
    }
}
