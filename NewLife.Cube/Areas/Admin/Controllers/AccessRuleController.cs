using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>访问规则</summary>
[DisplayName("访问规则")]
[AdminArea]
[Menu(0, false, Icon = "Star")]
public class AccessRuleController : EntityController<AccessRule, AccessRuleModel>
{
    private readonly BlockService _blockService;

    static AccessRuleController()
    {
        LogOnChange = true;

        ListFields.RemoveCreateField().RemoveRemarkField();

        // 过期时间列改造为解封操作列：点击自动封禁行的解封时间即可解除封禁
        if (ListFields.GetField("ExpireTime") is ListField df)
        {
            df.DisplayName = "解封时间";
            df.Url = "/api/Admin/AccessRule/Unblock?id={Id}";
        }
    }

    /// <summary>实例化访问规则控制器</summary>
    /// <param name="blockService">封禁服务</param>
    public AccessRuleController(BlockService blockService) => _blockService = blockService;

    /// <summary>解除自动封禁</summary>
    /// <param name="id">规则编号</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("解封")]
    public ActionResult Unblock(Int32 id)
    {
        var rule = AccessRule.FindById(id);
        if (rule == null || !rule.IsAutoBlock) return Json(1, "该规则不是自动封禁规则");

        var rs = _blockService.Unblock(rule.IP);

        // 浏览器直接点击列表链接时跳回列表页，接口调用返回JSON
        var accept = Request.Headers.Accept + "";
        if (accept.Contains("text/html")) return Redirect("/Admin/AccessRule");

        return rs > 0 ? Json(0, $"已解除 {rule.IP} 的封禁") : Json(1, "解封失败");
    }
}