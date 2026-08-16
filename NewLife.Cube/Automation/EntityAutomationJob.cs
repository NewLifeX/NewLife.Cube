using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Cube.Jobs;
using NewLife.Log;
using XCode;

namespace NewLife.Cube.Automation;

/// <summary>每分钟：定时触发、日期到达、延时续跑</summary>
[DisplayName("实体自动化流程调度")]
[Description("扫描 schedule/dateArrive 与 waiting 续跑")]
[CronJob("EntityAutomationTick", "0 * * * * ?", Enable = true)]
public class EntityAutomationJob : CubeJobBase
{
    /// <inheritdoc />
    public override Task<String> Execute(String argument)
    {
        var n1 = ResumeWaiting();
        var n2 = FireSchedules();
        var n3 = FireDateArrive();
        return Task.FromResult($"waiting={n1} schedule={n2} dateArrive={n3}");
    }

    static Int32 ResumeWaiting()
    {
        var list = AutomationRun.FindDueWaiting(DateTime.Now, 50);
        foreach (var run in list)
        {
            run.Status = "queued";
            run.Update();
            AutomationWorker.Post(run.Id);
        }
        return list.Count;
    }

    static Int32 FireSchedules()
    {
        var n = 0;
        var all = EntityAutomation.FindAll();
        foreach (var rule in all)
        {
            if (!rule.Enable || !rule.TriggerKind.EqualIgnoreCase("schedule")) continue;
            var cron = ReadJsonString(rule.TriggerConfig, "cron");
            if (cron.IsNullOrEmpty()) continue;
            try
            {
                var c = new NewLife.Threading.Cron();
                if (!c.Parse(cron)) continue;
                var next = c.GetNext(DateTime.Now.AddMinutes(-1));
                if (next > DateTime.Now) continue;
            }
            catch { continue; }
            var run = AutomationRun.Enqueue(rule, null, "schedule");
            AutomationWorker.Post(run.Id);
            n++;
        }
        return n;
    }

    static Int32 FireDateArrive()
    {
        var n = 0;
        var all = EntityAutomation.FindAll();
        foreach (var rule in all)
        {
            if (!rule.Enable || !rule.TriggerKind.EqualIgnoreCase("dateArrive")) continue;
            try
            {
                n += FireDateArriveOne(rule);
            }
            catch (Exception ex)
            {
                // 单规则失败不中断整轮扫描
                XTrace.WriteException(ex);
            }
        }
        return n;
    }

    static Int32 FireDateArriveOne(EntityAutomation rule)
    {
        var n = 0;
        var field = ReadJsonString(rule.TriggerConfig, "field");
        if (field.IsNullOrEmpty()) return 0;
        var offset = ReadJsonInt(rule.TriggerConfig, "offsetMinutes");
        var once = ReadJsonBool(rule.TriggerConfig, "once", true);
        var type = AutomationExecutor.ResolveEntityType(rule.TypePath);
        if (type == null) return 0;
        var fact = XCode.EntityFactory.CreateFactory(type);
        var now = DateTime.Now;

        // 全表分批扫描（每批 500，上限 10 批），避免 200 行截断导致大表记录永不触发
        for (var start = 0; start < 5000; start += 500)
        {
            IList<IEntity> list;
            try { list = fact.FindAll(null, null, null, start, 500); }
            catch (Exception ex) { XTrace.WriteException(ex); break; }
            if (list == null || list.Count == 0) break;

            foreach (var e in list)
            {
                try
                {
                    var dt = e[field].ToDateTime();
                    if (dt.Year < 2000) continue;
                    var due = dt.AddMinutes(offset);
                    if (due > now || due < now.AddMinutes(-2)) continue;
                    var key = AutomationPaths.RecordKey(e);
                    if (once)
                    {
                        var existed = AutomationRun.FindAllByTypePathAndRecordKey(rule.TypePath, key)
                            .Any(r => r.AutomationId == rule.Id && r.Status == "succeeded");
                        if (existed) continue;
                    }
                    var run = AutomationRun.Enqueue(rule, key, "dateArrive");
                    AutomationWorker.Post(run.Id);
                    n++;
                }
                catch (Exception ex)
                {
                    // 单条记录字段异常不影响其余扫描
                    XTrace.WriteException(ex);
                }
            }

            if (list.Count < 500) break;
        }
        return n;
    }

    static String ReadJsonString(String json, String name)
    {
        try
        {
            var n = System.Text.Json.Nodes.JsonNode.Parse(json ?? "{}");
            return n?[name]?.ToString();
        }
        catch { return null; }
    }

    static Int32 ReadJsonInt(String json, String name)
    {
        try
        {
            var n = System.Text.Json.Nodes.JsonNode.Parse(json ?? "{}");
            return n?[name]?.GetValue<Int32>() ?? 0;
        }
        catch { return 0; }
    }

    static Boolean ReadJsonBool(String json, String name, Boolean def)
    {
        try
        {
            var n = System.Text.Json.Nodes.JsonNode.Parse(json ?? "{}");
            return n?[name]?.GetValue<Boolean>() ?? def;
        }
        catch { return def; }
    }
}
