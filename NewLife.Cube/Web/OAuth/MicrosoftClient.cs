using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Http;
using NewLife.Serialization;

namespace NewLife.Web.OAuth
{
    /// <summary>微软身份验证提供者</summary>
    /// <remarks>
    /// 平台 https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps
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
        public override void Apply(OAuthConfig mi)
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
                var dic = url[(p + 1)..].SplitAsDictionary("=", "&").ToDictionary(e => e.Key, e => HttpUtility.UrlDecode(e.Value));
                url = url[..p];
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
            // 可以从token请求返回的id_token里面jwt解析得到email和name
            if (dic.TryGetValue("id_token", out var id_token) && !id_token.IsNullOrEmpty())
            {
                //WriteLog("id_token={0}", id_token);

                var jwt = new JwtBuilder();
                jwt.Parse(id_token);
                WriteLog("id_token={0}", jwt.Items.ToJson(true));
                foreach (var item in jwt.Items)
                {
                    dic[item.Key] = item.Value as String;
                }
            }

            base.OnGetInfo(dic);

            // 不能取name作为唯一用户名
            UserName = null;

            if (dic.TryGetValue("picture", out var str)) Avatar = str.Trim();
            if (dic.TryGetValue("name", out str)) NickName = str;
            if (dic.TryGetValue("preferred_username", out str)) UserName = str;
            if (dic.TryGetValue("email", out str)) Mail = str;

            if (UserName.IsNullOrEmpty() && Items.TryGetValue("preferred_username", out str)) UserName = str;

            // 去掉简体中文名字中的空格
            if (!UserName.IsNullOrEmpty() && UserName.Contains(" ") && Encoding.UTF8.GetByteCount(UserName) != UserName.Length) UserName = UserName.Replace(" ", null);
            if (!NickName.IsNullOrEmpty() && NickName.Contains(" ") && Encoding.UTF8.GetByteCount(NickName) != NickName.Length) NickName = NickName.Replace(" ", null);
        }

        /// <summary>获取Url，替换变量</summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected override String GetUrl(String name, String url) => base.GetUrl(name, url).Replace("{tenant}", Tenant);
    }
}