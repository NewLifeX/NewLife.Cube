using System;
using NewLife.Cube.Services;
using NewLife.Security;
using NewLife.Web;
using Xunit;

namespace XUnitTest;

/// <summary>应用令牌服务单元测试。覆盖令牌颁发与密钥格式校验</summary>
public class TokenServiceTests
{
    [Fact(DisplayName = "颁发令牌_刷新令牌与访问令牌内容不同")]
    public void IssueToken_RefreshTokenDiffersFromAccessToken()
    {
        var svc = new TokenService();
        var secret = $"HS256:{Rand.NextString(16)}";

        var token = svc.IssueToken("test", secret, 3600, "client1") as TokenModel;

        Assert.NotNull(token);
        Assert.False(String.IsNullOrEmpty(token.AccessToken));
        Assert.False(String.IsNullOrEmpty(token.RefreshToken));
        Assert.NotEqual(token.AccessToken, token.RefreshToken);
    }

    [Fact(DisplayName = "颁发令牌_刷新令牌Id不同且有效期更长")]
    public void IssueToken_RefreshTokenDecoded()
    {
        var svc = new TokenService();
        var secret = $"HS256:{Rand.NextString(16)}";
        var ss = secret.Split(':');

        var token = svc.IssueToken("test", secret, 3600, "client1") as TokenModel;

        var jwt1 = new JwtBuilder { Algorithm = ss[0], Secret = ss[1] };
        Assert.True(jwt1.TryDecode(token.AccessToken, out _));

        var jwt2 = new JwtBuilder { Algorithm = ss[0], Secret = ss[1] };
        Assert.True(jwt2.TryDecode(token.RefreshToken, out _));

        // 两个令牌主体一致，但 Id 不同、刷新令牌有效期更长
        Assert.Equal("test", jwt1.Subject);
        Assert.Equal("test", jwt2.Subject);
        Assert.NotEqual(jwt1.Id, jwt2.Id);
        Assert.True(jwt2.Expire > jwt1.Expire);
    }

    [Theory(DisplayName = "非法密钥格式_颁发令牌抛出ArgumentException")]
    [InlineData("")]
    [InlineData("HS256")]
    [InlineData("HS256:abc:def")]
    public void IssueToken_InvalidSecret_Throws(String secret)
    {
        var svc = new TokenService();

        Assert.Throws<ArgumentException>(() => svc.IssueToken("test", secret, 3600));
    }

    [Theory(DisplayName = "非法密钥格式_解码令牌抛出ArgumentException")]
    [InlineData("nosecret")]
    [InlineData("HS256")]
    public void DecodeToken_InvalidSecret_Throws(String secret)
    {
        var svc = new TokenService();

        Assert.Throws<ArgumentException>(() => svc.DecodeToken("abc", secret));
        Assert.Throws<ArgumentException>(() => svc.DecodeTokenWithError("abc", secret));
        Assert.Throws<ArgumentException>(() => svc.TryDecodeToken("abc", secret));
        Assert.Throws<ArgumentException>(() => svc.ValidAndIssueToken("name", "abc", secret, 3600));
    }
}
