using NewLife.Cube.Automation;
using NewLife.Cube.Entity;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.Membership;
using WorkflowDefinition = NewLife.Cube.Workflow.Entity.WorkflowDefinition;
using WorkflowInstance = NewLife.Cube.Workflow.Entity.WorkflowInstance;
using WorkflowSubject = NewLife.Cube.Workflow.Entity.WorkflowSubject;

namespace NewLife.Cube.Workflow;

/// <summary>类型级 GetPage workflow 块 + 行级 __wf* 覆盖（design §6.3）</summary>
/// <remarks>
/// 编译进 WebAPI 核心库 NewLife.Cube：由 ReadOnlyEntityController.GetPage/Index/Detail 在序列化前调用。
/// 匿名：GetTypeBlock 仅 enabled；行级覆盖仅登录用户；GetPage 匿名不下发 canStart/可写字段名。
/// </remarks>
public static class WorkflowPageOverlay
{
    /// <summary>构造 GetPage 类型级 workflow 块</summary>
    /// <param name="typePath">实体路径</param>
    /// <param name="user">当前用户，可空（匿名）</param>
    /// <returns>字典（序列化为 workflow JSON 对象），禁用时仅 enabled=false</returns>
    public static IDictionary<String, Object> GetTypeBlock(String typePath, IUser user)
    {
        var dict = new Dictionary<String, Object>();
        var defs = EnabledDefinitions(typePath);
        if (defs.Count == 0)
        {
            dict["enabled"] = false;
            return dict;
        }

        dict["enabled"] = true;
        // 匿名仅能力开关：不下发可写字段、canStart、definitionCount（AC-09）
        if (user == null) return dict;

        dict["definitionCount"] = defs.Count;
        // 多定义时若锁策略不一致不下发单一误导值（行级锁以在途实例定义为准）
        var policies = defs.Select(d => d.LockPolicy.IsNullOrEmpty() ? WorkflowStatuses.LockFull : d.LockPolicy)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (policies.Count == 1) dict["lockPolicy"] = policies[0];
        // 登录且对该实体有 Detail 才可发起（类型级开关，行级在 GetList 再判）
        dict["canStart"] = CanDetail(user, typePath);
        return dict;
    }

    /// <summary>列表行级覆盖：登录且类型启用时注入 __wfStatus / __wfInstanceId / __wfCanStart（批量 IN，无 N+1）</summary>
    /// <param name="rows">列表行</param>
    /// <param name="typePath">实体路径</param>
    /// <param name="user">当前用户</param>
    public static void ApplyRows(IEnumerable<IEntity> rows, String typePath, IUser user)
    {
        if (user == null || rows == null) return;
        var list = rows as IList<IEntity> ?? rows.ToList();
        if (list.Count == 0) return;

        var np = AutomationPaths.NormalizeTypePath(typePath);
        if (np.IsNullOrEmpty()) return;

        var defs = EnabledDefinitions(np);
        if (defs.Count == 0) return;

        var factory = ResolveFactorySafe(np);
        if (factory == null) return;

        // 主键文本集合
        var keys = new List<String>(list.Count);
        var byKey = new Dictionary<String, IEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in list)
        {
            var key = RowKey(row, factory);
            if (key.IsNullOrEmpty()) continue;
            keys.Add(key);
            byKey.TryAdd(key, row);
        }
        if (keys.Count == 0) return;

