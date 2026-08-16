using System.Collections.Concurrent;
using NewLife.Cube.Entity;
using NewLife.Data;

namespace NewLife.Cube.Automation;

/// <summary>
/// 自动化一次运行（内存队列/状态机，不落库）。
/// 终态流程日志写入系统 <see cref="XCode.Membership.Log"/>；对外查询以 Log 为准。
/// </summary>
public class AutomationRun
{
    /// <summary>编号（雪花）</summary>
    public Int64 Id { get; set; }
    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }
    /// <summary>规则 Id</summary>
    public Int64 AutomationId { get; set; }
    /// <summary>实体路径</summary>
    public String TypePath { get; set; }
    /// <summary>记录主键</summary>
    public String RecordKey { get; set; }
    /// <summary>触发种类</summary>
    public String TriggerKind { get; set; }
    /// <summary>状态 queued/running/waiting/succeeded/failed/cancelled</summary>
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

    static readonly ConcurrentDictionary<Int64, AutomationRun> _byId = new();
    static readonly ConcurrentDictionary<String, ConcurrentDictionary<Int64, Byte>> _byRecord = new(StringComparer.OrdinalIgnoreCase);
    static readonly Snowflake _snow = new();

    /// <summary>入队</summary>
    public static AutomationRun Enqueue(EntityAutomation rule, String recordKey, String triggerKind, Int32 depth = 0, DateTime resumeAt = default, String status = "queued")
    {
        var now = DateTime.Now;
        var run = new AutomationRun
        {
            Id = NextId(),
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
        Save(run);
        return run;
    }

    /// <summary>按 Id 查找</summary>
    public static AutomationRun FindById(Int64 id) =>
        id <= 0 ? null : (_byId.TryGetValue(id, out var r) ? r : null);

    /// <summary>按规则 Id</summary>
    public static IList<AutomationRun> FindAllByAutomationId(Int64 automationId) =>
        automationId <= 0
            ? []
            : _byId.Values.Where(e => e.AutomationId == automationId).OrderByDescending(e => e.Id).ToList();

    /// <summary>到期 waiting</summary>
    public static IList<AutomationRun> FindDueWaiting(DateTime now, Int32 max = 50) =>
        _byId.Values
            .Where(e => e.Status.EqualIgnoreCase("waiting") && e.ResumeAt <= now)
            .OrderBy(e => e.ResumeAt)
            .ThenBy(e => e.Id)
            .Take(max <= 0 ? 50 : max)
            .ToList();

    /// <summary>按实体路径+记录键</summary>
    public static IList<AutomationRun> FindAllByTypePathAndRecordKey(String typePath, String recordKey)
    {
        typePath = AutomationPaths.NormalizeTypePath(typePath);
        if (typePath.IsNullOrEmpty() || recordKey.IsNullOrEmpty()) return [];
        var key = RecordIndexKey(typePath, recordKey);
        if (!_byRecord.TryGetValue(key, out var set) || set.IsEmpty) return [];
        return set.Keys.Select(FindById).Where(e => e != null).ToList();
    }

    /// <summary>持久化到内存索引（Update/Insert 共用）</summary>
    public void Update() => Save(this);

    /// <summary>保存/更新内存行</summary>
    public static void Save(AutomationRun run)
    {
        if (run == null) return;
        if (run.Id <= 0) run.Id = NextId();
        run.UpdateTime = DateTime.Now;
        if (run.CreateTime.Year < 2000) run.CreateTime = run.UpdateTime;
        run.TypePath = AutomationPaths.NormalizeTypePath(run.TypePath);
        _byId[run.Id] = run;
        if (!run.TypePath.IsNullOrEmpty() && !run.RecordKey.IsNullOrEmpty())
            _byRecord.GetOrAdd(RecordIndexKey(run.TypePath, run.RecordKey), _ => new ConcurrentDictionary<Int64, Byte>())[run.Id] = 1;

        // 终态可从活跃索引瘦身，保留短时便于 once 去重
        if (run.Status.EqualIgnoreCase("succeeded", "failed", "cancelled") && _byId.Count > 5000)
            TrimOldTerminal();
    }

    static void TrimOldTerminal()
    {
        var old = _byId.Values
            .Where(e => e.Status.EqualIgnoreCase("succeeded", "failed", "cancelled"))
            .OrderBy(e => e.UpdateTime)
            .Take(Math.Max(0, _byId.Count - 4000))
            .ToList();
        foreach (var e in old)
        {
            _byId.TryRemove(e.Id, out _);
            if (!e.TypePath.IsNullOrEmpty() && !e.RecordKey.IsNullOrEmpty()
                && _byRecord.TryGetValue(RecordIndexKey(e.TypePath, e.RecordKey), out var set))
                set.TryRemove(e.Id, out _);
        }
    }

    /// <summary>测试重置（清空内存索引）</summary>
    public static void ResetForTests()
    {
        _byId.Clear();
        _byRecord.Clear();
    }

    static String RecordIndexKey(String typePath, String recordKey) =>
        AutomationPaths.NormalizeTypePath(typePath) + "/" + recordKey;

    static Int64 NextId() => _snow.NewId();
}
