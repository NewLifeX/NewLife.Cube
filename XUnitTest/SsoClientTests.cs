using System;
using System.IO;
using System.Threading.Tasks;
using NewLife.Cube.Web;
using NewLife.Remoting;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>SSO 客户端真实环境验证。依赖本地 https://localhost:5001 SSO 服务，未启动时自动跳过</summary>
[Trait("Category", "Integration")]
public class SsoClientTests
{
    /// <summary>本地 SSO 服务是否可用（端口预检），未启动时跳过避免干扰无环境 CI 基线</summary>
    private static Boolean SsoServerAvailable()
    {
        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            var task = tcp.ConnectAsync("127.0.0.1", 5001);
            if (!task.Wait(1000)) return false;
            return tcp.Connected;
        }
        catch { return false; }
    }

    [Fact]
    public async Task PasswordTest()
    {
        if (!SsoServerAvailable()) return;// SSO 服务未启动，跳过

        var client = new SsoClient
        {
            Server = "https://localhost:5001",
            //Server = "https://sso.newlifex.com",
            AppId = "test",
            Secret = "test1234",
        };

        var file = "..\\..\\Bin\\Keys\\SsoSecurity.pubkey".GetFullPath();
        if (File.Exists(file)) client.SecurityKey = File.ReadAllText(file);

        var token = await client.GetToken("admin", "admin");
        Assert.NotNull(token);
        Assert.NotEmpty(token.AccessToken);
        Assert.NotEmpty(token.RefreshToken);
        Assert.Equal(7200, token.ExpireIn);
        Assert.Equal(3, token.AccessToken.Split('.').Length);

        var user = await client.GetUser(token.AccessToken) as User;
        Assert.NotNull(user);
        Assert.Equal(1, user.ID);
        Assert.Equal("admin", user.Name);
        Assert.Equal("管理员", user.DisplayName);

        var jwt = new JwtBuilder();
        jwt.Parse(token.AccessToken);
        Assert.Equal("test", jwt.Audience);
        Assert.Equal("admin", jwt.Subject);

        var prv = new TokenProvider();
        var rs = prv.TryDecode(token.RefreshToken, out var name, out var expire);
        Assert.False(rs);
        var ss = name.Split('#');
        Assert.Equal("test", ss[0]);
        Assert.Equal("admin", ss[1]);
    }

    [Fact]
    public async Task ClientTest()
    {
        if (!SsoServerAvailable()) return;// SSO 服务未启动，跳过

        var client = new SsoClient
        {
            Server = "https://localhost:5001",
            //Server = "https://sso.newlifex.com",
            AppId = "test",
            Secret = "test1234",
        };

        var token = await client.GetToken("mydevice");
        Assert.NotNull(token);
        Assert.NotEmpty(token.AccessToken);
        Assert.NotEmpty(token.RefreshToken);
        Assert.Equal(7200, token.ExpireIn);
        Assert.Equal(3, token.AccessToken.Split('.').Length);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetUser(token.AccessToken));
        Assert.NotNull(ex);
        Assert.Equal(500, ex.Code);
        Assert.Equal("用户[mydevice]不存在", ex.Message);

        var jwt = new JwtBuilder();
        jwt.Parse(token.AccessToken);
        Assert.Equal("test", jwt.Audience);
        Assert.Equal("mydevice", jwt.Subject);

        var prv = new TokenProvider();
        var rs = prv.TryDecode(token.RefreshToken, out var name, out var expire);
        Assert.False(rs);
        var ss = name.Split('#');
        Assert.Equal("test", ss[0]);
        Assert.Equal("mydevice", ss[1]);
    }
}