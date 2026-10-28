using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using NewLife.Cube;
using NewLife.Cube.Attributes;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>CON-09 幂等测试。覆盖携带 Idempotency-Key 时重复提交复用首次成功结果，以及未携带时不缓存。</summary>
[Collection("TenantAuth")]
public class IdempotencyTests
{
    private readonly IServiceProvider _services;

    /// <summary>实例化</summary>
    public IdempotencyTests()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<ICacheProvider>(new TestCacheProvider());
        _services = sc.BuildServiceProvider();
    }

    private ActionContext CreateActionContext(String? idempotencyKey, Int32? responseCode = null)
    {
        var http = new DefaultHttpContext { RequestServices = _services };
        http.Request.Path = "/api/admin/user/import";
        if (!idempotencyKey.IsNullOrEmpty()) http.Request.Headers["Idempotency-Key"] = idempotencyKey;
        if (responseCode != null) http.Items[ControllerBaseX.ResponseCodeKey] = responseCode.Value;

        return new ActionContext(http, new RouteData(), new ActionDescriptor());
    }

    [Fact(DisplayName = "幂等_相同键复用首次成功结果")]
    public void SameKey_ReplaysFirstResult()
    {
        var attr = new IdempotentAttribute();
        var key = Guid.NewGuid().ToString("N");

        var first = new ActionExecutingContext(CreateActionContext(key), [], new Dictionary<String, Object>(), null);
        attr.OnActionExecuting(first);
        Assert.Null(first.Result);

        var executed = new ActionExecutedContext(CreateActionContext(key, 0), [], null)
        {
            Result = new ContentResult { Content = "{\"code\":0,\"data\":1}", StatusCode = 200 },
        };
        attr.OnActionExecuted(executed);

        var second = new ActionExecutingContext(CreateActionContext(key), [], new Dictionary<String, Object>(), null);
        attr.OnActionExecuting(second);

        var replay = Assert.IsType<ContentResult>(second.Result);
        Assert.Equal(200, replay.StatusCode);
        Assert.Contains("\"code\":0", replay.Content);
    }

    [Fact(DisplayName = "幂等_失败结果不缓存，允许修正后重试")]
    public void FailureResult_NotCached()
    {
        var attr = new IdempotentAttribute();
        var key = Guid.NewGuid().ToString("N");

        var executed = new ActionExecutedContext(CreateActionContext(key, 500), [], null)
        {
            Result = new ContentResult { Content = "{\"code\":500}", StatusCode = 200 },
        };
        attr.OnActionExecuted(executed);

        var next = new ActionExecutingContext(CreateActionContext(key), [], new Dictionary<String, Object>(), null);
        attr.OnActionExecuting(next);
        Assert.Null(next.Result);
    }

    [Fact(DisplayName = "幂等_未携带键时不缓存与不复用")]
    public void NoKey_NoReplay()
    {
        var attr = new IdempotentAttribute();

        var first = new ActionExecutingContext(CreateActionContext(null), [], new Dictionary<String, Object>(), null);
        attr.OnActionExecuting(first);
        attr.OnActionExecuted(new ActionExecutedContext(CreateActionContext(null), [], null)
        {
            Result = new ContentResult { Content = "{\"code\":0}", StatusCode = 200 },
        });

        var second = new ActionExecutingContext(CreateActionContext(null), [], new Dictionary<String, Object>(), null);
        attr.OnActionExecuting(second);
        Assert.Null(second.Result);
    }
}
