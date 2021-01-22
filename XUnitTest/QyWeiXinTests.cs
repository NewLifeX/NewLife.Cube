using System;
using System.IO;
using System.Net.Http;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web.OAuth;
using Xunit;

namespace XUnitTest
{
    public class QyWeiXinTests
    {
        static QyWeiXinTests()
        {
            DefaultTracer.Instance = new DefaultTracer();
        }

        protected String[] GetSecret(String name)
        {
            var file = $"Config/{name}.config".GetFullPath();
            if (File.Exists(file))
            {
                var str = File.ReadAllText(file);
                var ss = str.Split("#");
                if (ss != null && ss.Length >= 2) return ss;
            }
            else
            {
                file.EnsureDirectory(true);
                File.WriteAllText(file, "corpId#secret");
            }

            return new[] { "", "" };
        }

        [Fact]
        public async void GetToken()
        {
            var ss = GetSecret("Member");
            var wx = new QyWeiXin
            {
                CorpId = ss[0],
                CorpSecret = ss[1]
            };

            var token = await wx.GetToken();

            Assert.NotEmpty(token);
            Assert.NotEmpty(wx.AccessToken);
            Assert.True(wx.Expired > DateTime.Now);
        }

        [Fact]
        public async void GetDepartments()
        {
            var ss = GetSecret("Member");
            var wx = new QyWeiXin
            {
                CorpId = ss[0],
                CorpSecret = ss[1]
            };

            var dps = await wx.GetDepartments();

            Assert.NotNull(dps);
            Assert.Contains(dps, e => e.Name == "技术架构部");
        }

        [Fact]
        public async void GetUser()
        {
            var ss = GetSecret("Member");
            var wx = new QyWeiXin
            {
                CorpId = ss[0],
                CorpSecret = ss[1]
            };

            var user = await wx.GetUser("Stone");

            Assert.NotNull(user);
            Assert.Equal("大石头", user.Alias);
        }

        [Fact]
        public async void GetUsers()
        {
            var ss = GetSecret("Member");
            var wx = new QyWeiXin
            {
                CorpId = ss[0],
                CorpSecret = ss[1]
            };

            var users = await wx.GetUsers(17, true);

            Assert.NotNull(users);
            Assert.Contains(users, e => e.Alias == "大石头");
        }

        [Fact]
        public async void GetCheckIn()
        {
            var ss = GetSecret("CheckIn");
            var wx = new QyWeiXin
            {
                CorpId = ss[0],
                CorpSecret = ss[1]
            };

            var end = DateTime.Now;
            var start = new DateTime(end.Year, end.Month, 1);

            var cis = await wx.GetCheckIn(new[] { "MoRuo", "bigqiang", "stone" }, start, end);

            XTrace.WriteLine(cis.ToJson());
            Assert.NotNull(cis);
            Assert.Contains(cis, e => e.UserId == "stone");
        }

        [Fact]
        public async void GetApproval()
        {
            var ss = GetSecret("Approval");
            var wx = new QyWeiXin
            {
                CorpId = ss[0],
                CorpSecret = ss[1]
            };

            var end = DateTime.Now;
            var start = new DateTime(end.Year, end.Month, 1);

            var approvals = await wx.GetApprovals("3TkaYwKYVoU5xN5ws35sFKbN8vaYRZDgGxzKgfWo", start, end);

            Assert.NotNull(approvals);
            XTrace.WriteLine(approvals.ToJson());
            //Assert.Contains(cis, e => e.UserId == "huangguoshi");

            foreach (var item in approvals)
            {
                var info = await wx.GetApproval(item);
                Assert.NotNull(info);
                XTrace.WriteLine(info.ToJson());

                var pc = info.GetPunchCorrection();
            }
        }

        [Fact]
        public async void GetUserInfo()
        {
            var ss = GetSecret("SSO");
            var wx = new QyWeiXin
            {
                CorpId = ss[0],
                CorpSecret = ss[1]
            };

            var url = wx.Authorize("https://sso.diyibox.com/cube/info");
            XTrace.WriteLine(url);

            var http = new HttpClient();
            var rs = await http.GetAsync(url);
            rs.EnsureSuccessStatusCode();
            //var html = await rs.Content.ReadAsStringAsync();

            var url2 = rs.RequestMessage?.RequestUri + "";

            //var user = await wx.GetAccessToken("Stone");

            //Assert.NotNull(user);
            //Assert.Equal("大石头", user.Alias);
        }
    }
}