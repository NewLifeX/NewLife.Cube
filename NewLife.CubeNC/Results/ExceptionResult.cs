using Microsoft.AspNetCore.Mvc;
using NewLife.Remoting;

namespace NewLife.Cube.Results;

/// <summary>异常动作结果</summary>
public class ExceptionResult : ContentResult
{
    /// <summary>异常对象</summary>
    public Exception Exception { get; set; }

    ///// <summary>状态码</summary>
    //public Int32 StatusCode { get; set; }

    //Int32? IStatusCodeActionResult.StatusCode => StatusCode;

    /// <summary>执行并输出结果</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        if (StatusCode == null || StatusCode == 0)
        {
            if (Exception is ApiException ae)
                StatusCode = ae.Code;
            else
                StatusCode = 500;
        }

        Content = Exception?.GetTrue()?.Message;

        //var httpContext = context.HttpContext;

        //if (StatusCode != null)
        //    httpContext.Response.StatusCode = StatusCode.Value;

        await base.ExecuteResultAsync(context);
    }
}
