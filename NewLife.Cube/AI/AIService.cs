using System.Runtime.CompilerServices;
using NewLife.AI.Clients;
using NewLife.AI.Clients.OpenAI;
using NewLife.AI.Models;
using NewLife.AI.Tools;
using NewLife.Log;

namespace NewLife.Cube.AI;

/// <summary>AI 服务实现。根据配置创建对应 IChatClient，提供简单 Prompt+Data→LLM 调用</summary>
public class AIService : IAIService
{
    #region 属性
    private readonly CubeSetting _setting;
    private IChatClient _client;
    private String _lastProvider;
    private String _lastModel;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="setting"></param>
    public AIService(CubeSetting setting)
    {
        _setting = setting;
    }
    #endregion

    #region 方法
    /// <summary>用户交互场景的快速选项：关闭深度推理、低温度</summary>
    private static readonly ChatOptions _fastOptions = new()
    {
        EnableThinking = false,
        Temperature = 0.3,
    };

    /// <summary>深度分析选项：开启深度推理、适中温度，适合复杂数据洞察场景</summary>
    private static readonly ChatOptions _deepOptions = new()
    {
        EnableThinking = true,
        Temperature = 0.5,
    };

    /// <summary>获取或创建客户端，按需延迟初始化</summary>
    private IChatClient GetClient()
    {
        var provider = _setting.AIProvider;
        var model = _setting.AIModel;
        var endpoint = _setting.AIEndpoint;
        var apiKey = _setting.AIApiKey;

        // 配置未变则复用
        if (_client != null && _lastProvider == provider && _lastModel == model)
            return _client;

        _client = CreateClient(provider, apiKey, model, endpoint);
        _lastProvider = provider;
        _lastModel = model;

        return _client;
    }

    /// <summary>根据配置创建对应的 AI 客户端</summary>
    /// <remarks>
    /// 优先从 <see cref="AiClientRegistry"/> 查找已注册服务商（OpenAI / DeepSeek / DashScope 等），
    /// 未注册的服务商（如 NewLife 自定义网关）作为 OpenAI 兼容协议处理。
    /// </remarks>
    private static IChatClient CreateClient(String provider, String apiKey, String model, String endpoint)
    {
        if (provider.IsNullOrEmpty()) provider = "NewLifeAI";

        // 已注册的服务商走注册表工厂
        if (AiClientRegistry.Default.GetDescriptor(provider) != null)
            return AiClientRegistry.Default.CreateClient(provider, apiKey, model, endpoint);

        // 未注册的服务商作为 OpenAI 兼容协议处理
        return new OpenAIChatClient(apiKey, model, endpoint);
    }

    /// <summary>通用 AI 对话（后台任务），保留模型默认推理行为，适合 CronJob 等不赶时间的场景</summary>
    public Task<String> ChatAsync(String prompt, String data, CancellationToken cancellationToken = default)
        => ChatInternalAsync(prompt, data, null, cancellationToken);

    /// <summary>润色通知内容（用户交互场景，快速）</summary>
    public Task<String> PolishNotificationAsync(String title, String content, String style, CancellationToken cancellationToken = default)
    {
        var prompt = $@"你是文案专家。请将以下通知改写为{style}风格，保持原意不变，直接输出改写后的内容（不要加解释）：

标题：{title}
内容：{content}";

        return ChatFastAsync(prompt, String.Empty, cancellationToken);
    }

    /// <summary>系统健康诊断（用户交互场景，快速）</summary>
    public Task<String> DiagnoseSystemAsync(String sysInfoJson, CancellationToken cancellationToken = default)
        => ChatFastAsync(BuildDiagnosePrompt(), sysInfoJson, cancellationToken);

    /// <summary>系统健康诊断（流式输出）。逐块返回生成内容</summary>
    public async IAsyncEnumerable<String> DiagnoseSystemStreamAsync(String sysInfoJson, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_setting.AISwitch)
        {
            yield return "AI 未启用，请在系统设置中开启 AISwitch";
            yield break;
        }

        var error = default(String);
        IChatClient? client = null;

        try
        {
            client = GetClient();
            WriteLog("DiagnoseSystemStream 开始", sysInfoJson[..Math.Min(sysInfoJson.Length, 200)]);
        }
        catch (Exception ex)
        {
            WriteLog("DiagnoseSystemStream 失败", ex.ToString());
            error = $"\n\n---\n>  AI 调用失败：{ex.Message}";
        }

        if (error != null)
        {
            yield return error;
            yield break;
        }

        // 与 ChatInternalAsync 一致：提示词 + 数据拼接到一次对话
        var content = BuildDiagnosePrompt();
        if (!sysInfoJson.IsNullOrEmpty())
            content = $"{content}\n\n数据：\n{sysInfoJson}";

        await foreach (var chunk in client!.GetStreamingResponseAsync(content, _fastOptions, cancellationToken))
        {
            if (chunk?.Text != null)
                yield return chunk.Text;
        }

