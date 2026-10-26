using System;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Common;
using NewLife.Cube.Tests.Membership;
using NewLife.Web;
using NewLife.Web.OAuth;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// OAUTH-1 第三方登录客户端测试。覆盖按名称创建提供者实例、配置应用到客户端、授权跳转地址拼装与缺配置的拒绝行为。
/// 真实换取令牌/用户信息需外部服务，不在本测试范围。
/// </summary>
[Collection("TenantAuth")]
public class OAuthClientTests
{
    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    /// <summary>确保存在一条 GitHub 授权码配置（复用同一行，避免唯一索引冲突）</summary>
    private static void EnsureGithubConfig(String appId, String secret)
    {
        var config = OAuthConfig.FindByName("Github");
        if (config == null)
        {
            config = new OAuthConfig
            {
                TenantId = 0,
                Name = "Github",
                NickName = "GitHub 登录",
                GrantType = GrantTypes.AuthorizationCode,
                Visible = true,
                Sort = 1,
            };
        }

        config.AppId = appId;
        config.Secret = secret;
        config.Enable = true;
        config.Save();

        OAuthConfig.Meta.Cache.Clear("OAuth 客户端测试", true);
    }

    [Fact(DisplayName = "OAuth客户端_按名称创建GitHub提供者并应用配置")]
    public void Create_ByName_ReturnsGithubClient()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        var appId = Unique("app");
        var secret = Unique("sec");
        EnsureGithubConfig(appId, secret);

        var client = OAuthClient.Create(0, "Github");

        Assert.IsType<GithubClient>(client);
        Assert.Equal(appId, client.Key);
        Assert.Equal(secret, client.Secret);
        Assert.Equal("Github", client.Name);
    }

    [Fact(DisplayName = "OAuth客户端_授权地址包含客户端标识回跳地址与状态")]
    public void Authorize_BuildsUrl()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        var appId = Unique("app");
        EnsureGithubConfig(appId, Unique("sec"));

        var client = OAuthClient.Create(0, "Github");
        var url = client.Authorize("https://demo.example.com/oauth/callback", "state-123");

        Assert.StartsWith("https://github.com/login/oauth/authorize", url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"client_id={appId}", url);
        Assert.Contains("state=state-123", url);
        Assert.Contains("redirect_uri=", url);
        Assert.Contains("demo.example.com", Uri.UnescapeDataString(url));
    }

    [Fact(DisplayName = "OAuth客户端_未知服务商无配置时拒绝创建")]
    public void Create_UnknownProvider_Throws()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");

        // 有配置存在时，指定一个不存在的服务商名应被拒绝（防止静默创建无凭据客户端）
        EnsureGithubConfig(Unique("app"), Unique("sec"));

        Assert.ThrowsAny<Exception>(() => OAuthClient.Create(0, Unique("NoSuchProvider")));
    }
}
