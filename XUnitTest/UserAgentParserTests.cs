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
        public void ParseChrome()
        {
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Windows", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("Windows NT 10.0", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("Chrome/86.0.4240.198", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseFirefox()
        {
            var userAgent = "Mozilla/5.0 (Windows NT 6.4; WOW64; rv:35.0) Gecko/20100101 Firefox/35.0";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Windows", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("Windows NT 6.4", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("Firefox/35.0", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseSafari()
        {
            var userAgent = "Mozilla/5.0 (Macintosh; U; PPC Mac OS X; en) AppleWebKit/522.15.5 (KHTML, like Gecko) Version/3.0.3 Safari/522.15.5";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Macintosh", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("PPC Mac OS X", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("Safari/3.0.3", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseIE()
        {
            var userAgent = "Mozilla/4.0 (compatible; MSIE 4.0; Windows 98)";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/4.0", ua.Compatible);
            Assert.Equal("compatible", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("Windows 98", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("MSIE 4.0", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseNetscape()
        {
            var userAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:0.9.4) Gecko/20011128 Netscape6/6.2.1";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Windows", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Windows NT 5.1", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("Netscape6/6.2.1", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseOpera()
        {
            var userAgent = "Opera/8.0 (Windows NT 5.1; U; en)";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Opera/8.0", ua.Compatible);
            Assert.Equal("Windows", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Windows NT 5.1", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("Opera/8.0", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseSeaMonkey()
        {
            var userAgent = "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.8.1b2) Gecko/20060823 SeaMonkey/1.1a";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("X11", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Linux i686", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("SeaMonkey/1.1a", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseCamino()
        {
            var userAgent = "Mozilla/5.0 (Macintosh; U; Intel Mac OS X; en; rv:1.8.1.6) Gecko/20070809 Camino/1.5.1";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Macintosh", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Intel Mac OS X", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("Camino/1.5.1", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void Parse3()
        {
            var userAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 5_1_1 like Mac OS X) AppleWebKit/534.46 (KHTML, like Gecko) Version/5.1 Mobile/9B206 Safari/7534.48.3";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("iPhone", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("iPhone OS 5_1_1", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Equal("Safari/5.1", ua.Brower);
            Assert.Equal("Mobile/9B206", ua.Mobile);
        }

        [Fact]
        public void Parse4()
        {
            var userAgent = "Mozilla/5.0 (Linux; U; Android 7.0;m2 note Build/LMY47D) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/5.0.2 MQQBrowser/6.7 Mobile/15A372 Safari/537.36";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Android 7.0", ua.OSorCPU);
            Assert.Equal("m2 note", ua.Device);
            Assert.Equal("Build/LMY47D", ua.DeviceBuild);
            Assert.Equal("MQQBrowser/6.7", ua.Brower);
            Assert.Equal("Mobile/15A372", ua.Mobile);
        }
    }
}