using NewLife.AI.Clients;

namespace NewLife.Cube.AI;

/// <summary>AI 服务接口，封装提示词+数据→LLM 结果的简单调用模式</summary>
public interface IAIService
{
    /// <summary>获取或创建当前配置的 AI 客户端（含工具调用能力）。按配置惰性创建并复用</summary>
    IChatClient Client { get; }

    /// <summary>通用 AI 对话。拼接提示词和数据后请求 LLM 并返回结果</summary>
    /// <param name="prompt">提示词</param>
    /// <param name="data">数据（JSON 或文本，可为空）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<String> ChatAsync(String prompt, String data, CancellationToken cancellationToken = default);

    /// <summary>系统健康诊断（流式输出）。逐块返回生成内容</summary>
    /// <param name="sysInfoJson">系统指标 JSON</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式文本块的异步枚举</returns>
    IAsyncEnumerable<String> DiagnoseSystemStreamAsync(String sysInfoJson, CancellationToken cancellationToken = default);
}
