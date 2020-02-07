using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.Cube.Web
{
    /// <summary>钉钉身份验证提供者</summary>
    public class DingTalkClient : OAuthClient
    {
        static DingTalkClient()
        {
            // 输出帮助日志
            XTrace.WriteLine("钉钉登录分多种方式，由Scope参数区分。");
            XTrace.WriteLine("Scope=snsapi_qrlogin, 扫码登录");
            XTrace.WriteLine("Scope=snsapi_login, 密码登录");
            XTrace.WriteLine("Scope=snsapi_auth, 钉钉内免登");
        }

        /// <summary>实例化</summary>
        public DingTalkClient()
        {
            Server = "https://oapi.dingtalk.com/connect/oauth2/";

            AuthUrl = "sns_authorize?appid={key}&response_type=code&scope={scope}&state={state}&redirect_uri={redirect}";
            AccessUrl = null;
            OpenIDUrl = null;
            AccessUrl = "https://oapi.dingtalk.com/sns/getuserinfo_bycode?accessKey={key}&timestamp={timestamp}&signature={signature}";
        }

        /// <summary>应用参数</summary>
        /// <param name="mi"></param>
        public override void Apply(OAuthItem mi)
        {
            base.Apply(mi);

            switch (Scope)
            {
                // 扫码登录
                case "snsapi_qrlogin":
                    Server = "https://oapi.dingtalk.com/connect/";
                    AuthUrl = "qrconnect?appid={key}&response_type=code&scope=snsapi_login&state={state}&redirect_uri={redirect}";
                    break;
                // 密码登录
                case "snsapi_login":
                    Server = "https://oapi.dingtalk.com/connect/oauth2/";
                    AuthUrl = "sns_authorize?appid={key}&response_type=code&scope={scope}&state={state}&redirect_uri={redirect}";
                    break;
                // 钉钉内免登
                case "snsapi_auth":
                    Server = "https://oapi.dingtalk.com/connect/oauth2/";
                    AuthUrl = "sns_authorize?appid={key}&response_type=code&scope={scope}&state={state}&redirect_uri={redirect}";
                    break;
                default:
                    break;
            }
        }

        /// <summary>获取令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public override String GetAccessToken(String code)
        {
            var url = AccessUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(UserUrl), "未设置用户信息地址");

            var ts = DateTime.Now.ToLong() + "";
            var sign = ts.GetBytes().SHA256(Secret.GetBytes()).ToBase64();
            url = url.Replace("{timestamp}", ts).Replace("{signature}", HttpUtility.UrlEncode(sign));

            url = GetUrl(url);
            WriteLog("GetUserInfo {0}", url);

            var tmp_code = new { tmp_auth_code = code };

            var http = new HttpClient();
            var content = new StringContent(tmp_code.ToJson(), Encoding.UTF8, "application/json");
            var response = http.PostAsync(url, content).Result;

            var html = response.Content.ReadAsStringAsync().Result;
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();
            if (Log != null && Log.Enable) WriteLog(html);

            var dic = new JsonParser(html).Decode() as IDictionary<String, Object>;
            if (dic != null)
            {
                dic = dic["user_info"] as IDictionary<String, Object>;
                if (dic != null)
                {
                    NickName = dic["nick"] as String;
                    var openid = dic["openid"] as String;
                    var unionid = dic["unionid"] as String;

                    if (!openid.IsNullOrEmpty()) this.SetValue("OpenID", openid);

                    this.SetValue("Items", dic.ToDictionary(e => e.Key, e => e.Value as String));
                }
            }

            return html;
        }
    }
}