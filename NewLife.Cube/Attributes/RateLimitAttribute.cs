using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Model;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.Cube.Attributes;

/// <summary>接口限流。按「接口 + 客户端IP」维度做固定窗口计数，超限返回 HTTP 429 与 Retry-After</summary>
/// <remarks>
/// 用于登录、发码、字典查询、站点配置等匿名或高频接口，防止短信轰炸、暴力破解与资源消耗（OWASP API4/API6）。
/// 计数存放于 <see cref="ICacheProvider"/>，单机内存生效；接入 Redis 后多节点共享。
/// 受 <see cref="CubeSetting.EnableRateLimit"/> 总开关控制（默认开启），阈值按接口用 <see cref="Limit"/>/<see cref="Window"/> 覆盖。
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RateLimitAttribute : ActionFilterAttribute
{
    /// <summary>窗口内允许的最大请求次数，默认 300</summary>
    public Int32 Limit { get; set; } = 300;

    /// <summary>窗口长度（秒），默认 60</summary>
    public Int32 Window { get; set; } = 60;

    /// <summary>执行前校验配额</summary>
    /// <param name="context">动作执行上下文</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var set = CubeSetting.Current;
        if (!set.EnableRateLimit) return;
        if (Limit <= 0 || Window <= 0) return;

        var cache = context.HttpContext.RequestServices?.GetService(typeof(ICacheProvider)) as ICacheProvider;
        if (cache == null) return;

        var ip = context.HttpContext.GetUserHost() + "";
        if (ip.IsNullOrEmpty()) return;

        // 固定窗口：窗口序号参与键名，天然过期，避免长尾键堆积
        var window = Window;
        var bucket = Runtime.TickCount64 / (window * 1000L);
        var action = context.ActionDescriptor?.DisplayName + "";
        var key = $"RateLimit:{action}:{ip}:{bucket}";

        var count = cache.Cache.Increment(key, 1);
        // 必须每次重设过期：Increment 内部用 Set(key,value) 会按默认过期（通常为永不过期）重写，
        // 若不重设，窗口过期后计数仍会累加，最终导致该 IP 永久 429
        cache.Cache.SetExpire(key, TimeSpan.FromSeconds(window + 5));

        if (count <= Limit) return;

        var retry = (Int32)(window - DateTime.Now.Second);
        if (retry < 1) retry = 1;
        context.HttpContext.Response.Headers["Retry-After"] = retry + "";

        XTrace.WriteLine($"Rate limit exceeded: {action} ip={ip} count={count}");

        context.Result = new ContentResult
        {
            Content = new
            {
                code = 429,
                message = L.T("Too many requests. Please try again later."),
                data = (Object)null,
                traceId = DefaultSpan.Current?.TraceId,
            }.ToJson(),
            ContentType = "application/json",
            StatusCode = StatusCodes.Status429TooManyRequests,
        };
    }
}
