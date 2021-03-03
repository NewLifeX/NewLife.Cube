using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web;
using NewLife.Http;

namespace NewLife.Web.OAuth
{
    /// <summary>微软身份验证提供者</summary>
    /// <remarks>
    /// 参考文档 https://docs.microsoft.com/zh-cn/azure/active-directory/develop/v2-oauth2-auth-code-flow
    /// </remarks>
    public class MicrosoftClient : OAuthClient
    {
        #region 属性
        /// <summary>租户。默认common</summary>
        /// <remarks>
        /// 请求路径中的 {tenant} 值可用于控制哪些用户可以登录应用程序。 可以使用的值包括 common、organizations、consumers 和租户标识符。
        /// </remarks>
        public String Tenant { get; set; } = "common";
        #endregion

        /// <summary>实例化</summary>
        public MicrosoftClient()
        {
            Server = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/";

            AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = "token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&redirect_uri={redirect}";
            LogoutUrl = "logout?post_logout_redirect_uri={redirect}";
            UserUrl = "https://graph.microsoft.com/oidc/userinfo?access_token={token}&openid={openid}&lang=zh_CN";

            Scope = "openid profile email";
        }

        /// <summary>应用参数</summary>
        /// <param name="mi"></param>
        public override void Apply(NewLife.Cube.Entity.OAuthConfig mi)
        {
            base.Apply(mi);

            SetMode(Scope);
        }

        /// <summary>设置工作模式</summary>
        /// <param name="mode"></param>
        public virtual void SetMode(String mode)
        {
            switch (mode)
            {
                // 扫码登录
                case "snsapi_login":
                    AuthUrl = "qrconnect?response_type={response_type}&appid={key}&redirect_uri={redirect}&state={state}&scope={scope}";
                    Scope = mode;
                    break;
                // 静默授权，用户无感知
                case "snsapi_base":
                    AuthUrl = "authorize?response_type={response_type}&appid={key}&redirect_uri={redirect}&state={state}&scope={scope}#wechat_redirect";
                    Scope = mode;
                    break;
                // 授权需要用户手动同意
                case "snsapi_userinfo":
                    AuthUrl = "authorize?response_type={response_type}&appid={key}&redirect_uri={redirect}&state={state}&scope={scope}#wechat_redirect";
                    Scope = mode;
                    break;
            }
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
                var dic = url.Substring(p + 1).SplitAsDictionary("=", "&").ToDictionary(e => e.Key, e => HttpUtility.UrlDecode(e.Value));
                url = url.Substring(0, p);
                //WriteLog(dic.ToJson(true));

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

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected override void OnGetInfo(IDictionary<String, String> dic)
        {
            base.OnGetInfo(dic);

            //todo 其实可以从token请求返回的id_token里面jwt解析得到email和name

            if (dic.TryGetValue("picture", out var str)) Avatar = str.Trim();
        }

        /// <summary>获取Url，替换变量</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected override String GetUrl(String url) => base.GetUrl(url).Replace("{tenant}", Tenant);
    }
}