using System.ComponentModel;
using NewLife.AI.Tools;

namespace NewLife.Cube.AI;

/// <summary>配置表单工具服务。为表单类非实体页面（<see cref="IFormAiContext"/>，如魔方设置）注册 get_form_schema / fill_form 工具</summary>
/// <remarks>
/// 由全局 <see cref="Controllers.AiController"/> 在目标控制器实现 <see cref="IFormAiContext"/> 时注册，
/// 与实体工具集 <see cref="CubeTools{TEntity}"/> 的填表能力对齐：get_form_schema 提供字段结构，fill_form 生成值回填前端。
/// 工具方法均为转发，具体逻辑在 <see cref="IFormAiContext"/> 实现中（配置字段枚举、敏感过滤、类型转换）。
/// 工具方法为 virtual，二次开发者可继承重写。
/// </remarks>
/// <param name="ctx">表单 AI 上下文（当前页面控制器的能力实现）</param>
public class ConfigFormToolService(IFormAiContext ctx)
{
    /// <summary>获取当前表单的字段结构，包含字段名、类型、说明与当前值，供生成表单值使用</summary>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit（配置页恒为编辑既有配置）</param>
    /// <param name="context">工具调用上下文（由框架注入）</param>
    /// <returns>表单字段结构 JSON</returns>
    [ToolDescription("get_form_schema", ReadOnly = true)]
    [DisplayName("获取表单字段结构")]
    [Description("获取当前表单的字段结构，包含字段名、类型、说明与当前值，供生成表单值使用")]
    public virtual String GetFormSchema([Description("表单模式：add 新增 / edit 编辑")] String mode = "edit", ToolCallContext? context = null)
        => ctx.GetFormSchema(mode);

    /// <summary>生成表单字段值并回填到前端表单。不写数据库，由用户确认后提交</summary>
    /// <param name="values">字段值 JSON 字符串，键为字段名，值为要填入的值，如 {"AIModel":"qwen3.7-plus"}</param>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit</param>
    /// <param name="context">工具调用上下文（由框架注入）</param>
    /// <returns>工具结果，含回填值</returns>
    [ToolDescription("fill_form")]
    [DisplayName("回填表单")]
    [Description("生成表单字段值并回填到前端表单。不写数据库，由用户确认后提交")]
    public virtual IToolResult FillForm([Description("字段值 JSON 字符串，键为字段名，值为要填入的值")] String values, [Description("表单模式：add 新增 / edit 编辑")] String mode = "edit", ToolCallContext? context = null)
        => ctx.FillForm(values, mode);
}