        WriteLog("DiagnoseSystemStream 完成", "");
    }

    /// <summary>构建系统诊断提示词</summary>
    private static String BuildDiagnosePrompt() => @"你是系统运维专家。根据以下系统运行指标，给出健康诊断报告（中文）：
分析要点：是否存在瓶颈、是否需要扩容、是否需要关注的风险点。
直接输出诊断报告，不要加无关解释。";

    /// <summary>数据分析洞察。根据上下文数据生成分析报告</summary>
    public async Task<String> AnalyzeDataAsync(String prompt, Boolean think = false, CancellationToken cancellationToken = default)
    {
        var options = think ? _deepOptions : _fastOptions;
        return await ChatInternalAsync(prompt, null, options, cancellationToken);
    }

    /// <summary>数据分析洞察（流式输出）。逐块返回生成内容</summary>
    public async IAsyncEnumerable<String> AnalyzeDataStreamAsync(String prompt, Boolean think = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_setting.AISwitch)
        {
            yield return "AI 未启用，请在系统设置中开启 AISwitch";
            yield break;
        }

        var options = think ? _deepOptions : _fastOptions;
        var error = default(String);
        IChatClient? client = null;

        try
        {
            client = GetClient();
            WriteLog("AnalyzeDataStream 开始", prompt[..Math.Min(prompt.Length, 200)]);
        }
        catch (Exception ex)
        {
            WriteLog("AnalyzeDataStream 失败", ex.ToString());
            error = $"\n\n---\n>  AI 调用失败：{ex.Message}";
        }

        if (error != null)
        {
            yield return error;
            yield break;
        }

        await foreach (var chunk in client!.GetStreamingResponseAsync(prompt, options, cancellationToken))
        {
            if (chunk?.Text != null)
                yield return chunk.Text;
        }

        WriteLog("AnalyzeDataStream 完成", "");
    }

    /// <summary>AI 对话（含工具调用）。使用 ToolChatClient 自动多轮工具循环，流式返回响应块</summary>
    /// <remarks>
    /// 工具调用事件通过 <see cref="IChatResponse.ToolCallEvents"/> 随流式块透传，供上层转换为 SSE 事件。
    /// </remarks>
    /// <param name="messages">完整消息历史（含 system 消息）</param>
    /// <param name="providers">工具提供者列表（按工具名路由），可为空</param>
    /// <param name="options">对话选项（模型、温度、思考模式等）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式响应块，含文本与工具调用事件</returns>
    public IAsyncEnumerable<IChatResponse> ChatAgentStreamAsync(IList<ChatMessage> messages, IList<IToolProvider>? providers = null, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        var toolClient = new ToolChatClient(client, [.. providers ?? []]);
        toolClient.Log = Log;
        toolClient.OnToolExecuted = e =>
        {
            WriteLog("工具调用", $"{e.ToolName} 参数={e.Arguments} 结果={e.ResultSummary} 耗时={e.ElapsedMs}ms 成功={!e.IsError}");
            return Task.CompletedTask;
        };

        return toolClient.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    /// <summary>AI 对话（含工具调用）。非流式，一次返回完整响应（含文本与工具调用事件）</summary>
    /// <param name="messages">完整消息历史（含 system 消息）</param>
    /// <param name="providers">工具提供者列表（按工具名路由），可为空</param>
    /// <param name="options">对话选项（模型、温度、思考模式等）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整响应，含文本与工具调用事件</returns>
    public async Task<IChatResponse> ChatAgentAsync(IList<ChatMessage> messages, IList<IToolProvider>? providers = null, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        var toolClient = new ToolChatClient(client, [.. providers ?? []]);
        toolClient.Log = Log;
        toolClient.OnToolExecuted = e =>
        {
            WriteLog("工具调用", $"{e.ToolName} 参数={e.Arguments} 结果={e.ResultSummary} 耗时={e.ElapsedMs}ms 成功={!e.IsError}");
            return Task.CompletedTask;
        };

        return await toolClient.GetResponseAsync(ChatRequest.Create(messages, options, false), cancellationToken);
    }

    /// <summary>快速 AI 对话，关闭深度推理、低温度，适合用户交互场景</summary>
    private Task<String> ChatFastAsync(String prompt, String data, CancellationToken cancellationToken = default)
        => ChatInternalAsync(prompt, data, _fastOptions, cancellationToken);

    /// <summary>内部 AI 对话，支持自定义选项</summary>
    private async Task<String> ChatInternalAsync(String prompt, String data, ChatOptions? options, CancellationToken cancellationToken)
    {
        if (!_setting.AISwitch) return "AI 未启用，请在系统设置中开启 AISwitch";

        try
        {
            var client = GetClient();
            var content = prompt;
            if (!data.IsNullOrEmpty())
                content = $"{prompt}\n\n数据：\n{data}";

            var reply = await client.ChatAsync(content, options, cancellationToken);
            WriteLog("ChatAsync 成功", content[..Math.Min(content.Length, 200)]);

            return reply;
        }
        catch (Exception ex)
        {
            WriteLog("ChatAsync 失败", ex.ToString());
            return $"AI 调用失败：{ex.Message}";
        }
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
