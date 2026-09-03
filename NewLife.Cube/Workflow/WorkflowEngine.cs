using System.Text.Json.Nodes;
using JsonArray = System.Text.Json.Nodes.JsonArray;
using NewLife;
using NewLife.Cube.Automation;
using NewLife.Cube.Entity;
using NewLife.Cube.Membership;
using NewLife.Log;
using NewLife.Serialization;
using XCode;
using XCode.Membership;
using WorkflowComment = NewLife.Cube.Workflow.Entity.WorkflowComment;
using WorkflowDefinition = NewLife.Cube.Workflow.Entity.WorkflowDefinition;
using WorkflowInstance = NewLife.Cube.Workflow.Entity.WorkflowInstance;
using WorkflowSubject = NewLife.Cube.Workflow.Entity.WorkflowSubject;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Workflow;

/// <summary>OA 审批状态机。实例/任务/意见的读写与流转，全部经本引擎，禁止外部直接改状态列</summary>
/// <remarks>
/// 图钉扎在实例 GraphSnapshot；节点候选人复用 RecipientResolver 展开；条件求值复用 AutomationFilter。
/// 错误以 WorkflowException(code) 抛出：400 校验 / 403 越权 / 404 无记录 / 409 冲突。
/// </remarks>
public static class WorkflowEngine
{
    /// <summary>发起互斥：同进程内“在途检查 + 落库”原子化（防并发双插）。跨进程部署需 DB 唯一约束，见 WorkflowExclusiveTests 备注</summary>
    static readonly Object _startGate = new();
    #region 发起
    /// <summary>发起一批（N 条同 TypePath 记录 → 1 实例 + N 主体 + 首节点任务）</summary>
    /// <param name="definition">已发布定义</param>
    /// <param name="keys">业务主键（归一化字符串），1–100</param>
    /// <param name="starterId">发起人</param>
    /// <param name="starterName">发起人姓名</param>
    /// <param name="comment">发起意见</param>
    /// <returns>新实例</returns>
    public static WorkflowInstance Start(WorkflowDefinition definition, IList<String> keys, Int32 starterId, String starterName, String comment)
    {
        if (definition == null) throw new WorkflowException(404, "流程定义不存在");
        if (!definition.Published || !definition.Enable) throw new WorkflowException(400, "流程定义未发布或已停用");

        var typePath = AutomationPaths.NormalizeTypePath(definition.TypePath);
        if (typePath.IsNullOrEmpty()) throw new WorkflowException(400, "流程定义实体路径为空");

        // 去重 + 上限
        var kk = keys?.Where(e => !e.IsNullOrEmpty()).Distinct().ToList() ?? [];
        if (kk.Count == 0) throw new WorkflowException(400, "至少选择一条记录");
        if (kk.Count > 100) throw new WorkflowException(400, "一次最多提交 100 条记录");

        var factory = ResolveFactory(typePath);
        var pks = factory.Table?.PrimaryKeys ?? [];
        if (pks == null || pks.Length != 1) throw new WorkflowException(400, "复合主键实体不支持审批");
        var pkName = pks[0].Name;
        var pkField = factory.Fields?.FirstOrDefault(e => e.Name.EqualIgnoreCase(pkName));
        if (pkField == null) throw new WorkflowException(400, "无法识别实体主键");

        // 解析发起条件
        var filter = AutomationFilter.ParseViewFilter(definition.StartFilter);

        // 主键归一化：查询与落库使用同一条文本（数值 InvariantCulture、无前导零；字符串 trim）
        var keys2 = kk.Select(e => NormalizeKeyText(e, pkField)).ToList();

        // 在途排他与落库整体互斥（进程内原子窗口；跨进程需 DB 唯一约束，见 WorkflowExclusiveTests 备注）
        lock (_startGate)
        {
        // 加载业务行（首条用于条件与标题），同时做在途排他
        var rows = new List<IEntity>();
        for (var i = 0; i < keys2.Count; i++)
        {
            var key = keys2[i];
            // 同一记录同时最多一条在途
            if (WorkflowSubject.FindRunning(typePath, key) != null)
                throw new WorkflowException(409, $"记录[{key}]已有在途审批");

            var row = factory.FindByKey(NormalizeKey(kk[i], pkField));
            if (row == null || (row as IEntity).IsNullKey) throw new WorkflowException(404, $"记录[{key}]不存在");
            rows.Add(row);
        }

        // 发起条件：全部主体 Match，任一失败整批 400
        if (filter != null)
        {
            foreach (var row in rows)
            {
                if (!AutomationFilter.Match(row, filter))
                    throw new WorkflowException(400, "存在不满足发起条件的记录");
            }
        }

        // 发布快照合法
        var graph = WorkflowGraph.Parse(definition.PublishedGraphJson);
        if (graph == null || graph.Validate().Count > 0)
            throw new WorkflowException(400, "流程定义发布图非法，无法发起");

        var tenantId = definition.TenantId;
        var now = DateTime.Now;

        using var tran = WorkflowInstance.Meta.CreateTrans();
        try
        {
            // 实例
            var instance = new WorkflowInstance
            {
                TenantId = tenantId,
                DefinitionId = definition.Id,
                DefinitionVersion = definition.Version,
                TypePath = typePath,
                GraphSnapshot = definition.PublishedGraphJson,
                Status = WorkflowStatuses.Running,
                StarterId = starterId,
                StartComment = comment?.Cut(500),
            };
            instance.Insert();

            // 主体
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var master = factory.Table?.Master?.Name;
                var title = master.IsNullOrEmpty() ? null : row[master]?.ToString();
                if (title.IsNullOrEmpty()) title = keys2[i];
                var subject = new WorkflowSubject
                {
                    InstanceId = instance.Id,
                    TypePath = typePath,
                    EntityKey = keys2[i],
                    Title = title.Cut(100),
                };
                subject.Insert();
            }

            // 首节点任务（从 start 后推进；无审批节点链直接到 end 时实例直接 Approved）
            var start = graph.Start;
            if (start != null)
            {
                var next = graph.NextOf(start.Id);
                foreach (var nodeId in next)
                {
                    Advance(instance, graph, nodeId, tenantId, starterId, starterName, new HashSet<String>());
                }
            }

            AddComment(instance.Id, 0, starterId, starterName, "start", comment);

            tran.Commit();
            return instance;
        }
        catch
        {
            // 事务回滚后原样抛出
            throw;
        }
        } // lock _startGate
    }
    #endregion

    #region 任务操作
    /// <summary>同意。or 一人即过；and 全员/quorum；sequence 放行下一位</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="comment">意见</param>
    public static void Approve(Int64 taskId, Int32 userId, String comment)
        => HandleVote(taskId, userId, comment, false, false);

    /// <summary>驳回。整单 Rejected 并解锁主体</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="comment">意见</param>
    public static void Reject(Int64 taskId, Int32 userId, String comment)
        => HandleVote(taskId, userId, comment, true, false);

    /// <summary>超时通过/驳回（内部，候选人绕过）</summary>
    /// <param name="task">任务</param>
    /// <param name="reject">是否驳回</param>
    static void TimeoutVote(WorkflowTask task, Boolean reject)
        => HandleVote(task.Id, 0, "超时自动" + (reject ? "驳回" : "通过"), reject, true);

    /// <summary>认领（或签任务）</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="userId">操作人</param>
    public static void Claim(Int64 taskId, Int32 userId)
    {
        var task = WorkflowTask.FindById(taskId) ?? throw new WorkflowException(404, "任务不存在");
        EnsureCandidate(task, userId);
        if (task.Status is not (WorkflowStatuses.Pending or WorkflowStatuses.Active))
            throw new WorkflowException(409, "任务已结束，无法认领");
        if (task.AssigneeId > 0 && task.AssigneeId != userId)
            throw new WorkflowException(409, "任务已被他人认领");

        task.AssigneeId = userId;
        task.Status = WorkflowStatuses.Active;
        if (task.ClaimTime <= DateTime.MinValue) task.ClaimTime = DateTime.Now;
        task.Update();
    }

    /// <summary>转办。原任务置 Transferred，新任务按 to 单人</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="to">接收人（单用户）</param>
    /// <param name="comment">意见</param>
    public static void Transfer(Int64 taskId, Int32 userId, JsonObject to, String comment)
    {
        var task = LoadOpenTask(taskId, userId, true);
        var ids = ResolveIds(to, task, null, out var single);
        if (ids.Count == 0) throw new WorkflowException(400, "转办接收人为空");
        if (!single) throw new WorkflowException(400, "转办仅支持单个用户");

        var instance = WorkflowInstance.FindById(task.InstanceId);
        var targetId = ids.First();
        if (targetId == userId) throw new WorkflowException(400, "不能转办给自己");

        using var tran = WorkflowTask.Meta.CreateTrans();
        task.Status = WorkflowStatuses.Transferred;
        task.FinishTime = DateTime.Now;
        task.Update();
        var nt = CloneTask(task);
        nt.AssigneeId = targetId;
        nt.CandidateJson = $"[{targetId}]";
        nt.Status = WorkflowStatuses.Pending;
        nt.Visible = true;
        nt.ClaimTime = DateTime.MinValue;
        nt.FinishTime = DateTime.MinValue;
        nt.Id = 0;
        nt.Insert();
        AddComment(instance.Id, task.Id, userId, null, "transfer", comment);
        NotifyTaskArrive(instance, nt);
        tran.Commit();
    }

    /// <summary>知会。只写通知，不产生任务</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="to">接收人</param>
    /// <param name="comment">意见</param>
    public static void Cc(Int64 taskId, Int32 userId, JsonObject to, String comment)
    {
        var task = LoadOpenTask(taskId, userId, true);
        var ids = ResolveIds(to, task, null, out _);
        if (ids.Count == 0) throw new WorkflowException(400, "知会接收人为空");

        var instance = WorkflowInstance.FindById(task.InstanceId);
        foreach (var id in ids)
        {
            Notify(instance, id, "知会", BuildTitle(instance, task), comment);
        }
        AddComment(instance.Id, task.Id, userId, null, "cc", comment);
    }

    /// <summary>回退到已办节点。目标节点重开，其下游（含当前）取消</summary>
    /// <param name="taskId">当前任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="targetNodeId">目标节点 Id（必须已办过的 oa.approve）</param>
    /// <param name="comment">意见</param>
    public static void Rollback(Int64 taskId, Int32 userId, String targetNodeId, String comment)
    {
        var task = LoadOpenTask(taskId, userId, true);
        var instance = WorkflowInstance.FindById(task.InstanceId);
        var graph = ParseSnapshot(instance);
        var node = graph.Find(targetNodeId);
        if (node == null || node.Type != WorkflowGraph.ApproveType) throw new WorkflowException(400, "回退目标必须是审批节点");
        if (targetNodeId == task.NodeId) throw new WorkflowException(400, "不能回退到当前节点");

        // 已办过：目标节点存在 Done 任务
        var done = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == targetNodeId)
            .Any(e => e.Status == WorkflowStatuses.Done);
        if (!done) throw new WorkflowException(400, "回退目标不是已办节点");

        var downstream = graph.Downstream(targetNodeId);
        downstream.Add(task.NodeId);
        downstream.Add(targetNodeId); // 目标节点旧任务一并作废，重新生成

        using var tran = WorkflowTask.Meta.CreateTrans();
        var open = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id);
        foreach (var t in open)
        {
            if (downstream.Contains(t.NodeId) && t.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active)
            {
                t.Status = WorkflowStatuses.Cancelled;
                t.FinishTime = DateTime.Now;
                t.Update();
            }
        }
        AddComment(instance.Id, task.Id, userId, null, "rollback", comment);
        tran.Commit();

        // 目标节点按原 mode 重新生成（候选人重新展开）
        Advance(instance, graph, targetNodeId, instance.TenantId, instance.StarterId, null, []);
    }

    /// <summary>加签。before 前加签 / after 后加签，临时节点不写回定义</summary>
    /// <param name="taskId">当前任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="before">是否前加签</param>
    /// <param name="to">加签接收人</param>
    /// <param name="comment">意见</param>
    public static void AddSign(Int64 taskId, Int32 userId, Boolean before, JsonObject to, String comment)
    {
        var task = LoadOpenTask(taskId, userId, true);
        var instance = WorkflowInstance.FindById(task.InstanceId);
        var graph = ParseSnapshot(instance);
        var node = graph.Find(task.NodeId);
        // 临时节点上不允许再加签
        if (task.NodeId.Contains('#')) throw new WorkflowException(400, "加签节点不能再加签");
        if (node == null || !node.AllowAddSign) throw new WorkflowException(403, "当前节点不允许加签");

        var ids = ResolveIds(to, task, null, out _);
        if (ids.Count == 0) throw new WorkflowException(400, "加签接收人为空");

        using var tran = WorkflowTask.Meta.CreateTrans();
        var idx = 0;
        foreach (var id in ids)
        {
            var nt = CloneTask(task);
            nt.NodeId = before ? $"{task.NodeId}#addsign#{task.Id}" : $"{task.NodeId}#after#{task.Id}";
            nt.AssigneeId = id;
            nt.CandidateJson = $"[{id}]";
            nt.Mode = "or";
            nt.SequenceIndex = idx++;
            nt.Status = WorkflowStatuses.Pending;
            // 前加签：先办加签，原任务挂起；后加签：原任务完成后才轮到
            nt.Visible = before;
            nt.ClaimTime = DateTime.MinValue;
            nt.FinishTime = DateTime.MinValue;
            nt.Id = 0;
            nt.Insert();
        }

        // 前加签：原任务挂起；后加签：原任务保持待办
        if (before)
        {
            task.Visible = false;
            task.Status = WorkflowStatuses.Pending;
            task.Update();
        }
        AddComment(instance.Id, task.Id, userId, null, "addSign", comment);
        tran.Commit();
    }

    /// <summary>撤回。仅发起人且无人同意过</summary>
    /// <param name="instanceId">实例编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="comment">意见</param>
    public static void Withdraw(Int64 instanceId, Int32 userId, String comment)
    {
        var instance = LoadRunning(instanceId);
        if (instance.StarterId != userId) throw new WorkflowException(403, "仅发起人可撤回");
        if (WorkflowComment.FindAll(WorkflowComment._.InstanceId == instance.Id).Any(e => e.Action == "approve"))
            throw new WorkflowException(409, "已有审批意见，不能撤回");

        FinishInstance(instance, WorkflowStatuses.Withdrawn, userId, "withdraw", comment);
    }

    /// <summary>作废（管理员/定义 Update 权）。已发出的知会不收回</summary>
    /// <param name="instanceId">实例编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="comment">意见</param>
    public static void Cancel(Int64 instanceId, Int32 userId, String comment)
    {
        var instance = LoadRunning(instanceId);
        FinishInstance(instance, WorkflowStatuses.Cancelled, userId, "cancel", comment);
    }

    /// <summary>指定节点跳转（管理员）。取消全部未完成任务并跳到目标节点</summary>
    /// <param name="instanceId">实例编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="targetNodeId">目标节点</param>
    /// <param name="comment">意见</param>
    public static void Jump(Int64 instanceId, Int32 userId, String targetNodeId, String comment)
    {
        var instance = LoadRunning(instanceId);
        var graph = ParseSnapshot(instance);
        var node = graph.Find(targetNodeId);
        if (node == null) throw new WorkflowException(400, "跳转目标节点不存在");

        CancelAllOpen(instance.Id);
        AddComment(instance.Id, 0, userId, null, "jump", comment);
        Advance(instance, graph, targetNodeId, instance.TenantId, instance.StarterId, null, []);
    }
    #endregion

    #region 超时
    /// <summary>扫描到期任务并执行超时动作（pass/reject/transfer）。供 CronJob WorkflowTimeoutTick 调用</summary>
    /// <returns>处理汇总</returns>
    public static String TimeoutTick()
    {
        var now = DateTime.Now;
        var due = WorkflowTask.FindAll(WorkflowTask._.DueTime > DateTime.MinValue & WorkflowTask._.DueTime <= now)
            .Where(e => e.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active)
            .ToList();
        var pass = 0;
        var reject = 0;
        var transfer = 0;
        var fail = 0;
        foreach (var task in due)
        {
            try
            {
                switch (task.TimeoutAction)
                {
                    case "reject":
                        TimeoutVote(task, true);
                        reject++;
                        break;
                    case "transfer":
                        TransferByTimeout(task);
                        transfer++;
                        break;
                    default:
                        TimeoutVote(task, false);
                        pass++;
                        break;
                }
            }
            catch (Exception ex)
            {
                fail++;
                XTrace.WriteException(ex);
            }
        }
        return $"due={due.Count} pass={pass} reject={reject} transfer={transfer} fail={fail}";
    }

    /// <summary>超时转交：原任务 Transferred，按 timeoutTransferTo 生成新任务</summary>
    /// <param name="task">到期任务</param>
    static void TransferByTimeout(WorkflowTask task)
    {
        var instance = WorkflowInstance.FindById(task.InstanceId);
        var node = ParseSnapshot(instance).Find(task.NodeId);
        var to = node?.TimeoutTransferTo ?? JsonNode.Parse(task.TimeoutTransferTo) as JsonObject;
        var ids = RecipientResolver.Resolve(to, instance.TenantId, null).ToList();
        if (ids.Count == 0)
        {
            // 无目标则按通过处理，避免死锁
            TimeoutVote(task, false);
            return;
        }

        using var tran = WorkflowTask.Meta.CreateTrans();
        task.Status = WorkflowStatuses.Transferred;
        task.FinishTime = DateTime.Now;
        task.Update();
        var nt = CloneTask(task);
        nt.AssigneeId = ids.First();
        nt.CandidateJson = $"[{nt.AssigneeId}]";
        nt.Status = WorkflowStatuses.Pending;
        nt.Visible = true;
        nt.ClaimTime = DateTime.MinValue;
        nt.FinishTime = DateTime.MinValue;
        nt.Id = 0;
        nt.Insert();
        AddComment(instance.Id, task.Id, 0, "系统", "timeout", "超时转交");
        NotifyTaskArrive(instance, nt);
        tran.Commit();
    }
    #endregion

    #region 核心流转
    /// <summary>同意/驳回统一入口</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="comment">意见</param>
    /// <param name="reject">驳回</param>
    /// <param name="asTimeout">超时通道（跳过候选人校验）</param>
    static void HandleVote(Int64 taskId, Int32 userId, String comment, Boolean reject, Boolean asTimeout)
    {
        var userName = asTimeout ? "系统" : User.FindByID(userId)?.Name ?? userId + "";
        var task = asTimeout ? (WorkflowTask.FindById(taskId) ?? throw new WorkflowException(404, "任务不存在")) : LoadOpenTask(taskId, userId, !reject);
        var instance = WorkflowInstance.FindById(task.InstanceId) ?? throw new WorkflowException(404, "流程实例不存在");
        if (instance.Status != WorkflowStatuses.Running) throw new WorkflowException(409, "流程已结束");

        using var tran = WorkflowTask.Meta.CreateTrans();

        // 乐观并发：事务内重读
        var fresh = WorkflowTask.FindById(task.Id);
        if (fresh.Status is not (WorkflowStatuses.Pending or WorkflowStatuses.Active))
            throw new WorkflowException(409, "任务已被处理");
        if (!asTimeout) EnsureCandidate(fresh, userId);
        task = fresh;

        if (reject)
        {
            task.Status = WorkflowStatuses.Rejected;
            task.AssigneeId = asTimeout ? task.AssigneeId : userId;
            task.FinishTime = DateTime.Now;
            task.Update();
            AddComment(instance.Id, task.Id, userId, userName, "reject", comment);
            tran.Commit();

            FinishInstance(instance, WorkflowStatuses.Rejected, userId, null, null);
            return;
        }

        task.Status = WorkflowStatuses.Done;
        task.AssigneeId = asTimeout ? task.AssigneeId : userId;
        if (task.ClaimTime <= DateTime.MinValue) task.ClaimTime = DateTime.Now;
        task.FinishTime = DateTime.Now;
        task.Update();
        AddComment(instance.Id, task.Id, userId, userName, "approve", comment);
        tran.Commit();

        // 推进：处理加签/节点通过/下游
        AfterApprove(instance, task);
    }

    /// <summary>同意后推进</summary>
    /// <param name="instance">实例</param>
    /// <param name="task">已完成任务</param>
    static void AfterApprove(WorkflowInstance instance, WorkflowTask task)
    {
        var graph = ParseSnapshot(instance);

        // 加签临时任务：还原/接续
        if (TryHandleSynthetic(instance, graph, task)) return;

        // 依次签：放行下一位（未轮到的不参与通过判定）
        if (task.Mode == "sequence")
        {
            var nextSeq = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == task.NodeId)
                .Where(e => e.Status == WorkflowStatuses.Pending && !e.Visible)
                .OrderBy(e => e.SequenceIndex)
                .FirstOrDefault();
            if (nextSeq != null)
            {
                nextSeq.Visible = true;
                nextSeq.Update();
                NotifyTaskArrive(instance, nextSeq);
                return;
            }
        }

        // 节点通过判定
        if (!NodePassed(instance, graph, task.NodeId)) return;

        // 节点已通过：取消同节点剩余未完成任务，向下游推进
        CancelNodeOpen(instance.Id, task.NodeId);
        var next = graph.NextOf(task.NodeId);
        foreach (var nodeId in next)
        {
            Advance(instance, graph, nodeId, instance.TenantId, instance.StarterId, null, []);
        }
    }

    /// <summary>处理加签临时任务（NodeId 含 #）。返回 true 表示已处理（不再走常规下游）</summary>
    /// <param name="instance">实例</param>
    /// <param name="graph">图</param>
    /// <param name="task">已完成任务</param>
    /// <returns>是否已按加签语义处理</returns>
    static Boolean TryHandleSynthetic(WorkflowInstance instance, WorkflowGraph graph, WorkflowTask task)
    {
        if (task.NodeId.IsNullOrEmpty() || !task.NodeId.Contains('#')) return false;

        // 解析：{baseNode}#addsign#{baseTaskId} / {baseNode}#after#{baseTaskId}
        var parts = task.NodeId.Split('#');
        if (parts.Length < 3) return false;
        var baseNode = parts[0];
        var kind = parts[1];
        var baseTaskId = parts[2].ToLong();

        var baseTask = baseTaskId > 0 ? WorkflowTask.FindById(baseTaskId) : null;
        if (kind == "addsign")
        {
            // 前加签完成 → 恢复原任务
            if (baseTask != null)
            {
                baseTask.Visible = true;
                baseTask.Status = WorkflowStatuses.Active;
                baseTask.Update();
                NotifyTaskArrive(instance, baseTask);
            }
            return true;
        }
        if (kind == "after")
        {
            // 后加签完成 → 走原节点下游
            if (baseTask != null && !NodePassed(instance, graph, baseTask.NodeId))
            {
                // 若后加签不止一个，等全部完成再通过
                var pending = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.Status == WorkflowStatuses.Pending)
                    .Any(e => e.NodeId == task.NodeId);
                if (pending) return true;
            }
            var next = graph.NextOf(baseNode);
            CancelNodeOpen(instance.Id, baseNode);
            foreach (var nodeId in next)
            {
                Advance(instance, graph, nodeId, instance.TenantId, instance.StarterId, null, []);
            }
            return true;
        }
        return false;
    }

    /// <summary>节点是否通过（or 一人 / and quorum / sequence 全员）</summary>
    /// <param name="instance">实例</param>
    /// <param name="graph">图（取 quorum）</param>
    /// <param name="nodeId">节点 Id</param>
    /// <returns>是否通过</returns>
    static Boolean NodePassed(WorkflowInstance instance, WorkflowGraph graph, String nodeId)
    {
        var tasks = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == nodeId).ToList();
        if (tasks.Count == 0) return true;

        var mode = tasks.First().Mode;
        var done = tasks.Count(e => e.Status == WorkflowStatuses.Done);
        if (mode == "or") return done >= 1;
        if (mode == "sequence") return tasks.All(e => e.Status == WorkflowStatuses.Done);

        // and：quorum（默认全部；图节点 quorum 为空时按全部）
        var node = graph.Find(nodeId);
        var total = tasks.Count(e => e.Status != WorkflowStatuses.Cancelled);
        if (total == 0) return true;
        var q = node?.Quorum ?? 0;
        var need = q <= 0 ? total : q >= 1 ? (Int32)Math.Ceiling(q) : (Int32)Math.Ceiling(total * q);
        return done >= need;
    }

    /// <summary>推进到某节点（递归）：审批节点建任务；知会节点通知继续；网关分支；结束节点办结</summary>
    /// <param name="instance">实例</param>
    /// <param name="graph">图</param>
    /// <param name="nodeId">目标节点</param>
    /// <param name="tenantId">租户</param>
    /// <param name="starterId">发起人</param>
    /// <param name="starterName">发起人名</param>
    /// <param name="visited">防环</param>
    static void Advance(WorkflowInstance instance, WorkflowGraph graph, String nodeId, Int32 tenantId, Int32 starterId, String starterName, HashSet<String> visited)
    {
        if (nodeId.IsNullOrEmpty()) return;
        var node = graph.Find(nodeId);
        if (node == null) throw new WorkflowException(500, $"图数据损坏：节点[{nodeId}]不存在");

        if (!visited.Add(nodeId))
        {
            // 到过 end 或再次到达同一节点视为数据损坏（V1 无环）
            if (node.Type != WorkflowGraph.EndType) throw new WorkflowException(500, $"图数据损坏：存在环[{nodeId}]");
            return;
        }

        switch (node.Type)
        {
            case WorkflowGraph.ApproveType:
                // 同节点已有在办任务则不重复建
                var hasOpen = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id & WorkflowTask._.NodeId == nodeId)
                    .Any(e => e.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active);
                if (!hasOpen) CreateNodeTasks(instance, graph, node, tenantId, starterId, starterName);
                break;
            case WorkflowGraph.CcType:
                var ccIds = RecipientResolver.Resolve(node.To, tenantId, null);
                foreach (var id in ccIds)
                {
                    Notify(instance, id, "知会", $"{instance.TypePath} 流程知会", node.Name);
                }
                foreach (var next in graph.NextOf(nodeId)) Advance(instance, graph, next, tenantId, starterId, starterName, visited);
                break;
            case WorkflowGraph.XorType:
                var target = PickXor(instance, graph, node);
                foreach (var next in graph.NextOf(nodeId)) { if (next == target) Advance(instance, graph, next, tenantId, starterId, starterName, visited); }
                break;
            case WorkflowGraph.EndType:
                if (instance.Status == WorkflowStatuses.Running)
                {
                    CancelAllOpen(instance.Id);
                    instance.Status = WorkflowStatuses.Approved;
                    instance.FinishTime = DateTime.Now;
                    instance.Update();
                    NotifyStarter(instance, "审批通过", $"{instance.TypePath} 流程已通过");
                }
                break;
            default:
                throw new WorkflowException(500, $"图数据损坏：未知节点类型[{node.Type}]");
        }
    }

    /// <summary>为审批节点创建任务（或签单任务/会签逐人/依次签逐人）</summary>
    /// <param name="instance">实例</param>
    /// <param name="graph">图</param>
    /// <param name="node">审批节点</param>
    /// <param name="tenantId">租户</param>
    /// <param name="starterId">发起人</param>
    /// <param name="starterName">发起人名</param>
    static void CreateNodeTasks(WorkflowInstance instance, WorkflowGraph graph, WorkflowNode node, Int32 tenantId, Int32 starterId, String starterName)
    {
        var first = LoadFirstSubject(instance);
        var ids = RecipientResolver.Resolve(node.To, tenantId, first)?.ToList() ?? [];
        if (ids.Count == 0)
        {
            // 空候选人：不自动通过，记 error 并通知发起人
            AddComment(instance.Id, 0, starterId, starterName ?? starterId + "", "error", $"节点[{node.Name}]无候选人，等待人工处理");
            Notify(instance, starterId, "审批异常", $"{instance.TypePath} 节点[{node.Name}]无候选人", "请联系管理员处理");
            return;
        }

        var mode = node.Mode;
        var now = DateTime.Now;
        var due = node.TimeoutHours > 0 ? now.AddHours(node.TimeoutHours) : DateTime.MinValue;
        var tasks = new List<WorkflowTask>();

        if (mode == "or")
        {
            var t = NewTask(instance, node.Id, "or", ids.Count == 1 ? ids[0] : 0, ids, 0, true, due, node);
            tasks.Add(t);
        }
        else if (mode == "and")
        {
            var idx = 0;
            foreach (var id in ids)
            {
                tasks.Add(NewTask(instance, node.Id, "and", id, [id], idx++, true, due, node));
            }
        }
        else // sequence
        {
            var idx = 0;
            foreach (var id in ids)
            {
                tasks.Add(NewTask(instance, node.Id, "sequence", id, [id], idx, idx == 0, due, node));
                idx++;
            }
        }

        foreach (var t in tasks) t.Insert();

        // 通知可见候选人
        foreach (var t in tasks)
        {
            if (t.Visible) NotifyTaskArrive(instance, t);
        }
    }

    /// <summary>构造任务实体（未入库）</summary>
    /// <param name="instance">实例</param>
    /// <param name="nodeId">节点</param>
    /// <param name="mode">模式</param>
    /// <param name="assignee">办理人</param>
    /// <param name="candidates">候选人</param>
    /// <param name="seq">序号</param>
    /// <param name="visible">可见</param>
    /// <param name="due">截止</param>
    /// <param name="node">节点定义</param>
    /// <returns>任务</returns>
    static WorkflowTask NewTask(WorkflowInstance instance, String nodeId, String mode, Int32 assignee, List<Int32> candidates, Int32 seq, Boolean visible, DateTime due, WorkflowNode node)
    {
        return new WorkflowTask
        {
            InstanceId = instance.Id,
            NodeId = nodeId,
            Mode = mode,
            AssigneeId = assignee,
            CandidateJson = candidates.ToJson(),
            SequenceIndex = seq,
            Visible = visible,
            Status = WorkflowStatuses.Pending,
            DueTime = due,
            TimeoutAction = node?.TimeoutAction,
            TimeoutTransferTo = node?.TimeoutTransferTo?.ToJsonString(),
        };
    }

    /// <summary>网关分支选择：用第一条主体当前行按序 Match cases，全不命中走 default</summary>
    /// <param name="instance">实例</param>
    /// <param name="graph">图</param>
    /// <param name="node">网关节点</param>
    /// <returns>目标节点 Id</returns>
    static String PickXor(WorkflowInstance instance, WorkflowGraph graph, WorkflowNode node)
    {
        var entity = LoadFirstSubject(instance);
        foreach (var c in node.Cases)
        {
            if (c.Target.IsNullOrEmpty() || c.Filter == null) continue;
            var filter = AutomationFilter.ParseViewFilter(c.Filter.ToJsonString());
            if (entity != null && filter != null && AutomationFilter.Match(entity, filter))
                return c.Target;
        }
        if (!node.DefaultTarget.IsNullOrEmpty()) return node.DefaultTarget;
        throw new WorkflowException(500, $"网关[{node.Id}]无 default 且无命中，运行期视为数据损坏");
    }

    /// <summary>读取第一条主体的业务行（XOR/StartFilter 复读）</summary>
    /// <param name="instance">实例</param>
    /// <returns>实体行，缺失返回 null</returns>
    static IEntity LoadFirstSubject(WorkflowInstance instance)
    {
        var subject = WorkflowSubject.FindAll(WorkflowSubject._.InstanceId == instance.Id).OrderBy(e => e.Id).FirstOrDefault();
        if (subject == null) return null;
        try
        {
            var factory = ResolveFactory(subject.TypePath);
            var pk = factory.Table?.PrimaryKeys?.FirstOrDefault();
            return pk == null ? null : factory.FindByKey(subject.EntityKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>整单结束（驳回/撤回/作废/自然办结）：取消未完成任务并通知</summary>
    /// <param name="instance">实例</param>
    /// <param name="status">终态</param>
    /// <param name="userId">操作人</param>
    /// <param name="action">意见动作</param>
    /// <param name="comment">意见</param>
    static void FinishInstance(WorkflowInstance instance, String status, Int32 userId, String action, String comment)
    {
        if (instance.Status != WorkflowStatuses.Running) throw new WorkflowException(409, "流程已结束");

        CancelAllOpen(instance.Id);
        instance.Status = status;
        instance.FinishTime = DateTime.Now;
        instance.Update();
        if (!action.IsNullOrEmpty()) AddComment(instance.Id, 0, userId, User.FindByID(userId)?.Name ?? userId + "", action, comment);

        var text = status switch
        {
            WorkflowStatuses.Rejected => "审批被驳回",
            WorkflowStatuses.Withdrawn => "流程已撤回",
            WorkflowStatuses.Cancelled => "流程已作废",
            _ => "流程结束",
        };
        NotifyStarter(instance, text, $"{instance.TypePath} {text}");
    }

    /// <summary>取消实例全部未完成任务</summary>
    /// <param name="instanceId">实例编号</param>
    static void CancelAllOpen(Int64 instanceId)
    {
        var open = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instanceId)
            .Where(e => e.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active)
            .ToList();
        foreach (var t in open)
        {
            t.Status = WorkflowStatuses.Cancelled;
            t.FinishTime = DateTime.Now;
            t.Update();
        }
    }

    /// <summary>取消指定节点未完成任务</summary>
    /// <param name="instanceId">实例编号</param>
    /// <param name="nodeId">节点</param>
    static void CancelNodeOpen(Int64 instanceId, String nodeId)
    {
        var open = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instanceId & WorkflowTask._.NodeId == nodeId)
            .Where(e => e.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active)
            .ToList();
        foreach (var t in open)
        {
            t.Status = WorkflowStatuses.Cancelled;
            t.FinishTime = DateTime.Now;
            t.Update();
        }
    }
    #endregion

    #region 校验
    /// <summary>加载在途实例</summary>
    /// <param name="instanceId">实例编号</param>
    /// <returns>实例</returns>
    static WorkflowInstance LoadRunning(Int64 instanceId)
    {
        var instance = WorkflowInstance.FindById(instanceId) ?? throw new WorkflowException(404, "流程实例不存在");
        if (instance.Status != WorkflowStatuses.Running) throw new WorkflowException(409, "流程已结束");
        return instance;
    }

    /// <summary>加载开放任务并校验候选人</summary>
    /// <param name="taskId">任务编号</param>
    /// <param name="userId">操作人</param>
    /// <param name="needCandidate">是否需要候选人</param>
    /// <returns>任务</returns>
    static WorkflowTask LoadOpenTask(Int64 taskId, Int32 userId, Boolean needCandidate)
    {
        var task = WorkflowTask.FindById(taskId) ?? throw new WorkflowException(404, "任务不存在");
        if (task.Status is not (WorkflowStatuses.Pending or WorkflowStatuses.Active))
            throw new WorkflowException(409, "任务已结束");
        if (!task.Visible) throw new WorkflowException(409, "尚未轮到该任务");
        if (needCandidate) EnsureCandidate(task, userId);
        return task;
    }

    /// <summary>候选人校验：AssigneeId 或 CandidateJson 包含</summary>
    /// <param name="task">任务</param>
    /// <param name="userId">操作人</param>
    static void EnsureCandidate(WorkflowTask task, Int32 userId)
    {
        if (task.AssigneeId == userId) return;
        var cands = WorkflowHelper.ReadIntArray(JsonNode.Parse(task.CandidateJson) as JsonArray);
        if (cands.Contains(userId)) return;
        throw new WorkflowException(403, "非候选人不能操作该任务");
    }

    /// <summary>解析接收人（候选 + 单用户约束）</summary>
    /// <param name="to">to JSON</param>
    /// <param name="task">任务（冗余上下文）</param>
    /// <param name="entity">mode=field 行</param>
    /// <param name="single">是否单用户（users 仅 1 个）</param>
    /// <returns>用户 Id</returns>
    static List<Int32> ResolveIds(JsonObject to, WorkflowTask task, IEntity entity, out Boolean single)
    {
        var tenant = WorkflowInstance.FindById(task.InstanceId)?.TenantId ?? 0;
        var ids = RecipientResolver.Resolve(to, tenant, entity).ToList();
        var users = WorkflowHelper.ReadIntArray(to?["users"]);
        single = users.Count == 1 && ids.Count == 1;
        return ids;
    }

    /// <summary>解析实体工厂（typePath → 页面注册表反查）</summary>
    /// <param name="typePath">实体路径</param>
    /// <returns>工厂</returns>
    public static IEntityFactory ResolveFactory(String typePath)
    {
        var np = AutomationPaths.NormalizeTypePath(typePath);
        if (np.IsNullOrEmpty()) throw new WorkflowException(400, "实体路径为空");

        foreach (var kv in EntityPageRegistry.GetAll())
        {
            if (AutomationPaths.NormalizeTypePath(kv.Value.Url) == np)
                return EntityFactory.CreateFactory(kv.Key);
        }
        // 兼容：typePath == 实体类型名
        var type = np.Split('/').Last();
        var entityType = typeof(WorkflowEngine).Assembly.GetTypes().FirstOrDefault(e =>
            e.IsClass && !e.IsAbstract && typeof(IEntity).IsAssignableFrom(e) && e.Name.EqualIgnoreCase(type));
        if (entityType != null) return EntityFactory.CreateFactory(entityType);

        throw new WorkflowException(400, $"无法解析实体路径[{np}]，请确认该实体有管理页面");
    }

    /// <summary>主键归一化为可存储文本（数值 InvariantCulture、字符串 trim）</summary>
    /// <param name="key">原始值</param>
    /// <param name="field">主键字段</param>
    /// <returns>归一化文本</returns>
    static String NormalizeKeyText(String key, XCode.Configuration.FieldItem field)
    {
        var obj = NormalizeKey(key, field);
        if (obj == null) return "";
        if (obj is String s) return s.Trim();
        if (obj is Guid g) return g.ToString("N");
        return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>主键归一化（与 subject.EntityKey 同一格式）</summary>
    /// <param name="key">原始值</param>
    /// <param name="field">主键字段</param>
    /// <returns>归一化值</returns>
    static Object NormalizeKey(String key, XCode.Configuration.FieldItem field)
    {
        var type = field?.Type ?? typeof(Object);
        if (type == typeof(Int64)) return key.ToLong();
        if (type == typeof(Int32)) return key.ToInt();
        if (type == typeof(Int16)) return key.ToInt();
        if (type == typeof(Guid)) return Guid.TryParse(key, out var g) ? g : key;
        return key?.Trim();
    }

    /// <summary>解析实例快照图</summary>
    /// <param name="instance">实例</param>
    /// <returns>图</returns>
    static WorkflowGraph ParseSnapshot(WorkflowInstance instance)
    {
        var graph = WorkflowGraph.Parse(instance.GraphSnapshot);
        if (graph == null) throw new WorkflowException(500, "实例图快照损坏");
        return graph;
    }

    /// <summary>复制任务（转办/加签/超时转交）</summary>
    /// <param name="src">源任务</param>
    /// <returns>新任务</returns>
    static WorkflowTask CloneTask(WorkflowTask src) => new()
    {
        InstanceId = src.InstanceId,
        NodeId = src.NodeId,
        Mode = src.Mode,
        CandidateJson = src.CandidateJson,
        SequenceIndex = src.SequenceIndex,
        Visible = src.Visible,
        Status = src.Status,
        DueTime = src.DueTime,
        TimeoutAction = src.TimeoutAction,
        TimeoutTransferTo = src.TimeoutTransferTo,
    };
    #endregion

    #region 意见与通知
    /// <summary>写意见</summary>
    /// <param name="instanceId">实例</param>
    /// <param name="taskId">任务</param>
    /// <param name="userId">用户</param>
    /// <param name="userName">用户名</param>
    /// <param name="action">动作</param>
    /// <param name="content">内容</param>
    static void AddComment(Int64 instanceId, Int64 taskId, Int32 userId, String userName, String action, String content)
    {
        var comment = new WorkflowComment
        {
            InstanceId = instanceId,
            TaskId = taskId,
            Action = action?.Cut(16),
            Content = content?.Cut(1000),
        };
        // 触发拦截器写入创建人
        comment.Insert();
    }

    /// <summary>任务到达通知（待办候选）</summary>
    /// <param name="instance">实例</param>
    /// <param name="task">任务</param>
    static void NotifyTaskArrive(WorkflowInstance instance, WorkflowTask task)
    {
        var ids = task.AssigneeId > 0 ? [task.AssigneeId] : WorkflowHelper.ReadIntArray(JsonNode.Parse(task.CandidateJson) as JsonArray);
        foreach (var id in ids)
        {
            Notify(instance, id, "待办", BuildTitle(instance, task), $"请处理节点[{task.NodeId}]");
        }
    }

    /// <summary>通知发起人</summary>
    /// <param name="instance">实例</param>
    /// <param name="action">动作</param>
    /// <param name="body">正文</param>
    static void NotifyStarter(WorkflowInstance instance, String action, String body)
        => Notify(instance, instance.StarterId, action, $"{instance.TypePath} 流程", body);

    /// <summary>构造待办标题</summary>
    /// <param name="instance">实例</param>
    /// <param name="task">任务</param>
    /// <returns>标题</returns>
    static String BuildTitle(WorkflowInstance instance, WorkflowTask task)
    {
        var subject = WorkflowSubject.FindAll(WorkflowSubject._.InstanceId == instance.Id).OrderBy(e => e.Id).FirstOrDefault();
        return subject?.Title ?? instance.TypePath;
    }

    /// <summary>写站内信通知</summary>
    /// <param name="instance">实例</param>
    /// <param name="userId">用户</param>
    /// <param name="action">动作</param>
    /// <param name="title">标题</param>
    /// <param name="body">内容</param>
    static void Notify(WorkflowInstance instance, Int32 userId, String action, String title, String body)
    {
        if (userId <= 0) return;
        var rec = new NotificationRecord
        {
            TenantId = instance.TenantId,
            Action = "Workflow",
            Channel = "InApp",
            UserId = userId,
            Title = title?.Cut(200),
            Content = body?.Cut(2000),
            Success = true,
        };
        try { rec.Insert(); }
        catch (Exception ex) { XTrace.WriteException(ex); }
    }
    #endregion
}
