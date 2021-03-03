using System;
using System.IO;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Web;
using NewLife.Log;
using NewLife.Model;
using NewLife.Web;
using XCode.Membership;
using NewLife.Collections;
using System.Security.Cryptography;
using NewLife.Security;
using NewLife.Cube.Web.Models;
#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Mvc;
#endif

/*
 * 魔方OAuth在禁用本地登录，且只设置一个第三方登录时，形成单点登录。
 * 
 * 验证流程：
 *      进入登录页~/Admin/User/Login
 *      if 允许本地登录
 *          输入密码登录 或 选择第三方登录
 *      else if 多个第三方登录
 *          选择第三方登录
 *      else
 *          直接跳转唯一的第三方登录
 *      登录完成
 *      if 有绑定用户
 *          登录完成，跳转来源页
 *      else
 *          进入绑定流程
 * 
 * 绑定流程：
 *      if 本地已登录
 *          第三方绑定到当前已登录本地用户
 *      else 允许本地登录
 *          显示登录框，输入密码登录后绑定（暂不支持）
 *          或 直接进入，自动注册本地用户
 *      else
 *          直接进入，自动注册本地用户
 */

namespace NewLife.Cube.Controllers
{
    /// <summary>单点登录控制器</summary>
    public class SsoController : ControllerBaseX
    {
        /// <summary>当前提供者</summary>
        public static SsoProvider Provider { get; set; }

        /// <summary>单点登录服务端</summary>
        public static OAuthServer OAuth { get; set; }

        /// <summary>存储最近用过的code，避免用户刷新页面</summary>
        private static DictionaryCache<String, String> _codeCache = new DictionaryCache<string, string>()
        {
            Expire = 600,
            Period = 60
        };

        static SsoController()
        {
            Provider = new SsoProvider();
            OAuth = new OAuthServer
            {
                Log = LogProvider.Provider.AsLog("OAuth")
            };
        }

        /// <summary>首页</summary>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Index() => Redirect("~/");

        #region 单点登录客户端
        private String GetUserAgent()
        {
#if __CORE__
            return Request.Headers["User-Agent"] + "";
#else
            return Request.UserAgent;
#endif
        }

        /// <summary>第三方登录</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Login(String name)
        {
            var prov = Provider;
            var client = prov.GetClient(name);
            client.Init(GetUserAgent());

            var rurl = prov.GetReturnUrl(Request, true);

            return base.Redirect(OnLogin(client, null, rurl));
        }

        private String OnLogin(OAuthClient client, String state, String returnUrl)
        {
            var prov = Provider;
            var redirect = prov.GetRedirect(Request, "~/Sso/LoginInfo/" + client.Name);
            if (state.IsNullOrEmpty() && !returnUrl.IsNullOrEmpty()) state = $"r={returnUrl}";

            return client.Authorize(redirect, state);
        }

