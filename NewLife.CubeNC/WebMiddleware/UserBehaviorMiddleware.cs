using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Model;
using NewLife.Threading;
using XCode.Membership;

namespace NewLife.Cube.WebMiddleware
{
    /// <summary>用户行为中间件</summary>
    public class UserBehaviorMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>实例化</summary>
        /// <param name="next"></param>
        public UserBehaviorMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

        /// <summary>调用</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext ctx)
        {
            var sw = Stopwatch.StartNew();

            Exception error = null;
            try
            {
                await _next.Invoke(ctx);
            }
            catch (Exception ex)
            {
                error = ex;

                throw;
            }
            finally
            {
                sw.Stop();

                ProcessUserBehavior(ctx, error, sw);
            }
        }

        #region 用户行为
        void ProcessUserBehavior(HttpContext ctx, Exception error, Stopwatch sw)
        {
            if (ctx == null) return;

            var set = Setting.Current;
            if (!set.WebOnline && !set.WebBehavior && !set.WebStatistics) return;

            var req = ctx.Request;
            if (req == null) return;

            //var user = ctx.User?.Identity as IManageUser ?? ManageProvider.User as IManageUser;
            var user = ctx.User?.Identity as IManageUser;

            //var sid = ctx.Session?.Id;
            var sid = ctx.User?.Identity?.Name;
            var ip = ctx.GetUserHost();

            var page = GetPage(req);

            // 过滤后缀
            var ext = Path.GetExtension(page);
            if (!ext.IsNullOrEmpty() && ExcludeSuffixes.Contains(ext)) return;

            var title = GetTitle(ctx, req);
            var msg = GetMessage(ctx, req, title, error, sw);

            ThreadPoolX.QueueUserWorkItem(() =>
            {
                try
                {
                    // 统计网页状态
                    if (set.WebOnline && !sid.IsNullOrEmpty()) UserOnline.SetWebStatus(sid, page, msg, user, ip);

                    // 记录用户访问的Url
                    if (set.WebBehavior) SaveBehavior(user, ip, page, msg, error);

                    // 每个页面的访问统计
                    if (set.WebStatistics) SaveStatistics(ctx, user, ip, page, title, error, sw);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            });
        }

        String GetMessage(HttpContext ctx, HttpRequest req, String title, Exception error, Stopwatch sw)
        {
            var sb = Pool.StringBuilder.Get();

            if (!title.IsNullOrEmpty()) sb.Append(title + " ");
            sb.AppendFormat("{0} {1}", req.Method, req.Path);

            var err = error?.Message;
            if (!err.IsNullOrEmpty()) sb.Append(" " + err);

            sb.AppendFormat(" {0:n0}ms", sw.ElapsedMilliseconds);

            return sb.Put(true);
        }

        String GetTitle(HttpContext ctx, HttpRequest req)
        {
            var title = ctx.Items["Title"] + "";
            //if (title.IsNullOrEmpty()) title = req.Path;

            // 有些标题是 Description，需要截断处理
            if (title.Contains(",")) title = title.Substring(null, ",");
            if (title.Contains("，")) title = title.Substring(null, "，");
            if (title.Contains("。")) title = title.Substring(null, "。");

            return title;
        }

        String GetPage(HttpRequest req)
        {
            var p = req.Path + "";
            if (p.IsNullOrEmpty()) return null;

            var ss = p.Split('/');
            if (ss.Length == 0) return p;

            // 如果最后一段是数字，则可能是参数，需要去掉
            if (ss[^1].ToInt() > 0) p = "/" + ss.Take(ss.Length - 1).Join("/");

            return p;
        }

        /// <summary>忽略的后缀</summary>
        public static HashSet<String> ExcludeSuffixes { get; set; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase) {
            ".js", ".css", ".png", ".jpg", ".gif", ".ico", ".map",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        void SaveBehavior(IManageUser user, String ip, String page, String msg, Exception error)
        {
            if (page.IsNullOrEmpty()) return;

            if (error != null)
                LogProvider.Provider?.WriteLog("访问", "错误", msg, user?.ID ?? 0, user + "", ip);
            else
                LogProvider.Provider?.WriteLog("访问", "记录", msg, user?.ID ?? 0, user + "", ip);
        }

        void SaveStatistics(HttpContext ctx, IManageUser user, String ip, String page, String title, Exception error, Stopwatch sw)
        {
            var model = new VisitStatModel
            {
                Time = DateTime.Now,
                Page = page,
                Title = title,
                Cost = (Int32)sw.ElapsedMilliseconds,
                User = user + "",
                IP = ip,
                Error = error?.Message,
            };

            VisitStat.Process(model);
        }
        #endregion
    }
}