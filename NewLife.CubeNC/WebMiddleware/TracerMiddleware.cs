using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NewLife.Log;

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
                if (!action.IsNullOrEmpty()) span = Tracer.NewSpan(action);
            }

            try
            {
                await _next.Invoke(ctx);
            }
            catch (Exception ex)
            {
                span?.SetError(ex, ctx.Request.QueryString + "");

                throw;
            }
            finally
            {
                span?.Dispose();
            }
        }

        /// <summary>忽略的后缀</summary>
        public static String[] ExcludeSuffixes { get; set; } = new[] {
            ".html", ".htm", ".js", ".css", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        private static String GetAction(HttpContext ctx)
        {
            var p = ctx.Request.Path + "";
            if (p.EndsWithIgnoreCase(ExcludeSuffixes)) return null;

            var ss = p.Split('/');
            if (ss.Length == 0) return p;

            // 如果最后一段是数字，则可能是参数，需要去掉
            if ((ss.Length == 4 || ss.Length == 5) && ss[ss.Length - 1].ToInt() > 0) p = "/" + ss.Take(ss.Length - 1).Join("/");

            return p;
        }
    }
}