using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using NewLife.Cube.Entity;
using NewLife.Data;
using XCode;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Automation;

/// <summary>持久化后匹配规则并入队</summary>
public static class AutomationTrigger
{
    static readonly ConcurrentDictionary<String, Int64> _debounce = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>自身写入会再引爆自动化的实体：永不挂钩 Persistence</summary>
    static readonly HashSet<String> LoopSkipTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(EntityAutomation), nameof(NotificationRecord), nameof(EntityComment),
    };

    /// <summary>
    /// 作业心跳字段。JobService 每轮只改这些列；用户改名称/Cron/启用等仍应触发。
    /// 审计列一并列入，避免拦截器脏字段把心跳误判成业务更新。
    /// </summary>
    static readonly HashSet<String> CronJobRuntimeFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(CronJob.LastTime), nameof(CronJob.NextTime), nameof(CronJob.Data),
        nameof(CronJob.UpdateTime), nameof(CronJob.UpdateUserID), nameof(CronJob.UpdateIP),
    };

    /// <summary>是否跳过该实体类型（循环实体不包装 Persistence）</summary>
    public static Boolean ShouldSkip(Type type) => type != null && LoopSkipTypes.Contains(type.Name);

    /// <summary>Update 是否仅为作业心跳/审计字段（定时作业心跳不入队）</summary>
    public static Boolean IsRuntimeOnlyUpdate(Type type, String[] dirtys)
    {
        if (type == null || !type.Name.EqualIgnoreCase(nameof(CronJob))) return false;
        if (dirtys == null || dirtys.Length == 0) return true;
        return dirtys.All(d => CronJobRuntimeFields.Contains(d));
    }

    /// <summary>SQL 成功后入队</summary>
    public static void OnPersisted(IEntity entity, DataMethod method, String[] dirtys)
    {
        if (entity == null) return;
        var type = entity.GetType();
        if (ShouldSkip(type)) return;
        if (method == DataMethod.Update && IsRuntimeOnlyUpdate(type, dirtys)) return;

        // 启动后动态注册的工厂：首次写入时懒补挂（本次已过，从下一次写入开始拦截）
        try { AutomationHost.Ensure(EntityFactory.CreateFactory(type)); }
        catch { /* 包装失败不影响业务写入 */ }

        var typePath = AutomationPaths.ResolveTypePath(type);
        if (typePath.IsNullOrEmpty()) return;

        IList<EntityAutomation> rules;
        try { rules = EntityAutomation.FindEnabled(typePath); }
        catch { return; }
        if (rules == null || rules.Count == 0) return;

        var tenantId = TenantContext.CurrentId;
        var recordKey = AutomationPaths.RecordKey(entity);
        var kind = method switch
        {
            DataMethod.Insert => "insert",
            DataMethod.Update => "update",
            DataMethod.Delete => "delete",
            _ => null
        };
        if (kind == null) return;

        foreach (var rule in rules)
        {
            if (!TenantMatches(rule.TenantId, tenantId)) continue;
            if (!TriggerMatch(rule, kind, dirtys)) continue;

            var depth = AutomationScope.IsExecuting ? AutomationScope.Depth + 1 : 0;
            if (depth >= AutomationRuntime.MaxDepth)
            {
                FailDepth(rule, recordKey, kind, depth);
                continue;
            }
            if (Debounce(rule.Id, recordKey)) continue;

            AutomationScope.BatchCount++;
            var status = "queued";
            var resume = DateTime.MinValue;
            if (AutomationScope.BatchCount > AutomationRuntime.BatchLimit)
            {
                status = "waiting";
                var extra = AutomationScope.BatchCount - AutomationRuntime.BatchLimit;
                resume = DateTime.Now.AddSeconds(1 + extra / AutomationRuntime.BatchLimit);
            }

            try
            {
                var run = AutomationRun.Enqueue(rule, recordKey, kind, depth, resume, status);
                if (status == "queued") AutomationWorker.Post(run.Id);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }

    /// <summary>租户匹配（与 DataScope 当前租户一致）</summary>
    public static Boolean TenantMatches(Int32 ruleTenant, Int32 current)
    {
        if (!CubeSetting.Current.EnableTenant) return true;
        if (ruleTenant == 0) return current == 0;
        return ruleTenant == current;
    }

    /// <summary>测试重置防抖</summary>
    public static void ResetDebounce() => _debounce.Clear();

    static Boolean TriggerMatch(EntityAutomation rule, String persistKind, String[] dirtys)
    {
        var tk = (rule.TriggerKind + "").ToLowerInvariant();
        return tk switch
        {
            "insert" => persistKind == "insert",
            "delete" => persistKind == "delete",
            "update" => persistKind == "update",
            "insertorupdateif" => persistKind is "insert" or "update",
            "fieldchange" => persistKind == "update" && FieldChangeHit(rule.TriggerConfig, dirtys),
            _ => false,
        };
    }

    static Boolean FieldChangeHit(String triggerConfig, String[] dirtys)
    {
        if (dirtys == null || dirtys.Length == 0) return false;
        var watch = ParseWatchFields(triggerConfig);
        if (watch.Count == 0) return true;
        return dirtys.Any(d => watch.Any(w => w.EqualIgnoreCase(d)));
    }

    /// <summary>解析 watchFields</summary>
    public static List<String> ParseWatchFields(String json)
    {
        var list = new List<String>();
        if (json.IsNullOrEmpty()) return list;
        try
        {
            var node = JsonNode.Parse(json);
            var arr = node?["watchFields"] as JsonArray;
            if (arr == null) return list;
            foreach (var x in arr)
            {
                var s = (x?.ToString() + "").Trim();
                if (!s.IsNullOrEmpty() && list.Count < 32) list.Add(s);
            }
        }
        catch { /* 非法 JSON 视为无 watch */ }
        return list;
    }

    static Boolean Debounce(Int64 automationId, String recordKey)
    {
        var key = automationId + ":" + (recordKey ?? "");
        var now = DateTime.UtcNow.Ticks;
        if (_debounce.TryGetValue(key, out var last) && (now - last) < TimeSpan.FromMilliseconds(AutomationRuntime.DebounceMs).Ticks)
            return true;
        // design：3 秒内已有 queued/running 则跳过（进程重启后内存字典丢失时仍生效）
        try
        {
            if (AutomationRun.HasRecentActive(automationId, recordKey, AutomationRuntime.DebounceMs))
                return true;
        }
        catch { /* 查询失败不挡入队 */ }
        _debounce[key] = now;
        return false;
    }

    static void FailDepth(EntityAutomation rule, String recordKey, String kind, Int32 depth)
    {
        try
        {
            var run = AutomationRun.Enqueue(rule, recordKey, kind, depth, DateTime.MinValue, "failed");
            run.Error = "超过最大深度";
            run.Update();
            AutomationFlowLog.WriteTerminal(rule, run);
        }
        catch { /* 忽略 */ }
    }
}
