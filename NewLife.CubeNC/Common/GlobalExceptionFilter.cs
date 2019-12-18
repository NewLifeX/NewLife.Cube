using System;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>全局异常处理接口。主要用于写日志</summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        /// <summary>异常发生时</summary>
        /// <param name="context"></param>
        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception;
            if (ex != null && !context.ExceptionHandled)
            {
                //context.HttpContext.Items["GlobalException"] = ex;
                context.HttpContext.Items["ExceptionContext"] = context;

                var act = context.ActionDescriptor;
                var action = act?.DisplayName;

                var cad = act as ControllerActionDescriptor;
                if (cad != null) action = $"{cad.ControllerName}/{cad.ActionName}";

                XTrace.Log.Error("[{0}]的错误[{1}]", action, context.ExceptionHandled ? "已处理" : "未处理");

                if (cad != null) LogProvider.Provider?.WriteLog(cad.ControllerName, cad.ActionName, ex.GetTrue().Message);

                XTrace.WriteException(ex);

                context.ExceptionHandled = true;
            }
        }
    }
}