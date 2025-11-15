using System.IO;
using System.Threading.Tasks;
using NewLife.Cube.Web;
using NewLife.Remoting;
using NewLife.Web;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

public class SsoClientTests
{
    [Fact]
    public async Task PasswordTest()
    {
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
        Assert.Equal(7200, token.Expire);
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
        Assert.Equal(7200, token.Expire);
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