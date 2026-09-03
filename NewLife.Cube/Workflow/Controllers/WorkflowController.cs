using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Workflow.Entity;
using XCode.Membership;

namespace NewLife.Cube.Workflow.Controllers;

/// <summary>OA 审批流程 API（OSC-26090347f1）。前缀 /Cube/Workflow</summary>
[DisplayName("审批流程")]
[Route("Cube/Workflow")]
public class WorkflowController : ControllerBaseX
{
    /// <summary>能力探测。匿名返回 { enabled }；登录附加待办数。编译进 WebAPI 核心库 → enabled=true；MVC/CubeNC 无此控制器</summary>
    /// <returns>能力 JSON</returns>
    [AllowAnonymous]
    [HttpGet]
    public Object Meta()
    {
        var user = ManageProvider.User;
        var data = new Dictionary<String, Object>
        {
            ["enabled"] = true,
        };
        if (user != null)
        {
            data["todoCount"] = WorkflowTask.CountTodoByUser(user.ID);
        }
        return Json(0, null, data);
    }
}
