using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Common;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>系统设置控制器</summary>
[DisplayName("系统设置")]
[AdminArea]
[Menu(0, false)]
public class SysController : ConfigController<SysConfig>
{
    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        PageSetting.NavView = "_Object_Nav";
    }
}