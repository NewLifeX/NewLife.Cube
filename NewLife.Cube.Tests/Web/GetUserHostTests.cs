using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>客户端IP解析测试。覆盖可信代理链解析与防伪造</summary>
public class GetUserHostTests : IDisposable
{
    private readonly String _oldTrusted;
    private readonly String _oldLearned;
    private readonly Int32 _oldLearning;

    /// <summary>实例化，保存原始可信代理配置；用例期内关闭学习并清空已学习代理，避免跨用例与跨运行污染</summary>
    public GetUserHostTests()
    {
        var set = CubeSetting.Current;
        _oldTrusted = set.TrustedProxies;
        _oldLearned = set.LearnedProxies;
        _oldLearning = set.TrustedProxyLearning;

        set.TrustedProxyLearning = 0;
        set.LearnedProxies = null;
    }

    /// <summary>恢复原始可信代理配置。学习用例可能已写入配置文件，恢复盘面避免污染后续运行</summary>
    public void Dispose()
    {
        var set = CubeSetting.Current;
        var dirty = set.LearnedProxies != _oldLearned;

        set.TrustedProxies = _oldTrusted;
        set.LearnedProxies = _oldLearned;
        set.TrustedProxyLearning = _oldLearning;

        if (dirty)
        {
            try
            {
                set.Save();
            }
            catch
            {
                // 测试环境配置保存失败无碍用例结果
            }
        }
    }

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

    [Fact(DisplayName = "自动学习：内网直连来源被学习为可信代理")]
    public void AutoLearn_InnerRemote_Learned()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = null;
        set.LearnedProxies = null;
        set.TrustedProxyLearning = 4;

        var ctx = CreateContext("10.1.2.3");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";

        var ip = ((HttpContext)ctx).GetUserHost();

        // 直连地址被学习，转发头被信任并解析出真实客户端
        Assert.Equal("1.2.3.4", ip);
        Assert.Contains("10.1.2.3", CubeSetting.Current.LearnedProxies);
    }

    [Fact(DisplayName = "自动学习：公网直连来源不被学习")]
    public void AutoLearn_PublicRemote_NotLearned()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = null;
        set.LearnedProxies = null;
        set.TrustedProxyLearning = 4;

        var ctx = CreateContext("8.8.4.4");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("1.2.3.4", ip);
        Assert.Null(CubeSetting.Current.LearnedProxies);
    }

    [Fact(DisplayName = "已学习代理：参与链解析")]
    public void LearnedProxies_ParticipateInChain()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = null;
        set.LearnedProxies = "10.0.0.5";
        set.TrustedProxyLearning = 0;

        var ctx = CreateContext("10.0.0.5");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4, 10.0.0.5";

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("1.2.3.4", ip);
    }

    [Fact(DisplayName = "可信代理判定：学习地址受封禁保护")]
    public void IsTrustedProxyAddress_Learned_Protected()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = null;
        set.LearnedProxies = "10.0.0.5";
        set.TrustedProxyLearning = 0;

        Assert.True(WebHelper2.IsTrustedProxyAddress("10.0.0.5"));
        Assert.False(WebHelper2.IsTrustedProxyAddress("1.2.3.4"));
    }

    [Fact(DisplayName = "自动学习：无转发头不学习")]
    public void AutoLearn_NoForwarded_NotLearned()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = null;
        set.LearnedProxies = null;
        set.TrustedProxyLearning = 4;

        // 内网直连但不携带转发头：只是普通内网机器，不视为代理
        var ctx = CreateContext("10.2.3.4");

        var ip = ((HttpContext)ctx).GetUserHost();

        Assert.Equal("10.2.3.4", ip);
        Assert.Null(CubeSetting.Current.LearnedProxies);
    }

    [Fact(DisplayName = "自动学习：数量为0时不学习")]
    public void AutoLearn_ZeroDisables()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = null;
        set.LearnedProxies = null;
        set.TrustedProxyLearning = 0;

        var ctx = CreateContext("10.2.3.5");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";

        var ip = ((HttpContext)ctx).GetUserHost();

        // 兼容旧行为信任转发头，但不学习
        Assert.Equal("1.2.3.4", ip);
        Assert.Null(CubeSetting.Current.LearnedProxies);
    }

    [Fact(DisplayName = "自动学习：学满后不再学习")]
    public void AutoLearn_LimitReached()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = null;
        set.LearnedProxies = "10.0.0.9";
        set.TrustedProxyLearning = 1;

        var ctx = CreateContext("10.2.3.6");
        ctx.Request.Headers["X-Forwarded-For"] = "1.2.3.4";

        ((HttpContext)ctx).GetUserHost();

        // 已达上限，新来源不被学习
        Assert.Equal("10.0.0.9", CubeSetting.Current.LearnedProxies);
    }

    [Fact(DisplayName = "封禁保护：通配列表不保护任何地址")]
    public void IsTrustedProxyAddress_Wildcard_NotProtected()
    {
        var set = CubeSetting.Current;
        set.TrustedProxies = "*";
        set.LearnedProxies = null;
        set.TrustedProxyLearning = 0;

        // 通配只表示信任所有转发头，所有地址仍可被封禁
        Assert.False(WebHelper2.IsTrustedProxyAddress("1.2.3.4"));
    }
}
