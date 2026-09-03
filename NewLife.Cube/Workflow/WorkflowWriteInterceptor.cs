using System.Text.Json.Nodes;
using NewLife.Cube.Automation;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode;
using XCode.Membership;
using WorkflowComment = NewLife.Cube.Workflow.Entity.WorkflowComment;
using WorkflowDefinition = NewLife.Cube.Workflow.Entity.WorkflowDefinition;
using WorkflowInstance = NewLife.Cube.Workflow.Entity.WorkflowInstance;
using WorkflowSubject = NewLife.Cube.Workflow.Entity.WorkflowSubject;
using WorkflowTask = NewLife.Cube.Workflow.Entity.WorkflowTask;

namespace NewLife.Cube.Workflow;

/// <summary>审批写锁拦截器。审批中按 LockPolicy 拒绝普通 Update/Delete（design §6.2）</summary>
/// <remarks>
/// 注册于 EntityInterceptors.Global（WebAPI 核心库宿主经 WorkflowHost.Register 挂载）。
/// 只对存在「在途主体」的业务行生效；流程自有表/自动化表/附件/日志/参数/通知一律跳过。
/// 写锁矩阵：
/// - full：审批中普通 Update/Delete 拒绝；流程通道（WorkflowWriteScope）内且脏字段 ⊆ 节点可写字段放行
/// - nodeFields：普通 Update 仅当前节点候选人且脏字段 ⊆ 节点可写字段放行；Delete 拒绝
/// </remarks>
public class WorkflowWriteInterceptor : EntityInterceptor
{
    /// <summary>跳过实体：流程自有表 / 自动化相关 / 通知 / 附件 / 日志 / 参数 / 定时作业等系统表</summary>
    static readonly HashSet<String> _skip = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(WorkflowDefinition), nameof(WorkflowInstance), nameof(WorkflowSubject), nameof(WorkflowTask), nameof(WorkflowComment),
        nameof(EntityAutomation), "AutomationRun", "AutomationFlowLog", nameof(NotificationRecord),
        "Attachment", "Log", "Parameter", "CronJob", nameof(EntityComment), "AppLog", "UserOnline", "UserToken",
    };

    /// <summary>初始化。全局挂载，一律支持；具体类型在 Valid 内快速跳过</summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否支持</returns>
    protected override Boolean OnInit(Type entityType) => true;

    /// <summary>验证写入。仅 Update/Delete 需要评估审批锁</summary>
    /// <param name="entity">实体</param>
    /// <param name="method">数据方法</param>
    /// <returns>是否放行</returns>
    protected override Boolean OnValid(IEntity entity, DataMethod method)
    {
        if (method is not (DataMethod.Update or DataMethod.Delete)) return true;
        var type = entity.GetType();
        if (type == null || _skip.Contains(type.Name)) return true;
        // 无脏数据（如心跳更新）不拦
        if (method == DataMethod.Update && !entity.HasDirty) return true;

        try
        {
            return CheckLock(entity, method, type);
        }
        catch (WorkflowException)
        {
            // 业务拦截按原样抛出，交由上层呈现（如“审批中不可修改”）
            throw;
        }
        catch (Exception ex)
        {
            // Workflow 连接未初始化/表不存在等一律放行，避免影响非工作流宿主
            XTrace.WriteLine("WorkflowWriteInterceptor 检查跳过：{0}", ex.Message);
            return true;
        }
    }

    /// <summary>评估写锁</summary>
    /// <param name="entity">实体</param>
    /// <param name="method">数据方法</param>
    /// <param name="type">实体类型</param>
    /// <returns>是否放行</returns>
    static Boolean CheckLock(IEntity entity, DataMethod method, Type type)
    {
        var typePath = AutomationPaths.ResolveTypePath(type);
        if (typePath.IsNullOrEmpty()) return true;

        var key = AutomationPaths.RecordKey(entity);
        if (key.IsNullOrEmpty()) return true;

        var subject = WorkflowSubject.FindRunning(typePath, key);
        if (subject == null) return true;

        var instance = WorkflowInstance.FindById(subject.InstanceId);
        if (instance == null || instance.Status != WorkflowStatuses.Running) return true;

        var def = WorkflowDefinition.FindById(instance.DefinitionId);
        var policy = def?.LockPolicy ?? WorkflowStatuses.LockFull;
        var dirty = Dirtys(entity);

        // 流程写通道：仅允许对应实例且脏字段 ⊆ 通道字段
        var scope = WorkflowWriteScope.Current;
        if (scope != null)
        {
            if (scope.InstanceId != instance.Id) throw new WorkflowException(403, "审批写通道与实例不匹配");
            if (method == DataMethod.Delete) throw new WorkflowException(403, "审批中禁止删除");
            if (scope.FieldNames.Count > 0 && dirty.Any(e => !scope.FieldNames.Contains(e)))
                throw new WorkflowException(403, $"审批中仅允许修改节点可写字段：[{String.Join(",", scope.FieldNames)}]");
            return true;
        }

        if (method == DataMethod.Delete) throw new WorkflowException(403, "审批中禁止删除，请先结束流程");
        if (policy == WorkflowStatuses.LockFull)
            throw new WorkflowException(403, "审批中不可修改，请通过审批流程处理");

        // nodeFields：仅当前节点候选人可按节点可写字段改
        var userId = ManageProvider.User?.ID ?? 0;
        if (userId <= 0) throw new WorkflowException(403, "审批中不可修改");

        var writable = CurrentWritable(instance, userId);
        if (writable.Count == 0) throw new WorkflowException(403, "当前用户不是节点候选人，审批中不可修改");
        if (dirty.Any(e => !writable.Contains(e)))
            throw new WorkflowException(403, $"审批中仅允许修改节点可写字段：[{String.Join(",", writable)}]");
        return true;
    }

    /// <summary>当前用户在该实例可见任务上的节点可写字段并集（nodeFields）</summary>
    /// <param name="instance">实例</param>
    /// <param name="userId">用户</param>
    /// <returns>字段名集合</returns>
    static HashSet<String> CurrentWritable(WorkflowInstance instance, Int32 userId)
    {
        var set = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        var tasks = WorkflowTask.FindAll(WorkflowTask._.InstanceId == instance.Id)
            .Where(e => e.Status is WorkflowStatuses.Pending or WorkflowStatuses.Active && e.Visible)
            .ToList();
        var graph = WorkflowGraph.Parse(instance.GraphSnapshot);
        foreach (var task in tasks)
        {
            var ok = task.AssigneeId == userId;
            if (!ok)
            {
                var cands = WorkflowHelper.ReadIntArray(System.Text.Json.Nodes.JsonNode.Parse(task.CandidateJson) as JsonArray);
                ok = cands.Contains(userId);
            }
            if (!ok) continue;
            var node = graph?.Find(task.NodeId);
            if (node == null) continue;
            foreach (var f in node.WritableFields)
            {
                if (!f.IsNullOrEmpty()) set.Add(f);
            }
        }
        return set;
    }

    /// <summary>脏字段名</summary>
    /// <param name="entity">实体</param>
    /// <returns>字段名</returns>
    static List<String> Dirtys(IEntity entity)
    {
        try
        {
            return entity.Dirtys.ToList();
        }
        catch
        {
            // 个别实体 Dirtys 不可用
            return [];
        }
    }
}
