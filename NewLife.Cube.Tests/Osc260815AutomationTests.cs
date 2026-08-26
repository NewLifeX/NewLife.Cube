using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        // 未知字段：isNull 为 true（与 matchesViewFilter undefined）；其它 op 恒 false
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "x" }, F("all", C("Ghost", "neq", 1))));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "x" }, F("all", C("Ghost", "isNull"))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "x" }, F("all", C("Ghost", "notNull"))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "x" }, F("all", C("Ghost", "eq", null))));

        // contains 大小写敏感（与前端 includes）
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "ABC" }, F("all", C("Name", "contains", "abc"))));
        Assert.True(AutomationFilter.Match(new Dictionary<String, Object> { ["name"] = "ABC" }, F("all", C("Name", "contains", "AB"))));

        // 非数字 lt/lte 不再恒 true
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = "abc" }, F("all", C("Age", "lt", 10))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = "abc" }, F("all", C("Age", "lte", 10))));
        Assert.False(AutomationFilter.Match(new Dictionary<String, Object> { ["age"] = null }, F("all", C("Age", "lt", 10))));
    }

    [Fact(DisplayName = "found 连续段：空跳过 + 有数据时每条执行整段")]
    public void Executor_FoundSegment_RunsPerRecord()
    {
        foreach (var r in EntityAutomation.FindAll())
        {
            if (!r.Enable) continue;
            r.Enable = false;
            r.Update();
        }

        // 空 found：连续 found 段跳过，不 failed
        EntityPageRegistry.Register(typeof(NotificationRecord), "Cube/NotificationRecord", "Id");
        var emptyRule = InsertRule("Cube/NotificationRecord", "insert");
        emptyRule.Enable = false;
        emptyRule.GraphJson =
            "{\"version\":1,\"nodes\":[" +
            "{\"id\":\"n0\",\"type\":\"start\",\"data\":{}}," +
            "{\"id\":\"n1\",\"type\":\"findRecords\",\"data\":{\"typePath\":\"Cube/NotificationRecord\",\"limit\":10,\"filter\":{\"logic\":\"all\",\"conditions\":[{\"field\":\"Title\",\"op\":\"eq\",\"value\":\"__no_such_seg__\"}]}}}," +
            "{\"id\":\"n2\",\"type\":\"updateRecord\",\"data\":{\"target\":\"found\",\"fields\":[{\"name\":\"Title\",\"value\":\"x\"}]}}," +
            "{\"id\":\"n3\",\"type\":\"end\",\"data\":{}}" +
            "],\"edges\":[{\"id\":\"e0\",\"source\":\"n0\",\"target\":\"n1\"},{\"id\":\"e1\",\"source\":\"n1\",\"target\":\"n2\"},{\"id\":\"e2\",\"source\":\"n2\",\"target\":\"n3\"}]}";
        emptyRule.Update();
        var emptyRun = AutomationRun.Enqueue(emptyRule, "0", "insert");
        AutomationExecutor.Execute(emptyRun);
        emptyRun = AutomationRun.FindById(emptyRun.Id);
        Assert.True(emptyRun.Status == "succeeded", emptyRun.Status + ": " + emptyRun.Error + "\n" + emptyRun.NodeTrace);
        Assert.Contains("found empty skip", emptyRun.NodeTrace);

        // 有 found：用 NotificationRecord（Log 库真实表）插 3 条
        AutomationRuntime.Immediate = false;
        try
        {
            var prefix = "seg-" + Guid.NewGuid().ToString("N")[..8];
            var ids = new List<Int64>();
            for (var i = 0; i < 3; i++)
            {
                var rec = new NotificationRecord
                {
                    Action = "Notify",
                    Channel = "InApp",
                    UserId = 1,
                    Title = prefix + "-" + i,
                    Content = "c",
                    Success = true,
                };
                Assert.True(rec.Insert() > 0, "NotificationRecord.Insert 失败");
                ids.Add(rec.Id);
            }

            var rule = InsertRule("Cube/NotificationRecord", "insert");
            rule.Enable = false;
            rule.GraphJson =
                "{\"version\":1,\"nodes\":[" +
                "{\"id\":\"n0\",\"type\":\"start\",\"data\":{}}," +
                "{\"id\":\"n1\",\"type\":\"findRecords\",\"data\":{\"typePath\":\"Cube/NotificationRecord\",\"limit\":10,\"filter\":{\"logic\":\"all\",\"conditions\":[{\"field\":\"Title\",\"op\":\"contains\",\"value\":\"" + prefix + "\"}]}}}," +
                "{\"id\":\"n2\",\"type\":\"updateRecord\",\"data\":{\"target\":\"found\",\"fields\":[{\"name\":\"Content\",\"value\":\"done\"}]}}," +
                "{\"id\":\"n3\",\"type\":\"updateRecord\",\"data\":{\"target\":\"found\",\"fields\":[{\"name\":\"Title\",\"value\":\"done\"}]}}," +
                "{\"id\":\"n4\",\"type\":\"end\",\"data\":{}}" +
                "],\"edges\":[" +
                "{\"id\":\"e0\",\"source\":\"n0\",\"target\":\"n1\"},{\"id\":\"e1\",\"source\":\"n1\",\"target\":\"n2\"}," +
                "{\"id\":\"e2\",\"source\":\"n2\",\"target\":\"n3\"},{\"id\":\"e3\",\"source\":\"n3\",\"target\":\"n4\"}" +
                "]}";
            rule.Update();
            var run = AutomationRun.Enqueue(rule, ids[0] + "", "insert");
            AutomationExecutor.Execute(run);
            run = AutomationRun.FindById(run.Id);
            Assert.True(run.Status == "succeeded", run.Status + ": " + run.Error + "\n" + run.NodeTrace);
            Assert.Contains("found 3", run.NodeTrace);
            var updates = 0;
            var foundKeys = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in System.Text.Json.Nodes.JsonNode.Parse(run.NodeTrace)!.AsArray())
            {
                if (x?["type"]?.ToString() != "updateRecord") continue;
                Assert.True(x?["ok"]?.GetValue<Boolean>() == true, x?.ToJsonString());
                updates++;
                var fk = x?["foundKey"]?.ToString();
                if (!fk.IsNullOrEmpty()) foundKeys.Add(fk);
            }
            Assert.Equal(6, updates);
            Assert.Equal(3, foundKeys.Count);
        }
        finally
        {
            AutomationRuntime.Immediate = true;
        }
    }

    [Fact(DisplayName = "runAutomation 指向自身保存拒绝；SSRF 拒绝 localhost")]
    public void Graph_SelfRef_And_HttpSsrf()
    {
        var self = AutomationGraph.Parse("""
            {"version":1,"nodes":[
              {"id":"n0","type":"start","data":{}},
              {"id":"n1","type":"runAutomation","data":{"automationId":42}},
              {"id":"n2","type":"end","data":{}}
            ],"edges":[{"id":"e0","source":"n0","target":"n1"},{"id":"e1","source":"n1","target":"n2"}]}
            """);
        Assert.Contains("自身", AutomationGraph.ValidateForSave(self, 42));
        Assert.Null(AutomationGraph.ValidateForSave(self, 7));

        Assert.False(AutomationActions.TryValidatePublicHttpUrl("http://127.0.0.1/x", out _, out var err1));
        Assert.Contains("本机", err1);
        Assert.False(AutomationActions.TryValidatePublicHttpUrl("http://localhost/x", out _, out _));
        Assert.False(AutomationActions.TryValidatePublicHttpUrl("http://192.168.1.1/x", out _, out var err2));
        Assert.Contains("私网", err2);
        Assert.False(AutomationActions.TryValidatePublicHttpUrl("file:///etc/passwd", out _, out _));
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

    [Fact(DisplayName = "循环实体仍跳过；CronJob 用户改名称入队、心跳字段不入队")]
    public void Trigger_CronJob_UserEdit_Enqueues_RuntimeSkip()
    {
        Assert.True(AutomationTrigger.ShouldSkip(typeof(EntityAutomation)));
        Assert.True(AutomationTrigger.ShouldSkip(typeof(NotificationRecord)));
        Assert.True(AutomationTrigger.ShouldSkip(typeof(EntityComment)));
        Assert.False(AutomationTrigger.ShouldSkip(typeof(CronJob)));
        Assert.True(AutomationTrigger.IsRuntimeOnlyUpdate(typeof(CronJob), ["LastTime", "NextTime", "UpdateTime"]));
        Assert.False(AutomationTrigger.IsRuntimeOnlyUpdate(typeof(CronJob), ["Name", "LastTime"]));

        EntityPageRegistry.Register(typeof(CronJob), "Cube/CronJob", "Id");
        var rule = InsertRule("Cube/CronJob", "update");
        try
        {
            var job = new CronJob { Id = 1, Name = "实体自动化流程调度" };
            AutomationTrigger.ResetDebounce();
            AutomationTrigger.OnPersisted(job, DataMethod.Update, ["LastTime", "NextTime", "UpdateTime", "UpdateUserID"]);
            Assert.Empty(AutomationRun.FindAllByAutomationId(rule.Id));

            AutomationTrigger.ResetDebounce();
            AutomationTrigger.OnPersisted(job, DataMethod.Update, ["Name", "UpdateTime", "UpdateUserID"]);
            Assert.NotEmpty(AutomationRun.FindAllByAutomationId(rule.Id));
        }
        finally
        {
            rule.Enable = false;
            rule.Update();
        }
    }

    [Fact(DisplayName = "模板 {{Name}} 与 {{显示名}} 均可替换")]
    public void Executor_ApplyTemplate_DisplayName()
    {
        var e = new CronJob { Name = "备份数据库" };
        var ctx = new AutomationContext { Current = e };
        Assert.Equal("定时作业 备份数据库 修改了名称！", AutomationExecutor.ApplyTemplate("定时作业 {{Name}} 修改了名称！", ctx));
        Assert.Equal("定时作业 备份数据库 修改了名称！", AutomationExecutor.ApplyTemplate("定时作业 {{名称}} 修改了名称！", ctx));
        Assert.Equal("定时作业 备份数据库 修改了名称！", AutomationExecutor.ApplyTemplate("定时作业 {{trigger.Name}} 修改了名称！", ctx));
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
