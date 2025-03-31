using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>访问规则</summary>
[DisplayName("访问规则")]
[AdminArea]
[Menu(0, false, Icon = "fa-star")]
public class AccessRuleController : EntityController<AccessRule, AccessRuleModel>
{
    static AccessRuleController()
    {
        LogOnChange = true;
    }

    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        PageSetting.NavView = "_Object_Nav";
    }
}