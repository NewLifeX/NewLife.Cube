using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Serialization;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Web
{
    /// <summary>钉钉身份验证提供者</summary>
    /// <remarks>
    /// 平台 https://open-dev.dingtalk.com/
    /// </remarks>
    public class DingTalkClient : OAuthClient
    {
        private static Int32 _show;
        static void ShowHelp()
        {
            if (_show > 0 || Interlocked.CompareExchange(ref _show, 1, 0) != 0) return;

            // 输出帮助日志
            XTrace.WriteLine("钉钉登录分多种方式，由Scope参数区分。");
            XTrace.WriteLine("Scope=snsapi_qrlogin, 扫码登录");
            XTrace.WriteLine("Scope=snsapi_login, 密码登录");
            XTrace.WriteLine("Scope=snsapi_auth, 钉钉内免登");
        }

        /// <summary>实例化</summary>
        public DingTalkClient()
        {
            Name = "Ding";
            Server = "https://oapi.dingtalk.com/connect/oauth2/";

            AuthUrl = "sns_authorize?appid={key}&response_type=code&scope={scope}&state={state}&redirect_uri={redirect}";
            AccessUrl = "https://oapi.dingtalk.com/sns/getuserinfo_bycode?accessKey={key}&timestamp={timestamp}&signature={signature}";
            OpenIDUrl = null;
            UserUrl = "https://oapi.dingtalk.com/user/get?access_token={token}&userid={userid}";
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
                case "snsapi_qrlogin":
                    Server = "https://oapi.dingtalk.com/connect/";
                    AuthUrl = "qrconnect?appid={key}&response_type=code&scope=snsapi_login&state={state}&redirect_uri={redirect}";
                    break;
                // 密码登录
                case "snsapi_login":
                    Server = "https://oapi.dingtalk.com/connect/oauth2/";
                    AuthUrl = "sns_authorize?appid={key}&response_type=code&scope=snsapi_login&state={state}&redirect_uri={redirect}";
                    break;
                // 钉钉内免登
                case "snsapi_auth":
                    Server = "https://oapi.dingtalk.com/connect/oauth2/";
                    AuthUrl = "sns_authorize?appid={key}&response_type=code&scope=snsapi_auth&state={state}&redirect_uri={redirect}";
                    break;
                default:
                    break;
            }
        }

        /// <summary>是否支持指定用户端，也就是判断是否在特定应用内打开，例如QQ/DingDing/WeiXin</summary>
        /// <remarks>
        /// Mozilla/5.0 (Linux; U; Android 10; zh-CN; YAL-AL10 Build/HUAWEIYAL-AL10) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/69.0.3497.100 UWS/3.22.0.36 Mobile Safari/537.36 AliApp(DingTalk/6.0.7) com.alibaba.android.rimet/14599822 Channel/227200 language/zh-CN UT4Aplus/0.2.25 colorScheme/light
        /// </remarks>
        /// <param name="userAgent"></param>
        /// <returns></returns>
        public override Boolean Support(String userAgent) => !userAgent.IsNullOrEmpty() && userAgent.Contains("DingTalk/");

        /// <summary>针对指定客户端进行初始化</summary>
        /// <param name="userAgent"></param>
        public override void Init(String userAgent)
        {
            // 钉钉内打开时，自动切换为应用内免登
            if (Support(userAgent))
            {
                Scope = "snsapi_auth";
                SetMode(Scope);
            }
        }

        /// <summary>获取令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public override String GetAccessToken(String code)
        {
            ShowHelp();

            var url = AccessUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(UserUrl), "未设置用户信息地址");

            var ts = DateTime.UtcNow.ToLong() + "";
            var sign = ts.GetBytes().SHA256(Secret.GetBytes()).ToBase64();
            url = url.Replace("{timestamp}", ts).Replace("{signature}", HttpUtility.UrlEncode(sign));

            url = GetUrl(url);

            var tmp_code = new { tmp_auth_code = code };
            WriteLog("GetUserInfo {0} {1}", url, tmp_code.ToJson());

            // 请求OpenId
            var http = CreateClient();
            var dic = Task.Run(() => http.InvokeAsync<IDictionary<String, Object>>(HttpMethod.Post, url, tmp_code, null, "user_info")).Result;

            if (dic != null)
            {
                XTrace.WriteLine(dic.ToJson());

                //NickName = dic["nick"] as String;
                //OpenID = dic["openid"] as String;
                //UnionID = dic["unionid"] as String;

                Items = dic.ToDictionary(e => e.Key, e => e.Value as String);

                OnGetInfo(Items);
            }

            return null;
        }

        private static String _ding_access_token;
        private static DateTime _ding_expire;
        /// <summary>获取用户信息</summary>
        /// <returns></returns>
        public override String GetUserInfo()
        {
            //return base.GetUserInfo();

            if (UnionID.IsNullOrEmpty()) return null;

            // 如果配置了企业级账号，可以获取更详细信息
            var token = _ding_access_token;
            if (token.IsNullOrEmpty() || _ding_expire < DateTime.Now)
            {
                var key = Parameter.GetOrAdd(0, "钉钉", "appkey");
                var secret = Parameter.GetOrAdd(0, "钉钉", "appsecret");

                _ding_access_token = null;
                if (!key.Value.IsNullOrEmpty() && !secret.Value.IsNullOrEmpty())
                {
                    token = _ding_access_token = GetToken(key.Value, secret.Value);
                }

                _ding_expire = DateTime.Now.AddSeconds(7200 - 60);
            }

            if (!token.IsNullOrEmpty())
            {
                try
                {
                    // 根据UnionId换取员工Id
                    var userid = GetUseridByUnionid(token, UnionID);
                    if (!userid.IsNullOrEmpty())
                    {
                        // 钉钉Id一般不是自己设置的，很乱，不可取
                        //client.UserName = userid;

                        //client.GetUserInfo(token, userid);
                        GetUserInfo(token, userid);
                    }
                }
                catch (AggregateException ex)
                {
                    // 某些用户不是本团队成员，此处会抛出异常
                    if (!(ex.GetTrue() is ApiException)) throw;
                }
            }

            return null;
        }

        #region 服务端Api
        /// <summary>企业内部应用获取凭证，有效期7200秒</summary>
        /// <param name="appkey"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static String GetToken(String appkey, String appsecret)
        {
            var url = $"https://oapi.dingtalk.com/gettoken?appkey={appkey}&appsecret={appsecret}";

            var http = CreateClient();
            return Task.Run(() => http.InvokeAsync<String>(HttpMethod.Get, url, null, null, "access_token")).Result;
        }

        /// <summary>企业内部应用获取用户信息</summary>
        /// <param name="access_token"></param>
        /// <param name="userid">员工id</param>
        /// <returns></returns>
        public IDictionary<String, Object> GetUserInfo(String access_token, String userid)
        {
            var url = UserUrl.Replace("{token}", access_token).Replace("{userid}", userid);

            var http = CreateClient();
            var buf = Task.Run(() => http.GetAsync<Byte[]>(url)).Result;
            var str = buf.ToStr();
            var js = new JsonParser(str).Decode() as IDictionary<String, Object>;

            UserName = js["name"] + "";
            NickName = js["nick"] + "";
            Avatar = js["avatar"] + "";
            Mail = js["email"] + "";
            Mobile = js["mobile"] + "";
            Code = js["jobnumber"] + "";

            // 合并字典
            var dic = Items;
            if (dic == null)
                Items = js.ToDictionary(e => e.Key, e => e.Value as String);
            else
            {
                foreach (var item in js)
                {
                    if (!dic.ContainsKey(item.Key)) dic[item.Key] = item.Value as String;
                }
            }

            return js;
        }

        ///// <summary>填充信息</summary>
        ///// <param name="user"></param>
        //public override void Fill(IManageUser user)
        //{
        //    var dic = Items;
        //    if (dic != null && user is User user2)
        //    {
        //        if (user2.Mail.IsNullOrEmpty() && dic.TryGetValue("email", out var email)) user2.Mail = email;
        //        if (user2.Mobile.IsNullOrEmpty() && dic.TryGetValue("mobile", out var mobile)) user2.Mobile = mobile;
        //        if (user2.Code.IsNullOrEmpty() && dic.TryGetValue("jobnumber", out var code)) user2.Code = code;
        //    }

        //    base.Fill(user);
        //}

        /// <summary>根据unionid获取userid</summary>
        /// <param name="access_token"></param>
        /// <param name="unionId">员工在当前开发者企业账号范围内的唯一标识，系统生成，固定值，不会改变</param>
        /// <returns></returns>
        public static String GetUseridByUnionid(String access_token, String unionId)
        {
            var url = $"https://oapi.dingtalk.com/user/getUseridByUnionid?access_token={access_token}&unionid={unionId}";

            var http = CreateClient();
            return Task.Run(() => http.InvokeAsync<String>(HttpMethod.Get, url, null, null, "userid")).Result;
        }
        #endregion
    }
}