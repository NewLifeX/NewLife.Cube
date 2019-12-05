using System;
using System.Collections.Generic;
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

            ctx.Items[_QueryTimes] = DAL.QueryTimes;
            ctx.Items[_ExecuteTimes] = DAL.ExecuteTimes;
            ctx.Items[_RequestTimestamp] = DateTime.Now;

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop)
            {
                var sqlList = new List<String>();
                ctx.Items["XCode_SQLList"] = sqlList;
                DAL.LocalFilter = s =>
                {
                    sqlList.Add(s);
                };
            }

            await _next.Invoke(ctx);

            DAL.LocalFilter = null;
            ManageProvider.UserHost = null;
        }

        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒";

        const String _QueryTimes = "DAL.QueryTimes";
        const String _ExecuteTimes = "DAL.ExecuteTimes";
        const String _RequestTimestamp = "RequestTimestamp";

        /// <summary>获取执行时间和查询次数等信息</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static String GetInfo(HttpContext ctx)
        {
            //var ctx = NewLife.Web.HttpContext.Current;
            var ts = DateTime.Now - (DateTime)ctx.Items[_RequestTimestamp];

            var startQueryTimes = (Int32)ctx.Items[_QueryTimes];
            var startExecuteTimes = (Int32)ctx.Items[_ExecuteTimes];

            var inf = String.Format(DbRunTimeFormat, DAL.QueryTimes - startQueryTimes, DAL.ExecuteTimes - startExecuteTimes, ts.TotalMilliseconds);

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop)
            {
                var list = ctx.Items["XCode_SQLList"] as List<String>;
                if (list != null && list.Count > 0) inf += "<br />" + list.Select(e => HttpUtility.HtmlEncode(e)).Join("<br />" + Environment.NewLine);
            }

            return inf;
        }
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