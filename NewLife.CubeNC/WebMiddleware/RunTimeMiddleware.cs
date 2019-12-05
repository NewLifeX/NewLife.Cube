using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NewLife.Common;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace NewLife.Cube.WebMiddleware
{
    /// <summary>页面查询执行时间中间件</summary>
    public class RunTimeMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>实例化</summary>
        /// <param name="next"></param>
        public RunTimeMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

        /// <summary>调用</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext ctx)
        {
            ManageProvider.UserHost = ctx.GetUserHost();

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

            await _next.Invoke(ctx);

            sw.Stop();

            DAL.LocalFilter = null;
            ManageProvider.UserHost = null;
        }

        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒";

        /// <summary>获取执行时间和查询次数等信息</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static String GetInfo(HttpContext ctx)
        {
            //var ctx = NewLife.Web.HttpContext.Current;
            //var ts = DateTime.Now - (DateTime)ctx.Items[_RequestTimestamp];

            //var startQueryTimes = (Int32)ctx.Items[_QueryTimes];
            //var startExecuteTimes = (Int32)ctx.Items[_ExecuteTimes];

            var rtinf = ctx.Items[nameof(RunTimeInfo)] as RunTimeInfo;
            if (rtinf == null) return null;

            var inf = String.Format(DbRunTimeFormat,
                                    DAL.QueryTimes - rtinf.QueryTimes,
                                    DAL.ExecuteTimes - rtinf.ExecuteTimes,
                                    rtinf.Watch.Elapsed.TotalMilliseconds);

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop)
            {
                //var list = ctx.Items["XCode_SQLList"] as List<String>;
                var list = rtinf.Sqls;
                if (list != null && list.Count > 0) inf += "<br />" + list.Select(e => HttpUtility.HtmlEncode(e)).Join("<br />" + Environment.NewLine);
            }

            return inf;
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

    /// <summary>中间件扩展</summary>
    public static class RunTimeMiddlewareExtensions
    {
        /// <summary>使用执行时间模块</summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRunTime(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<RunTimeMiddleware>();
        }
    }
}