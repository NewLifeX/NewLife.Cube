using NewLife.Web;
using XCode;

namespace NewLife.Cube.AI;

/// <summary>实体 AI 能力提供者。实体控制器实现本接口后，全局 <see cref="Controllers.AiController"/> 可调用其 AI 相关重载点</summary>
/// <remarks>
/// 仿 <see cref="IPageDataContext"/> 的能力接口模式：实体控制器（<see cref="ReadOnlyEntityController{TEntity}"/>）实现本接口，
/// 将 AI 对话所需的数据查询、工具集与提示词等可重载点暴露给全局 AI 端点。
/// 全局 <c>/Ai/AiChat</c> 端点依据请求中的 area/controller 解析目标控制器，若其实现本接口，
/// 则注册实体数据工具集（get_data_context / get_form_schema / fill_form）并使用其定制提示词；
/// 否则回退为通用工具与通用提示词。
/// 接口成员均为对实体控制器 protected virtual 成员（<c>SearchData</c> / <c>CreateCubeTools</c> / <c>BuildChatSystemPrompt</c>）的委托，
/// 子类重载仍然生效。实现时应仅暴露允许 AI 读取的数据，避免泄露敏感信息。
/// </remarks>
public interface IEntityAiContext
{
    /// <summary>实体工厂</summary>
    IEntityFactory Factory { get; }

    /// <summary>按查询条件搜索数据（保留子类重载与数据权限）</summary>
    /// <param name="p">查询条件</param>
    /// <returns>当前页数据</returns>
    IEnumerable<Object> SearchData(Pager p);

    /// <summary>创建 AI 工具集（保留子类重载 <c>CreateCubeTools</c>），供 <c>ToolRegistry</c> 扫描注册</summary>
    /// <param name="pager">当前查询条件（可为空）</param>
    /// <param name="entityId">当前记录编号</param>
    /// <returns>实体上下文工具集对象（含 <c>[ToolDescription]</c> 标注的工具方法）</returns>
    Object CreateCubeTools(Pager? pager, Int64 entityId);

    /// <summary>构建 AI 对话系统提示词（保留子类重载）</summary>
    /// <param name="req">对话请求</param>
    /// <param name="pager">当前查询条件</param>
    /// <returns>系统提示词</returns>
    String BuildChatSystemPrompt(AiChatRequest req, Pager? pager);
}
