using System;
using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewLife.Cube;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// BASE-4 跨域支持测试。覆盖 AddCube 依据配置注册 cube_cors 策略：通配符来源放开、指定来源列表收紧、未配置时不注册策略。
/// </summary>
public class CorsPolicyTests
{
    private const String PolicyName = "cube_cors";

    private static CorsPolicy? GetPolicy(String origins)
    {
        var set = CubeSetting.Current;
        var old = set.CorsOrigins;
        set.CorsOrigins = origins;
        try
        {
            var services = new ServiceCollection();
            services.AddCube();
            using var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

            return options.GetPolicy(PolicyName);
        }
        finally
        {
            set.CorsOrigins = old;
        }
    }

    [Fact(DisplayName = "跨域_通配符来源放开全部方法头与凭据")]
    public void Cors_Wildcard_AllowsAll()
    {
        var policy = GetPolicy("*");

        Assert.NotNull(policy);
        Assert.True(policy.AllowAnyMethod);
        Assert.True(policy.AllowAnyHeader);
        Assert.True(policy.SupportsCredentials);
        Assert.True(policy.IsOriginAllowed("https://any.example.com"));
    }

    [Fact(DisplayName = "跨域_指定来源按逗号与分号拆分为多个地址")]
    public void Cors_SpecificOrigins_AreRestricted()
    {
        var policy = GetPolicy("https://a.example.com;https://b.example.com, https://c.example.com");

        Assert.NotNull(policy);
        Assert.Equal(3, policy.Origins.Count);
        Assert.Contains("https://a.example.com", policy.Origins);
        Assert.Contains("https://b.example.com", policy.Origins);
        Assert.Contains("https://c.example.com", policy.Origins);
        Assert.True(policy.IsOriginAllowed("https://b.example.com"));
        Assert.False(policy.IsOriginAllowed("https://d.example.com"));
        Assert.False(policy.AllowAnyOrigin);
        Assert.True(policy.SupportsCredentials);
    }

    [Fact(DisplayName = "跨域_未配置来源时不注册策略")]
    public void Cors_EmptyOrigins_NoPolicy()
    {
        var policy = GetPolicy("");

        Assert.Null(policy);
    }
}

