using NewLife.AI.Models;
using NewLife.AI.Tools;

namespace NewLife.Cube.AI;

/// <summary>AI 对话服务接口。统一封装 AI 助手对话：会话管理、工具循环、SSE 事件输出，
/// 供实体控制器薄壳 / 全局控制器 / 各前端宿主复用</summary>
public interface IAIChatService
{
    /// <summary>执行 AI 对话（含工具调用）。管理会话历史，流式/非流式调用 LLM，产出 SSE JSON 事件</summary>
    /// <remarks>
    /// 事件为 OpenAI SSE 兼容的 JSON 字符串（不含 <c>data: </c> 前缀与换行），由宿主负责写响应：
    /// <list type="bullet">
    /// <item><c>{"type":"meta","model":...,"thinking":...}</c> — 元数据</item>
    /// <item><c>{"type":"text","content":"..."}</c> — 文本增量</item>
    /// <item><c>{"type":"tool","event":"start|done|error","id":"...","name":"...","value":"..."}</c> — 工具调用</item>
    /// <item><c>{"type":"error","message":"..."}</c> — 错误</item>
    /// <item><c>{"type":"done"}</c> — 结束</item>
    /// </list>
    /// </remarks>
    /// <param name="req">对话请求（含会话编号、消息、页面上下文）</param>
    /// <param name="systemPrompt">系统提示词（注入页面上下文），由宿主构建以便保留子类重载</param>
    /// <param name="providers">工具提供者列表（按工具名路由），由宿主构建（如 CubeTools + 内置工具）</param>
    /// <param name="writeEvent">事件回调，收到 SSE JSON 字符串（不含 <c>data: </c> 前缀）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ChatAsync(AiChatRequest req, String systemPrompt, IList<IToolProvider> providers, Func<String, Task> writeEvent, CancellationToken cancellationToken = default);
}