        /// <summary>第三方登录完成后跳转到此</summary>
        /// <param name="id">提供者</param>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult LoginInfo(String id, String code, String state)
        {
            if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

            var name = id;
            var prov = Provider;
            var client = prov.GetClient(name);
            client.Init(GetUserAgent());

            client.WriteLog("LoginInfo name={0} code={1} state={2} {3}", name, code, state, Request.GetRawUrl());

            var ds = state.SplitAsDictionary("=", "&");

            //// 无法拿到code时，跳回去再来
            //if (code.IsNullOrEmpty())
            //{
            //    if (state == "refresh") throw new Exception("非法请求，无法取得code");

            //    return Redirect(OnLogin(client, $"{name}_refresh", null));
            //}
            //// 短期内用过的code也跳回
            //if (!_codeCache.TryAdd(code, code, false, out _))
            //{
            //    return Redirect(OnLogin(client, $"{name}_refresh", null));
            //}

            // 构造redirect_uri，部分提供商（百度）要求获取AccessToken的时候也要传递
            var redirect = prov.GetRedirect(Request, "~/Sso/LoginInfo/" + client.Name);
            client.Authorize(redirect);

            //var returnUrl = prov.GetReturnUrl(Request, false);
            var returnUrl = ds["r"];

            try
            {
                // 获取访问令牌
                if (!client.AccessUrl.IsNullOrEmpty())
                {
                    var html = client.GetAccessToken(code);

                    // 如果拿不到访问令牌或用户信息，则重新跳转
                    if (client.AccessToken.IsNullOrEmpty() && client.OpenID.IsNullOrEmpty() && client.UserID == 0 && client.UserName.IsNullOrEmpty())
                    {
                        XTrace.WriteLine("[{2}]拿不到访问令牌 code={0} state={1}", code, state, id);
                        XTrace.WriteLine(Request.GetRawUrl() + "");
                        if (!html.IsNullOrEmpty()) XTrace.WriteLine(html);

                        throw new InvalidOperationException($"内部错误，无法获取令牌 code={code}");
                    }
                }

                //// 特殊处理钉钉
                //if (client is DingTalkClient ding) DoDingDing(ding);

                // 获取OpenID。部分提供商不需要
                if (!client.OpenIDUrl.IsNullOrEmpty()) client.GetOpenID();

                // 短时间内不要重复拉取用户信息
                // 注意，这里可能因为没有OpenID和UserName，无法判断得到用户链接，需要GetUserInfo后方能匹配UserConnect
                var set = Setting.Current;
                var uc = prov.GetConnect(client);
                if (uc.UpdateTime.AddSeconds(set.RefreshUserPeriod) < DateTime.Now)
                {
                    // 获取用户信息
                    if (!client.UserUrl.IsNullOrEmpty()) client.GetUserInfo();
                }

                // 如果前面没有取得用户链接，需要再次查询
                if (uc.ID == 0) uc = prov.GetConnect(client);
                uc.Fill(client);

#if __CORE__
                var url = prov.OnLogin(client, HttpContext.RequestServices, uc, ds["a"] == "bind");
#else
                var url = prov.OnLogin(client, HttpContext, uc, ds["a"] == "bind");
#endif

                // 标记登录提供商
                Session["Cube_Sso"] = client.Name;

                if (!returnUrl.IsNullOrEmpty()) url = returnUrl;

                // 子系统颁发token给前端
                if (state == "front-end")
                {
                    var jwt = ManagerProviderHelper.GetJwt();
                    jwt.Expire = DateTime.Now.Add(TimeSpan.FromHours(2));
                    jwt.Subject = uc.User.Name;
                    var token = jwt.Encode(null);
                    url += $"#token={token}";
                }

                return Redirect(url);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex.GetTrue());

                throw;
            }
        }

        /// <summary>注销登录</summary>
        /// <remarks>
        /// 子系统引导用户跳转到这里注销登录。
        /// </remarks>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Logout()
        {
            // 先读Session，待会会清空
            var prov = Provider;
            var name = Session["Cube_Sso"] as String;
            var client = prov.GetClient(name);
            client.Init(GetUserAgent());

            var prv = Provider;
            prv?.Logout();

            var url = "";

            // 准备跳转到验证中心
            var set = Setting.Current;
            if (client != null && set.LogoutAll)
            {
                if (client.LogoutUrl.IsNullOrEmpty() && name.EqualIgnoreCase("NewLife")) client.LogoutUrl = "logout?client_id={key}&redirect_uri={redirect}&state={state}";
                if (!client.LogoutUrl.IsNullOrEmpty())
                {
                    // 准备返回地址
                    url = GetRequest("r");
                    if (url.IsNullOrEmpty()) url = prv.SuccessUrl;

                    var state = GetRequest("state");

                    url = url.AsUri(Request.GetRawUrl()) + "";

                    // 跳转到验证中心注销地址
                    url = client.Logout(url, state);

                    return Redirect(url);
                }
            }

            url = Provider?.GetReturnUrl(Request, false);
            if (url.IsNullOrEmpty()) url = "~/";

            return Redirect(url);
        }

