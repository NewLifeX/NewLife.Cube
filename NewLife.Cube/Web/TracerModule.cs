using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NewLife.Log;

namespace NewLife.Cube.Web
{
    /// <summary>跟踪器模块</summary>
    public class TracerModule : IHttpModule
    {
        /// <summary>跟踪器</summary>
        public static ITracer Tracer { get; set; }

        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            context.BeginRequest += OnInit;
            context.PostReleaseRequestState += OnEnd;
        }

        void OnInit(Object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            var ctx = app?.Context;

            if (ctx != null && Tracer != null)
            {
                var action = GetAction(ctx);
                if (!action.IsNullOrEmpty())
                {
                    var span = Tracer.NewSpan(action);
                    ctx.Items["__span"] = span;
                }
            }
        }

        void OnEnd(Object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            var ctx = app?.Context;
            if (ctx != null)
            {
                if (ctx.Items["__span"] is ISpan span)
                {
                    var ex = ctx.Error;
                    if (ex != null) span.SetError(ex, ctx.Request.QueryString);

                    span.Dispose();
                }
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