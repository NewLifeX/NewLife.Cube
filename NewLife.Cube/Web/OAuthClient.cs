using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>OAuth 2.0 客户端</summary>
    /// <remarks>
    /// 最简单易懂的 OAuth2.0
    /// http://www.ruanyifeng.com/blog/2019/04/oauth-grant-types.html
    /// 
    /// OAuth 2.0 的四种方式:
    /// 1，code 授权码
    /// 2，token 隐藏式
    /// 3，password 密码式
    /// 4，client_credentials 凭证式
    /// </remarks>
    public class OAuthClient
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>验证服务器地址</summary>
        public String Server { get; set; }

        /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
        public String AccessServer { get; set; }

        /// <summary>应用Key</summary>
        public String Key { get; set; }

        /// <summary>安全码</summary>
        public String Secret { get; set; }

        /// <summary>验证地址</summary>
        public String AuthUrl { get; set; }

        /// <summary>访问令牌地址</summary>
        public String AccessUrl { get; set; }

        /// <summary>响应类型</summary>
        /// <remarks>
        /// 验证服务器跳转回来子系统时的类型，默认code，此时还需要子系统服务端请求验证服务器换取AccessToken；
        /// 可选token，此时验证服务器直接返回AccessToken，子系统不需要再次请求。
        /// </remarks>
        public String ResponseType { get; set; } = "code";

        /// <summary>作用域</summary>
        public String Scope { get; set; }

        /// <summary>APM跟踪器</summary>
        public static ITracer Tracer { get; set; } = DefaultTracer.Instance;
        #endregion

        #region 返回参数
        /// <summary>授权码</summary>
        public String Code { get; set; }

        /// <summary>访问令牌</summary>
        public String AccessToken { get; set; }

        /// <summary>刷新令牌</summary>
        public String RefreshToken { get; set; }

        /// <summary>统一标识。当前应用下唯一</summary>
        public String OpenID { get; set; }

        /// <summary>企业级标识。当前企业所有应用下唯一</summary>
        public String UnionID { get; set; }

        /// <summary>过期时间</summary>
        public DateTime Expire { get; set; }

        /// <summary>访问项</summary>
        public IDictionary<String, String> Items { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public OAuthClient()
        {
            Name = GetType().Name.TrimEnd("Client");

            // 标准地址格式
            AuthUrl = "authorize?response_type={response_type}&client_id={key}&redirect_uri={redirect}&state={state}&scope={scope}";
            AccessUrl = "access_token?grant_type=authorization_code&client_id={key}&client_secret={secret}&code={code}&state={state}&redirect_uri={redirect}";
        }
        #endregion

        #region 静态创建
        private static IDictionary<String, Type> _map;
        /// <summary>根据名称创建客户端</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static OAuthClient Create(String name)
        {
            //if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));

            // 初始化映射表
            if (_map == null)
            {
                var dic = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in typeof(OAuthClient).GetAllSubclasses())
                {
                    var key = item.Name.TrimEnd("Client");
                    var ct = item.CreateInstance() as OAuthClient;
                    if (!ct.Name.IsNullOrEmpty()) key = ct.Name;

                    dic[key] = item;
                }

                _map = dic;
            }

            if (name.IsNullOrEmpty())
            {
                var ms = OAuthConfig.GetValids();
                if (ms.Count > 0) name = ms[0].Name;
            }
            if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name), "未正确配置OAuth");

            //if (!_map.TryGetValue(name, out var type)) throw new Exception($"找不到[{name}]的OAuth客户端");
            // 找不到就用默认
            _map.TryGetValue(name, out var type);

            var client = type?.CreateInstance() as OAuthClient ?? new OAuthClient();
            client.Apply(name);

            // NewLife支持注销
            if (name.EqualIgnoreCase("NewLife") && client.LogoutUrl.IsNullOrEmpty()) client.LogoutUrl = "logout?client_id={key}&redirect_uri={redirect}&state={state}";

            return client;
        }
        #endregion

        #region 方法
        /// <summary>应用参数设置</summary>
        /// <param name="name"></param>
        public void Apply(String name)
        {
            var ms = OAuthConfig.GetValids();
            if (ms.Count == 0) throw new InvalidOperationException("未设置OAuth服务端");

            var mi = ms.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
            if (mi == null && name.IsNullOrEmpty()) mi = ms[0];
            if (mi == null || !mi.Enable) throw new InvalidOperationException($"未找到有效的OAuth服务端设置[{name}]");

            Name = mi.Name;

            if (mi.Debug) Log = XTrace.Log;

            Apply(mi);
        }

        /// <summary>应用参数设置</summary>
        /// <param name="mi"></param>
        public virtual void Apply(OAuthConfig mi)
        {
            Name = mi.Name;
            if (!mi.Server.IsNullOrEmpty()) Server = mi.Server;
            if (!mi.AccessServer.IsNullOrEmpty()) AccessServer = mi.AccessServer;
            if (!mi.AppId.IsNullOrEmpty()) Key = mi.AppId;
            if (!mi.Secret.IsNullOrEmpty()) Secret = mi.Secret;
            if (!mi.Scope.IsNullOrEmpty()) Scope = mi.Scope;
        }

        /// <summary>是否支持指定用户端，也就是判断是否在特定应用内打开，例如QQ/DingDing/WeiXin</summary>
        /// <param name="userAgent"></param>
        /// <returns></returns>
        public virtual Boolean Support(String userAgent) => false;

        /// <summary>构造跳转验证前，针对指定客户端进行初始化</summary>
        /// <param name="userAgent"></param>
        public virtual void Init(String userAgent) { }
        #endregion

        #region 1-跳转验证
        private String _redirect;
        private String _state;

        /// <summary>构建跳转验证地址</summary>
        /// <param name="redirect">验证完成后调整的目标地址</param>
        /// <param name="state">用户状态数据</param>
        /// <param name="baseUri">相对地址的基地址</param>
        public virtual String Authorize(String redirect, String state = null, Uri baseUri = null)
        {
            if (redirect.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect));

            if (Key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Key), "未设置应用标识");
            if (Secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Secret), "未设置应用密钥");

            //if (state.IsNullOrEmpty()) state = Rand.Next().ToString();

            // 如果是相对路径，自动加上前缀。需要考虑反向代理的可能，不能直接使用Request.Url
            //redirect = redirect.AsUri(baseUri) + "";
            _redirect = redirect;
            _state = state;

            var url = GetUrl(AuthUrl);
            if (!state.IsNullOrEmpty()) WriteLog("Authorize {0}", url);

            return url;
        }
        #endregion

        #region 2-获取访问令牌
        /// <summary>根据授权码获取访问令牌</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual String GetAccessToken(String code)
        {
            if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code), "未设置授权码");

            Code = code;

            var url = GetUrl(AccessUrl);
            WriteLog("GetAccessToken {0}", url);

            var html = GetHtml(nameof(GetAccessToken), url);
            if (html.IsNullOrEmpty()) return null;

            var dic = GetNameValues(html);
            if (dic != null)
            {
                if (dic.ContainsKey("access_token")) AccessToken = dic["access_token"].Trim();
                if (dic.ContainsKey("expires_in")) Expire = DateTime.Now.AddSeconds(dic["expires_in"].Trim().ToInt());
                if (dic.ContainsKey("refresh_token")) RefreshToken = dic["refresh_token"].Trim();

                // 如果响应区域包含用户信息，则增加用户地址
                if (UserUrl.IsNullOrEmpty() && dic.ContainsKey("scope"))
                {
                    var ss = dic["scope"].Trim().Split(",");
                    if (ss.Contains("UserInfo"))
                    {
                        UserUrl = "userinfo?access_token={token}";
                        LogoutUrl = "logout?client_id={key}&redirect_uri={redirect}&state={state}";
                    }
                }

                OnGetInfo(dic);
            }
            Items = dic;

            return html;
        }
        #endregion

        #region 3-获取OpenID
        /// <summary>OpenID地址</summary>
        public String OpenIDUrl { get; set; }

        /// <summary>根据授权码获取访问令牌</summary>
        /// <returns></returns>
        public virtual String GetOpenID()
        {
            if (AccessToken.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AccessToken), "未设置授权码");

            var url = GetUrl(OpenIDUrl);
            WriteLog("GetOpenID {0}", url);

            var html = GetHtml(nameof(GetOpenID), url);
            if (html.IsNullOrEmpty()) return null;

            var dic = GetNameValues(html);
            if (dic != null)
            {
                if (dic.ContainsKey("expires_in")) Expire = DateTime.Now.AddSeconds(dic["expires_in"].Trim().ToInt());
                if (dic.ContainsKey("openid")) OpenID = dic["openid"].Trim();

                OnGetInfo(dic);
            }
            Items = dic;

            return html;
        }
        #endregion

        #region 4-用户信息
        /// <summary>用户信息地址</summary>
        public String UserUrl { get; set; }

        /// <summary>用户ID</summary>
        public Int64 UserID { get; set; }

        /// <summary>用户名</summary>
        public String UserName { get; set; }

        /// <summary>昵称</summary>
        public String NickName { get; set; }

        /// <summary>性别。0未知，1男，2女</summary>
        public Int32 Sex { get; set; }

        /// <summary>用户代码</summary>
        public String UserCode { get; set; }

        /// <summary>部门代码，唯一标识</summary>
        public String DepartmentCode { get; set; }

        /// <summary>部门名称</summary>
        public String DepartmentName { get; set; }

        /// <summary>手机</summary>
        public String Mobile { get; set; }

        /// <summary>邮箱</summary>
        public String Mail { get; set; }

        /// <summary>头像</summary>
        public String Avatar { get; set; }

        /// <summary>明细</summary>
        public String Detail { get; set; }

        /// <summary>设备标识。</summary>
        public String DeviceId { get; set; }

        /// <summary>获取用户信息</summary>
        /// <returns></returns>
        public virtual String GetUserInfo()
        {
            var url = UserUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(UserUrl), "未设置用户信息地址");

            url = GetUrl(url);
            WriteLog("GetUserInfo {0}", url);

            var html = GetHtml(nameof(GetUserInfo), url);
            if (html.IsNullOrEmpty()) return null;

            var dic = GetNameValues(html);
            if (dic != null)
            {

                OnGetInfo(dic);

                // 合并字典
                if (Items == null)
                    Items = dic;
                else
                {
                    foreach (var item in dic)
                    {
                        Items[item.Key] = item.Value;
                    }
                }
            }

            return html;
        }

        /// <summary>填充用户，登录成功并获取用户信息之后</summary>
        /// <param name="user"></param>
        public virtual void Fill(IManageUser user)
        {
            if (user.Name.IsNullOrEmpty()) user.Name = UserName ?? UnionID ?? OpenID;
            if (user.NickName.IsNullOrEmpty()) user.NickName = NickName;

            //// 头像
            //if (!Avatar.IsNullOrEmpty()) user.SetValue(nameof(Avatar), Avatar);
        }
        #endregion

        #region 5-注销
        /// <summary>注销地址</summary>
        public String LogoutUrl { get; set; }

        /// <summary>注销</summary>
        /// <param name="redirect">完成后调整的目标地址</param>
        /// <param name="state">用户状态数据</param>
        /// <param name="baseUri">相对地址的基地址</param>
        /// <returns></returns>
        public virtual String Logout(String redirect = null, String state = null, Uri baseUri = null)
        {
            var url = LogoutUrl;
            if (url.IsNullOrEmpty()) throw new ArgumentNullException(nameof(LogoutUrl), "未设置注销地址");

            // 如果是相对路径，自动加上前缀。需要考虑反向代理的可能，不能直接使用Request.Url
            //redirect = redirect.AsUri(baseUri) + "";
            _redirect = redirect;
            _state = state;

            url = GetUrl(url);
            WriteLog("Logout {0}", url);

            return url;
        }
        #endregion

        #region 辅助
        /// <summary>替换地址模版参数</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual String GetUrl(String url)
        {
            if (!url.StartsWithIgnoreCase("http://", "https://"))
            {
                // 授权以外的连接，使用令牌服务地址
                if (!AccessServer.IsNullOrEmpty() && !url.StartsWithIgnoreCase("auth"))
                    url = AccessServer.EnsureEnd("/") + url.TrimStart('/');
                else
                    url = Server.EnsureEnd("/") + url.TrimStart('/');
            }

            url = url
               .Replace("{key}", HttpUtility.UrlEncode(Key + ""))
               .Replace("{secret}", HttpUtility.UrlEncode(Secret + ""))
               .Replace("{response_type}", HttpUtility.UrlEncode(ResponseType) + "")
               .Replace("{token}", HttpUtility.UrlEncode(AccessToken + ""))
               .Replace("{code}", HttpUtility.UrlEncode(Code + ""))
               .Replace("{openid}", HttpUtility.UrlEncode(OpenID + ""))
               .Replace("{unionid}", HttpUtility.UrlEncode(UnionID + ""))
               .Replace("{redirect}", HttpUtility.UrlEncode(_redirect + ""))
               .Replace("{scope}", HttpUtility.UrlEncode(Scope + ""))
               .Replace("{state}", HttpUtility.UrlEncode(_state + ""));

            return url;
        }

        /// <summary>获取名值字典</summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected virtual IDictionary<String, String> GetNameValues(String html)
        {
            // 部分提供者的返回Json不是{开头，比如QQ
            var p1 = html.IndexOf('{');
            var p2 = html.LastIndexOf('}');
            if (p1 > 0 && p2 > p1) html = html.Substring(p1, p2 - p1 + 1);

            IDictionary<String, String> dic = null;
            // Json格式转为名值字典
            if (p1 >= 0 && p2 > p1)
            {
                var js = JsonParser.Decode(html);
                dic = new Dictionary<String, String>();
                foreach (var item in js)
                {
                    var v = item.Value;
                    if (v is IList<Object> list)
                        dic[item.Key] = "[" + list.Join() + "]";
                    else if (v is IDictionary<String, Object> dic2)
                        dic[item.Key] = dic2.ToJson();
                    else if (v != null)
                        dic[item.Key] = v + "";
                }
            }
            // Url格式转为名值字典
            else if (html.Contains("=") && html.Contains("&"))
            {
                dic = html.SplitAsDictionary("=", "&");
            }

            return dic.ToNullable(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>最后一次请求的响应内容</summary>
        public String LastHtml { get; set; }

        /// <summary>发起请求，获取内容</summary>
        /// <param name="action"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual String GetHtml(String action, String url)
        {
            // 部分提供者密钥写在头部
            var client = GetClient();
            var html = client.GetStringAsync(url).Result;
            if (html.IsNullOrEmpty()) return null;

            html = html.Trim();
            if (Log != null && Log.Enable) WriteLog(html);

            return html;
        }

        private HttpClient _Client;
        /// <summary>获取客户端</summary>
        /// <returns></returns>
        protected virtual HttpClient GetClient()
        {
            if (_Client != null) return _Client;

            //// 允许宽松头部
            //WebClientX.SetAllowUnsafeHeaderParsing(true);

            var client = CreateClient();

            return _Client = client;
        }

        /// <summary>创建客户端</summary>
        /// <returns></returns>
        protected static HttpClient CreateClient()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var agent = "";
            if (asm != null) agent = $"{asm.GetName().Name} v{asm.GetName().Version}";

            var client = Tracer?.CreateHttpClient();
            var headers = client.DefaultRequestHeaders;
            headers.UserAgent.ParseAdd(agent);

            return client;
        }

        /// <summary>从响应数据中获取信息</summary>
        /// <param name="dic"></param>
        protected virtual void OnGetInfo(IDictionary<String, String> dic)
        {
            if (dic.TryGetValue("openid", out var str)) OpenID = str.Trim();
            if (dic.TryGetValue("unionid", out str)) UnionID = str.Trim();

            if (dic.TryGetValue("uid", out str)) UserID = str.ToLong();
            if (dic.TryGetValue("userid", out str)) UserID = str.ToLong();
            if (dic.TryGetValue("user_id", out str)) UserID = str.ToLong();

            if (dic.TryGetValue("name", out str)) UserName = str.Trim();
            if (dic.TryGetValue("username", out str)) UserName = str.Trim();
            if (dic.TryGetValue("user_name", out str)) UserName = str.Trim();

            if (dic.TryGetValue("sex", out str)) Sex = str.Trim().ToInt();

            if (dic.TryGetValue("nick", out str)) NickName = str.Trim();
            if (dic.TryGetValue("nickname", out str)) NickName = str.Trim();
            if (dic.TryGetValue("nick_name", out str)) NickName = str.Trim();

            if (dic.TryGetValue("code", out str)) UserCode = str.Trim();
            if (dic.TryGetValue("usercode", out str)) UserCode = str.Trim();
            if (dic.TryGetValue("user_code", out str)) UserCode = str.Trim();

            if (dic.TryGetValue("departmentCode", out str)) DepartmentCode = str.Trim();
            if (dic.TryGetValue("department_code", out str)) DepartmentCode = str.Trim();

            if (dic.TryGetValue("departmentName", out str)) DepartmentName = str.Trim();
            if (dic.TryGetValue("department_name", out str)) DepartmentName = str.Trim();

            if (dic.TryGetValue("mobile", out str)) Mobile = str.Trim();
            if (dic.TryGetValue("usermobile", out str)) Mobile = str.Trim();
            if (dic.TryGetValue("user_mobile", out str)) Mobile = str.Trim();

            if (dic.TryGetValue("mail", out str)) Mail = str.Trim();
            if (dic.TryGetValue("email", out str)) Mail = str.Trim();
            if (dic.TryGetValue("usermail", out str)) Mail = str.Trim();
            if (dic.TryGetValue("user_mail", out str)) Mail = str.Trim();

            if (dic.TryGetValue("detail", out str)) Detail = str.Trim();
            if (dic.TryGetValue("userdetail", out str)) Detail = str.Trim();
            if (dic.TryGetValue("user_detail", out str)) Detail = str.Trim();

            if (dic.TryGetValue("Avatar", out str)) Avatar = str.Trim();

            // 获取用户信息出错时抛出异常
            if (dic.TryGetValue("error", out str)) throw new InvalidOperationException(str);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}