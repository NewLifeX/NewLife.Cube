using NewLife.AI.Models;

namespace NewLife.Cube.AI;

/// <summary>魔方 AI 对话请求。继承 NAI 通用请求（SessionId/Message/Think/Stream），
/// 附加魔方页面上下文字段（目标页面/记录/查询条件），供全局 <see cref="Controllers.AiController"/> 解析目标控制器使用</summary>
public class CubeAiChatRequest : AiChatRequest
{
    /// <summary>页面类型：list / form / detail</summary>
    public String? Page { get; set; }

    /// <summary>表单模式：add / edit</summary>
    public String? Mode { get; set; }

    /// <summary>记录编号</summary>
    public Int64 Id { get; set; }

    /// <summary>查询条件 Base64（_query）</summary>
    public String? Query { get; set; }

    /// <summary>目标页面区域。如 Admin；非区域页面为空</summary>
    public String? Area { get; set; }

    /// <summary>目标页面控制器名。如 User，由前端从路由注入，全局 AiController 据此解析目标控制器</summary>
    public String? Controller { get; set; }

    /// <summary>目标页面路径。如 /Admin/UserStat；由前端从 location.pathname 注入，后端按用户+页面作用域隔离会话</summary>
    public String? Url { get; set; }
}

/// <summary>浏览器操作回传结果。前端执行 run_js 脚本后 POST 到全局 AiController.OperationResult 端点</summary>
public class BrowserOpResult
{
    /// <summary>检查点编号。与 SSE run_js 事件中的 checkpointId 对应</summary>
    public String? CheckpointId { get; set; }

    /// <summary>前端回传的结果 JSON（<c>{ok,value,error}</c>）</summary>
    public String? Result { get; set; }
}
