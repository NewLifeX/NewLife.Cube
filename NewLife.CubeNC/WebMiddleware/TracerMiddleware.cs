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
        public TracerMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

        /// <summary>调用</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext ctx)
        {
            // APM跟踪
            ISpan span = null;
            if (Tracer != null)
            {
                var action = GetAction(ctx);
                if (!action.IsNullOrEmpty())
                {
                    // 聚合请求头作为强制采样的数据标签
                    var vs = ctx.Request.Headers.ToDictionary(e => e.Key, e => e.Value + "");

                    span = Tracer.NewSpan(action);
                    span.Tag = $"{ctx.GetUserHost()} {ctx.Request.Method} {ctx.Request.GetRawUrl()}";
                    if (span is DefaultSpan ds && ds.TraceFlag > 0)
                        span.Tag += Environment.NewLine + vs.Join(Environment.NewLine, e => $"{e.Key}:{e.Value}");
                    span.Detach(vs);
                }
            }

            try
            {
                await _next.Invoke(ctx);

                // 根据状态码识别异常
                if (span != null)
                {
                    var code = ctx.Response.StatusCode;
                    if (code >= 400) span.SetError(new HttpRequestException($"Http Error {code} {(HttpStatusCode)code}"), null);
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
            if (ss.Length >= 4 && ss[3].EqualIgnoreCase("detail", "add", "edit")) p = "/" + ss.Take(4).Join("/");

            return p;
        }
    }
}