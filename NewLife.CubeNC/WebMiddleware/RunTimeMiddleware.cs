using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;
using XCode.DataAccessLayer;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.WebMiddleware
{
    /// <summary>运行时中间件。页面查询执行时间、异常拦截</summary>
    public class RunTimeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly UserService _userService;

        /// <summary>会话提供者</summary>
        static readonly SessionProvider _sessionProvider = new SessionProvider();

        /// <summary>实例化</summary>
        /// <param name="next"></param>
        /// <param name="userService"></param>
        public RunTimeMiddleware(RequestDelegate next, UserService userService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _userService = userService;
        }

        /// <summary>调用</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext ctx)
        {
            var userAgent = ctx.Request.Headers["User-Agent"] + "";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            // 识别拦截爬虫
            if (!ValidRobot(ctx, ua)) return;

            var ip = ctx.GetUserHost();
            ManageProvider.UserHost = ip;

            // 创建Session集合
            var token = CreateSession(ctx);

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

            // 日志控制，精确标注Web类型线程
            WriteLogEventArgs.Current.IsWeb = true;

            UserOnline olt = null;
            var user = ManageProvider.User;
            try
            {
                var p = ctx.Request.Path + "";
                if (!p.EndsWithIgnoreCase(ExcludeSuffixes))
                {
                    var sessionId = token?.MD5_16() ?? ip;
                    olt = _userService.SetWebStatus(sessionId, p, userAgent, ua, user, ip);
                    ctx.Items["Cube_Online"] = olt;
                }
                await _next.Invoke(ctx);
            }
            catch (Exception ex)
            {
                var uri = ctx.Request.GetRawUrl();
                if (olt != null) olt.SetError(ex.Message);

                XTrace.Log.Error("[{0}]的错误[{1}] {2}", uri, ip, ctx.TraceIdentifier);

                LogProvider.Provider?.WriteLog("访问", "错误", false, ex.Message + " " + uri + Environment.NewLine + ex.GetMessage(), user?.ID ?? 0, user + "", ip);

                XTrace.WriteException(ex);

                // 传递给异常处理页面
                ctx.Items["Exception"] = new ErrorModel
                {
                    RequestId = DefaultSpan.Current?.TraceId ?? Activity.Current?.Id ?? ctx.TraceIdentifier,
                    Uri = uri,
                    Exception = ex
                };

                throw;
            }
            finally
            {
                sw.Stop();

                DAL.LocalFilter = null;
                ManageProvider.UserHost = null;

                // 日志控制
                WriteLogEventArgs.Current.IsWeb = false;
            }
        }

        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒";

        /// <summary>获取执行时间和查询次数等信息</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static String GetInfo(Microsoft.AspNetCore.Http.HttpContext ctx)
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

        private static String CreateSession(HttpContext ctx)
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

                return token;
            }

            return null;
        }

        /// <summary>忽略的后缀</summary>
        public static String[] ExcludeSuffixes { get; set; } = new[] {
            ".html", ".htm", ".js", ".css", ".map", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        private static Boolean ValidRobot(HttpContext ctx, UserAgentParser ua)
        {
            if (ua.Compatible.IsNullOrEmpty()) return true;

            // 判断爬虫
            var code = Setting.Current.RobotError;
            if (code > 0 && ua.IsRobot && !ua.Brower.IsNullOrEmpty())
            {
                var name = ua.Brower;
                var p = name.IndexOf('/');
                if (p > 0) name = name[..p];

                // 埋点
                using var span = DefaultTracer.Instance?.NewSpan($"bot:{name}", ua.UserAgent);

                ctx.Response.StatusCode = code;
                return false;
            }

            return true;
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