        /// <summary>绑定</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual ActionResult Bind(String id)
        {
            var prov = Provider;

            var user = prov.Current;
            if (user == null)
            {
#if __CORE__
                var returnUrl = Request.GetEncodedPathAndQuery();
#else
                var returnUrl = Request.Url?.PathAndQuery;
#endif
                var rurl = "~/Admin/User/Login".AppendReturn(returnUrl);
                return Redirect(rurl);
            }

            var url = prov.GetReturnUrl(Request, true);
            var client = prov.GetClient(id);
            client.Init(GetUserAgent());

            var redirect = prov.GetRedirect(Request, "~/Sso/LoginInfo/" + client.Name);
            var state = $"r={url}&a=bind";
            url = client.Authorize(redirect, state);

            return Redirect(url);
        }

        /// <summary>取消绑定</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual ActionResult UnBind(String id)
        {
            var user = Provider.Current;
            if (user == null) throw new Exception("未登录！");

            var binds = UserConnect.FindAllByUserID(user.ID);

            foreach (var uc in binds)
            {
                if (uc.Provider.EqualIgnoreCase(id))
                {
                    uc.Enable = false;
                    uc.Update();
                }
            }

            if (IsJsonRequest) return Ok();

            var url = Provider.GetReturnUrl(Request, true);
            if (url.IsNullOrEmpty()) url = "/";

            return Redirect(url);
        }
        #endregion

        #region 单点登录服务端
        /// <summary>1，验证用户身份</summary>
        /// <remarks>
        /// 子系统需要验证访问者身份时，引导用户跳转到这里。
        /// 用户登录完成后，得到一个独一无二的code，并跳转回去子系统。
        /// </remarks>
        /// <param name="client_id">应用标识</param>
        /// <param name="redirect_uri">回调地址</param>
        /// <param name="response_type">响应类型。默认code</param>
        /// <param name="scope">授权域</param>
        /// <param name="state">用户状态数据</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Authorize(String client_id, String redirect_uri, String response_type = null, String scope = null, String state = null)
        {
            // 参数不完整时，跳转到登录页面，避免爬虫抓取而导致误报告警
            if (client_id.IsNullOrEmpty()) return Redirect(Provider.LoginUrl);

            //if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
            if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));
            if (response_type.IsNullOrEmpty()) response_type = "code";

            // 判断合法性，然后跳转到登录页面，登录完成后跳转回来
            var key = OAuth.Authorize(client_id, redirect_uri, response_type, scope, state);

            var prov = Provider;
            var url = "";

            // 如果已经登录，直接返回。否则跳到登录页面
            var user = prov?.Current;
            if (user == null)
            {
#if __CORE__
                user = prov?.Provider.TryLogin(HttpContext);
#else
                user = prov?.Provider.TryLogin(HttpContext.ApplicationInstance.Context);
#endif
            }
            if (user != null)
                url = OAuth.GetResult(key, user);
            else
                url = prov.GetLoginUrl(key);

            return Redirect(url);
        }

        /// <summary>2，用户登录成功后返回这里</summary>
        /// <remarks>
        /// 构建身份验证结构，返回code给子系统
        /// </remarks>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Auth2(String id)
        {
            if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

            var user = Provider?.Current;
            //if (user == null) throw new InvalidOperationException("未登录！");
            // 未登录时跳转到登录页面，重新认证
            if (user == null) return Redirect(Provider.GetLoginUrl(id));

            // 返回给子系统的数据：
            // code 授权码，子系统凭借该代码来索取用户信息
            // state 子系统传过来的用户状态数据，原样返回

            var url = OAuth.GetResult(id, user);

            return Redirect(url);
        }

        /// <summary>3，根据code获取令牌</summary>
        /// <remarks>
        /// 子系统根据验证用户身份时得到的code，直接在服务器间请求本系统。
        /// 传递应用标识和密钥，主要是为了向本系统表明其合法身份。
        /// </remarks>
        /// <param name="client_id">应用标识</param>
        /// <param name="client_secret">密钥</param>
        /// <param name="code">代码</param>
        /// <param name="grant_type">授权类型</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Access_Token(String client_id, String client_secret, String code, String grant_type = null)
        {
            if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
            if (client_secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_secret));
            if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code));
            if (grant_type.IsNullOrEmpty()) grant_type = "authorization_code";

            if (!grant_type.EqualIgnoreCase("authorization_code")) throw new NotSupportedException(nameof(grant_type));

            // 返回给子系统的数据：
            // access_token 访问令牌
            // expires_in 有效期
            // refresh_token 刷新令牌
            // openid 用户唯一标识

            try
            {
                var token = Provider.GetAccessToken(OAuth, client_id, client_secret, code, UserHost);

                // 返回UserInfo告知客户端可以请求用户信息
                var rs = new
                {
                    access_token = token.AccessToken,
                    refresh_token = token.RefreshToken,
                    expires_in = token.Expire,
                    scope = token.Scope,
                };
#if __CORE__
                return Json(rs);
#else
                return Json(rs, JsonRequestBehavior.AllowGet);
#endif
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"Access_Token client_id={client_id} client_secret={client_secret} code={code}");
                XTrace.WriteException(ex);
