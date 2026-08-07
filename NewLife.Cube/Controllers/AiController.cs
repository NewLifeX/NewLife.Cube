using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.AI.Tools;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Cube.AI;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Controllers;

/// <summary>AI 全局接口。承载与实体无关的 AI 辅助端点，如通用对话与浏览器操作结果回传</summary>
/// <remarks>
/// 实体页面（EntityController）自带 <c>{控制器}/AiChat</c> 对话端点（含数据上下文工具）；
/// 非实体页面（首页、魔方设置、系统信息、系统诊断等）无此端点，由本控制器提供全局 <c>/Ai/AiChat</c>，
/// 注册系统信息与浏览器操作等通用工具，SSE 流式返回，与实体端点共用 <see cref="AiChatEndpoint"/> 输出管道。
/// 浏览器操作回传亦放全局控制器而非各实体控制器，避免为每个实体页面重复增加接口。
/// 本控制器为全局控制器（不标记 <see cref="AdminArea"/>），路由统一为 <c>/Ai/[action]</c>（无区域前缀），
/// 所有调用方（MVC _AiAssistant / ai-assistant.js / Vue AiAssistant.vue）统一使用该地址；
/// 非区域控制器命中不了 <c>{area}/{controller}/{action}</c> 约定路由，只能靠属性路由显式声明。
/// </remarks>
[DisplayName("AI")]
[Route("Ai/[action]")]
public class AiController : ControllerBaseX
{
    /// <summary>AI 对话（全局）。供非实体页面使用的通用对话端点，SSE 流式返回</summary>
    /// <returns></returns>
    [DisplayName("AI 对话")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<ActionResult> AiChat()
    {
        var (error, svc, req) = await AiChatEndpoint.ParseAsync(this);
        if (error != null || svc == null || req == null) return error!;

        // 通用工具：系统信息 + 浏览器操作，无实体上下文工具。
        // get_system_info 由 SystemInfoToolService 提供（与实体工具集 CubeTools 共用实现），
        // 供"系统诊断"等场景采集服务器指标；内置工具另含 get_current_time / calculate。
        var registry = new ToolRegistry();
        registry.AddTools(new SystemInfoToolService());
        registry.AddTools(new BuiltinToolService());

        var systemPrompt = BuildChatSystemPrompt(req);

        // SSE 输出（含浏览器操作工具注册）
        await AiChatEndpoint.RunSseAsync(this, svc, req, systemPrompt, registry);

        return new EmptyResult();
    }

    /// <summary>构建 AI 对话系统提示词（全局端点，无实体上下文）</summary>
    /// <param name="req">对话请求</param>
    /// <returns></returns>
    private static String BuildChatSystemPrompt(AiChatRequest req)
    {
        var sysName = SysConfig.Current?.DisplayName;
        if (sysName.IsNullOrEmpty()) sysName = "魔方后台管理系统";

        var sb = Pool.StringBuilder.Get();
        sb.AppendLine($"你是{sysName}的 AI 助手，正在协助管理员操作当前页面。");
        sb.AppendLine();
        sb.AppendLine("当前页面不属于实体数据页面（可能是系统设置、首页或系统信息等），没有可分析的数据表上下文。");
        sb.AppendLine();
        sb.AppendLine("可用工具：get_system_info / run_js（详细说明见函数定义，按需调用）");
        sb.AppendLine();
        sb.AppendLine("规则：");
        sb.AppendLine("1. 使用简体中文回答，语言简洁专业");
        sb.AppendLine("2. 用户询问系统状态/诊断时，调用 get_system_info 获取数据，再给出分析结论与建议");
        sb.AppendLine("3. 用户要求读取或操作当前页面元素（填写输入框、点击按钮、读取标题等）时，可调用 run_js 执行 JavaScript；脚本在用户浏览器当前页面执行，可用 document.querySelector 等定位元素；修改页面内容或提交表单等写操作前，先向用户说明将执行的操作");
        sb.AppendLine("4. 用户要求分析数据或填写表单时，说明当前页面没有可分析的数据上下文");
        sb.AppendLine("5. 不要编造数据；信息不足时主动询问用户澄清");

        return sb.Return(true);
    }

    /// <summary>浏览器操作结果回传。前端执行 run_js 脚本后回传结果，完成等待中的工具调用</summary>
    /// <returns></returns>
    [DisplayName("浏览器操作结果回传")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<ActionResult> OperationResult()
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        if (body.IsNullOrEmpty()) return Json(500, null, "请求体为空");
        var req = body.ToJsonEntity<BrowserOpResult>();
        if (req == null || req.CheckpointId.IsNullOrEmpty()) return Json(500, null, "参数错误");

        var user = HttpContext.User.Identity as IUser;
        var ok = PageCheckpointService.Instance.Respond(req.CheckpointId, user?.ID ?? 0, req.Result ?? "{}");

        return ok ? Json(0, null, "ok") : Json(500, null, "操作不存在或已过期");
    }
}
