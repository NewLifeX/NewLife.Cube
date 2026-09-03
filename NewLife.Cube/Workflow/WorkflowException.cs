namespace NewLife.Cube.Workflow;

/// <summary>工作流业务异常。Code 与 HTTP 语义一致：400 校验、403 越权、404 无模块/无记录、409 冲突</summary>
public class WorkflowException : Exception
{
    /// <summary>业务错误码</summary>
    public Int32 Code { get; }

    /// <summary>实例化</summary>
    /// <param name="code">业务错误码</param>
    /// <param name="message">错误消息</param>
    public WorkflowException(Int32 code, String message) : base(message) => Code = code;
}
