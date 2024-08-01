using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Web;
using IActionFilter = Microsoft.AspNetCore.Mvc.Filters.IActionFilter;

namespace NewLife.Cube;

/// <summary>业务接口控制器基类</summary>
/// <remarks>
/// 提供统一的令牌解码验证架构
/// </remarks>
[ApiFilter]
public abstract class BaseController : ControllerBase, IActionFilter
{
    #region 属性
    /// <summary>令牌</summary>
    public String Token { get; private set; }

    /// <summary>用户主机</summary>
    public String UserHost => HttpContext.GetUserHost();

    private IDictionary<String, Object> _args;
    #endregion

    #region 令牌验证
    void IActionFilter.OnActionExecuting(ActionExecutingContext context)
    {
        _args = context.ActionArguments;

        var token = Token = context.HttpContext.LoadToken();

        try
        {
            if (context.ActionDescriptor is ControllerActionDescriptor act && !act.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute)))
            {
                var rs = !token.IsNullOrEmpty() && OnAuthorize(token);
                if (!rs) throw new ApiException(403, "认证失败");
            }
        }
        catch (Exception ex)
        {
            var traceId = DefaultSpan.Current?.TraceId;
            context.Result = ex is ApiException aex
                ? new JsonResult(new { code = aex.Code, data = aex.Message, traceId })
                : new JsonResult(new { code = 500, data = ex.Message, traceId });

            WriteError(ex, context);
        }
    }

    /// <summary>验证令牌，并获取应用</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    protected abstract Boolean OnAuthorize(String token);

    void IActionFilter.OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception != null) WriteError(context.Exception, context);
    }

    private void WriteError(Exception ex, ActionContext context)
    {
        // 拦截全局异常，写日志
        var action = context.HttpContext.Request.Path + "";
        if (context.ActionDescriptor is ControllerActionDescriptor act) action = $"{act.ControllerName}/{act.ActionName}";

        OnWriteError(action, ex?.GetTrue() + Environment.NewLine + _args?.ToJson(true));
    }

    /// <summary>输出错误日志</summary>
    /// <param name="action"></param>
    /// <param name="message"></param>
    protected abstract void OnWriteError(String action, String message);
    #endregion
}