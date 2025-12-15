using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Filters;
using Stardust;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>设置控制器</summary>
[DisplayName("星尘设置")]
[AdminArea]
[Menu(0, false, Icon = "fa-bomb")]
public class StarController : ConfigController<StarSetting>
{
    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        PageSetting.NavView = "_Object_Nav";
    }
}