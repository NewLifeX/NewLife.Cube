using NewLife.AI;
using NewLife.AI.Clients;
using NewLife.AI.Clients.OpenAI;
using NewLife.AI.Models;
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

    /// <summary>分析错误日志（用户交互场景，快速）</summary>
    public Task<String> AnalyzeLogsAsync(String logsJson, CancellationToken cancellationToken = default)
    {
        var prompt = @"你是系统运维专家。请分析以下错误日志，用中文给出：
1. 错误类型归类（按频率排序）
2. 最可能的根因
3. 建议的排查步骤

直接输出分析报告，不要加无关解释。";

        return ChatFastAsync(prompt, logsJson, cancellationToken);
    }

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
    {
        var prompt = @"你是系统运维专家。根据以下系统运行指标，给出健康诊断报告（中文）：
分析要点：是否存在瓶颈、是否需要扩容、是否需要关注的风险点。
直接输出诊断报告，不要加无关解释。";

        return ChatFastAsync(prompt, sysInfoJson, cancellationToken);
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
