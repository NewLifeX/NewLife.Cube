using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace NewLife.Cube.WebMiddleware
{
    /// <summary>页面查询执行时间中间件</summary>
    public class RunTimeMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>会话提供者</summary>
        static readonly SessionProvider _sessionProvider = new SessionProvider();

        /// <summary>跟踪器</summary>
        public static ITracer Tracer { get; set; }

#if DEBUG
        static RunTimeMiddleware()
        {
            Tracer = DefaultTracer.Instance;
            DAL.GlobalTracer = DefaultTracer.Instance;
        }
#endif

        /// <summary>实例化</summary>
        /// <param name="next"></param>
        public RunTimeMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

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

            ManageProvider.UserHost = ctx.GetUserHost();

            // 创建Session集合
            CreateSession(ctx);

            var inf = new RunTimeInfo();
            ctx.Items[nameof(RunTimeInfo)] = inf;

            var query = DAL.QueryTimes;
            var execute = DAL.ExecuteTimes;
            var sw = Stopwatch.StartNew();

            inf.QueryTimes = query;
            inf.ExecuteTimes = execute;
            inf.Watch = sw;

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop)
            {
                inf.Sqls = new List<String>();
                DAL.LocalFilter = s => inf.Sqls.Add(s);
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
                sw.Stop();
                span?.Dispose();

                DAL.LocalFilter = null;
                ManageProvider.UserHost = null;
            }
        }

        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒";

        /// <summary>获取执行时间和查询次数等信息</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static String GetInfo(HttpContext ctx)
        {
            var rtinf = ctx.Items[nameof(RunTimeInfo)] as RunTimeInfo;
            if (rtinf == null) return null;

            var inf = String.Format(DbRunTimeFormat,
                                    DAL.QueryTimes - rtinf.QueryTimes,
                                    DAL.ExecuteTimes - rtinf.ExecuteTimes,
                                    rtinf.Watch.ElapsedMilliseconds);

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop)
            {
                var list = rtinf.Sqls;
                if (list != null && list.Count > 0) inf += "<br />" + list.Select(e => HttpUtility.HtmlEncode(e)).Join("<br />" + Environment.NewLine);
            }

            return inf;
        }

        private static void CreateSession(HttpContext ctx)
        {
            // 准备Session
            var ss = ctx.Session;
            if (ss != null)
            {
                //var token = Request.Cookies["Token"];
                var token = ss.GetString("Cube_Token");
                if (token.IsNullOrEmpty())
                {
                    token = Rand.NextString(16);
                    //Response.Cookies.Append("Token", token, new CookieOptions { });
                    ss.SetString("Cube_Token", token);
                }

                //Session = _sessionProvider.GetSession(ss.Id);
                ctx.Items["Session"] = _sessionProvider.GetSession(token);
            }
        }

        /// <summary>忽略的后缀</summary>
        public static String[] ExcludeSuffixes { get; set; } = new[] {
            ".js", ".css", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        private static String GetAction(HttpContext ctx)
        {
            var p = ctx.Request.Path + "";
            if (p.EndsWithIgnoreCase(ExcludeSuffixes)) return null;

            var ss = p.Split('/');
            if (ss.Length == 0) return p;

            // 如果最后一段是数字，则可能是参数，需要去掉
            if ((ss.Length == 4 || ss.Length == 5) && ss[^1].ToInt() > 0) p = "/" + ss.Take(ss.Length - 1).Join("/");

            return p;
        }
    }

    /// <summary>运行时间信息</summary>
    public class RunTimeInfo
    {
        /// <summary>查询次数</summary>
        public Int32 QueryTimes { get; set; }

        /// <summary>执行次数</summary>
        public Int32 ExecuteTimes { get; set; }

        /// <summary>执行耗时</summary>
        public Stopwatch Watch { get; set; }

        /// <summary>查询次数</summary>
        public IList<String> Sqls { get; set; }
    }
}