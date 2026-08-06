using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.AI;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Controllers;

/// <summary>AI 全局接口。承载与实体无关的 AI 辅助端点，如浏览器操作结果回传</summary>
/// <remarks>
/// 浏览器操作回传放全局控制器而非各实体控制器，避免为每个实体页面重复增加接口。
/// 前端执行 run_js 脚本后 <c>POST /Admin/Ai/OperationResult</c> 回传结果，
/// 经 <see cref="PageCheckpointService"/> 解除对应工具调用的挂起等待。
/// </remarks>
[DisplayName("AI")]
public class AiController : ControllerBaseX
{
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
