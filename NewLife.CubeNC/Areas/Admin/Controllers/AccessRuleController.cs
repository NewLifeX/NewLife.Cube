using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Entity;
using NewLife.Web;
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

    /// <summary>首页</summary>
    public override ActionResult Index(Pager p = null)
    {
        if (p["nav"].ToInt() > 0)
        {
            PageSetting.NavView = "_Object_Nav";
            PageSetting.EnableNavbar = false;
        }

        return base.Index(p);
    }
}