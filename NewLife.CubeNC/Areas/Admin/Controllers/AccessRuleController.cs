using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>访问规则</summary>
[DisplayName("访问规则")]
[AdminArea]
[Menu(0, false, Icon = "fa-star")]
public class AccessRuleController : EntityController<AccessRule, AccessRuleModel>
{
    private readonly BlockService _blockService;

    static AccessRuleController()
    {
        LogOnChange = true;

        // 过期时间列改造为解封操作列：自动封禁行显示解封时间与解封链接
        if (ListFields.GetField("ExpireTime") is ListField df)
        {
            df.DisplayName = "解封时间";
            df.AddService(new UnblockLink());
        }
    }

    /// <summary>实例化访问规则控制器</summary>
    /// <param name="blockService">封禁服务</param>
    public AccessRuleController(BlockService blockService) => _blockService = blockService;

    /// <summary>首页</summary>
    /// <param name="p">分页参数</param>
    /// <returns></returns>
    public override ActionResult Index(Pager p = null)
    {
        if (p["nav"].ToInt() > 0)
        {
            PageSetting.NavView = "_Object_Nav";
            PageSetting.EnableNavbar = false;
        }

        return base.Index(p);
    }

    /// <summary>解除自动封禁</summary>
    /// <param name="id">规则编号</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("解封")]
    public ActionResult Unblock(Int32 id)
    {
        var rule = AccessRule.FindById(id);
        if (rule == null) return Json(1, "规则不存在");
        if (!rule.IsAutoBlock) return Json(1, "该规则不是自动封禁规则，无需解封");

        var rs = _blockService.Unblock(rule.IP);

        return rs > 0 ? Json(0, $"已解除 {rule.IP} 的封禁") : Json(1, "解封失败，规则可能已被删除");
    }

    /// <summary>解封链接。仅自动封禁行渲染可点击的解封时间，其它行显示占位</summary>
    class UnblockLink : ILinkExtend
    {
        /// <summary>解析超链接HTML</summary>
        /// <param name="field">字段</param>
        /// <param name="data">行数据</param>
        /// <returns></returns>
        public String Resolve(DataField field, IModel data)
        {
            if (data is not AccessRule rule || !rule.IsAutoBlock) return "-";
            if (rule.ExpireTime.Year <= 2000) return "-";

            var text = rule.ExpireTime <= DateTime.Now ? "已过期" : rule.ExpireTime.ToString("MM-dd HH:mm");

            return $"<a href=\"/Admin/AccessRule/Unblock?id={rule.Id}\" data-action=\"action\" data-confirm=\"确认解除对 {rule.IP} 的封禁？\">{text}</a>";
        }
    }
}