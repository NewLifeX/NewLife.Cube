namespace NewLife.Cube.AI;

/// <summary>AI 对话请求</summary>
public class AiChatRequest
{
    /// <summary>会话编号。前端生成并持久化到本地存储</summary>
    public String? SessionId { get; set; }

    /// <summary>用户消息</summary>
    public String? Message { get; set; }

    /// <summary>页面类型：list / form / detail</summary>
    public String? Page { get; set; }

    /// <summary>表单模式：add / edit</summary>
    public String? Mode { get; set; }

    /// <summary>记录编号</summary>
    public Int64 Id { get; set; }

    /// <summary>查询条件 Base64（_query）</summary>
    public String? Query { get; set; }

    /// <summary>是否深度推理</summary>
    public Boolean Think { get; set; }

    /// <summary>是否流式输出（SSE）。默认 true</summary>
    public Boolean Stream { get; set; } = true;
}
