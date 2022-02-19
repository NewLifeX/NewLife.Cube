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
            Assert.Null(ua.Version);
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
            Assert.Equal("rv:35.0", ua.Version);
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
            Assert.Null(ua.Version);
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
            Assert.Null(ua.Version);
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
            Assert.Equal("rv:0.9.4", ua.Version);
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
            Assert.Null(ua.Version);
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
            Assert.Equal("rv:1.8.1b2", ua.Version);
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
            Assert.Equal("rv:1.8.1.6", ua.Version);
            Assert.Equal("Camino/1.5.1", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseHuaweiBrowser()
        {
            var userAgent = "Mozilla/5.0 (Linux; Android 10; HarmonyOS; SCMR-W09; HMSCore 6.3.0.327) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.105 HuaweiBrowser/12.0.3.310 Safari/537.36";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("HarmonyOS", ua.OSorCPU);
            Assert.Equal("SCMR-W09", ua.Device);
            Assert.Equal("HMSCore 6.3.0.327", ua.Version);
            Assert.Equal("HuaweiBrowser/12.0.3.310", ua.Brower);
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
            Assert.Null(ua.Version);
            Assert.Equal("Safari/5.1", ua.Brower);
            Assert.Equal("Mobile/9B206", ua.Mobile);
        }

        [Fact]
        public void ParseMQQBrowser()
        {
            var userAgent = "Mozilla/5.0 (Linux; U; Android 7.0;m2 note Build/LMY47D) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/5.0.2 MQQBrowser/6.7 Mobile/15A372 Safari/537.36";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Android 7.0", ua.OSorCPU);
            Assert.Equal("m2 note", ua.Device);
            Assert.Null(ua.Version);
            Assert.Equal("Build/LMY47D", ua.DeviceBuild);
            Assert.Equal("MQQBrowser/6.7", ua.Brower);
            Assert.Equal("Mobile/15A372", ua.Mobile);
        }

        [Fact]
        public void ParseDingTalk()
        {
            var userAgent = "Mozilla/5.0 (Linux; U; Android 8.1.0; zh-CN; EML-AL00 Build/HUAWEIEML-AL00) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/57.0.2987.108 baidu.sogo.uc.UCBrowser/11.9.4.974 UWS/2.13.1.48 Mobile Safari/537.36 AliApp(DingTalk/4.5.11) com.alibaba.android.rimet/10487439 Channel/227200 language/zh-CN";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Equal("U", ua.Encryption);
            Assert.Equal("Android 8.1.0", ua.OSorCPU);
            Assert.Equal("EML-AL00", ua.Device);
            Assert.Equal("Build/HUAWEIEML-AL00", ua.DeviceBuild);
            Assert.Null(ua.Version);
            Assert.Equal("DingTalk/4.5.11", ua.Brower);
            Assert.Equal("Mobile", ua.Mobile);
        }

        [Fact]
        public void ParseWechat()
        {
            var userAgent = "Mozilla/5.0 (Linux; Android 10; YAL-AL10 Build/HUAWEIYAL-AL10; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/86.0.4240.99 XWEB/3185 MMWEBSDK/20220105 Mobile Safari/537.36 MMWEBID/6851 MicroMessenger/8.0.19.2080(0x2800133B) Process/toolsmp WeChat/arm64 Weixin NetType/WIFI Language/zh_CN ABI/arm64";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("Android 10", ua.OSorCPU);
            Assert.Equal("YAL-AL10", ua.Device);
            Assert.Equal("Build/HUAWEIYAL-AL10", ua.DeviceBuild);
            Assert.Equal("wv", ua.Version);
            Assert.Equal("MicroMessenger/8.0.19.2080", ua.Brower);
            Assert.Equal("Mobile", ua.Mobile);
            Assert.Equal("WIFI", ua.NetType);
        }

        [Fact]
        public void ParseWechat2()
        {
            var userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36 NetType/WIFI MicroMessenger/7.0.20.1781(0x6700143B) WindowsWechat(0x6305002e)";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Windows", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("Windows NT 6.1", ua.OSorCPU);
            Assert.Null(ua.Device);
            Assert.Null(ua.DeviceBuild);
            Assert.Null(ua.Version);
            Assert.Equal("MicroMessenger/7.0.20.1781", ua.Brower);
            Assert.Null(ua.Mobile);
        }

        [Fact]
        public void ParseQyWeixin()
        {
            var userAgent = "Mozilla/5.0 (Linux; Android 10; YAL-AL10 Build/HUAWEIYAL-AL10; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/77.0.3865.120 MQQBrowser/6.2 TBS/045737 Mobile Safari/537.36 wxwork/4.0.0 ColorScheme/Light MicroMessenger/7.0.1 NetType/WIFI Language/zh Lang/zh";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("Android 10", ua.OSorCPU);
            Assert.Equal("YAL-AL10", ua.Device);
            Assert.Equal("Build/HUAWEIYAL-AL10", ua.DeviceBuild);
            Assert.Equal("wv", ua.Version);
            Assert.Equal("wxwork/4.0.0", ua.Brower);
            Assert.Equal("Mobile", ua.Mobile);
            Assert.Equal("WIFI", ua.NetType);
        }

        [Fact]
        public void ParseQQ()
        {
            var userAgent = "Mozilla/5.0 (Linux; Android 10; YAL-AL10 Build/HUAWEIYAL-AL10; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/89.0.4389.72 MQQBrowser/6.2 TBS/045913 Mobile Safari/537.36 V1_AND_SQ_8.8.68_2538_YYB_D A_8086800 QQ/8.8.68.7265 NetType/WIFI WebP/0.3.0 Pixel/1080 StatusBarHeight/108 SimpleUISwitch/1 QQTheme/3065 InMagicWin/0 StudyMode/0 CurrentMode/1 CurrentFontScale/1.0 GlobalDensityScale/0.90000004 AppId/537112550";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Null(ua.Encryption);
            Assert.Equal("Android 10", ua.OSorCPU);
            Assert.Equal("YAL-AL10", ua.Device);
            Assert.Equal("Build/HUAWEIYAL-AL10", ua.DeviceBuild);
            Assert.Equal("wv", ua.Version);
            Assert.Equal("QQ/8.8.68.7265", ua.Brower);
            Assert.Equal("Mobile", ua.Mobile);
            Assert.Equal("WIFI", ua.NetType);
        }

        [Fact]
        public void Parseokhttp()
        {
            var userAgent = "okhttp/3.12.13";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.True(ua.IsRobot);
        }

        [Fact]
        public void ParseRobot()
        {
            var userAgent = "Mozilla/5.0 (compatible; Barkrowler/0.9; +https://babbar.tech/crawler)";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.True(ua.IsRobot);
            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("compatible", ua.Platform);
            Assert.Equal("Barkrowler/0.9", ua.Brower);
        }

        [Fact]
        public void ParseRobot2()
        {
            var userAgent = "Mozilla/5.0 (compatible; SemrushBot/7~bl; +http://www.semrush.com/bot.html)";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.True(ua.IsRobot);
            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("compatible", ua.Platform);
            Assert.Equal("SemrushBot/7~bl", ua.Brower);
        }

        [Fact]
        public void ParseBytespider()
        {
            var userAgent = "Mozilla/5.0 (Linux; Android 5.0) AppleWebKit/537.36 (KHTML, like Gecko) Mobile Safari/537.36 (compatible; Bytespider; https://zhanzhang.toutiao.com/)";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("Android", ua.Platform);
            Assert.Equal("Android 5.0", ua.OSorCPU);
            Assert.Equal("Bytespider", ua.Device);
            Assert.Equal("Safari/537.36", ua.Brower);
            Assert.True(ua.IsRobot);
        }

        [Fact]
        public void Parsebingbot()
        {
            var userAgent = "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)";
            var ua = new UserAgentParser();
            ua.Parse(userAgent);

            Assert.Equal("Mozilla/5.0", ua.Compatible);
            Assert.Equal("compatible", ua.Platform);
            Assert.Equal("bingbot/2.0", ua.Brower);
            Assert.True(ua.IsRobot);
        }
    }
}