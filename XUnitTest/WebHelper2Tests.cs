using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using NewLife.Cube;
using NewLife.Cube.Entity;
using Xunit;

namespace XUnitTest;

/// <summary>WebHelper2 外部来源判断单元测试</summary>
/// <remarks>验证 GetExternalRefer 对站内/外部/空值/非法来源的判断，UserOnline 来源记录依赖此逻辑</remarks>
public class WebHelper2Tests
{
    [Theory]
    [DisplayName("GetExternalRefer_站内同主机_返回空")]
    [InlineData("https://cube.newlifex.com/admin", "cube.newlifex.com")]
    [InlineData("HTTP://CUBE.NEWLIFEX.COM/admin/user", "cube.newlifex.com")]
    public void SameHost_ReturnsNull(String refer, String host)
    {
        Assert.Null(WebHelper2.GetExternalRefer(refer, host));
    }

    [Fact]
    [DisplayName("GetExternalRefer_外部来源_返回原值")]
    public void ExternalRefer_ReturnsRefer()
    {
        var refer = "https://erp.newlifex.com/dashboard";

        Assert.Equal(refer, WebHelper2.GetExternalRefer(refer, "cube.newlifex.com"));
    }

    [Theory]
    [DisplayName("GetExternalRefer_空值或无法解析_返回空")]
    [InlineData(null, "cube.newlifex.com")]
    [InlineData("", "cube.newlifex.com")]
    [InlineData("relative/path", "cube.newlifex.com")]
    [InlineData("not a url", "cube.newlifex.com")]
    [InlineData("https://cube.newlifex.com/admin", null)]
    [InlineData("https://cube.newlifex.com/admin", "")]
    public void EmptyOrInvalid_ReturnsNull(String refer, String host)
    {
        Assert.Null(WebHelper2.GetExternalRefer(refer, host));
    }

    [Theory]
    [DisplayName("GetHost_提取主机名")]
    [InlineData("https://erp.newlifex.com/dashboard", "erp.newlifex.com")]
    [InlineData("http://cube.newlifex.com/admin", "cube.newlifex.com")]
    [InlineData("not a url", null)]
    [InlineData(null, null)]
    public void GetHost_Parse(String url, String host)
    {
        Assert.Equal(host, WebHelper2.GetHost(url));
    }

    [Fact]
    [DisplayName("GetSourceUrl_优先取在线会话来源")]
    public void GetSourceUrl_FromOnline()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["Cube_Online"] = new UserOnline { Referer = "https://erp.newlifex.com/dashboard" };

        Assert.Equal("https://erp.newlifex.com/dashboard", ctx.GetSourceUrl());
    }

    [Fact]
    [DisplayName("GetSourceUrl_无在线会话时取请求引用页")]
    public void GetSourceUrl_FromReferer()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Referer"] = "https://erp.newlifex.com/dashboard";
        ctx.Request.Host = new HostString("cube.newlifex.com");

        Assert.Equal("https://erp.newlifex.com/dashboard", ctx.GetSourceUrl());
    }
}
