using Microsoft.AspNetCore.Mvc;
using NewLife.AI.Tools;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.AI;

/// <summary>AI 对话端点公共逻辑。实体控制器（带实体工具）与全局控制器（通用工具）共用请求解析与 SSE 输出管道</summary>
/// <remarks>
/// 实体页面对话走 <c>{控制器}/AiChat</c>（实体控制器自带数据上下文工具），
/// 非实体页面（首页、魔方设置、系统信息等）走全局 <c>/Ai/AiChat</c>（通用工具）。
/// 两个宿主共用本类的请求校验与 SSE 流式输出逻辑，避免重复实现。
/// </remarks>
public static class AiChatEndpoint
{
    /// <summary>解析并校验 AI 对话请求</summary>
    /// <param name="ctrl">当前控制器</param>
    /// <returns>校验失败返回错误响应与空值；成功返回 <see cref="IAIChatService"/> 与对话请求</returns>
    public static async Task<(ActionResult? Error, IAIChatService? Svc, AiChatRequest? Req)> ParseAsync(ControllerBaseX ctrl)
    {
        var set = CubeSetting.Current;
        if (!set.AISwitch) return (ctrl.Json(500, null, "AI 未启用，请联系系统管理员开启 AISwitch"), null, null);

        var svc = ctrl.HttpContext.RequestServices.GetService<IAIChatService>();
        if (svc == null) return (ctrl.Json(500, null, "AI 对话服务未注册"), null, null);

        // 读取 JSON 请求体
        var body = await new StreamReader(ctrl.Request.Body).ReadToEndAsync();
        if (body.IsNullOrEmpty()) return (ctrl.Json(500, null, "请求体为空"), null, null);
        var req = body.ToJsonEntity<AiChatRequest>();
        if (req == null || req.Message.IsNullOrEmpty()) return (ctrl.Json(500, null, "消息不能为空"), null, null);

        return (null, svc, req);
    }

    /// <summary>执行 SSE 流式对话。设置响应头、注册浏览器工具、调用对话服务</summary>
    /// <param name="ctrl">当前控制器</param>
    /// <param name="svc">AI 对话服务</param>
    /// <param name="req">对话请求</param>
    /// <param name="systemPrompt">系统提示词（由宿主构建以保留子类重载）</param>
    /// <param name="registry">工具注册表（宿主可先追加实体上下文工具与内置工具）</param>
    public static async Task RunSseAsync(ControllerBaseX ctrl, IAIChatService svc, AiChatRequest req, String systemPrompt, ToolRegistry registry)
    {
        // SSE 写回调：ChatAsync 与浏览器工具服务共用，浏览器工具经它下发 run_js 事件到前端
        Func<String, Task> writeEvent = async json =>
        {
            await ctrl.Response.WriteAsync($"data: {json}\n\n", ctrl.HttpContext.RequestAborted);
            await ctrl.Response.Body.FlushAsync(ctrl.HttpContext.RequestAborted);
        };

        // 浏览器工具服务：run_js 等工具经页面检查点服务下发脚本到前端执行，结果回传后端继续对话（全局 AiController.OperationResult 端点回传）。
        // 检查点服务优先取 DI（事件总线广播，支持分布式集群），兜底进程内实例。
        var user = ctrl.HttpContext.User.Identity as IUser;
        var cp = ctrl.HttpContext.RequestServices.GetService<PageCheckpointService>();
        var browser = new BrowserToolService(user?.ID ?? 0) { Writer = writeEvent, CheckpointService = cp };
        registry.AddTools(browser);

        // 页面数据上下文工具：优先宿主控制器服务端实现（IPageDataContext），否则浏览器采集兜底。所有端点统一注册
        registry.AddTools(new PageDataContextToolService(ctrl, browser));

        // SSE 输出
        ctrl.Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
        ctrl.Response.Headers["Cache-Control"] = "no-cache";
        ctrl.Response.Headers["X-Accel-Buffering"] = "no";

        // 对话核心逻辑下沉到全局服务：会话管理、工具循环、SSE 事件、空响应兜底
        await svc.ChatAsync(req, systemPrompt, [registry], writeEvent, ctrl.HttpContext.RequestAborted);
    }
}
