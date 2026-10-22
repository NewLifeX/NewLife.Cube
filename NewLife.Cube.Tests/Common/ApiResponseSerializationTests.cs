using System;
using System.Collections.Generic;
using NewLife.Cube;
using NewLife.Web;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>API 响应信封序列化契约测试。锁定"只有 code 与 data 恒定输出"的约定</summary>
public class ApiResponseSerializationTests
{
    /// <summary>暴露受保护序列化方法的测试控制器</summary>
    private class TestController : ControllerBaseX
    {
        public String Serialize(Object data) => OnJsonSerialize(data);
    }

    [Fact(DisplayName = "空消息、空跟踪号与空字段错误均不输出")]
    public void Serialize_OmitsNullEnvelopeFields()
    {
        var json = new TestController().Serialize(new ApiResponse<Object> { Code = 0, Data = new { id = 1 } });

        Assert.Contains("\"code\":0", json);
        Assert.Contains("\"data\"", json);
        Assert.DoesNotContain("message", json);
        Assert.DoesNotContain("traceId", json);
        Assert.DoesNotContain("fieldErrors", json);
    }

    [Fact(DisplayName = "有值时正常输出消息、跟踪号与字段错误")]
    public void Serialize_KeepsPresentEnvelopeFields()
    {
        var data = new ApiResponse<Object>
        {
            Code = -2,
            Message = "参数错误",
            TraceId = "trace-1",
            FieldErrors = [new FieldError { Field = "Name", Message = "必填" }],
        };

        var json = new TestController().Serialize(data);

        Assert.Contains("\"message\":\"参数错误\"", json);
        Assert.Contains("\"traceId\":\"trace-1\"", json);
        Assert.Contains("\"fieldErrors\"", json);
    }

    [Fact(DisplayName = "列表响应无统计行时不输出 stat")]
    public void Serialize_OmitsNullStat()
    {
        var data = new ApiListResponse<Object>
        {
            Code = 0,
            Data = new List<Object>(),
            Page = new PageModel { PageIndex = 1, PageSize = 20, TotalCount = 0 },
        };

        var json = new TestController().Serialize(data);

        Assert.Contains("\"page\"", json);
        Assert.DoesNotContain("\"stat\"", json);
    }
}
