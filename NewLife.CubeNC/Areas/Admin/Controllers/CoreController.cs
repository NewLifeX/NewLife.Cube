using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Filters;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>设置控制器</summary>
[DisplayName("基本设置")]
[AdminArea]
[Menu(0, false, Icon = "fa-bomb")]
public class CoreController : ConfigController<NewLife.Setting>
{
    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        PageSetting.NavView = "_Object_Nav";
    }
}