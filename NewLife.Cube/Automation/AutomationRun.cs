using System.Collections.Concurrent;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Automation;

/// <summary>
/// 一次自动化执行的内存队列状态（非 XCode 实体）。
/// 终态审计写入系统 <see cref="XCode.Membership.Log"/>（<see cref="AutomationFlowLog"/>），不另建运行表。
/// </summary>
public class AutomationRun
{
    static readonly ConcurrentDictionary<Int64, AutomationRun> Store = new();
    static Int64 _nextId;

    /// <summary>编号</summary>
    public Int64 Id { get; set; }
    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }
    /// <summary>规则编号</summary>
    public Int64 AutomationId { get; set; }
    /// <summary>实体路径</summary>
    public String TypePath { get; set; }
    /// <summary>记录主键</summary>
    public String RecordKey { get; set; }
    /// <summary>触发种类</summary>
    public String TriggerKind { get; set; }
    /// <summary>状态。queued/running/waiting/succeeded/failed/cancelled</summary>
    public String Status { get; set; }
    /// <summary>调用深度</summary>
    public Int32 Depth { get; set; }
    /// <summary>延时续跑时间</summary>
    public DateTime ResumeAt { get; set; }
    /// <summary>节点轨迹 JSON</summary>
    public String NodeTrace { get; set; }
    /// <summary>错误摘要</summary>
    public String Error { get; set; }
    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }
    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>入队（仅内存）。默认 Status=queued。</summary>
    public static AutomationRun Enqueue(EntityAutomation rule, String recordKey, String triggerKind, Int32 depth = 0, DateTime resumeAt = default, String status = "queued")
    {
        var now = DateTime.Now;
        var run = new AutomationRun
        {
            Id = Interlocked.Increment(ref _nextId),
            TenantId = rule?.TenantId ?? 0,
            AutomationId = rule?.Id ?? 0,
            TypePath = AutomationPaths.NormalizeTypePath(rule?.TypePath),
            RecordKey = recordKey,
            TriggerKind = triggerKind,
            Status = status.IsNullOrEmpty() ? "queued" : status,
            Depth = depth,
            ResumeAt = resumeAt,
            CreateTime = now,
            UpdateTime = now,
        };
        Store[run.Id] = run;
        TrimIfNeeded();
        return run;
    }

    /// <summary>按 Id 查找</summary>
    public static AutomationRun FindById(Int64 id) =>
        id > 0 && Store.TryGetValue(id, out var run) ? run : null;

    /// <summary>同规则（测试与防抖）</summary>
    public static IList<AutomationRun> FindAllByAutomationId(Int64 automationId)
    {
        if (automationId <= 0) return [];
        return Store.Values.Where(e => e.AutomationId == automationId).OrderBy(e => e.Id).ToList();
    }

    /// <summary>同路径同记录</summary>
    public static IList<AutomationRun> FindAllByTypePathAndRecordKey(String typePath, String recordKey)
    {
        typePath = AutomationPaths.NormalizeTypePath(typePath);
        return Store.Values
            .Where(e => e.TypePath.EqualIgnoreCase(typePath) && (e.RecordKey + "") == (recordKey ?? ""))
            .ToList();
    }

    /// <summary>到期 waiting</summary>
    public static IList<AutomationRun> FindDueWaiting(DateTime now, Int32 max = 50)
    {
        if (max <= 0) max = 50;
        return Store.Values
            .Where(e => e.Status.EqualIgnoreCase("waiting") && e.ResumeAt <= now)
            .OrderBy(e => e.ResumeAt).ThenBy(e => e.Id)
            .Take(max)
            .ToList();
    }

    /// <summary>待消费 queued</summary>
    public static IList<AutomationRun> FindQueued(Int32 max = 20)
    {
        if (max <= 0) max = 20;
        return Store.Values.Where(e => e.Status.EqualIgnoreCase("queued")).OrderBy(e => e.Id).Take(max).ToList();
    }

    /// <summary>同规则同记录窗口内是否已有 queued/running</summary>
    public static Boolean HasRecentActive(Int64 automationId, String recordKey, Int32 withinMs)
    {
        if (automationId <= 0 || withinMs <= 0) return false;
        var since = DateTime.Now.AddMilliseconds(-withinMs);
        return Store.Values.Any(e =>
            e.AutomationId == automationId
            && (e.RecordKey ?? "") == (recordKey ?? "")
            && (e.Status.EqualIgnoreCase("queued") || e.Status.EqualIgnoreCase("running"))
            && e.CreateTime >= since);
    }

    /// <summary>刷新更新时间（兼容原实体 Update）</summary>
    public Int32 Update()
    {
        UpdateTime = DateTime.Now;
        return 1;
    }

    /// <summary>测试清空</summary>
    public static void ResetForTests()
    {
        Store.Clear();
        Interlocked.Exchange(ref _nextId, 0);
    }

    static void TrimIfNeeded()
    {
        if (Store.Count <= 2000) return;
        foreach (var old in Store.Values
            .Where(e => e.Status.EqualIgnoreCase("succeeded") || e.Status.EqualIgnoreCase("failed") || e.Status.EqualIgnoreCase("cancelled"))
            .OrderBy(e => e.UpdateTime)
            .Take(Store.Count - 1500)
            .ToArray())
        {
            Store.TryRemove(old.Id, out _);
        }
    }
}
