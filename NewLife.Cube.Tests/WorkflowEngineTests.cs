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

/// <summary>OSC-26090347f1 状态机矩阵：or/and/sequence、驳回、排他、加签、回退、超时</summary>
public class WorkflowEngineTests
{
    public WorkflowEngineTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc47f1Cube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc47f1Log;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc47f1Wf;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureWorkflowTables();

        // 清空上轮数据，保证用例独立（意见表为只写日志型，禁止删除，跳过）
        WorkflowTask.FindAll().Delete();
        WorkflowSubject.FindAll().Delete();
        WorkflowInstance.FindAll().Delete();
        WorkflowDefinition.FindAll().Delete();

        // typePath 解析依赖页面注册表（引擎 ResolveFactory 反查）
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

    static WorkflowDefinition NewDefinition(String mode, String users, Boolean startFilterEmpty = true)
    {
        var to = users;
        var def = new WorkflowDefinition
        {
            TenantId = 0,
            TypePath = "Cube/NotificationRecord",
            Name = "测试" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
            Published = true,
            Version = 1,
            LockPolicy = WorkflowStatuses.LockFull,
            StartFilter = "{}",
            GraphJson = BuildGraph(mode, to),
        };
        def.PublishedGraphJson = def.GraphJson;
        def.Insert();
        return def;
    }

    static String BuildGraph(String mode, String users)
    {
        var seq = "";
        if (mode == "sequence")
        {
            seq = "\"mode\":\"sequence\",";
        }
        else if (mode == "and")
        {
            seq = "\"mode\":\"and\",";
        }
        else
        {
            seq = "\"mode\":\"or\",";
        }
        return "{\"version\":1," +
            "\"nodes\":[" +
            "{\"id\":\"start\",\"type\":\"oa.start\",\"data\":{}}," +
            "{\"id\":\"n1\",\"type\":\"oa.approve\",\"data\":{\"name\":\"审批\", " + seq + " \"to\":{\"kind\":\"users\",\"users\":[" + users + "]},\"fields\":{\"visible\":[\"*\"],\"writable\":[]},\"timeoutHours\":24,\"timeoutAction\":\"pass\"}}," +
            "{\"id\":\"end\",\"type\":\"oa.end\",\"data\":{}}" +
            "]," +
            "\"edges\":[" +
            "{\"source\":\"start\",\"target\":\"n1\"}," +
            "{\"source\":\"n1\",\"target\":\"end\"}" +
            "]}";
    }

    static IList<WorkflowTask> Tasks(Int64 instanceId) => WorkflowTask.FindAll(WorkflowTask._.InstanceId == instanceId).OrderBy(e => e.Id).ToList();

    [Fact(DisplayName = "提交生成 1 实例 + N 主体 + 首节点任务；或签一人同意即 Approved")]
    public void Start_And_Or_Approve()
    {
        var def = NewDefinition("or", "101,102");
        var id1 = InsertRecord("r1");
        var id2 = InsertRecord("r2");

        var instance = WorkflowEngine.Start(def, [$"{id1}", $"{id2}"], 9, "发起人", "请审批");
        Assert.Equal(WorkflowStatuses.Running, instance.Status);
        Assert.Equal(2, WorkflowSubject.FindAll(WorkflowSubject._.InstanceId == instance.Id).Count);
        var tasks = Tasks(instance.Id);
        var task = Assert.Single(tasks); // or 单任务
        Assert.Equal("Pending", task.Status);
        Assert.Contains("101", task.CandidateJson);

        WorkflowEngine.Approve(task.Id, 101, "同意");
        instance = WorkflowInstance.FindById(instance.Id);
        Assert.Equal(WorkflowStatuses.Approved, instance.Status);
        // 主体解锁：不再有在途
        Assert.Null(WorkflowSubject.FindRunning("Cube/NotificationRecord", $"{id1}"));
    }

    [Fact(DisplayName = "会签 and：需全部同意才通过；中途驳回整单 Rejected")]
    public void And_All_Required()
    {
        var def = NewDefinition("and", "101,102");
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var tasks = Tasks(instance.Id);
        Assert.Equal(2, tasks.Count);

        WorkflowEngine.Approve(tasks[0].Id, 101, "同意1");
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);

        WorkflowEngine.Reject(tasks[1].Id, 102, "不同意");
        Assert.Equal(WorkflowStatuses.Rejected, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "依次签 sequence：先仅第一人可见，逐个同意后通过")]
    public void Sequence_OneByOne()
    {
        var def = NewDefinition("sequence", "101,102,103");
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var tasks = Tasks(instance.Id);
        Assert.Equal(3, tasks.Count);
        Assert.True(tasks[0].Visible);
        Assert.False(tasks[1].Visible);
        Assert.False(tasks[2].Visible);

        WorkflowEngine.Approve(tasks[0].Id, 101, "ok");
        var t2 = WorkflowTask.FindById(tasks[1].Id);
        Assert.True(t2.Visible);
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);

        WorkflowEngine.Approve(tasks[1].Id, 102, "ok");
        Assert.True(WorkflowTask.FindById(tasks[2].Id).Visible);

        WorkflowEngine.Approve(tasks[2].Id, 103, "ok");
        Assert.Equal(WorkflowStatuses.Approved, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "非候选人操作 403；同一记录在途二次提交 409")]
    public void NonCandidate_And_Exclusive()
    {
        var def = NewDefinition("or", "101");
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");
        var task = Assert.Single(Tasks(instance.Id));

        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Approve(task.Id, 999, "x"));
        Assert.Equal(403, ex.Code);

        var ex2 = Assert.Throws<WorkflowException>(() => WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", ""));
        Assert.Equal(409, ex2.Code);
    }

    [Fact(DisplayName = "发起人撤回（无人同意）→ Withdrawn；先批后不可撤")]
    public void Withdraw_Rule()
    {
        var def = NewDefinition("or", "101,102");
        var id = InsertRecord("r1");
        var instance = WorkflowEngine.Start(def, [$"{id}"], 9, "发起人", "");

        WorkflowEngine.Withdraw(instance.Id, 9, "暂不走了");
        Assert.Equal(WorkflowStatuses.Withdrawn, WorkflowInstance.FindById(instance.Id).Status);

        // 新实例有人同意后，发起人不能撤
        var def2 = NewDefinition("or", "101,102");
        var id2 = InsertRecord("r2");
        var instance2 = WorkflowEngine.Start(def2, [$"{id2}"], 9, "发起人", "");
        WorkflowEngine.Approve(Tasks(instance2.Id)[0].Id, 101, "同意");
        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Withdraw(instance2.Id, 9, "想撤"));
        Assert.Equal(409, ex.Code);
    }

    [Fact(DisplayName = "常用语：空回落内置三条；保存后读回")]
    public void Phrases_Roundtrip()
    {
        var p = XCode.Membership.Parameter.FindByUserIDAndCategoryAndName(0, WorkflowHelper.PhraseCategory, "tenant:0");
        if (p != null) p.Delete();
        var list = WorkflowHelper.PhraseList(0);
        Assert.Equal(3, list.Count);

        WorkflowHelper.SavePhrases(0, ["同意", "加急", "同意"]);
        list = WorkflowHelper.PhraseList(0);
        Assert.Equal(2, list.Count);
        Assert.Contains("加急", list.Select(e => (e as dynamic).text.ToString()).ToList());
    }
}