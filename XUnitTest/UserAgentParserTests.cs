using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Cube.Web;
using Xunit;

namespace XUnitTest
{
    public class UserAgentParserTests
    {
        [Theory]
        [InlineData("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36")]
        [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 5_1_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B206 Safari/7534.48.3")]
        [InlineData("Mozilla/5.0 (Linux; U; Android 7.0;m2 note Build/LMY47D) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/5.0.2 MQQBrowser/6.7 Mobile/15A372 Safari/537.36")]
        public void Parse(String userAgent)
        {
            var ua = new UserAgentParser();
            var rs = ua.Parse(userAgent);
            Assert.True(rs);
            //Assert.Equal(userAgent, ua.Brower);
        }

        [Fact]
        public void Parse2()
        {
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Windows NT 10.0", ua.Platform);
            Assert.Equal("WOW64", ua.OSorCPU);
            Assert.Equal("Chrome/86.0.4240.198", ua.Brower);
        }

        [Fact]
        public void Parse3()
        {
            var userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B206 Safari/7534.48.3";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("iPhone", ua.Platform);
            Assert.Equal("iPhone OS 5_1_1", ua.OSorCPU);
            Assert.Equal("Safari/5.1", ua.Brower);
        }

        [Fact]
        public void Parse4()
        {
            var userAgent = "Mozilla/5.0 (Linux; U; Android 7.0;m2 note Build/LMY47D) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/5.0.2 MQQBrowser/6.7 Mobile/15A372 Safari/537.36";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Linux", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Android 7.0", ua.OSorCPU);
            Assert.Equal("m2 note", ua.Device);
            Assert.Equal("Build/LMY47D", ua.DeviceBuild);
            Assert.Equal("MQQBrowser/6.7", ua.Brower);
            Assert.Equal("Mobile/15A372", ua.Mobile);
        }
    }
}