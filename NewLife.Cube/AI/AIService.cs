using System.Runtime.CompilerServices;
using NewLife.AI.Clients;
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
    private String _lastEndpoint;
    private String _lastApiKey;
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

    /// <summary>获取或创建当前配置的 AI 客户端（含工具调用能力）。按配置惰性创建并复用</summary>
    public IChatClient Client => GetClient();

    /// <summary>获取或创建客户端，按需延迟初始化</summary>
    private IChatClient GetClient()
    {
        var provider = _setting.AIProvider;
        var model = _setting.AIModel;
        var endpoint = _setting.AIEndpoint;
        var apiKey = _setting.AIApiKey;

        // 配置未变则复用（provider/model/endpoint/apiKey 任一变化都重建，保证运行中修改配置即时生效）
        if (_client != null && _lastProvider == provider && _lastModel == model && _lastEndpoint == endpoint && _lastApiKey == apiKey)
            return _client;

        _client = CreateClient(provider, apiKey, model, endpoint);
        _lastProvider = provider;
        _lastModel = model;
        _lastEndpoint = endpoint;
        _lastApiKey = apiKey;

        return _client;
    }

    /// <summary>根据配置创建对应的 AI 客户端。未注册的服务商直接抛异常，让管理员修正配置（不静默回退 OpenAI 兼容，避免掩盖协议不匹配）</summary>
    /// <param name="provider">服务商编码</param>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="model">默认模型</param>
    /// <param name="endpoint">API 地址</param>
    /// <returns>已绑定连接参数的客户端实例</returns>
    /// <exception cref="ArgumentException">服务商未注册时抛出</exception>
    private static IChatClient CreateClient(String provider, String apiKey, String model, String endpoint)
    {
        if (provider.IsNullOrEmpty()) provider = "NewLifeAI";

        return AiClientRegistry.Default.CreateClient(provider, apiKey, model, endpoint);
    }

    /// <summary>通用 AI 对话（后台任务），保留模型默认推理行为，适合 CronJob 等不赶时间的场景</summary>
    public Task<String> ChatAsync(String prompt, String data, CancellationToken cancellationToken = default)
        => ChatInternalAsync(prompt, data, null, cancellationToken);

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
