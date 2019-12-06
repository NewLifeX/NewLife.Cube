using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Log;

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
            if (ex != null)
            {
                var act = context.ActionDescriptor;
                if (act != null)
                {
                    XTrace.Log.Error("[{0}]的错误[{1}]",  act.DisplayName, context.ExceptionHandled ? "已处理" : "未处理");
                }
                XTrace.WriteException(ex);
            }
        }
    }
}