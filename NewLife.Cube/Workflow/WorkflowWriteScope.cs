namespace NewLife.Cube.Workflow;

/// <summary>审批写通道。审批人在节点 writable 字段内改业务行的作用域标记（AsyncLocal）</summary>
/// <remarks>
/// WorkflowWriteInterceptor 校验普通 Update/Delete 时，若本 Scope 激活且脏字段 ⊆ 节点可写字段则放行。
/// 仅在 WorkflowEngine 内部使用；禁止业务代码直接 Enter。
/// </remarks>
public class WorkflowWriteScope : IDisposable
{
    private static readonly AsyncLocal<WorkflowWriteScope> _current = new();

    /// <summary>当前作用域</summary>
    public static WorkflowWriteScope Current => _current.Value;

    /// <summary>是否处于审批写通道</summary>
    public static Boolean IsActive => _current.Value != null;

    /// <summary>流程实例编号</summary>
    public Int64 InstanceId { get; }

    /// <summary>节点编号（可为空表示任意节点）</summary>
    public String NodeId { get; }

    /// <summary>允许写入的字段名集合。空=不限（审批通道全字段放行由调用方另行约束）</summary>
    public HashSet<String> FieldNames { get; }

    private WorkflowWriteScope(Int64 instanceId, String nodeId, IEnumerable<String> fieldNames)
    {
        InstanceId = instanceId;
        NodeId = nodeId;
        FieldNames = new HashSet<String>(fieldNames ?? [], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>进入审批写通道。原作用域存在时合并字段（嵌套场景少见）</summary>
    /// <param name="instanceId">流程实例编号</param>
    /// <param name="nodeId">节点编号，可空</param>
    /// <param name="fieldNames">允许字段名，可空表示不限</param>
    /// <returns>可释放作用域</returns>
    public static WorkflowWriteScope Enter(Int64 instanceId, String nodeId, IEnumerable<String> fieldNames = null)
    {
        var scope = new WorkflowWriteScope(instanceId, nodeId, fieldNames);
        _current.Value = scope;
        return scope;
    }

    /// <summary>释放作用域</summary>
    public void Dispose() => _current.Value = null;
}
