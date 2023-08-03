using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>设置控制器</summary>
[DisplayName("数据中间件")]
[Area("Admin")]
[Menu(0, false)]
public class XCodeController : ConfigController<XCodeSetting>
{
    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        PageSetting.NavView = "_Object_Nav";
    }
}