#if __CORE__
                return Json(new { errcode = 500, error = ex.GetTrue().Message });
#else
                return Json(new { errcode = 500, error = ex.GetTrue().Message }, JsonRequestBehavior.AllowGet);
#endif
            }
        }

        /// <summary>3，根据password/client_credentials获取令牌</summary>
        /// <remarks>
        /// 密码式：
        /// 用户把用户名和密码，直接告诉该应用。该应用就使用你的密码，申请令牌，这种方式称为"密码式"（password）。
        /// 为了避免密码暴露在Url中，需要用表单Post方式提交。
        /// 凭证式：
        /// 凭证式（client credentials），适用于没有前端的命令行应用，即在命令行下请求令牌。
        /// 针对第三方应用，而不是针对用户的，即有可能多个用户共享同一个令牌。
        /// </remarks>
        /// <param name="client_id">应用标识</param>
        /// <param name="client_secret">密钥</param>
        /// <param name="username">用户名。可以是设备编码等唯一使用者标识</param>
        /// <param name="password">密码</param>
        /// <param name="refresh_token">刷新令牌</param>
        /// <param name="grant_type">授权类型</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Token(String client_id, String client_secret, String username, String password, String refresh_token, String grant_type = null)
        {
            if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
            if (grant_type.IsNullOrEmpty()) grant_type = "password";

            // 返回给子系统的数据：
            // access_token 访问令牌
            // expires_in 有效期
            // refresh_token 刷新令牌
            // openid 用户唯一标识

            try
            {
                TokenInfo token = null;
                switch (grant_type.ToLower())
                {
                    case "password":
                        if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username));
                        if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password));

                        token = Provider.GetAccessTokenByPassword(OAuth, client_id, username, password, UserHost);
                        break;

                    case "client_credentials":
                        if (client_secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_secret));

                        // username 可以是设备编码等唯一使用者标识
                        token = Provider.GetAccessTokenByClientCredentials(OAuth, client_id, client_secret, username, UserHost);
                        break;

                    case "refresh_token":
                        if (refresh_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(refresh_token));

                        token = Provider.RefreshToken(OAuth, client_id, refresh_token, UserHost);
                        break;
                }

                var rs = new
                {
                    access_token = token.AccessToken,
                    refresh_token = token.RefreshToken,
                    expires_in = token.Expire,
                    scope = token.Scope,
                };
#if __CORE__
                return Json(rs);
#else
                return Json(rs, JsonRequestBehavior.AllowGet);
#endif
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"Token client_id={client_id} username={username} grant_type={grant_type}");
                XTrace.WriteException(ex);
#if __CORE__
                return Json(new { errcode = 500, error = ex.GetTrue().Message });
