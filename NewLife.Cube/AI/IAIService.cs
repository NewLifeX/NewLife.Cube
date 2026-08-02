namespace NewLife.Cube.AI;

/// <summary>AI 服务接口，封装提示词+数据→LLM 结果的简单调用模式</summary>
public interface IAIService
{
    /// <summary>通用 AI 对话。拼接提示词和数据后请求 LLM 并返回结果</summary>
    /// <param name="prompt">提示词</param>
    /// <param name="data">数据（JSON 或文本，可为空）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<String> ChatAsync(String prompt, String data, CancellationToken cancellationToken = default);

    /// <summary>润色通知内容。将原始通知文案改写为目标风格</summary>
    /// <param name="title">通知标题</param>
    /// <param name="content">原始内容</param>
    /// <param name="style">目标风格（正式/友好/简洁）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<String> PolishNotificationAsync(String title, String content, String style, CancellationToken cancellationToken = default);

    /// <summary>系统健康诊断。根据运行指标生成诊断报告</summary>
    /// <param name="sysInfoJson">系统指标 JSON</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<String> DiagnoseSystemAsync(String sysInfoJson, CancellationToken cancellationToken = default);

    /// <summary>系统健康诊断（流式输出）。逐块返回生成内容</summary>
    /// <param name="sysInfoJson">系统指标 JSON</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式文本块的异步枚举</returns>
    IAsyncEnumerable<String> DiagnoseSystemStreamAsync(String sysInfoJson, CancellationToken cancellationToken = default);

    /// <summary>数据分析洞察。根据上下文数据生成分析报告</summary>
    /// <param name="prompt">完整的分析提示词（含统计摘要和数据样本）</param>
    /// <param name="think">是否启用深度推理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<String> AnalyzeDataAsync(String prompt, Boolean think = false, CancellationToken cancellationToken = default);

    /// <summary>数据分析洞察（流式输出）。逐块返回生成内容</summary>
    /// <param name="prompt">完整的分析提示词（含统计摘要和数据样本）</param>
    /// <param name="think">是否启用深度推理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流式文本块的异步枚举</returns>
    IAsyncEnumerable<String> AnalyzeDataStreamAsync(String prompt, Boolean think = false, CancellationToken cancellationToken = default);
}
