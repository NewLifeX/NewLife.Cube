using System;
using System.Collections.Generic;
using NewLife.Web;
using NewLife.Web.OAuth;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// OAUTH-1 令牌换取链路测试。用可替换响应内容的 GitHub 客户端跑完整链路（授权码→令牌→用户信息/OpenID），
/// 验证请求地址拼装与响应字段映射，无需访问外部平台。
/// </summary>
public class OAuthTokenFlowTests
{
    /// <summary>固定返回预置响应的 GitHub 客户端，避免真实网络请求</summary>
    private class FakeGithubClient : GithubClient
    {
        public Dictionary<String, String> Responses { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<String> Calls { get; } = [];

        protected override String GetHtml(String action, String url)
        {
            Calls.Add(action + " " + url);

            return Responses.TryGetValue(action, out var html) ? html : null;
        }
    }

    private static FakeGithubClient CreateClient()
    {
        var client = new FakeGithubClient
        {
            Key = "test-key",
            Secret = "test-secret",
            Scope = "user",
            AccessUrl = "access_token?grant_type=authorization_code&code={code}&state={state}",
            UserUrl = "https://api.example.com/user",
            OpenIDUrl = "openid?token={token}",
        };

        return client;
    }

    [Fact(DisplayName = "OAuth令牌_解析访问令牌并拼装请求地址")]
    public void GetAccessToken_ParsesJsonAndBuildsUrl()
    {
        var client = CreateClient();
        client.Responses[nameof(OAuthClient.GetAccessToken)] = """{"access_token":"gho_token","token_type":"bearer","expires_in":3600,"refresh_token":"refresh_1","scope":"user"}""";

        var html = client.GetAccessToken("code_abc");

        Assert.False(html.IsNullOrEmpty());
        Assert.Equal("gho_token", client.AccessToken);
        Assert.Equal("refresh_1", client.RefreshToken);
        Assert.Equal("bearer", client.TokenType);
        Assert.True(client.Expire > DateTime.Now.AddMinutes(50));
        Assert.NotNull(client.Items);
        Assert.Equal("gho_token", client.Items!["access_token"] + "");

        // 请求地址带上授权码与服务地址前缀
        var call = Assert.Single(client.Calls);
        Assert.Contains("code=code_abc", call);
        Assert.Contains("https://github.com/login/oauth/access_token", call);
    }

    [Fact(DisplayName = "OAuth令牌_用户信息字段映射并合并到Items")]
    public void GetUserInfo_MapsGithubFields()
    {
        var client = CreateClient();
        client.AccessToken = "gho_token";
        client.Responses[nameof(OAuthClient.GetAccessToken)] = """{"access_token":"gho_token","token_type":"bearer"}""";
        client.Responses[nameof(OAuthClient.GetUserInfo)] = """{"id":42,"login":"octocat","name":"The Octocat","avatar_url":"https://a.example.com/avatar.png","bio":"hello"}""";

        client.GetAccessToken("code_abc");
        var html = client.GetUserInfo();

        Assert.False(html.IsNullOrEmpty());
        Assert.Equal(42, client.UserID);
        Assert.Equal("octocat", client.UserName);
        Assert.Equal("The Octocat", client.NickName);
        Assert.Equal("https://a.example.com/avatar.png", client.Avatar);
        Assert.Equal("hello", client.Detail);
        // 用户信息合并进 Items，令牌信息不丢
        Assert.Equal("gho_token", client.Items!["access_token"] + "");
        Assert.Equal("octocat", client.Items!["login"] + "");
    }

    [Fact(DisplayName = "OAuth令牌_OpenID解析")]
    public void GetOpenID_ParsesOpenid()
    {
        var client = CreateClient();
        client.AccessToken = "gho_token";
        client.Responses[nameof(OAuthClient.GetOpenID)] = """{"openid":"oid-1","expires_in":120}""";

        var html = client.GetOpenID();

        Assert.False(html.IsNullOrEmpty());
        Assert.Equal("oid-1", client.OpenID);
        Assert.Contains("token=gho_token", Assert.Single(client.Calls));
    }

    [Fact(DisplayName = "OAuth令牌_缺少授权码时抛参数异常")]
    public void GetAccessToken_EmptyCode_Throws()
    {
        var client = CreateClient();

        Assert.Throws<ArgumentNullException>(() => client.GetAccessToken(""));
    }

    [Fact(DisplayName = "OAuth令牌_响应为空时返回空且不写入令牌")]
    public void GetAccessToken_EmptyResponse_NoToken()
    {
        var client = CreateClient();

        var html = client.GetAccessToken("code_abc");

        Assert.True(html.IsNullOrEmpty());
        Assert.True(client.AccessToken.IsNullOrEmpty());
    }
}