#else
                return Json(new { errcode = 500, error = ex.GetTrue().Message }, JsonRequestBehavior.AllowGet);
#endif
            }
        }

        /// <summary>3，根据password/client_credentials获取令牌</summary>
        /// <remarks>
        /// 密码式：
        /// 用户把用户名和密码，直接告诉该应用。该应用就使用你的密码，申请令牌，这种方式称为"密码式"（password）。
        /// 凭证式：
        /// 凭证式（client credentials），适用于没有前端的命令行应用，即在命令行下请求令牌。
        /// 针对第三方应用，而不是针对用户的，即有可能多个用户共享同一个令牌。
        /// </remarks>
        /// <param name="model">请求模型</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult PasswordToken([FromBody] SsoTokenModel model)
        {
            if (model.client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.client_id));
            if (model.grant_type.IsNullOrEmpty()) model.grant_type = "password";

            try
            {
                TokenInfo token = null;
                switch (model.grant_type.ToLower())
                {
                    case "password":
                        if (model.UserName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.UserName));
                        if (model.Password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.Password));

                        token = Provider.GetAccessTokenByPassword(OAuth, model.client_id, model.UserName, model.Password, UserHost);
                        break;

                    case "client_credentials":
                        if (model.client_secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.client_secret));

                        // username 可以是设备编码等唯一使用者标识
                        token = Provider.GetAccessTokenByClientCredentials(OAuth, model.client_id, model.client_secret, model.UserName, UserHost);
                        break;
                }

                var rs = new
                {
                    access_token = token.AccessToken,
                    refresh_token = token.RefreshToken,
                    expires_in = token.Expire,
                    scope = token.Scope,
                };
#if __CORE__
                return Json(rs);
#else
                return Json(rs, JsonRequestBehavior.AllowGet);
#endif
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"Token client_id={model.client_id} username={model.UserName} grant_type={model.grant_type}");
                XTrace.WriteException(ex);
#if __CORE__
                return Json(new { errcode = 500, error = ex.GetTrue().Message });
#else
                return Json(new { errcode = 500, error = ex.GetTrue().Message }, JsonRequestBehavior.AllowGet);
