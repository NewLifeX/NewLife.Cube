using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Controllers;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Tests;

/// <summary>OSC-260922201a 选人、空人、办理节点</summary>
public class WorkflowOsc260922Tests
{
    public WorkflowOsc260922Tests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc201aCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc201aLog;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc201aWf;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Membership", "Data Source=Osc201aMbr;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureWorkflowTables();
        WorkflowTask.FindAll().Delete();
        WorkflowSubject.FindAll().Delete();
        WorkflowInstance.FindAll().Delete();
        WorkflowDefinition.FindAll().Delete();
        EntityPageRegistry.Register(typeof(NotificationRecord), "Cube/NotificationRecord", "Id");
    }

    [Fact]
    public void OldGraph_WithoutKind_StillPublishes()
    {
        var json = Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"users\":[101]}");
        var errors = WorkflowGraph.Parse(json).Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void Level2_AndHandleSequence_FailPublish()
    {
        var level = Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"kind\":\"manager\",\"level\":2}");
        Assert.Contains(WorkflowGraph.Parse(level).Validate(), e => e.Contains("level"));

        var seq = Graph("oa.handle", "\"mode\":\"sequence\",\"to\":{\"kind\":\"users\",\"users\":[101]}");
        Assert.Contains(WorkflowGraph.Parse(seq).Validate(), e => e.Contains("依次"));
    }

    [Fact]
    public void EmptyPolicyPass_ApprovesInstance()
    {
        var def = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"emptyPolicy\":\"pass\",\"to\":{\"kind\":\"manager\"}"));
        var id = InsertRecord("空人");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 1, "甲", "发起");
        Assert.Equal(WorkflowStatuses.Approved, instance.Status);
        var comment = WorkflowComment.FindAll(WorkflowComment._.InstanceId == instance.Id).FirstOrDefault(e => (e.Content ?? "").Contains("自动通过"));
        Assert.NotNull(comment);
    }

    [Fact]
    public void HandleNode_Reject_Returns400()
    {
        var def = NewDef(Graph("oa.handle", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}"));
        var id = InsertRecord("办理");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 1, "甲", "发起");
        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Reject(task.Id, 101, "不行"));
        Assert.Equal(400, ex.Code);
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "同一人已同意：下一审批节点自动跳过；办理节点不跳过")]
    public void SamePerson_SkipApprove_NotHandle()
    {
        // 审批链 n1(101) → n2(101)：101 同意 n1 后 n2 自动跳过 → 实例通过
        var graph = "{\"version\":1,\"nodes\":[" +
            "{\"id\":\"s\",\"type\":\"oa.start\",\"data\":{}}," +
            "{\"id\":\"n1\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}}}," +
            "{\"id\":\"n2\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}}}," +
            "{\"id\":\"e\",\"type\":\"oa.end\",\"data\":{}}]," +
            "\"edges\":[{\"source\":\"s\",\"target\":\"n1\"},{\"source\":\"n1\",\"target\":\"n2\"},{\"source\":\"n2\",\"target\":\"e\"}]}";
        var def = NewDef(graph);
        var id = InsertRecord("跳过");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 101, "甲", "发起");

        var n1Task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == "n1").First();
        WorkflowEngine.Approve(n1Task.Id, 101, "同意");

        var n2Task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == "n2").First();
        Assert.Equal(WorkflowStatuses.Done, n2Task.Status);
        Assert.Contains(WorkflowComment.FindAll(WorkflowComment._.InstanceId == instance.Id), c => c.Content == "自动跳过");
        Assert.Equal(WorkflowStatuses.Approved, WorkflowInstance.FindById(instance.Id).Status);

        // 办理链 n1(101) → h2(101)：办理节点不参与跳过，仍产生待办
        var handleGraph = graph.Replace("\"id\":\"n2\",\"type\":\"oa.approve\"", "\"id\":\"n2\",\"type\":\"oa.handle\"");
        var def2 = NewDef(handleGraph);
        var id2 = InsertRecord("办理不跳");
        var instance2 = WorkflowEngine.Start(def2, [id2.ToString()], 101, "甲", "发起");
        var n1b = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance2.Id & WorkflowTask._.NodeId == "n1").First();
        WorkflowEngine.Approve(n1b.Id, 101, "同意");

        var h2 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance2.Id & WorkflowTask._.NodeId == "n2").First();
        Assert.Equal(WorkflowStatuses.Pending, h2.Status);
        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance2.Id).Status);
    }

    [Fact(DisplayName = "XOR 多分支：从左到右命中第一条；全不中走默认分支")]
    public void Xor_MultiCase_FirstHit_ElseDefault()
    {
        var graph = "{\"version\":1,\"nodes\":[" +
            "{\"id\":\"s\",\"type\":\"oa.start\",\"data\":{}}," +
            "{\"id\":\"x\",\"type\":\"oa.xor\",\"data\":{" +
            "\"cases\":[" +
            "{\"filter\":{\"logic\":\"all\",\"conditions\":[{\"field\":\"Title\",\"op\":\"eq\",\"value\":\"ZZZ\"}]},\"target\":\"nb\"}," +
            "{\"filter\":{\"logic\":\"all\",\"conditions\":[{\"field\":\"Title\",\"op\":\"eq\",\"value\":\"AAA\"}]},\"target\":\"nc\"}]," +
            "\"defaultTarget\":\"nd\"}}," +
            "{\"id\":\"nb\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[201]}}}," +
            "{\"id\":\"nc\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[202]}}}," +
            "{\"id\":\"nd\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[203]}}}," +
            "{\"id\":\"e\",\"type\":\"oa.end\",\"data\":{}}]," +
            "\"edges\":[{\"source\":\"s\",\"target\":\"x\"}," +
            "{\"source\":\"x\",\"target\":\"nb\"},{\"source\":\"x\",\"target\":\"nc\"},{\"source\":\"x\",\"target\":\"nd\"}," +
            "{\"source\":\"nb\",\"target\":\"e\"},{\"source\":\"nc\",\"target\":\"e\"},{\"source\":\"nd\",\"target\":\"e\"}]}";
        var def = NewDef(graph);

        // 第一条 case 不中、第二条命中 → 只走 nc
        var id = InsertRecord("AAA");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 9, "甲", "发起");
        var task = Assert.Single(WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id));
        Assert.Equal("nc", task.NodeId);
        Assert.Contains("202", task.CandidateJson ?? "");

        // 都不中 → 默认分支 nd
        var id2 = InsertRecord("BBB");
        var instance2 = WorkflowEngine.Start(def, [id2.ToString()], 9, "甲", "发起");
        var task2 = Assert.Single(WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance2.Id));
        Assert.Equal("nd", task2.NodeId);
    }

    [Fact(DisplayName = "旧图（无 kind）空候选人：停住并通知发起人，不自动通过")]
    public void OldGraph_EmptyCandidate_NotifiesStarter()
    {
        // 图校验要求 to 非空，用不存在的角色触发运行时空候选人；无 kind 的旧图写法
        var def = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"roles\":[999]}"));
        var id = InsertRecord("空候选");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 9, "甲", "发起");

        Assert.Equal(WorkflowStatuses.Running, WorkflowInstance.FindById(instance.Id).Status);
        Assert.Empty(WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id));
        Assert.Contains(WorkflowComment.FindAll(WorkflowComment._.InstanceId == instance.Id), c => c.Action == "error");
        Assert.Contains(NotificationRecord.FindAll(), n => n.UserId == 9 && n.Action == "Workflow" && (n.Title ?? "").Contains("无候选人"));
    }

    [Fact(DisplayName = "效率聚合：办理/自动跳过/排除节点不进样本；未结束只计超长等待")]
    public void Efficiency_SamplesAndExclusions()
    {
        var now = DateTime.Now;
        var winFrom = now.AddDays(-30);

        // D1：两个正常审批节点 → 2 条样本
        var d1 = NewDef(TwoNodeGraph("\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[102]}"));
        var r1 = InsertRecord("eff1");
        var i1 = WorkflowEngine.Start(d1, [r1.ToString()], 9, "甲", "");
        WorkflowEngine.Approve(WorkflowTask.FindAll(WorkflowTask._.InstanceId == i1.Id & WorkflowTask._.NodeId == "n1").First().Id, 101, "ok");
        WorkflowEngine.Approve(WorkflowTask.FindAll(WorkflowTask._.InstanceId == i1.Id & WorkflowTask._.NodeId == "n2").First().Id, 102, "ok");

        // D2：节点 excludeStats → 无样本，行不出现
        var d2 = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"excludeStats\":true,\"to\":{\"kind\":\"users\",\"users\":[101]}"));
        var r2 = InsertRecord("eff2");
        var i2 = WorkflowEngine.Start(d2, [r2.ToString()], 9, "甲", "");
        WorkflowEngine.Approve(WorkflowTask.FindAll(WorkflowTask._.InstanceId == i2.Id).First().Id, 101, "ok");

        // D3：同一人自动跳过 → 只计 n1 一条
        var d3 = NewDef(TwoNodeGraph("\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}"));
        var r3 = InsertRecord("eff3");
        var i3 = WorkflowEngine.Start(d3, [r3.ToString()], 9, "甲", "");
        WorkflowEngine.Approve(WorkflowTask.FindAll(WorkflowTask._.InstanceId == i3.Id & WorkflowTask._.NodeId == "n1").First().Id, 101, "ok");

        // D4：办理节点不进统计（n1 计 1 条，h2 不计）
        var d4 = NewDef(TwoNodeGraph("\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}", "oa.handle"));
        var r4 = InsertRecord("eff4");
        var i4 = WorkflowEngine.Start(d4, [r4.ToString()], 9, "甲", "");
        WorkflowEngine.Approve(WorkflowTask.FindAll(WorkflowTask._.InstanceId == i4.Id & WorkflowTask._.NodeId == "n1").First().Id, 101, "ok");

        // D5：未结束节点等待 50 小时 → 只进超长等待，不进平均
        var d5 = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}"));
        var r5 = InsertRecord("eff5");
        var i5 = WorkflowEngine.Start(d5, [r5.ToString()], 9, "甲", "");
        var t5 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == i5.Id).First();
        t5.CreateTime = DateTime.Now.AddHours(-50);
        t5.Update();

        var result = WorkflowController.BuildEfficiency("process", winFrom, DateTime.MaxValue, DateTime.Now, null, null, null, null, null);

        Assert.False(result.Truncated);
        Assert.Equal(1, result.OverdueOpen);
        Assert.NotNull(result.AvgHours);
        Assert.Equal(0.6, result.CompletionRate);
        Assert.Contains(result.Rows, e => e.Key == d1.Id.ToString() && e.Count == 2);
        Assert.DoesNotContain(result.Rows, e => e.Key == d2.Id.ToString());
        Assert.Contains(result.Rows, e => e.Key == d3.Id.ToString() && e.Count == 1);
        Assert.Contains(result.Rows, e => e.Key == d4.Id.ToString() && e.Count == 1);
        Assert.DoesNotContain(result.Rows, e => e.Key == d5.Id.ToString());

        // slow：节点维度只返回该定义该节点的最慢条（不等待）
        var nodeResult = WorkflowController.BuildEfficiency("node", winFrom, DateTime.MaxValue, DateTime.Now, d1.Id.ToString(), null, null, "n1", null);
        Assert.Single(nodeResult.Slow);
        Assert.False(nodeResult.Slow[0].Waiting);
        Assert.Equal("n1", nodeResult.Slow[0].NodeId);

        var other = WorkflowController.BuildEfficiency("process", winFrom, DateTime.MaxValue, DateTime.Now, null, null, "999", null, null);
        Assert.Equal(0, other.OverdueOpen);
        Assert.Null(other.AvgHours);
        Assert.Null(other.CompletionRate);
    }

    [Fact(DisplayName = "IsHandleNodeTask：办理节点任务 true，审批节点任务 false")]
    public void IsHandleNodeTask_Detects()
    {
        var def = NewDef(TwoNodeGraph("\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}", "oa.handle"));
        var id = InsertRecord("判定");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 101, "甲", "发起");
        var t1 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == "n1").First();
        Assert.False(WorkflowEngine.IsHandleNodeTask(t1));

        WorkflowEngine.Approve(t1.Id, 101, "同意");
        var h2 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == "n2").First();
        Assert.True(WorkflowEngine.IsHandleNodeTask(h2));
    }

    /// <summary>两节点链 s→n1→n2→e，n2Type 可换 oa.handle</summary>
    static String TwoNodeGraph(String n1, String n2, String n2Type = "oa.approve") =>
        "{\"version\":1,\"nodes\":[" +
        "{\"id\":\"s\",\"type\":\"oa.start\",\"data\":{}}," +
        "{\"id\":\"n1\",\"type\":\"oa.approve\",\"data\":{" + n1 + "}}," +
        "{\"id\":\"n2\",\"type\":\"" + n2Type + "\",\"data\":{" + n2 + "}}," +
        "{\"id\":\"e\",\"type\":\"oa.end\",\"data\":{}}]," +
        "\"edges\":[{\"source\":\"s\",\"target\":\"n1\"},{\"source\":\"n1\",\"target\":\"n2\"},{\"source\":\"n2\",\"target\":\"e\"}]}";

    /// <summary>建部门+负责人+员工（员工可禁用），用于 manager/禁用改派用例</summary>
    static (Department Dept, User Boss, User Staff) NewDeptWithManager(Boolean staffEnabled = true)
    {
        _ = User.Meta.Count;
        _ = Department.Meta.Count;

        var dept = new Department { Name = "d" + Guid.NewGuid().ToString("N")[..6], Enable = true };
        dept.Insert();
        var boss = new User { Name = "b" + Guid.NewGuid().ToString("N")[..6], Enable = true };
        boss.Insert();
        dept.ManagerId = boss.ID;
        dept.Update();
        var staff = new User { Name = "s" + Guid.NewGuid().ToString("N")[..6], Enable = staffEnabled, DepartmentID = dept.ID };
        staff.Insert();
        return (dept, boss, staff);
    }

    [Fact(DisplayName = "manager 选人：发起人所在部门负责人得任务（AC-01）")]
    public void Manager_Resolves_To_DepartmentManager()
    {
        var (_, boss, staff) = NewDeptWithManager();
        var def = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"kind\":\"manager\"}"));
        var id = InsertRecord("负责人");
        var instance = WorkflowEngine.Start(def, [id.ToString()], staff.ID, staff.Name, "发起");

        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        Assert.Equal(boss.ID, task.AssigneeId);
        Assert.Equal(WorkflowStatuses.Pending, task.Status);
    }

    [Fact(DisplayName = "emptyPolicy=manager：节点无人时落到发起人部门负责人（AC-01）")]
    public void EmptyPolicy_Manager_FallsBack_To_DepartmentManager()
    {
        var (_, boss, staff) = NewDeptWithManager();
        // field 字段值为 0 → 无候选人 → emptyPolicy=manager 兜底（空数组会在发布校验被拦）
        var def = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"emptyPolicy\":\"manager\",\"to\":{\"kind\":\"field\",\"field\":\"UserId\"}"));
        var rec = new NotificationRecord { Action = "Notify", Channel = "InApp", UserId = 0, Title = "空人负责人", Content = "c", Success = true };
        rec.Insert();
        var instance = WorkflowEngine.Start(def, [rec.Id.ToString()], staff.ID, staff.Name, "发起");

        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        Assert.Equal(boss.ID, task.AssigneeId);
    }

    [Fact(DisplayName = "禁用用户改派：任务落到其部门负责人（AC-10）")]
    public void Disabled_User_Reroutes_To_DepartmentManager()
    {
        var (_, boss, staff) = NewDeptWithManager(staffEnabled: false);
        var def = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[" + staff.ID + "]}"));
        var id = InsertRecord("禁用改派");
        var instance = WorkflowEngine.Start(def, [id.ToString()], staff.ID, staff.Name, "发起");

        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        Assert.Equal(boss.ID, task.AssigneeId);
    }

    [Fact(DisplayName = "starterPick：缺 picks 抛 400；带 picks 任务落自选人（AC-05）")]
    public void StarterPick_Uses_Picks_Or_400()
    {
        var (_, boss, staff) = NewDeptWithManager();
        var def = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"kind\":\"starterPick\",\"scope\":\"all\"}"));
        var id = InsertRecord("自选");

        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Start(def, [id.ToString()], staff.ID, staff.Name, "发起"));
        Assert.Equal(400, ex.Code);

        var instance = WorkflowEngine.Start(def, [id.ToString()], staff.ID, staff.Name, "发起", null, null,
            new Dictionary<String, List<Int32>> { ["n1"] = [boss.ID] });
        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        Assert.Equal(boss.ID, task.AssigneeId);
    }

    [Fact(DisplayName = "field 选人：取实体字段值作为办理人")]
    public void Field_Resolves_From_Entity()
    {
        var def = NewDef(Graph("oa.approve", "\"mode\":\"or\",\"to\":{\"kind\":\"field\",\"field\":\"UserId\"}"));
        var rec = new NotificationRecord { Action = "Notify", Channel = "InApp", UserId = 4242, Title = "字段选人", Content = "c", Success = true };
        rec.Insert();

        var instance = WorkflowEngine.Start(def, [rec.Id.ToString()], 1, "甲", "发起");
        var task = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        Assert.Equal(4242, task.AssigneeId);
    }

    [Fact(DisplayName = "加签任务不可绕过办理节点驳回：NodeId 归一化后 400")]
    public void Handle_AddSign_Task_Reject_Returns400()
    {
        var def = NewDef(Graph("oa.handle", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}"));
        var id = InsertRecord("加签驳回");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 101, "甲", "发起");

        var h = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).First();
        var extra = new WorkflowTask
        {
            InstanceId = instance.Id,
            NodeId = h.NodeId + "#addsign#99",
            Mode = "or",
            AssigneeId = 101,
            CandidateJson = "[101]",
            Status = WorkflowStatuses.Pending,
            Visible = true,
            CreateTime = DateTime.Now,
        };
        extra.Insert();

        var ex = Assert.Throws<WorkflowException>(() => WorkflowEngine.Reject(extra.Id, 101, "不行"));
        Assert.Equal(400, ex.Code);
    }

    [Fact(DisplayName = "加签同意参与同人跳过：下游同候选人自动跳过（NodeId 归一化）")]
    public void AddSign_Done_Counts_For_SamePerson_Skip()
    {
        // n1(101) → n2(102)；n1 上 102 有已完成加签任务 → 101 同意 n1 后 n2 自动跳过、实例通过
        var def = NewDef(TwoNodeGraph("\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}", "\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[102]}"));
        var id = InsertRecord("加签跳过");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 9, "甲", "发起");

        var n1 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == "n1").First();
        var extra = new WorkflowTask
        {
            InstanceId = instance.Id,
            NodeId = "n1#addsign#99",
            Mode = "or",
            AssigneeId = 102,
            CandidateJson = "[102]",
            Status = WorkflowStatuses.Done,
            Visible = true,
            CreateTime = DateTime.Now.AddMinutes(-5),
        };
        extra.Insert();

        WorkflowEngine.Approve(n1.Id, 101, "同意");

        var n2 = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == "n2").First();
        Assert.Equal(WorkflowStatuses.Done, n2.Status);
        Assert.Contains(WorkflowComment.FindAll(WorkflowComment._.InstanceId == instance.Id), c => c.Content == "自动跳过");
        Assert.Equal(WorkflowStatuses.Approved, WorkflowInstance.FindById(instance.Id).Status);
    }

    [Fact(DisplayName = "并行分支：两条都进入，都同意后才汇合；一条驳回取消另一条")]
    public void Parallel_JoinAndReject()
    {
        var both = ParallelGraph("{\"target\":\"a\",\"filter\":{}}", "{\"target\":\"b\",\"filter\":{}}", "e", "e");
        var def = NewDef(both);
        var id = InsertRecord("并行都进");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 1, "甲", "发起");
        var tasks = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).Where(t => t.Visible).ToList();
        Assert.Equal(2, tasks.Count);
        Assert.Equal(WorkflowStatuses.Running, instance.Status);

        WorkflowEngine.Approve(tasks.First(t => t.AssigneeId == 101).Id, 101, "过");
        instance = WorkflowInstance.FindById(instance.Id);
        Assert.Equal(WorkflowStatuses.Running, instance.Status);

        WorkflowEngine.Approve(tasks.First(t => t.AssigneeId == 102).Id, 102, "过");
        instance = WorkflowInstance.FindById(instance.Id);
        Assert.Equal(WorkflowStatuses.Approved, instance.Status);

        var def2 = NewDef(both);
        var id2 = InsertRecord("并行驳回");
        var inst2 = WorkflowEngine.Start(def2, [id2.ToString()], 1, "甲", "发起");
        var open = WorkflowTask.FindAll(WorkflowTask._.InstanceId == inst2.Id).Where(t => t.Visible).ToList();
        WorkflowEngine.Reject(open.First(t => t.AssigneeId == 101).Id, 101, "不行");
        var sibling = WorkflowTask.FindAll(WorkflowTask._.InstanceId == inst2.Id).First(t => t.AssigneeId == 102);
        Assert.Equal(WorkflowStatuses.Cancelled, sibling.Status);
        Assert.Equal(WorkflowStatuses.Rejected, WorkflowInstance.FindById(inst2.Id).Status);
    }

    [Fact(DisplayName = "并行分支：只进命中的一条；都不命中走其他情况")]
    public void Parallel_MatchOne_OrDefault()
    {
        var one = ParallelGraph(
            "{\"target\":\"a\",\"filter\":{\"logic\":\"all\",\"conditions\":[{\"field\":\"Title\",\"op\":\"eq\",\"value\":\"不会出现\"}]}}",
            "{\"target\":\"b\",\"filter\":{}}",
            "e",
            "e");
        var def = NewDef(one);
        var id = InsertRecord("只进一条");
        var instance = WorkflowEngine.Start(def, [id.ToString()], 1, "甲", "发起");
        var visible = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id).Where(t => t.Visible).ToList();
        Assert.Single(visible);
        Assert.Equal(102, visible[0].AssigneeId);

        var none = "{\"version\":1,\"nodes\":[" +
            "{\"id\":\"s\",\"type\":\"oa.start\",\"data\":{}}," +
            "{\"id\":\"p\",\"type\":\"oa.parallel\",\"data\":{\"cases\":[" +
            "{\"target\":\"a\",\"filter\":{\"logic\":\"all\",\"conditions\":[{\"field\":\"Title\",\"op\":\"eq\",\"value\":\"不会出现\"}]}}," +
            "{\"target\":\"b\",\"filter\":{\"logic\":\"all\",\"conditions\":[{\"field\":\"Title\",\"op\":\"eq\",\"value\":\"也不会\"}]}}" +
            "],\"defaultTarget\":\"d\",\"joinTarget\":\"e\"}}," +
            "{\"id\":\"a\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}}}," +
            "{\"id\":\"b\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[102]}}}," +
            "{\"id\":\"d\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[104]}}}," +
            "{\"id\":\"e\",\"type\":\"oa.end\",\"data\":{}}" +
            "],\"edges\":[{\"source\":\"s\",\"target\":\"p\"},{\"source\":\"p\",\"target\":\"a\"},{\"source\":\"p\",\"target\":\"b\"},{\"source\":\"p\",\"target\":\"d\"}," +
            "{\"source\":\"a\",\"target\":\"e\"},{\"source\":\"b\",\"target\":\"e\"},{\"source\":\"d\",\"target\":\"e\"}]}";
        var def3 = NewDef(none);
        var id3 = InsertRecord("走其他");
        var inst3 = WorkflowEngine.Start(def3, [id3.ToString()], 1, "甲", "发起");
        var got = WorkflowTask.FindAll(WorkflowTask._.InstanceId == inst3.Id).Where(t => t.Visible).ToList();
        Assert.Single(got);
        Assert.Equal(104, got[0].AssigneeId);
    }

    [Fact(DisplayName = "并行分支：缺 joinTarget 发布校验失败")]
    public void Parallel_MissingJoin_FailValidation()
    {
        var graph = WorkflowGraph.Parse(ParallelGraph("{\"target\":\"a\",\"filter\":{}}", "{\"target\":\"b\",\"filter\":{}}", "e", ""));
        Assert.NotNull(graph);
        var errors = graph.Validate();
        Assert.Contains(errors, e => e.Contains("joinTarget"));
    }

    static String ParallelGraph(String caseA, String caseB, String defaultTarget, String join) =>
        "{\"version\":1,\"nodes\":[" +
        "{\"id\":\"s\",\"type\":\"oa.start\",\"data\":{}}," +
        "{\"id\":\"p\",\"type\":\"oa.parallel\",\"data\":{\"cases\":[" + caseA + "," + caseB + "],\"defaultTarget\":\"" + defaultTarget + "\",\"joinTarget\":\"" + join + "\"}}," +
        "{\"id\":\"a\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[101]}}}," +
        "{\"id\":\"b\",\"type\":\"oa.approve\",\"data\":{\"mode\":\"or\",\"to\":{\"kind\":\"users\",\"users\":[102]}}}," +
        "{\"id\":\"e\",\"type\":\"oa.end\",\"data\":{}}" +
        "],\"edges\":[{\"source\":\"s\",\"target\":\"p\"},{\"source\":\"p\",\"target\":\"a\"},{\"source\":\"p\",\"target\":\"b\"}," +
        "{\"source\":\"a\",\"target\":\"e\"},{\"source\":\"b\",\"target\":\"e\"}]}";

    static WorkflowDefinition NewDef(String graph)
    {
        var def = new WorkflowDefinition
        {
            TenantId = 0,
            TypePath = "Cube/NotificationRecord",
            Name = "t" + Guid.NewGuid().ToString("N")[..6],
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

    static String Graph(String type, String data) =>
        "{\"nodes\":[" +
        "{\"id\":\"s\",\"type\":\"oa.start\",\"data\":{}}," +
        "{\"id\":\"n1\",\"type\":\"" + type + "\",\"data\":{" + data + "}}," +
        "{\"id\":\"e\",\"type\":\"oa.end\",\"data\":{}}]," +
        "\"edges\":[{\"source\":\"s\",\"target\":\"n1\"},{\"source\":\"n1\",\"target\":\"e\"}]}";

    static Int64 InsertRecord(String title)
    {
        var rec = new NotificationRecord { Action = "Notify", Channel = "InApp", UserId = 1, Title = title, Content = "c", Success = true };
        rec.Insert();
        return rec.Id;
    }
}
