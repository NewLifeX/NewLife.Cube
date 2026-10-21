using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Log;

namespace NewLife.Cube;

/// <summary>文件管理授权过滤器。魔方设置关闭文件管理（EnableFileManager=false）时，
/// 禁用文件管理全部接口，防止越权浏览、上传、下载、删除服务器文件</summary>
public class FileManagerAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>授权检查。未启用文件管理时直接返回403，跳过控制器动作</summary>
    /// <param name="context">授权上下文</param>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var set = CubeSetting.Current;
        if (set.EnableFileManager) return;

        var act = (ControllerActionDescriptor)context.ActionDescriptor;
        XTrace.WriteLine("File management is not enabled; access denied for {0}/{1}", act.ControllerName, act.ActionName);

        context.HttpContext.Response.StatusCode = 403;
        context.Result = new JsonResult(new { code = 403, message = L.T("File management is disabled. Enable it in Cube settings.") });
    }
}
