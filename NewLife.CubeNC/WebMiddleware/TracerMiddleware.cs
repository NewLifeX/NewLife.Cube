using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NewLife.Log;
using NewLife.Web;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.WebMiddleware
{
    /// <summary>性能跟踪中间件</summary>
    public class TracerMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>跟踪器</summary>
        public static ITracer Tracer { get; set; }

        /// <summary>实例化</summary>
        /// <param name="next"></param>
        public TracerMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>调用</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext ctx)
        {
            // APM跟踪
            //var span = Tracer?.NewSpan(ctx.Request.Path);
            ISpan span = null;
            if (Tracer != null)
            {
                var action = GetAction(ctx);
                if (!action.IsNullOrEmpty())
                {
                    span = Tracer.NewSpan(action);
                    span.Tag = ctx.GetUserHost() + " " + ctx.Request.GetRawUrl();
                    span.Detach(ctx.Request.Headers.ToDictionary(e => e.Key, e => (Object)e.Value));
                }
            }

            try
            {
                await _next.Invoke(ctx);

                // 根据状态码识别异常
                if (span != null)
                {
                    var code = ctx.Response.StatusCode;
                    if (code >= 200 && code <= 299) span.SetError(new HttpRequestException($"Http Error {code} {(HttpStatusCode)code}"), null);
                }
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);

                throw;
            }
            finally
            {
                span?.Dispose();
            }
        }

        /// <summary>忽略的后缀</summary>
        public static String[] ExcludeSuffixes { get; set; } = new[] {
            ".html", ".htm", ".js", ".css", ".map", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        private static String GetAction(HttpContext ctx)
        {
            var p = ctx.Request.Path + "";
            if (p.EndsWithIgnoreCase(ExcludeSuffixes)) return null;

            var ss = p.Split('/');
            if (ss.Length == 0) return p;

            // 如果是魔方格式，保留3段
            if (ss.Length >= 4 && ss[3].EqualIgnoreCase("detail", "add", "edit")) p = ss.Take(4).Join("/");

            return p;
        }
    }
}