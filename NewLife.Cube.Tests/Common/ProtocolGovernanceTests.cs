using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Caching;
using NewLife.Cube;
using NewLife.Cube.Attributes;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// 协议治理测试。覆盖限流拒绝（HTTP 429 + Retry-After）、协议层状态码映射开关（CON-05），
/// 以及查询串令牌的弃用与 Authorization 头优先（SEC-04）。
/// </summary>
[Collection("TenantAuth")]
public class ProtocolGovernanceTests
{
    private readonly IServiceProvider _services;

    /// <summary>实例化</summary>
    public ProtocolGovernanceTests()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<ICacheProvider>(new TestCacheProvider());
        _services = sc.BuildServiceProvider();
    }

    private ActionExecutingContext CreateContext(String action = "Test.Login")
    {
        var http = new DefaultHttpContext { RequestServices = _services };
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.1.2.3");

        var descriptor = new ControllerActionDescriptor { DisplayName = action };

        return new ActionExecutingContext(
            new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), descriptor),
            [],
            new Dictionary<String, Object>(),
            null);
    }

    #region CON-07 限流
    [Fact(DisplayName = "限流_超过阈值返回429并带RetryAfter")]
    public void RateLimit_Exceeded_Returns429()
    {
        var prev = CubeSetting.Current.EnableRateLimit;
        CubeSetting.Current.EnableRateLimit = true;
        try
        {
            var attr = new RateLimitAttribute { Limit = 2, Window = 60 };

            for (var i = 0; i < 2; i++)
            {
                var ctx = CreateContext();
                attr.OnActionExecuting(ctx);
                Assert.Null(ctx.Result);
            }

            var third = CreateContext();
            attr.OnActionExecuting(third);

            var result = Assert.IsType<ContentResult>(third.Result);
            Assert.Equal(StatusCodes.Status429TooManyRequests, result.StatusCode);
            Assert.False(String.IsNullOrEmpty(third.HttpContext.Response.Headers["Retry-After"] + ""));
            Assert.Contains("\"code\":429", result.Content);
        }
        finally
        {
            CubeSetting.Current.EnableRateLimit = prev;
        }
    }

    [Fact(DisplayName = "限流_总开关关闭时不拦截")]
    public void RateLimit_Disabled_NoIntercept()
    {
        var prev = CubeSetting.Current.EnableRateLimit;
        CubeSetting.Current.EnableRateLimit = false;
        try
        {
            var attr = new RateLimitAttribute { Limit = 1, Window = 60 };

            for (var i = 0; i < 5; i++)
            {
                var ctx = CreateContext();
                attr.OnActionExecuting(ctx);
                Assert.Null(ctx.Result);
            }
        }
        finally
        {
            CubeSetting.Current.EnableRateLimit = prev;
        }
    }
    #endregion

    #region CON-05 协议层状态码
    private sealed class StatusProbe : ControllerBaseX
    {
        public Int32 Status(Int32 code) => GetHttpStatusCode(code);
    }

    [Fact(DisplayName = "状态码映射_开关开启时协议码写回对应状态")]
    public void HttpStatus_MappedWhenEnabled()
    {
        var prev = CubeSetting.Current.EnableHttpStatusCodes;
        CubeSetting.Current.EnableHttpStatusCodes = true;
        try
        {
            var probe = new StatusProbe();
            Assert.Equal(401, probe.Status(401));
            Assert.Equal(403, probe.Status(403));
            Assert.Equal(404, probe.Status(404));
            Assert.Equal(429, probe.Status(429));
            Assert.Equal(500, probe.Status(500));

            // 业务码保持 200
            Assert.Equal(200, probe.Status(0));
            Assert.Equal(200, probe.Status(-1));
            Assert.Equal(200, probe.Status(-4));
        }
        finally
        {
            CubeSetting.Current.EnableHttpStatusCodes = prev;
        }
    }

    [Fact(DisplayName = "状态码映射_开关关闭时全部200")]
    public void HttpStatus_All200WhenDisabled()
    {
        var prev = CubeSetting.Current.EnableHttpStatusCodes;
        CubeSetting.Current.EnableHttpStatusCodes = false;
        try
        {
            var probe = new StatusProbe();
            foreach (var code in new[] { 0, -1, 401, 403, 404, 429, 500 })
                Assert.Equal(200, probe.Status(code));
        }
        finally
        {
            CubeSetting.Current.EnableHttpStatusCodes = prev;
        }
    }
    #endregion

    #region SEC-04 令牌来源
    [Fact(DisplayName = "令牌解析_Authorization头优先于查询串")]
    public void Token_HeaderWins()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Authorization"] = "Bearer header-token";

        Assert.Equal("header-token", ctx.ResolveAccessToken("query-token"));
        Assert.True(String.IsNullOrEmpty(ctx.Response.Headers["Warning"] + ""));
    }

    [Fact(DisplayName = "令牌解析_仅查询串时输出弃用告警")]
    public void Token_QueryFallback_Warns()
    {
        var ctx = new DefaultHttpContext();

        Assert.Equal("query-token", ctx.ResolveAccessToken("query-token"));
        Assert.Contains("deprecated", ctx.Response.Headers["Warning"] + "");
    }
    #endregion
}
