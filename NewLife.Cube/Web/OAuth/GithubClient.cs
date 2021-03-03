using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace NewLife.Web.OAuth
{
    /// <summary>身份验证提供者</summary>
    /// <remarks>
    /// 平台 https://github.com/settings/developers
    /// </remarks>
    public class GithubClient : OAuthClient
    {
        /// <summary>实例化</summary>
        public GithubClient()
        {
            Server = "https://github.com/login/oauth/";

            AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = "access_token?grant_type=authorization_code&code={code}&state={state}&redirect_uri={redirect}";
            UserUrl = "https://api.github.com/user";
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            if (dic.TryGetValue("id", out var str)) UserID = str.Trim('\"').ToLong();
            if (dic.TryGetValue("login", out str)) UserName = str.Trim();
            if (dic.TryGetValue("name", out str)) NickName = str.Trim();
            if (dic.TryGetValue("avatar_url", out str)) Avatar = str.Trim();
            if (dic.TryGetValue("bio", out str)) Detail = str.Trim();
        }

        //private HttpClient _Client;

        ///// <summary>创建客户端</summary>
        ///// <param name="url">路径</param>
        ///// <returns></returns>
        //protected override String Request(String url)
        //{
        //    if (_Client == null)
        //    {
        //        // 允许宽松头部
        //        WebClientX.SetAllowUnsafeHeaderParsing(true);

        //        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        //        var agent = "";
        //        if (asm != null) agent = $"{asm.GetName().Name} v{asm.GetName().Version}";

        //        var client = new HttpClient(new HttpClientHandler { UseProxy = false });
        //        var headers = client.DefaultRequestHeaders;
        //        headers.UserAgent.ParseAdd(agent);
        //        headers.Add("", Key);
        //        headers.Add("", Secret);

        //        _Client = client;
        //    }
        //    return LastHtml = _Client.GetStringAsync(url).Result;
        //}

        /// <summary>发起请求，获取内容</summary>
        /// <param name="action"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected override String GetHtml(String action, String url)
        {
            // 部分提供者密钥写在头部
            var client = GetClient();
            var html = "";
            if (action == nameof(GetAccessToken))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", $"{Key}:{Secret}".GetBytes().ToBase64());
                html = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            }
            else if (!AccessToken.IsNullOrEmpty())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("token", AccessToken);
                html = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
            }
            else
            {
                html = client.GetStringAsync(url).Result;
            }
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();
            if (Log != null && Log.Enable) WriteLog(html);

            return html;
        }
    }
}