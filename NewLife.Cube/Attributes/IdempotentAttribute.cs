using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Caching;
using NewLife.Model;
using XCode.Membership;

namespace NewLife.Cube.Attributes;

/// <summary>幂等执行。客户端携带 <c>Idempotency-Key</c> 请求头时，重复提交复用首次成功结果，避免重复落库/重复计费</summary>
/// <remarks>
/// 仅对显式传头的请求生效（可选增强，默认不影响既有调用）。缓存键含 用户、请求路径与客户端键，
/// 结果缓存 <see cref="Seconds"/> 秒；仅缓存 HTTP 200 的 JSON 结果，失败响应可重试。
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IdempotentAttribute : ActionFilterAttribute
{
    /// <summary>结果缓存时长（秒），默认 600</summary>
    public Int32 Seconds { get; set; } = 600;

    private const String HeaderName = "Idempotency-Key";

    /// <summary>命中缓存时直接返回首次结果</summary>
    /// <param name="context">动作执行上下文</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var key = BuildKey(context);
        if (key == null) return;

        var json = GetCache(context)?.Get<String>(key);
        if (json.IsNullOrEmpty()) return;

        context.Result = new ContentResult
        {
            Content = json,
            ContentType = "application/json; charset=utf-8",
            StatusCode = 200,
        };
    }

    /// <summary>缓存本次成功结果</summary>
    /// <param name="context">动作执行上下文</param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is not ContentResult result) return;
        if (result.Content.IsNullOrEmpty() || result.StatusCode is not (null or 200)) return;

        var key = BuildKey(context);
        if (key == null) return;

        GetCache(context)?.Set(key, result.Content, Seconds);
    }

    private static ICache GetCache(ActionContext context) =>
        (context.HttpContext.RequestServices?.GetService(typeof(ICacheProvider)) as ICacheProvider)?.Cache;

    private static String BuildKey(ActionContext context)
    {
        var id = context.HttpContext.Request.Headers[HeaderName] + "";
        if (id.IsNullOrEmpty()) return null;

        var userId = ManageProvider.User?.ID ?? 0;
        return $"Idem:{userId}:{context.HttpContext.Request.Path}:{id}";
    }
}
