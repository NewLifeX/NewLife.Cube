using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NewLife.Remoting;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>API 异常信封测试。验证 ControllerBaseX 与 ApiFilter 都能把未捕获异常包装为统一 JSON 响应</summary>
public class ApiExceptionEnvelopeTests
{
    /// <summary>构造动作执行后上下文</summary>
    /// <param name="controller">控制器实例</param>
    /// <param name="ex">抛出的异常</param>
    /// <returns></returns>
    private static ActionExecutedContext CreateContext(ControllerBaseX controller, Exception ex)
    {
        var http = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var descriptor = new ControllerActionDescriptor { ControllerName = "Test", ActionName = "Throw" };
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http, ActionDescriptor = descriptor };

        var actionContext = new ActionContext(http, new RouteData(), descriptor);
        return new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller) { Exception = ex };
    }

    /// <summary>读取 JsonResult 匿名对象的字段</summary>
    /// <param name="result">JsonResult 实例</param>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    private static Object GetValue(JsonResult result, String name) => result.Value!.GetType().GetProperty(name)!.GetValue(result.Value)!;

    [Fact(DisplayName = "实体控制器把 XException 映射为 500 并保留消息")]
    public void ControllerBaseX_XException_AsInternalServerError()
    {
        var controller = new ControllerBaseX();
        var context = CreateContext(controller, new XException("数据不存在"));

        controller.OnActionExecuted(context);

        var json = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(500, GetValue(json, "code"));
        Assert.Equal("数据不存在", GetValue(json, "message"));
        Assert.True(context.ExceptionHandled);
    }

    [Fact(DisplayName = "实体控制器把 ApiException 的原错误码与消息透出")]
    public void ControllerBaseX_ApiException_KeepsCode()
    {
        var controller = new ControllerBaseX();
        var context = CreateContext(controller, new ApiException(403, "无权限访问"));

        controller.OnActionExecuted(context);

        var json = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(403, GetValue(json, "code"));
        Assert.Equal("无权限访问", GetValue(json, "message"));
        Assert.True(context.ExceptionHandled);
    }

    [Fact(DisplayName = "实体控制器把未预期异常映射为全局 ApiCode.InternalServerError")]
    public void ControllerBaseX_UnknownException_AsInternalServerError()
    {
        var controller = new ControllerBaseX();
        var context = CreateContext(controller, new InvalidOperationException("boom"));

        controller.OnActionExecuted(context);

        var json = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(500, GetValue(json, "code"));
        Assert.True(context.ExceptionHandled);
    }

    [Fact(DisplayName = "ApiFilter 在结果为空时也能包装异常，而不是丢失")]
    public void ApiFilter_WrapsException_WhenResultIsNull()
    {
        var http = new DefaultHttpContext();
        var descriptor = new ControllerActionDescriptor { ControllerName = "Test", ActionName = "Throw" };
        var actionContext = new ActionContext(http, new RouteData(), descriptor);
        var context = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new Object())
        {
            Exception = new ApiException(403, "无权限访问")
        };

        new ApiFilterAttribute().OnActionExecuted(context);

        var json = Assert.IsType<JsonResult>(context.Result);
        Assert.Equal(403, GetValue(json, "code"));
        Assert.Equal("无权限访问", GetValue(json, "message"));
        Assert.True(context.ExceptionHandled);
    }
}
