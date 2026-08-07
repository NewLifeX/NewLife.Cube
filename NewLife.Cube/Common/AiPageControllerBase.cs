using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.AI.Tools;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Cube.AI;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>AI 页面控制器基类。非实体页面继承后获得 <c>{控制器}/AiChat</c> 对话端点（含通用工具），MVC 前端自动生效</summary>
/// <remarks>
/// 实体页面（<see cref="ReadOnlyEntityController{TEntity}"/>）自带 AiChat 端点（含实体上下文工具）；
/// 非实体页面（首页/报表/配置页等）继承本基类即可获得同样的 AI 对话能力：
/// <list type="number">
/// <item>自带 <c>{控制器}/AiChat</c> 端点（SSE 流式，经 <see cref="AiChatEndpoint"/> 统一管道）</item>
/// <item>注册通用工具：get_system_info / get_page_context / run_js</item>
/// <item>实现 <see cref="IPageDataContext"/> 接口后，get_page_context 自动优先调用服务端实现，否则浏览器采集兜底</item>
/// </list>
/// MVC 前端（_AiAssistant.cshtml）按 <c>GetMethod("AiChat")</c> 自动探测，继承后前端零改动自动走本控制器端点。
/// 系统提示词可重写 <see cref="BuildChatSystemPrompt"/> 定制。
/// </remarks>
public abstract class AiPageControllerBase : ControllerBaseX
{
    /// <summary>AI 对话助手。通过工具调用完成页面分析等操作，SSE 流式返回</summary>
    /// <returns></returns>
    [DisplayName("AI 对话")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<ActionResult> AiChat()
    {
        // 请求校验与 SSE 输出管道由 AiChatEndpoint 统一提供（含浏览器工具与页面上下文工具注册）
        var (error, svc, req) = await AiChatEndpoint.ParseAsync(this);
        if (error != null || svc == null || req == null) return error!;

        var registry = new ToolRegistry();
        registry.AddTools(new SystemInfoToolService());
        registry.AddTools(new BuiltinToolService());

        var systemPrompt = BuildChatSystemPrompt(req);

        await AiChatEndpoint.RunSseAsync(this, svc, req, systemPrompt, registry);

        return new EmptyResult();
    }

    /// <summary>构建 AI 对话系统提示词（非实体页面，无实体表上下文）</summary>
    /// <param name="req">对话请求</param>
    /// <returns></returns>
    protected virtual String BuildChatSystemPrompt(AiChatRequest req)
    {
        var sysName = SysConfig.Current?.DisplayName;
        if (sysName.IsNullOrEmpty()) sysName = "魔方后台管理系统";

        var sb = Pool.StringBuilder.Get();
        sb.AppendLine($"你是{sysName}的 AI 助手，正在协助管理员操作当前页面。");
        sb.AppendLine();
        sb.AppendLine("当前页面不属于实体数据页面（可能是报表、配置或业务页面），无实体表上下文。");
        sb.AppendLine();
        sb.AppendLine("可用工具：get_page_context / get_system_info / run_js（详细说明见函数定义，按需调用）");
        sb.AppendLine();
        sb.AppendLine("规则：");
        sb.AppendLine("1. 使用简体中文回答，语言简洁专业");
        sb.AppendLine("2. 用户询问当前页面内容/数据/结构时，调用 get_page_context 获取页面上下文（优先页面服务端提供，否则自动采集浏览器页面内容），再给出分析与结论");
        sb.AppendLine("3. 用户询问系统状态/诊断时，调用 get_system_info 获取数据，再给出分析结论与建议");
        sb.AppendLine("4. 用户要求读取或操作当前页面元素（填写输入框、点击按钮、读取标题等）时，可调用 run_js 执行 JavaScript；脚本在用户浏览器当前页面执行，可用 document.querySelector 等定位元素；修改页面内容或提交表单等写操作前，先向用户说明将执行的操作");
        sb.AppendLine("5. 不要编造数据；信息不足时主动询问用户澄清");

        return sb.Return(true);
    }
}
