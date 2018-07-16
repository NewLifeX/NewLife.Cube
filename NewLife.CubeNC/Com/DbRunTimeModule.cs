using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NewLife.Common;
using NewLife.CubeNC.WebMiddleware;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace NewLife.Cube
{
    /// <summary>页面查询执行时间中间件</summary>
    public class DbRunTimeModule
    {
        private readonly RequestDelegate _next;

        public DbRunTimeModule(RequestDelegate next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
        }

        public async Task Invoke(HttpContext ctx)
        {
            ctx.Items[_QueryTimes] = DAL.QueryTimes;
            ctx.Items[_ExecuteTimes] = DAL.ExecuteTimes;
            ctx.Items[_RequestTimestamp] = DateTime.Now;

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop) ctx.Items["XCode_SQLList"] = new List<String>();

            await _next.Invoke(ctx);

            var i = 0;
            i++;
            var ts = DateTime.Now - (DateTime)ctx.Items[_RequestTimestamp];
        }

        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒";

        const String _QueryTimes = "DAL.QueryTimes";
        const String _ExecuteTimes = "DAL.ExecuteTimes";
        const String _RequestTimestamp = "RequestTimestamp";


        /// <summary>初始化模块，准备拦截请求。</summary>
        //void OnInit()
        //{
        //    var ctx = HttpContext.Current;
        //    ctx.Items[_QueryTimes] = DAL.QueryTimes;
        //    ctx.Items[_ExecuteTimes] = DAL.ExecuteTimes;

        //    // 设计时收集执行的SQL语句
        //    if (SysConfig.Current.Develop) ctx.Items["XCode_SQLList"] = new List<String>();
        //}

        private static Boolean _tip;
        /// <summary>获取执行时间和查询次数等信息</summary>
        /// <returns></returns>
        public static String GetInfo()
        {
            var ctx = NewLife.Web.HttpContext.Current;
            var ts = DateTime.Now - (DateTime)ctx.Items[_RequestTimestamp];

            //if (!ctx.Items.Contains(_QueryTimes) || !ctx.Items.Contains(_ExecuteTimes))
            //{
            //    //throw new XException("设计错误！需要在web.config中配置{0}", typeof(DbRunTimeModule).FullName);
            //    if (!_tip)
            //    {
            //        _tip = true;
            //        XTrace.WriteLine("设计错误！需要在web.config中配置{0}", typeof(DbRunTimeModule).FullName);
            //    }
            //    return null;
            //}

            var StartQueryTimes = (Int32)ctx.Items[_QueryTimes];
            var StartExecuteTimes = (Int32)ctx.Items[_ExecuteTimes];

            var inf = String.Format(DbRunTimeFormat, DAL.QueryTimes - StartQueryTimes, DAL.ExecuteTimes - StartExecuteTimes, ts.TotalMilliseconds);

            // 设计时收集执行的SQL语句
            if (SysConfig.Current.Develop)
            {
                var list = ctx.Items["XCode_SQLList"] as List<String>;
                if (list != null && list.Count > 0) inf += "<br />" + list.Select(e => HttpUtility.HtmlEncode(e)).Join("<br />" + Environment.NewLine);
            }

            return inf;
        }

        private static Boolean? _Enable;
        /// <summary>是否启用显示运行时间</summary>
        public static Boolean Enable
        {
            get
            {
                if (_Enable == null) _Enable = Setting.Current.ShowRunTime;
                return _Enable.Value;
            }
        }
    }

    public static class DbRunTimeMiddlewareExtensions
    {
        public static IApplicationBuilder UseDbRunTimeModule(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<DbRunTimeModule>();
        }
    }
}