        // 一次取全部主体（含历史），按 EntityKey 取最新实例；再按实例 Id 批量取状态
        var subjects = WorkflowSubject.FindAll(WorkflowSubject._.TypePath == np & WorkflowSubject._.EntityKey.In(keys)).ToList();
        var instIds = new HashSet<Int64>();
        var latestByKey = new Dictionary<String, WorkflowSubject>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in subjects)
        {
            if (!latestByKey.TryGetValue(s.EntityKey, out var old) || s.Id > old.Id)
                latestByKey[s.EntityKey] = s;
            instIds.Add(s.InstanceId);
        }
        var instMap = new Dictionary<Int64, WorkflowInstance>();
        if (instIds.Count > 0)
        {
            foreach (var inst in WorkflowInstance.FindAll(WorkflowInstance._.Id.In(instIds)))
            {
                instMap[inst.Id] = inst;
            }
        }

        var typeCanStart = CanDetail(user, np);
        // 多定义：行级 canStart 对「任一」已发布定义 StartFilter 命中即可（提交时再选 definitionId，G-08）
        var filters = defs
            .Select(d => d.StartFilter.IsNullOrEmpty() || d.StartFilter == "{}" ? null : AutomationFilter.ParseViewFilter(d.StartFilter))
            .ToList();

        foreach (var row in list)
        {
            var key = RowKey(row, factory);
            var status = "none";
            var instanceId = 0L;
            var running = false;
            if (latestByKey.TryGetValue(key, out var s) && instMap.TryGetValue(s.InstanceId, out var inst))
            {
                instanceId = inst.Id;
                status = MapStatus(inst.Status);
                running = inst.Status == WorkflowStatuses.Running;
            }
            row.SetItem("__wfStatus", status);
            row.SetItem("__wfInstanceId", instanceId > 0 ? instanceId.ToString() : "0");
            // 审批中 / 已通过：不可再发起（驳回/撤回可再发起）
            var canStart = !running && status != "approved" && typeCanStart;
            if (canStart && filters.Count > 0)
                canStart = filters.Any(f => f == null || AutomationFilter.Match(row, f));
            row.SetItem("__wfCanStart", canStart);
        }
    }

    /// <summary>单行覆盖（GetDetail / 行操作）：__wfStatus + __wfInstanceId + 节点可写字段 __wfWritable</summary>
    /// <param name="row">详情行</param>
    /// <param name="typePath">实体路径</param>
    /// <param name="user">当前用户</param>
    public static void ApplyRow(IEntity row, String typePath, IUser user)
    {
        if (row == null) return;
        ApplyRows([row], typePath, user);

        // 审批中：注入当前用户可见节点可写字段
        if (user == null) return;
        var running = WorkflowSubject.FindRunning(AutomationPaths.NormalizeTypePath(typePath), RowKey(row, ResolveFactorySafe(typePath)));
        if (running == null) return;
        var instance = WorkflowInstance.FindById(running.InstanceId);
        if (instance == null) return;
        var writable = WorkflowWriteInterceptor.CurrentWritable(instance, user.ID);
        row.SetItem("__wfWritable", writable.ToList());
    }

    /// <summary>状态映射为前端小写</summary>
    /// <param name="status">实例状态</param>
    /// <returns>前端状态</returns>
    static String MapStatus(String status)
    {
        if (status == WorkflowStatuses.Approved) return "approved";
        if (status == WorkflowStatuses.Rejected) return "rejected";
        if (status == WorkflowStatuses.Withdrawn) return "withdrawn";
        return status?.ToLowerInvariant() ?? "none";
    }

    /// <summary>已启用已发布定义</summary>
    /// <param name="typePath">实体路径（已归一化）</param>
    /// <returns>定义列表</returns>
    static IList<WorkflowDefinition> EnabledDefinitions(String typePath)
    {
        var np = AutomationPaths.NormalizeTypePath(typePath);
        if (np.IsNullOrEmpty()) return [];
        return WorkflowDefinition.FindPublished(TenantContext.CurrentId, np);
    }

    /// <summary>用户是否有实体 Detail 权限</summary>
    /// <param name="user">用户</param>
    /// <param name="typePath">实体路径</param>
    /// <returns>是否可发起（有 Detail）</returns>
    public static Boolean CanDetail(IUser user, String typePath)
    {
        if (user == null) return false;
        if (AutomationAuth.IsSystem(user)) return true;
        try
        {
            var menu = AutomationAuth.FindMenu(typePath);
            return menu == null || user.Has(menu, PermissionFlags.Detail);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>安全解析工厂（失败返回 null）</summary>
    /// <param name="typePath">实体路径</param>
    /// <returns>工厂</returns>
    static IEntityFactory ResolveFactorySafe(String typePath)
    {
        try { return WorkflowEngine.ResolveFactory(typePath); }
        catch { return null; }
    }

    /// <summary>行主键归一化文本（与 WorkflowSubject.EntityKey 一致）</summary>
    /// <param name="row">实体</param>
    /// <param name="factory">工厂</param>
    /// <returns>主键文本</returns>
    public static String RowKey(IEntity row, IEntityFactory factory)
    {
        if (row == null) return null;
        factory ??= EntityFactory.CreateFactory(row.GetType());
        var pk = factory?.Unique?.Name ?? factory?.Table?.PrimaryKeys?.FirstOrDefault()?.Name;
        if (pk.IsNullOrEmpty()) return null;
        var value = row[pk];
        if (value == null) return null;
        if (value is String s2) return s2.Trim();
        if (value is Guid g) return g.ToString("N");
        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
