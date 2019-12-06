using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Logging;
using NewLife.Log;

namespace NewLife.Cube.WebMiddleware
{
    /// <summary>异常处理中间件</summary>
    public class ErrorMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>实例化异常处理中间件</summary>
        /// <param name="next"></param>
        public ErrorMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>异常处理逻辑</summary>
        /// <param name="context"></param>
        /// <param name="viewEngine"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context, IRazorViewEngine viewEngine, ILogger<ErrorMiddleware> logger)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);

                //var rs = context.Response;
                //if (!rs.HasStarted)
                //{
                //    rs.Clear();
                //    rs.StatusCode = 500;
                //    return;
                //}

                throw;
            }
        }
    }
}