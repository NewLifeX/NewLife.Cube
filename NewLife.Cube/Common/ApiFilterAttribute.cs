using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Extensions;
using NewLife.Log;

namespace NewLife.Cube;

/// <summary>统一Api过滤处理</summary>
/// <remarks>
/// 1，解析访问令牌
/// 2，包装响应结果为标准Json格式
/// 3，拦截异常，包装为标准Json格式
/// </remarks>
public sealed class ApiFilterAttribute : ActionFilterAttribute
{
    ///// <summary>从请求头中获取令牌</summary>
    ///// <param name="httpContext"></param>
    ///// <returns></returns>
    //public static String GetToken(HttpContext httpContext)
    //{
    //    var request = httpContext.Request;
    //    var token = request.Query["Token"] + "";
    //    if (token.IsNullOrEmpty()) token = (request.Headers["Authorization"] + "").TrimStart("Bearer ");
    //    if (token.IsNullOrEmpty()) token = request.Headers["X-Token"] + "";
    //    if (token.IsNullOrEmpty()) token = request.Cookies["Token"] + "";

    //    return token;
    //}

    /// <summary>执行前，验证模型</summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        //if (!context.ModelState.IsValid)
        //    throw new ApplicationException(context.ModelState.Values.First(p => p.Errors.Count > 0).Errors[0].ErrorMessage);

        // 访问令牌
        var token = context.HttpContext.LoadToken();
        context.HttpContext.Items["Token"] = token;
        if (!context.ActionArguments.ContainsKey("token")) context.ActionArguments.Add("token", token);

        base.OnActionExecuting(context);
    }

    /// <summary>执行后，包装结果和异常</summary>
    /// <param name="context"></param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.HttpContext.WebSockets.IsWebSocketRequest) return;

        if (context.Result != null)
            if (context.Result is ObjectResult obj)
                context.Result = new JsonResult(new { code = obj.StatusCode ?? 0, data = obj.Value });
            else if (context.Result is EmptyResult)
            {
                DefaultTracer.Instance?.NewSpan("apiFilter-EmptyResult");
                context.Result = new JsonResult(new { code = 0, data = new { } });
            }
            else if (context.Exception != null && !context.ExceptionHandled)
            {
                var ex = context.Exception.GetTrue();
                if (ex is Remoting.ApiException aex)
                    context.Result = new JsonResult(new { code = aex.Code, message = aex.Message, data = aex.Message });
                else
                {
                    context.Result = new JsonResult(new { code = 500, message = ex.Message, data = ex.Message });
                    //context.Result = new JsonResult(ex.Message.ToFailApiResponse(ex.Message));

                    // 埋点拦截业务异常
                    var action = context.HttpContext.Request.Path + "";
                    if (context.ActionDescriptor is ControllerActionDescriptor act) action = $"/{act.ControllerName}/{act.ActionName}";

                    DefaultTracer.Instance?.NewError(action, ex);
                }

                context.ExceptionHandled = true;
            }

        base.OnActionExecuted(context);
    }
}