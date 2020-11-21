using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using NewLife.Common;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Reflection;
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

        /// <summary>会话提供者</summary>
        static readonly SessionProvider _sessionProvider = new SessionProvider();

        /// <summary>实例化</summary>
        /// <param name="next"></param>
        public RunTimeMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

        /// <summary>调用</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext ctx)
        {
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

            // 日志控制，精确标注Web类型线程
            WriteLogEventArgs.Current.IsWeb = true;

            try
            {
                await _next.Invoke(ctx);
            }
            catch (Exception ex)
            {
                var uri = ctx.Request.GetRawUrl();
                var ps = uri.AbsolutePath.Split('/');

                XTrace.Log.Error("[{0}]的错误[{1}] {2}", uri, ManageProvider.UserHost, ctx.TraceIdentifier);

                LogProvider.Provider?.WriteLog("访问", "错误", false, uri + Environment.NewLine + ex.GetMessage());

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