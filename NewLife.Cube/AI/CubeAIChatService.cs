using NewLife.AI.Models;
using NewLife.AI.Tools;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Serialization;
using XCode.Membership;
using ILog = NewLife.Log.ILog;

namespace NewLife.Cube.AI;

/// <summary>AI 对话服务实现。负责 AI 助手对话的会话管理与 LLM 调用编排，产出 SSE 事件 JSON</summary>
/// <remarks>实例化 AI 对话服务</remarks>
/// <param name="ai">底层 AI 服务（含工具调用能力）</param>
/// <param name="setting">系统配置</param>
/// <param name="services">服务提供者，用于解析检查点服务等</param>
public class CubeAIChatService(IAIService ai, CubeSetting setting, IServiceProvider services) : IAIChatService
{
    #region 方法
    /// <summary>执行 AI 对话（含工具调用）。管理会话历史，流式/非流式调用 LLM，产出 SSE 事件 JSON</summary>
    /// <param name="req">对话请求（含会话编号、消息、页面上下文）</param>
    /// <param name="systemPrompt">系统提示词（注入页面上下文），由宿主构建以便保留子类重载</param>
    /// <param name="providers">工具提供者列表（按工具名路由），由宿主构建</param>
    /// <param name="writeEvent">事件回调，收到 SSE JSON 字符串（不含 <c>data: </c> 前缀）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ChatAsync(AiChatRequest req, String systemPrompt, IList<IToolProvider> providers, Func<String, Task> writeEvent, CancellationToken cancellationToken = default)
    {
        // AISwitch 检查：统一由服务保障，任何宿主（实体控制器/全局控制器/Vue）都无需重复检查
        if (!setting.AISwitch)
        {
            await writeEvent(new { type = "error", message = "AI 未启用，请联系系统管理员开启 AISwitch" }.ToJson());
            return;
        }

        // 会话历史（内存缓存，限 30 条）
        var sessionKey = $"CubeAI_Session_{req.SessionId}";
        var history = MemoryCache.Instance.Get<IList<ChatMessage>>(sessionKey) ?? [];
        history.Add(new ChatMessage { Role = "user", Content = req.Message });

        // 组装消息：system + 历史
        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = systemPrompt }
        };
        messages.AddRange(history.TakeLast(30));

        var think = req.Think || setting.AIDefaultThink;
        var options = new ChatOptions
        {
            EnableThinking = think,
            Temperature = think ? 0.5 : 0.3,
        };

        // 注入浏览器通道上下文：工具经 ToolCallContext.Items 读取（SSE 写回调/检查点服务/用户编号），实现工具与宿主的松耦合
        var cp = services.GetService<PageCheckpointService>() ?? PageCheckpointService.Instance;
        var uid = ManageProvider.User?.ID ?? 0;
        options.Items[CubeBrowserContext.BrowserContextKey] = new CubeBrowserContext
        {
            Writer = writeEvent,
            CheckpointService = cp,
            UserId = uid,
            TimeoutSeconds = 30,
        };

        await writeEvent(new { type = "meta", model = setting.AIModel, thinking = think }.ToJson());

        var sb = Pool.StringBuilder.Get();
        var hasText = false;
        var hasError = false;
        try
        {
            if (!req.Stream)
            {
                // 非流式：一次返回完整响应（含工具调用事件与文本）
                var response = await ai.ChatAgentAsync(messages, providers, options, cancellationToken);
                if (response is ChatResponse cr && cr.ToolCallEvents != null)
                {
                    foreach (var ev in cr.ToolCallEvents)
                    {
                        await writeEvent(new { type = "tool", @event = ev.Type, id = ev.ToolCallId, name = ev.Name, value = ev.Value }.ToJson());
                    }
                }
                var text = response?.Text;
                if (!text.IsNullOrEmpty())
                {
                    hasText = true;
                    sb.Append(text);
                    await writeEvent(new { type = "text", content = text }.ToJson());
                }
            }
            else
            {
                await foreach (var chunk in ai.ChatAgentStreamAsync(messages, providers, options, cancellationToken))
                {
                    // 文本增量
                    var text = chunk?.Text;
                    if (!text.IsNullOrEmpty())
                    {
                        hasText = true;
                        sb.Append(text);
                        await writeEvent(new { type = "text", content = text }.ToJson());
                    }

                    // 工具调用事件
                    if (chunk is ChatResponse cr && cr.ToolCallEvents != null)
                    {
                        foreach (var ev in cr.ToolCallEvents)
                        {
                            await writeEvent(new { type = "tool", @event = ev.Type, id = ev.ToolCallId, name = ev.Name, value = ev.Value }.ToJson());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            hasError = true;
            WriteLog("AI 对话失败", ex.ToString());
            await writeEvent(new { type = "error", message = ex.Message }.ToJson());
        }

        // 空响应兜底：工具回合未完成（常见于服务商不支持函数调用），给出提示而非静默结束
        if (!hasText && !hasError)
        {
            var note = "⚠️ AI 未返回有效结果。若需要数据分析/填表等工具能力，请确认 AI 服务商支持函数调用（如 DeepSeek/OpenAI/Ollama 工具模型）。";
            await writeEvent(new { type = "text", content = $"\n\n> {note}" }.ToJson());
        }

        // 保存助手回复到会话历史
        var reply = sb.Return(true);
        if (!reply.IsNullOrEmpty())
        {
            history.Add(new ChatMessage { Role = "assistant", Content = reply });
            MemoryCache.Instance.Set(sessionKey, history, 3600);
        }

        await writeEvent(new { type = "done" }.ToJson());
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="action"></param>
    /// <param name="message"></param>
    private void WriteLog(String action, String message) => Log.Info("[AI] {0} {1}", action, message);
    #endregion
}