#endif
            }
        }

        /// <summary>4，根据token获取用户信息</summary>
        /// <param name="access_token">访问令牌</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult UserInfo(String access_token)
        {
            if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

            var sso = OAuth;
            IManageUser user = null;

            var msg = "";
            try
            {
                var username = OAuth.Decode(access_token);
                user = Provider?.GetUser(sso, username);
                if (user == null) throw new XException("用户[{0}]不存在", username);

                var rs = Provider.GetUserInfo(sso, access_token, user);
#if __CORE__
                return Json(rs);
#else
                return Json(rs, JsonRequestBehavior.AllowGet);
#endif
            }
            catch (Exception ex)
            {
                msg = ex.GetTrue().Message;

                XTrace.WriteLine($"UserInfo {access_token}");
                XTrace.WriteException(ex);
#if __CORE__
                return Json(new { errcode = 500, error = ex.GetTrue().Message });
#else
                return Json(new { errcode = 500, error = ex.GetTrue().Message }, JsonRequestBehavior.AllowGet);
#endif
            }
            finally
            {
                sso.WriteLog("UserInfo {0} access_token={1} msg={2}", user, access_token, msg);
            }
        }

        /// <summary>5，刷新令牌</summary>
        /// <remarks>
        /// 若access_token已超时，那么进行refresh_token会获取一个新的access_token，新的超时时间；
        /// 若access_token未超时，那么进行refresh_token不会改变access_token，但超时时间会刷新，相当于续期access_token。
        /// </remarks>
        /// <param name="client_id">应用标识</param>
        /// <param name="grant_type">授权类型</param>
        /// <param name="refresh_token">刷新令牌</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Refresh_Token(String client_id, String grant_type, String refresh_token)
        {
            if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
            if (grant_type.IsNullOrEmpty()) grant_type = "refresh_token";

            //!!! 由于访问令牌使用JWT，做不到微信的机制，刷新令牌时access_token不改变且续期。JWT想要续期，就必须重新颁发。

            try
            {
                if (refresh_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(refresh_token));

                var token = Provider.RefreshToken(OAuth, client_id, refresh_token, UserHost);

                var rs = new
                {
                    access_token = token.AccessToken,
                    refresh_token = token.RefreshToken,
                    expires_in = token.Expire,
                    scope = token.Scope,
                };
#if __CORE__
                return Json(rs);
#else
                return Json(rs, JsonRequestBehavior.AllowGet);
#endif
            }
            catch (Exception ex)
            {
                XTrace.WriteLine($"RefreshToken client_id={client_id} grant_type={grant_type} refresh_token={refresh_token}");
                XTrace.WriteException(ex);
#if __CORE__
                return Json(new { errcode = 500, error = ex.GetTrue().Message });
#else
                return Json(new { errcode = 500, error = ex.GetTrue().Message }, JsonRequestBehavior.AllowGet);
#endif
            }
        }

        /// <summary>验证令牌是否有效</summary>
        /// <param name="access_token">应用</param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Auth(String access_token)
        {
            if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

            var prv = Provider.Provider;
            var username = OAuth.Decode(access_token);

            // 设置登录用户，
            var user = prv.FindByName(username);
            prv.Current = user ?? throw new XException("用户[{0}]不存在", username);

#if __CORE__
            return Json(new { errcode = 0, error = "ok" });
#else
            return Json(new { errcode = 0, error = "ok" }, JsonRequestBehavior.AllowGet);
#endif
        }

        /// <summary>获取应用公钥，用于验证令牌</summary>
        /// <param name="client_id">应用</param>
        /// <param name="client_secret">密钥</param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult GetKey(String client_id, String client_secret)
        {
            var app = OAuth.Auth(client_id, client_secret);
            if (app == null) throw new ArgumentException($"无效应用[{client_id}]");

            var prv = OAuth.GetProvider();
            var dsa = new DSACryptoServiceProvider();
            dsa.FromXmlStringX(prv.Key);

            _ = dsa.ExportParameters(true);
            var key = dsa.ToXmlString(false);
            var rs = new { algorithm = "DSA", key };
#if __CORE__
            return Json(rs);
#else
            return Json(rs, JsonRequestBehavior.AllowGet);
#endif
        }

        /// <summary>验证令牌，回写cookie</summary>
        /// <param name="access_token">应用</param>
        /// <param name="redirect_uri">回调地址</param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Verify(String access_token, String redirect_uri)
        {
            if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

            var prv = Provider.Provider;
            var username = OAuth.Decode(access_token);

            // 设置登录用户，
            var user = prv.FindByName(username);
            prv.Current = user ?? throw new XException("用户[{0}]不存在", username);

            if (redirect_uri.IsNullOrEmpty()) return Content("ok");

            return Redirect(redirect_uri);
        }
        #endregion

        #region 辅助
        /// <summary>获取用户头像</summary>
        /// <param name="id">用户编号</param>
        /// <returns></returns>
        [AllowAnonymous]
        public virtual ActionResult Avatar(Int32 id)
        {
            if (id <= 0) throw new ArgumentNullException(nameof(id));

            var prv = Provider;
            if (prv == null) throw new ArgumentNullException(nameof(Provider));

            var set = Setting.Current;
            var av = set.AvatarPath.CombinePath(id + ".png").GetBasePath();
            if (!System.IO.File.Exists(av))
            {
                var user = prv.Provider?.FindByID(id);
                if (user == null) throw new Exception("用户不存在 " + id);

                prv.FetchAvatar(user);
            }
            if (!System.IO.File.Exists(av)) throw new Exception("用户头像不存在 " + id);

#if __CORE__
            var vs = System.IO.File.ReadAllBytes(av);
            return File(vs, "image/png");
#else
            return File(av, "image/png");
#endif
        }
        #endregion
    }
}