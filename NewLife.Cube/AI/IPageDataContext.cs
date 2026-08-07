namespace NewLife.Cube.AI;

/// <summary>页面 AI 数据上下文提供者。页面控制器实现本接口后，AI 的 get_page_context 工具优先调用其服务端实现</summary>
/// <remarks>
/// 任意页面控制器实现本接口，即可为 AI 助手提供当前页面的结构化数据上下文（图表数据、报表聚合、配置摘要等 DOM 无法体现的内容）。
/// 全局 <c>/Ai/AiChat</c> 端点解析目标页面控制器后，若其实现本接口，get_page_context 优先调用其服务端实现；
/// 未实现本接口的页面自动降级为浏览器采集兜底（由 get_page_context 工具内部处理，无需页面改动）。
/// 实现时应仅返回允许 AI 读取的安全字段，避免泄露敏感数据（可参考 <see cref="AiDataHelper.FilterSafeFields"/>）。
/// </remarks>
public interface IPageDataContext
{
    /// <summary>收集当前页面数据上下文（JSON 字符串），供 AI 分析当前页面。仅返回允许 AI 读取的安全字段</summary>
    /// <returns>结构化 JSON 字符串</returns>
    Task<String> GetPageDataContextAsync();
}
