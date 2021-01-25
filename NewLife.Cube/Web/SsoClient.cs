using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Model;
using NewLife.Remoting;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Web
{
    /// <summary>SSO客户端</summary>
    public class SsoClient
    {
        #region 属性
        /// <summary>服务器</summary>
        public String Server { get; set; }

        /// <summary>应用标识</summary>
        public String AppId { get; set; }

        /// <summary>应用密钥</summary>
        public String Secret { get; set; }

        private HttpClient _client;
        #endregion

        private HttpClient GetClient()
        {
            if (_client != null) return _client;

            _client = DefaultTracer.Instance.CreateHttpClient();
            _client.BaseAddress = new Uri(Server);

            return _client;
        }

        /// <summary>验证账号密码，并返回令牌</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetTokenInfo(String username, String password)
        {
            var client = GetClient();

            return await client.GetAsync<IDictionary<String, Object>>("sso/token", new
            {
                grant_type = "password",
                client_id = AppId,
                client_secret = Secret,
                username,
                password
            });
        }

        /// <summary>验证账号密码，并返回令牌</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<String> GetToken(String username, String password)
        {
            var rs = await GetTokenInfo(username, password);

            return rs["access_token"] as String;
        }

        /// <summary>通过令牌获取用户信息</summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetUserInfo(String accessToken)
        {
            var client = GetClient();

            return await client.GetAsync<IDictionary<String, Object>>("sso/userinfo", new { access_token = accessToken });
        }

        /// <summary>通过令牌获取用户信息</summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<IManageUser> GetUser(String accessToken)
        {
            var dic = await GetUserInfo(accessToken);

            var user = JsonHelper.Convert<User>(dic);

            if (dic.TryGetValue("userid", out var str)) user.ID = str.ToInt();
            if (dic.TryGetValue("usercode", out str)) user.Code = str as String;
            if (dic.TryGetValue("username", out str)) user.Name = str as String;
            if (dic.TryGetValue("nickname", out str)) user.DisplayName = str as String;

            return user;
        }
    }
}