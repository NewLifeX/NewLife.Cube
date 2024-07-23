using System.Diagnostics;
using System.Web;
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

namespace NewLife.Cube.WebMiddleware;

/// <summary>运行时中间件。页面查询执行时间、异常拦截</summary>
public class RunTimeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly UserService _userService;
    private readonly AccessService _accessService;

    /// <summary>会话提供者</summary>
    static readonly SessionProvider _sessionProvider = new();

    /// <summary>实例化</summary>
    /// <param name="next"></param>
    /// <param name="userService"></param>
    /// <param name="accessService"></param>
    public RunTimeMiddleware(RequestDelegate next, UserService userService, AccessService accessService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _userService = userService;
        _accessService = accessService;
    }

    /// <summary>调用</summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext ctx)
    {
        // 分析浏览器
        var userAgent = ctx.Request.Headers.UserAgent + "";
        var ua = new UserAgentParser();
        ua.Parse(userAgent);
        ctx.Items["UserAgent"] = ua;

        // 识别拦截爬虫
        if (!WebHelper.ValidRobot(ctx, ua)) return;

        // 强制访问Https
        if (MiddlewareHelper.CheckForceRedirect(ctx)) return;

        // 创建Session集合。后续 ManageProvider.User 需要用到Session
        var (token, session) = CreateSession(ctx);

        var url = ctx.Request.GetRawUrl();
        var ip = ctx.GetUserHost();
        ManageProvider.UserHost = ip;

        // 获取当前用户。先找Items，再找Session2，没有自动登录能力
        var user = ManageProvider.User;

        // 安全访问
        var rule = _accessService.Valid(url + "", ua, ip, user, session);
        if (rule != null && rule.ActionKind is AccessActionKinds.Block or AccessActionKinds.Limit)
        {
            if (rule.BlockCode == 302)
            {
                ctx.Response.Redirect(rule.BlockContent);
            }
            else if (rule.BlockCode > 0)
            {
                ctx.Response.StatusCode = rule.BlockCode;
                ctx.Response.ContentType = "text/html";
                await ctx.Response.WriteAsync(rule.BlockContent);
                await ctx.Response.CompleteAsync();
            }
            else
            {
                ctx.Abort();
            }

            return;
        }

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
            inf.Sqls = [];
            DAL.LocalFilter = s => inf.Sqls.Add(s);
        }

        // 日志控制，精确标注Web类型线程
        WriteLogEventArgs.Current.IsWeb = true;

        var online = session["Online"] as UserOnline;
        try
        {
            var set = CubeSetting.Current;
            var p = ctx.Request.Path + "";
            if (!p.EndsWithIgnoreCase(ExcludeSuffixes) &&
                (set.EnableUserOnline == 2 || set.EnableUserOnline == 1 && user != null))
            {
                // 浏览器设备标识作为会话标识
                var deviceId = WebHelper.FillDeviceId(ctx);
                //var sessionId = token?.MD5_16() ?? ip;
                var sessionId = deviceId;
                online = _userService.SetWebStatus(online, sessionId, deviceId, p, userAgent, ua, user, ip);
                //FillDeviceId(ctx, olt);
                session["Online"] = online;
                ctx.Items["Cube_Online"] = online;
            }
            await _next.Invoke(ctx);
        }
        catch (Exception ex)
        {
            var uri = ctx.Request.GetRawUrl();
            online?.SetError(ex.Message);

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
    public static String GetInfo(HttpContext ctx)
    {
        if (ctx.Items[nameof(RunTimeInfo)] is not RunTimeInfo rtinf) return null;

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

    private static (String, IDictionary<String, Object>) CreateSession(HttpContext ctx)
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
            var session = _sessionProvider.GetSession(token);
            ctx.Items["Session"] = session;

            return (token, session);
        }

        return (null, null);
    }

    /// <summary>忽略的后缀</summary>
    public static String[] ExcludeSuffixes { get; set; } = new[] {
        ".html", ".htm", ".js", ".css", ".map", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
        ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
    };
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