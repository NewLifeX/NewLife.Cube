using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>客户端IP解析测试。覆盖可信代理链解析与防伪造</summary>
public class GetUserHostTests : IDisposable
{
    private readonly String _oldTrusted;

    /// <summary>实例化，保存原始可信代理配置</summary>
    public GetUserHostTests() => _oldTrusted = CubeSetting.Current.TrustedProxies;

    /// <summary>恢复原始可信代理配置</summary>
    public void Dispose() => CubeSetting.Current.TrustedProxies = _oldTrusted;

    private static DefaultHttpContext CreateContext(String remoteIp)
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

        return ctx;
    }

    [Fact(DisplayName = "未配置可信代理：兼容旧行为取转发头链首")]
    public void NoTrusted_TakeFirstForwarded()
    {
        CubeSetting.Current.TrustedProxies = null;

        var ctx = CreateContext("10.0.0.1");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4, 10.0.0.1";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("1.2.3.4", ip);
    }

    [Fact(DisplayName = "未配置可信代理：无转发头时取直连地址")]
    public void NoTrusted_FallbackRemote()
    {
        CubeSetting.Current.TrustedProxies = null;

        var ctx = CreateContext("10.9.8.7");

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("10.9.8.7", ip);
    }

    [Fact(DisplayName = "可信代理CIDR：直连可信时从链上取真实客户端")]
    public void TrustedCidr_ParseChain()
    {
        CubeSetting.Current.TrustedProxies = "10.0.0.0/8";

        var ctx = CreateContext("10.0.0.1");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4, 10.0.0.5";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("1.2.3.4", ip);
    }

    [Fact(DisplayName = "可信代理：直连不可信时忽略转发头防伪造")]
    public void UntrustedDirect_IgnoreForwarded()
    {
        CubeSetting.Current.TrustedProxies = "10.0.0.0/8";

        var ctx = CreateContext("8.8.8.8");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("8.8.8.8", ip);
    }

    [Fact(DisplayName = "可信代理通配：10.* 形式生效")]
    public void TrustedWildcard_Works()
    {
        CubeSetting.Current.TrustedProxies = "10.*";

        var ctx = CreateContext("10.1.2.3");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("1.2.3.4", ip);
    }

    [Fact(DisplayName = "全链可信：回退链首地址")]
    public void AllTrusted_FallbackFirst()
    {
        CubeSetting.Current.TrustedProxies = "10.0.0.0/8,1.2.3.0/24";

        var ctx = CreateContext("10.0.0.1");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4, 10.0.0.5";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("1.2.3.4", ip);
    }

    [Fact(DisplayName = "X-Remote-Ip 单值头优先")]
    public void RemoteIpHeader_Priority()
    {
        CubeSetting.Current.TrustedProxies = null;

        var ctx = CreateContext("10.0.0.1");
        ctx.Request.Headers["X-Remote-Ip"] = "5.6.7.8";
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("5.6.7.8", ip);
    }
}
