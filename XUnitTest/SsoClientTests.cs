using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Cube.Web;
using NewLife.Log;
using XCode.Membership;
using Xunit;

namespace XUnitTest
{
    public class SsoClientTests
    {
        [Fact]
        public async void Test()
        {
            var client = new SsoClient
            {
                Server = "https://localhost:5001",
                //Server = "https://sso.newlifex.com",
                AppId = "NewLife.Cube",
                Secret = "",
            };

            var token = await client.GetToken("admin", "admin");
            Assert.NotEmpty(token);
            XTrace.WriteLine(token);

            var user = await client.GetUser(token) as User;
            Assert.NotNull(user);
            Assert.Equal(1, user.ID);
            Assert.Equal("admin", user.Name);
            Assert.Equal("管理员", user.DisplayName);
        }
    }
}