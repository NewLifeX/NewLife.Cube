using System.Diagnostics;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http.Features;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Security;
using NewLife.Web;
using Stardust.Extensions;
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

        // 设置魔方区域
        TracerMiddleware.AreaNames = CubeService.AreaNames;

        // 创建Session集合。后续 ManageProvider.User 需要用到Session
        var session = CreateSession(ctx);

        var url = HttpExtensions.GetRawUrl(ctx.Request);
        var ip = ctx.GetUserHost();
        ManageProvider.UserHost = ip;

        // 获取当前用户。先找Items，再找Session2，没有自动登录能力
        var user = ManageProvider.User;

        // 安全访问。读取请求体用于威胁检测（仅文本类内容，超限不读）
        var body = await MiddlewareHelper.ReadRequestBodyAsync(ctx);
        var rule = _accessService.Valid(url + "", body, ua, ip, user, session, ctx);
        if (rule != null && rule.ActionKind is AccessActionKinds.Block or AccessActionKinds.Limit)
        {
            if (rule.BlockCode == 302)
            {
                ctx.Response.Redirect(rule.BlockContent);
            }
            else if (rule.BlockCode > 0)
            {
                ctx.Response.StatusCode = rule.BlockCode;
                ctx.Response.ContentType = "text/html; charset=utf-8";
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
                // 外部跳转来源。站内跳转或空时返回空，仅首次外部来源写入在线表
                var refer = WebHelper2.GetExternalRefer(ctx.Request);
                if (user == null)
                    // 匿名请求不采信请求头JWT（GetUserByToken 仅解析不验签可伪造），在线记录以设备/IP标识，避免伪造用户名污染在线列表
                    online = _userService.SetStatus(online, sessionId, deviceId, p, userAgent, ua, 0, null, ip, refer);
                else
                    online = _userService.SetWebStatus(online, sessionId, deviceId, p, userAgent, ua, user, ip, refer);
                //FillDeviceId(ctx, olt);
                session["Online"] = online;
                ctx.Items["Cube_Online"] = online;
            }
            await _next.Invoke(ctx);

            // 响应完成后追踪HTTP状态码，检测爱虹虫或web扫描攻击
            _accessService.TrackResponse(ctx.Response.StatusCode, url + "", ip, user, session);
        }
        catch (Exception ex)
        {
            var uri = HttpExtensions.GetRawUrl(ctx.Request);
            online?.SetError(ex.Message);

            XTrace.Log.Error(L.TLog("[{0}] error [{1}] {2}", uri, ip, ctx.TraceIdentifier));

            LogProvider.Provider?.WriteLog(L.TLog("Access"), L.TLog("Error"), false, ex.Message + " " + uri + Environment.NewLine + ex.GetMessage(), user?.Id ?? 0, user + "", ip);

            XTrace.WriteException(ex);

            // 传递给异常处理页面
            ctx.Items["Exception"] = new ErrorModel
            {
                RequestId = DefaultSpan.Current?.TraceId ?? Activity.Current?.Id ?? ctx.TraceIdentifier,
                Uri = uri?.ToUri(),
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
    /// <remarks>英文为源文，展示前按当前语言翻译，缺译文时回退英文</remarks>
    public static String DbRunTimeFormat { get; set; } = "Queries {0}, executes {1}, elapsed {2:n0} ms";

    /// <summary>获取执行时间和查询次数等信息</summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public static String GetInfo(HttpContext ctx)
    {
        if (ctx.Items[nameof(RunTimeInfo)] is not RunTimeInfo rtinf) return null;

        var inf = String.Format(L.T(DbRunTimeFormat),
                                DAL.QueryTimes - rtinf.QueryTimes,
                                DAL.ExecuteTimes - rtinf.ExecuteTimes,
                                rtinf.Watch.ElapsedMilliseconds);

        // 设计时收集执行的SQL语句
        var sqls = SysConfig.Current.Develop ? rtinf.Sqls : null;
        if (sqls == null || sqls.Count == 0) return $"{RunInfoStyle}<div class=\"cube-runinfo cube-runinfo-static\"><span class=\"cube-runinfo-stats\">{inf}</span></div>";

        var copy = HttpUtility.HtmlEncode(L.T("Copy"));
        var copied = HttpUtility.HtmlEncode(L.T("✓ Copied"));
        var sb = new StringBuilder(RunInfoStyle.Length + 512 + sqls.Count * 128);
        sb.Append(RunInfoStyle);
        sb.Append("<details class=\"cube-runinfo\"><summary class=\"cube-runinfo-bar\">");
        sb.Append("<span class=\"cube-runinfo-stats\">").Append(inf).Append("</span>");
        sb.Append("<span class=\"cube-runinfo-toggle\">");
        sb.Append("<span class=\"cube-runinfo-count\">SQL ").Append(sqls.Count).Append("</span>");
        sb.Append("<span class=\"cube-runinfo-chevron\" aria-hidden=\"true\"></span>");
        sb.Append("</span></summary>");
        sb.Append("<div class=\"cube-runinfo-panel\"><div class=\"cube-runinfo-toolbar\">");
        sb.Append("<button type=\"button\" class=\"cube-runinfo-copy\" data-cube-runinfo-copy");
        sb.Append(" data-copy=\"").Append(copy).Append("\" data-copied=\"").Append(copied).Append("\">");
        sb.Append(copy).Append("</button></div><div class=\"cube-runinfo-list\">");
        foreach (var item in sqls)
        {
            var (source, sql) = SplitSql(item);
            sb.Append("<div class=\"cube-runinfo-item\">");
            if (!source.IsNullOrEmpty()) sb.Append("<span class=\"cube-runinfo-source\">").Append(HttpUtility.HtmlEncode(source)).Append("</span>");
            sb.Append("<code class=\"cube-runinfo-sql\">").Append(HttpUtility.HtmlEncode(sql)).Append("</code>");
            sb.Append("</div>");
        }
        sb.Append("</div></div></details>");
        sb.Append(RunInfoScript);

        return sb.ToString();
    }

    /// <summary>拆分SQL日志中的连接名与语句。日志格式为“[连接名] SQL”</summary>
    /// <param name="value">SQL日志</param>
    /// <returns></returns>
    private static (String? Source, String Sql) SplitSql(String value)
    {
        if (!value.IsNullOrEmpty() && value[0] == '[')
        {
            var p = value.IndexOf(']');
            if (p > 1 && p + 1 < value.Length && value[p + 1] == ' ')
            {
                // 排除 [dbo].[User] 这类以方括号开头的SQL
                var source = value[1..p];
                if (source.IndexOf('.') < 0 && source.IndexOf(' ') < 0) return (source, value[(p + 1)..].TrimStart());
            }
        }

        return (null, value);
    }

    /// <summary>运行信息条的样式。随片段输出，保证各皮肤一致，且仅在显示运行时间时加载</summary>
    private const String RunInfoStyle = """
        <style>
        .cube-runinfo{--cr-primary:var(--cube-primary,#2b7dbc);--cr-primary-light:var(--cube-primary-light,#e8f1f9);--cr-border:var(--cube-border,#e4e6e9);--cr-border-light:var(--cube-border-light,#eef1f4);--cr-text:var(--cube-text,#3a3f44);--cr-muted:var(--cube-text-muted,#8a9199);box-sizing:border-box;width:100%;text-align:left;background:var(--cube-card,#fff);border:1px solid var(--cr-border);border-radius:6px;color:var(--cr-muted);font-size:12px;line-height:1.6;overflow:hidden}
        .cube-runinfo *{box-sizing:border-box}
        .cube-runinfo-bar{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:7px 12px;cursor:pointer;list-style:none;user-select:none;transition:background-color .15s ease}
        .cube-runinfo-bar::-webkit-details-marker{display:none}
        .cube-runinfo-bar:hover{background:#f7fafd}
        .cube-runinfo-bar:focus-visible{outline:2px solid rgba(43,125,188,.35);outline-offset:-2px}
        .cube-runinfo[open]>.cube-runinfo-bar{background:#fafbfc;border-bottom:1px solid var(--cr-border-light)}
        .cube-runinfo-stats{min-width:0}
        .cube-runinfo-toggle{display:inline-flex;align-items:center;gap:6px;flex:none;color:var(--cr-primary);font-weight:600}
        .cube-runinfo-count{padding:0 8px;border-radius:999px;background:var(--cr-primary-light);color:var(--cr-primary);font-size:11px;font-weight:600;line-height:18px;white-space:nowrap;letter-spacing:.2px}
        .cube-runinfo-chevron{width:6px;height:6px;border-right:1.5px solid currentColor;border-bottom:1.5px solid currentColor;transform:rotate(-45deg);transition:transform .18s ease;margin-right:2px}
        .cube-runinfo[open] .cube-runinfo-chevron{transform:rotate(45deg)}
        .cube-runinfo-panel{padding:0 12px 8px;background:#fbfcfd}
        .cube-runinfo-toolbar{display:flex;justify-content:flex-end;padding:6px 0 2px}
        .cube-runinfo-copy{padding:0;border:0;border-radius:0;background:none;color:var(--cr-primary);font:inherit;font-size:12px;line-height:1.4;cursor:pointer;opacity:.85}
        .cube-runinfo-copy:hover{opacity:1;text-decoration:underline}
        .cube-runinfo-list{max-height:320px;overflow:auto;scrollbar-width:thin}
        .cube-runinfo-item{display:flex;align-items:flex-start;gap:8px;padding:6px 0;border-top:1px dashed var(--cr-border-light)}
        .cube-runinfo-item:first-child{border-top:0}
        .cube-runinfo-source{flex:none;margin-top:1px;padding:0 6px;border-radius:999px;background:var(--cr-primary-light);color:var(--cr-primary);font-size:11px;line-height:18px;white-space:nowrap}
        .cube-runinfo-sql{flex:1;min-width:0;margin:0;padding:0;border:0;border-radius:0;background:none;color:var(--cr-text);font-family:Consolas,Monaco,"Courier New",monospace;font-size:12px;line-height:1.6;white-space:pre-wrap;overflow-wrap:anywhere;word-break:break-word}
        .cube-runinfo-static{padding:7px 12px}
        @media (max-width:767px){.cube-runinfo-bar{flex-wrap:wrap;gap:2px 12px}.cube-runinfo-list{max-height:240px}}
        </style>
        """;

    /// <summary>复制SQL的脚本。随片段输出，保证各皮肤一致</summary>
    private const String RunInfoScript = """
        <script>
        (function () {
            if (window.cubeRunInfoCopy) return;
            window.cubeRunInfoCopy = true;
            document.addEventListener('click', function (e) {
                var btn = e.target && e.target.closest ? e.target.closest('[data-cube-runinfo-copy]') : null;
                if (!btn) return;
                var panel = btn.closest('.cube-runinfo-panel');
                if (!panel) return;
                var text = Array.prototype.map.call(panel.querySelectorAll('.cube-runinfo-sql'), function (x) { return x.textContent; }).join('\n');
                var done = function () {
                    btn.textContent = btn.getAttribute('data-copied');
                    setTimeout(function () { btn.textContent = btn.getAttribute('data-copy'); }, 1600);
                };
                var fallback = function () {
                    var box = document.createElement('textarea');
                    box.value = text;
                    box.style.position = 'fixed';
                    box.style.opacity = '0';
                    document.body.appendChild(box);
                    box.select();
                    try { document.execCommand('copy'); } catch (ex) { }
                    box.remove();
                    done();
                };
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(text).then(done, fallback);
                } else {
                    fallback();
                }
            });
        })();
        </script>
        """;

    private static IDictionary<String, Object> CreateSession(HttpContext ctx)
    {
        // 准备Session，避免未启用Session时ctx.Session直接抛出异常
        //var ss = ctx.Session;
        var ss = ctx.Features.Get<ISessionFeature>()?.Session;
        if (ss != null && !ss.IsAvailable) ss = null;

        // 关键点在于确定一个唯一的SessionId，没有Session和Cookie时，使用令牌
        var key = ".Cube.Session";
        var sid = "";
        if (sid.IsNullOrEmpty()) sid = ss?.GetString(key);
        if (sid.IsNullOrEmpty()) sid = ctx.Request.Cookies[key];
        if (sid.IsNullOrEmpty()) sid = ctx.Request.Cookies[".AspNetCore.Session"];
        if (sid.IsNullOrEmpty()) sid = ctx.LoadToken();
        if (sid.IsNullOrEmpty())
        {
            sid = Rand.NextString(16);
            if (ss != null)
                ss.SetString(key, sid);
            else
                ctx.Response.Cookies.Append(key, sid);
        }
        else
        {
            // 避免过长
            if (sid.Length > 32) sid = sid.MD5();

            ss?.SetString(key, sid);
        }

        var session = SessionProvider.Instance.GetSession(sid);
        ctx.Items["Session"] = session;

        return session;
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
