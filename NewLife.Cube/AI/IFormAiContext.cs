using NewLife.AI.Tools;

namespace NewLife.Cube.AI;

/// <summary>表单 AI 能力接口。非实体表单页面（如配置页 <see cref="ConfigController{TConfig}"/>）实现后，全局端点注册 get_form_schema / fill_form 工具，支持 AI 填表</summary>
/// <remarks>
/// 仿 <see cref="IEntityAiContext"/> 的能力接口模式：实体表单由 <see cref="CubeTools{TEntity}"/> 提供填表能力，
/// 而配置类页面（魔方设置等，非实体控制器）默认没有 get_form_schema / fill_form 工具，AI 无法按字段结构生成填表值。
/// 任意表单页面控制器实现本接口，全局 <c>/Ai/AiChat</c> 端点即为其注册表单工具：
/// <list type="number">
/// <item><c>get_form_schema</c>：枚举表单字段（名称/显示名/说明/类型/当前值），敏感字段与只读字段标注不可填</item>
/// <item><c>fill_form</c>：接收 AI 按 Schema 生成的字段值，做类型转换与安全过滤后返回，前端按 name 回填表单控件</item>
/// </list>
/// 实现时应仅返回允许 AI 读取的安全字段（可参考 <see cref="AiFormHelper.IsSensitiveField"/>）。
/// </remarks>
public interface IFormAiContext
{
    /// <summary>获取当前表单的字段结构（JSON），供 AI 生成填表值</summary>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit（配置页恒为编辑既有配置）</param>
    /// <returns>表单字段结构 JSON：<c>{entity, description, fields:[{name,displayName,type,description,value,fillable,...}]}</c></returns>
    String GetFormSchema(String mode = "edit");

    /// <summary>生成表单字段值并回填到前端表单。不写数据库，由用户确认后提交</summary>
    /// <param name="values">字段值 JSON 字符串，键为字段名，值为要填入的值</param>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 edit</param>
    /// <returns>工具结果，含回填值（供前端 applyFormValues 回填）与给 LLM 的说明</returns>
    IToolResult FillForm(String values, String mode = "edit");

    /// <summary>构建 AI 对话系统提示词（含表单上下文），引导 LLM 使用 get_form_schema / fill_form</summary>
    /// <param name="req">对话请求（含页面类型/模式等上下文）</param>
    /// <returns>系统提示词</returns>
    String BuildFormSystemPrompt(AiChatRequest req);
}
