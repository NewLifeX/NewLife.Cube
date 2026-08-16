using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Cube;
using NewLife.Cube.Automation;
using NewLife.Cube.Entity;
using NewLife.Data;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-260815fa86 实体自动化：Filter / 图 / 深度 / 防抖 / Dirtys / 租户 / Webhook 限流</summary>
public class Osc260815AutomationTests
{
    public Osc260815AutomationTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc260815Cube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc260815Log;Mode=Memory;Cache=Shared", null, "SQLite");
        AutomationTrigger.ResetDebounce();
        AutomationHookRate.Reset();
        AutomationHookRate.Limit = 60;
        AutomationRuntime.Immediate = true;
        AutomationScope.BatchCount = 0;
        AutomationRun.ResetForTests();
    }

    static ViewFilterDto F(String logic, params ViewFilterConditionDto[] conds) =>
        new() { Logic = logic, Conditions = [.. conds] };

    static ViewFilterConditionDto C(String field, String op, Object value = null) =>
        new() { Field = field, Op = op, Value = value };

    [Fact(DisplayName = "Filter 与 searchFilters 同构：all/any/eq/contains/isNull/gt/after")]
    public void Filter_Isomorphic()
    {
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase), F("all")));

        var any = F("any", C("Status", "eq", 1), C("Name", "eq", "x"));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["status"] = 2, ["name"] = "x" }, any));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["status"] = 2, ["name"] = "y" }, any));

        var all = F("all", C("Status", "eq", 1), C("Name", "eq", "x"));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["status"] = 1, ["name"] = "x" }, all));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["status"] = 1, ["name"] = "y" }, all));

        var gte = F("all", C("Age", "gte", 18));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = 20 }, gte));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = 10 }, gte));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = null }, gte));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = "" }, gte));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object>(), gte));

        var after = F("all", C("CreateTime", "after", "2026-01-01"));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["createTime"] = "2026-06-01" }, after));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["createTime"] = "2025-06-01" }, after));

        var contains = F("all", C("Name", "contains", "公司"));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "上海分公司" }, contains));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "行政部" }, contains));

        var isNull = F("all", C("Manager", "isNull"));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["manager"] = null }, isNull));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["manager"] = "" }, isNull));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["manager"] = 5 }, isNull));

        var eqStr = F("all", C("Type", "eq", "1"));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["type"] = 1 }, eqStr));

        // 未知字段恒 false（含 neq/isNull，与前端 matchesViewFilter 对齐）
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "x" }, F("all", C("Ghost", "neq", 1))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "x" }, F("all", C("Ghost", "isNull"))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "x" }, F("all", C("Ghost", "eq", null))));

        // 非数字 lt/lte 不再恒 true
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = "abc" }, F("all", C("Age", "lt", 10))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = "abc" }, F("all", C("Age", "lte", 10))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = null }, F("all", C("Age", "lt", 10))));
    }

    [Fact(DisplayName = "Compile 拒绝环与预留 type")]
    public void Graph_RejectsCycleAndReserved()
    {
        var cycle = AutomationGraph.Parse("""
            {"version":1,"nodes":[{"id":"n0","type":"start","data":{}},{"id":"n1","type":"end","data":{}}],
             "edges":[{"id":"e0","source":"n0","target":"n1"},{"id":"e1","source":"n1","target":"n0"}]}
            """);
        Assert.Equal("图存在环", AutomationGraph.ValidateForSave(cycle));

        var reserved = AutomationGraph.Parse("""
            {"version":1,"nodes":[
              {"id":"n0","type":"start","data":{}},
              {"id":"n1","type":"approval","data":{}},
              {"id":"n2","type":"end","data":{}}
            ],"edges":[{"id":"e0","source":"n0","target":"n1"},{"id":"e1","source":"n1","target":"n2"}]}
            """);
        Assert.Contains("未实现", AutomationGraph.ValidateForSave(reserved));
    }

    [Fact(DisplayName = "Webhook HMAC 与限流 61 次")]
    public void Hook_HmacAndRate()
    {
        var hex = AutomationHookRate.HmacHex("abc", "{\"a\":1}");
        Assert.Equal(64, hex.Length);
        Assert.Equal(hex, AutomationHookRate.HmacHex("abc", "{\"a\":1}"));
        Assert.NotEqual(hex, AutomationHookRate.HmacHex("abc", "{\"a\":2}"));

        AutomationHookRate.Reset();
        for (var i = 0; i < 60; i++)
            Assert.True(AutomationHookRate.TryAcquire("tok"));
        Assert.False(AutomationHookRate.TryAcquire("tok"));
        Assert.Null(EntityAutomation.FindByHookToken("no-such-token-xxxxxxxx"));
    }

    [Fact(DisplayName = "Debounce 3 秒内同规则同记录不重复入队")]
    public void Trigger_Debounce()
    {
        var rule = InsertRule("OscAutoItem", "insert");
        var e = new OscAutoItem { Name = "a" };
        AutomationTrigger.ResetDebounce();
        AutomationTrigger.OnPersisted(e, DataMethod.Insert, ["Name"]);
        AutomationTrigger.OnPersisted(e, DataMethod.Insert, ["Name"]);
        var n = AutomationRun.FindAllByAutomationId(rule.Id).Count;
        Assert.Equal(1, n);
    }

    [Fact(DisplayName = "Depth>=3 丢弃并写 failed")]
    public void Trigger_MaxDepth()
    {
        var rule = InsertRule("OscAutoItem", "insert");
        var e = new OscAutoItem { Name = "d" };
        AutomationTrigger.ResetDebounce();
        using (AutomationScope.Enter(2))
        {
            AutomationTrigger.OnPersisted(e, DataMethod.Insert, ["Name"]);
        }
        var run = AutomationRun.FindAllByAutomationId(rule.Id)[0];
        Assert.Equal("failed", run.Status);
        Assert.Contains("深度", run.Error);
    }

    [Fact(DisplayName = "fieldChange 仅在 watch 字段出现于 Dirtys 时触发")]
    public void Trigger_FieldChange_Dirtys()
    {
        var rule = InsertRule("OscAutoItem", "fieldChange", """{"watchFields":["Name"]}""");
        var e = new OscAutoItem { Name = "n" };
        AutomationTrigger.ResetDebounce();
        AutomationTrigger.OnPersisted(e, DataMethod.Update, ["Age"]);
        Assert.Empty(AutomationRun.FindAllByAutomationId(rule.Id));
        AutomationTrigger.OnPersisted(e, DataMethod.Update, ["Name"]);
        Assert.NotEmpty(AutomationRun.FindAllByAutomationId(rule.Id));
    }

    [Fact(DisplayName = "租户不匹配不入队")]
    public void Trigger_TenantMismatch()
    {
        var old = CubeSetting.Current.EnableTenant;
        var oldCtx = TenantContext.Current;
        try
        {
            CubeSetting.Current.EnableTenant = true;
            TenantContext.Current = new TenantContext { TenantId = 2 };
            var rule = InsertRule("OscAutoItem", "insert");
            rule.TenantId = 1;
            rule.Update();
            AutomationTrigger.ResetDebounce();
            AutomationTrigger.OnPersisted(new OscAutoItem { Name = "t" }, DataMethod.Insert, ["Name"]);
            Assert.Empty(AutomationRun.FindAllByAutomationId(rule.Id));
        }
        finally
        {
            CubeSetting.Current.EnableTenant = old;
            TenantContext.Current = oldCtx;
        }
    }

    [Fact(DisplayName = "脏 approval 节点执行 failed")]
    public void Executor_ApprovalFailClosed()
    {
        var rule = InsertRule("OscAutoItem", "insert");
        rule.GraphJson = """
            {"version":1,"nodes":[
              {"id":"n0","type":"start","data":{"triggerKind":"insert"}},
              {"id":"n1","type":"approval","data":{}},
              {"id":"n2","type":"end","data":{}}
            ],"edges":[{"id":"e0","source":"n0","target":"n1"},{"id":"e1","source":"n1","target":"n2"}]}
            """;
        rule.Update();
        var run = AutomationRun.Enqueue(rule, "0", "insert");
        AutomationExecutor.Execute(run);
        run = AutomationRun.FindById(run.Id);
        Assert.Equal("failed", run.Status);
        Assert.Contains("未实现", run.Error);
    }

    [Fact(DisplayName = "TypePath 前导斜杠与触发路径归一后仍能入队")]
    public void Trigger_TypePath_SlashNormalized()
    {
        var rule = InsertRule("/Admin/OscAutoItem", "insert");
        Assert.Equal("/Admin/OscAutoItem", rule.TypePath);
        // 触发侧归一为 Admin/OscAutoItem，须能命中库内 /Admin/OscAutoItem
        Assert.Contains(EntityAutomation.FindEnabled("Admin/OscAutoItem"), e => e.Id == rule.Id);
        Assert.Contains(EntityAutomation.FindEnabled("/Admin/OscAutoItem"), e => e.Id == rule.Id);

        // 模拟 OnPersisted：ResolveTypePath 归一后入队
        var resolved = AutomationPaths.NormalizeTypePath("/Admin/OscAutoItem");
        Assert.Equal("Admin/OscAutoItem", resolved);
        var e = new OscAutoItem { Name = "slash" };
        AutomationTrigger.ResetDebounce();
        // 直接按归一路径走 OnPersisted 内部等价路径：FindEnabled 已覆盖；再验证整链
        EntityPageRegistry.Register(typeof(OscAutoItem), "/Admin/OscAutoItem", "Id");
        try
        {
            AutomationTrigger.OnPersisted(e, DataMethod.Insert, ["Name"]);
            Assert.NotEmpty(AutomationRun.FindAllByAutomationId(rule.Id));
        }
        finally
        {
            // 还原为仅类型名，避免污染同文件其它用例的 TypePath=OscAutoItem
            EntityPageRegistry.Register(typeof(OscAutoItem), "OscAutoItem", "Id");
        }
    }

    [Fact(DisplayName = "AutomationRun 落库：入队可查、waiting 到期续跑、终态持久化")]
    public void Run_Persisted_QueueAndResume()
    {
        var rule = InsertRule("OscAutoItem", "insert");
        var run = AutomationRun.Enqueue(rule, "7", "insert", 0, DateTime.MinValue, "queued");
        Assert.True(run.Id > 0);
        var got = AutomationRun.FindById(run.Id);
        Assert.NotNull(got);
        Assert.Equal("queued", got.Status);
        Assert.Equal(rule.Id, got.AutomationId);
        Assert.Equal("OscAutoItem", got.TypePath);

        // waiting 到期续跑（重启后仍可由 Tick 拾取）
        var w = AutomationRun.Enqueue(rule, "8", "insert", 0, DateTime.Now.AddMinutes(-1), "waiting");
        var due = AutomationRun.FindDueWaiting(DateTime.Now);
        Assert.Contains(due, e => e.Id == w.Id);

        // once 查重路径
        var by = AutomationRun.FindAllByTypePathAndRecordKey("OscAutoItem", "7");
        Assert.Contains(by, e => e.Id == run.Id);

        // 终态更新落库
        got.Status = "succeeded";
        got.Update();
        Assert.Equal("succeeded", AutomationRun.FindById(run.Id).Status);
    }

    [Fact(DisplayName = "流程日志 Remark JSON 往返（写入系统 Log 不改表结构）")]
    public void FlowLog_Remark_Roundtrip()
    {
        var rule = new EntityAutomation { Id = 9, Name = "通知", TypePath = "Admin/User" };
        var run = new AutomationRun
        {
            Id = 99,
            AutomationId = 9,
            TriggerKind = "insert",
            Status = "succeeded",
            RecordKey = "5",
            NodeTrace = """[{"type":"start","ok":true},{"type":"notify","ok":true},{"type":"end","ok":true}]""",
        };
        var remark = AutomationFlowLog.BuildRemark(rule, run);
        Assert.Contains("\"automationId\":9", remark);
        Assert.Contains("\"detail\":", remark);
        Assert.True(AutomationFlowLog.TryParseRemark(remark, out var row));
        Assert.Equal(9, row.AutomationId);
        Assert.Equal("通知", row.Name);
        Assert.Equal("insert", row.TriggerKind);
        Assert.Equal("succeeded", row.Status);
        Assert.Equal(99, row.RunId);
        Assert.Equal("5", row.RecordKey);
        Assert.Contains("发送通知", row.Nodes);
        Assert.False(String.IsNullOrEmpty(row.Detail));
        Assert.Contains("添加新记录", row.Detail);
    }

    static EntityAutomation InsertRule(String typePath, String kind, String cfg = "{}")
    {
        var e = new EntityAutomation
        {
            TypePath = typePath,
            Name = kind + "-" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
            Priority = 100,
            TriggerKind = kind,
            TriggerConfig = cfg,
            GraphJson = """{"version":1,"nodes":[{"id":"n0","type":"start","data":{}},{"id":"n1","type":"end","data":{}}],"edges":[{"id":"e0","source":"n0","target":"n1"}]}""",
            Version = 1,
        };
        e.Insert();
        return e;
    }
}

[DisplayName("OSC260815 自动化测试实体")]
[BindTable("OscAutoItem", ConnName = "Cube", DbType = DatabaseType.None)]
public class OscAutoItem : Entity<OscAutoItem>
{
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    public Int32 Id { get; set; }

    [DisplayName("名称")]
    public String Name { get; set; }

    [DisplayName("年龄")]
    public Int32 Age { get; set; }
}
