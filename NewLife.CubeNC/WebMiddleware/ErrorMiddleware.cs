using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NewLife.Cube.Extensions;
using NewLife.Log;
using NewLife.Serialization;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
                //logger.Log(LogLevel.Error, 1, ex, ex, (e1, e2) => ex.ToString());

                //if (context.Response.HasStarted) throw;

                //context.Response.Clear();
                //context.Response.StatusCode = 500;
                //await ExceptionHandler(context, ex, viewEngine);

                throw;
            }
        }

        ///// <summary>处理异常</summary>
        ///// <param name="context"></param>
        ///// <param name="ex"></param>
        ///// <param name="viewEngine"></param>
        //private async Task ExceptionHandler(HttpContext context, Exception ex, IRazorViewEngine viewEngine)
        //{
        //    //判断api请求还是页面请求
        //    if (context.Request.IsAjaxRequest())
        //    {
        //        var data = ex.GetTrue().Message;
        //        var rs = new { result = false, data };
        //        var json = rs.ToJson();
        //        var bytes = json.GetBytes();
        //        await context.Response.Body.WriteAsync(bytes);
        //        context.Response.ContentLength = bytes.Length;
        //    }
        //    else
        //    {
        //        var viewName = "CubeError";
        //        var viewEngineResult = viewEngine.FindView(new ActionContext()
        //        {
        //            HttpContext = context,
        //            ActionDescriptor = new ActionDescriptor(),
        //            RouteData = new RouteData()
        //        }, viewName, false);

        //        if (!viewEngineResult.Success)
        //        {
        //            throw new ArgumentNullException($"没有找到名为{viewName}的视图文件");
        //        }

        //        using (var sw = new StringWriter())
        //        {
        //            var viewContext = new ViewContext()
        //            {
        //                HttpContext = context,
        //                Writer = sw,
        //                ViewData = new ViewDataDictionary<Exception>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        //                {
        //                    { "Exception", ex }
        //                }
        //            };

        //            await viewEngineResult.View.RenderAsync(viewContext);
        //            var bytes = sw.ToString().GetBytes();
        //            await context.Response.Body.WriteAsync(bytes);
        //            context.Response.ContentLength = bytes.Length;
        //        }
        //    }
        //}
    }

    /// <summary>异常处理中间件扩展</summary>
    public static class ErrorMiddlewareExtensions
    {
        /// <summary>使用异常处理中间件</summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseError(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<ErrorMiddleware>();
        }
    }
}