using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics.RazorViews;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using NewLife.Cube.Extensions;
using NewLife.Log;
using NewLife.Serialization;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace NewLife.Cube.WebMiddleware
{
    public class ErrorModuleMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorModuleMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context, IRazorViewEngine viewEngine, ILogger<ErrorModuleMiddleware> logger)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception e)
            {
                XTrace.WriteException(e);
                logger.Log(LogLevel.Error, 1, e, e, null);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = 500;
                await ExceptionHandler(context, e, viewEngine);
            }
        }

        /// <summary>处理异常</summary>
        /// <param name="context"></param>
        private async Task ExceptionHandler(HttpContext context, Exception ex, IRazorViewEngine viewEngine)
        {
            //判断api请求还是页面请求
            if (context.Request.IsAjaxRequest())
            {
                var data = ex.GetTrue().Message;
                var rs = new { result = false, data };
                var json = rs.ToJson();
                var bytes = json.GetBytes();
                await context.Response.Body.WriteAsync(bytes);
                context.Response.ContentLength = bytes.Length;
            }
            else
            {
                var viewName = "CubeError";
                var viewEngineResult = viewEngine.FindView(new ActionContext()
                {
                    HttpContext = context,
                    ActionDescriptor = new ActionDescriptor(),
                    RouteData = new RouteData()
                }, viewName, false);

                if (!viewEngineResult.Success)
                {
                    throw new ArgumentNullException($"没有找到名为{viewName}的视图文件");
                }

                using (var sw = new StringWriter())
                {
                    var viewContext = new ViewContext()
                    {
                        HttpContext = context,
                        Writer = sw,
                        ViewData = new ViewDataDictionary<Exception>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                        {
                            { "Exception", ex }
                        }
                    };

                    await viewEngineResult.View.RenderAsync(viewContext);
                    var bytes = sw.ToString().GetBytes();
                    await context.Response.Body.WriteAsync(bytes);
                    context.Response.ContentLength = bytes.Length;
                }
            }
        }
    }

    public static class ErrorModuleMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorModule(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ErrorModuleMiddleware>();
        }
    }
}