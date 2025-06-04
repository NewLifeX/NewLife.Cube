using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Http;
using NewLife.Serialization;

namespace NewLife.Web.OAuth
{
    /// <summary>IdentityServer4身份验证提供者</summary>
    /// <remarks>
    /// </remarks>
    public class Id4Client : OAuthClient
    {
        private static NewLife.Caching.MemoryCache _cache = new Caching.MemoryCache { Expire = 60 * 60 };

        #region 属性
        /// <summary>租户。默认common</summary>
        /// <remarks>
        /// 请求路径中的 {tenant} 值可用于控制哪些用户可以登录应用程序。 可以使用的值包括 common、organizations、consumers 和租户标识符。
        /// </remarks>
        public String Tenant { get; set; } = "common";
        #endregion

        /// <summary>实例化</summary>
        public Id4Client()
        {

            AuthUrl = "authorize?client_id={key}&response_type={response_type}&scope={scope}&redirect_uri={redirect}&state={state}&code_challenge={code_challenge}&code_challenge_method=S256";
            //AccessUrl = "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&redirect_uri={redirect}";
            //LogoutUrl = "logout?post_logout_redirect_uri={redirect}";
            //UserUrl = "https://graph.microsoft.com/oidc/userinfo?access_token={token}&openid={openid}&lang=zh_CN";

            OpenIDUrl = "userinfo";

            Scope = "openid profile email";
        }


        /// <summary>发起请求，获取内容</summary>
        /// <param name="action"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected override String GetHtml(String action, String url)
        {
            if (action == nameof(GetAccessToken))
            {
                var p = url.IndexOf('?');
                var dic = url[(p + 1)..].SplitAsDictionary("=", "&").ToDictionary(e => e.Key, e => HttpUtility.UrlDecode(e.Value));
                url = url[..p];
                //WriteLog(dic.ToJson(true));
                var state = HttpContext.Current.Request.Query["state"].FirstOrDefault();
                if (!state.IsNullOrEmpty() && _cache.ContainsKey(state))
                    dic.Add("code_verifier", _cache.Get<string>(state));

                var client = GetClient();
                var html = client.PostFormAsync(url, dic).Result;
                if (html.IsNullOrEmpty()) return null;

                html = html.Trim();
                if (Log != null && Log.Enable) WriteLog(html);

                return html;
            }
            else if (action == nameof(GetUserInfo))
            {
                var client = GetClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                var html = client.GetStringAsync(url).Result;
                if (html.IsNullOrEmpty()) return null;

                html = html.Trim();
                if (Log != null && Log.Enable) WriteLog(html);

                return html;
            }

            return base.GetHtml(action, url);
        }

        public override String GetOpenID()
        {
            var url= Server.EnsureEnd("/") + OpenIDUrl.TrimStart('/');

            var client = GetClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            var html = client.GetStringAsync(url).Result;

            if (!html.IsNullOrEmpty())
            {
                var dic = GetNameValues(html);

                if (dic.ContainsKey("sub")) OpenID = dic["sub"].Trim();
                if (dic.ContainsKey("name")) UserName = dic["name"].Trim();

            }


            return null;
        }

        /// <summary>获取Url，替换变量</summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected override String GetUrl(String name, String url)
        {
            var baseUrl = base.GetUrl(name, url);
            var p = baseUrl.IndexOf('?');
            var dic = baseUrl[(p + 1)..].SplitAsDictionary("=", "&").ToDictionary(e => e.Key, e => HttpUtility.UrlDecode(e.Value));
            if (dic.ContainsKey("state"))
            {
                var state = dic["state"];
                _ = _cache.GetOrAdd(state, k =>
                {
                    var codeVerifier = GenerateRandomDataBase64url(32);
                    var codeChallenge = Base64UrlEncodeNoPadding(Sha256Ascii(codeVerifier));
                    baseUrl = baseUrl.Replace("{code_challenge}", codeChallenge);
                    return codeVerifier;
                });

            }

            return baseUrl;

        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        private static string GenerateRandomDataBase64url(int length)
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(length);
            return Base64UrlEncodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string, which is assumed to be ASCII.
        /// </summary>
        private static byte[] Sha256Ascii(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            return SHA256.HashData(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        private static string Base64UrlEncodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

    }


}