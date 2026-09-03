namespace NewLife.Cube.Workflow;

/// <summary>审批流程状态与锁策略常量</summary>
public static class WorkflowStatuses
{
    /// <summary>实例运行中</summary>
    public const String Running = "Running";

    /// <summary>实例已通过</summary>
    public const String Approved = "Approved";

    /// <summary>实例已驳回</summary>
    public const String Rejected = "Rejected";

    /// <summary>实例已撤回</summary>
    public const String Withdrawn = "Withdrawn";

    /// <summary>实例已取消</summary>
    public const String Cancelled = "Cancelled";

    /// <summary>任务待认领/待轮到</summary>
    public const String Pending = "Pending";

    /// <summary>任务进行中</summary>
    public const String Active = "Active";

    /// <summary>任务已完成</summary>
    public const String Done = "Done";

    /// <summary>任务已转交</summary>
    public const String Transferred = "Transferred";

    /// <summary>全字段锁定</summary>
    public const String LockFull = "full";

    /// <summary>仅锁定节点字段</summary>
    public const String LockNodeFields = "nodeFields";
}
