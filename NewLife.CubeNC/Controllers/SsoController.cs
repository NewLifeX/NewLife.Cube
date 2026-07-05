using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Services.Sso;
using NewLife.Cube.Web;
using NewLife.Cube.Web.Models;
using NewLife.Log;
using NewLife.Model;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Web;
using XCode.Membership;

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

namespace NewLife.Cube.Controllers;

/// <summary>单点登录控制器</summary>
public class SsoController : ControllerBaseX
{
    private const String OAuthPendingPrefix = "OAuthPending:";

    /// <summary>存储最近用过的code，避免用户刷新页面</summary>
    private readonly ICache _cache;
    private readonly CubeSetting _setting;
    private readonly ISsoClientService _clientService;
    private readonly ISsoServerService _serverService;
    private readonly ITokenService _tokenService;
    private readonly IOAuthAppService _appService;
    private readonly IUserBindingService _bindingService;

    /// <summary>实例化单点登录控制器</summary>
    /// <param name="setting"></param>
    /// <param name="cacheProvider"></param>
    /// <param name="clientService"></param>
    /// <param name="serverService"></param>
    /// <param name="tokenService"></param>
    /// <param name="appService"></param>
    /// <param name="bindingService"></param>
    public SsoController(CubeSetting setting, ICacheProvider cacheProvider,
        ISsoClientService clientService, ISsoServerService serverService,
        ITokenService tokenService, IOAuthAppService appService, IUserBindingService bindingService)
    {
        _cache = cacheProvider.Cache;
        _setting = setting;
        _clientService = clientService;
        _serverService = serverService;
        _tokenService = tokenService;
        _appService = appService;
        _bindingService = bindingService;

        ((OAuthAppService)_appService).Setting = _setting;

        // 设置SSO客户端服务的用户绑定服务（ASP.NET Core DI 不支持属性注入）
        ((SsoClientService)_clientService).BindingService = bindingService;

        // 设置SSO服务端服务的依赖属性
        ((SsoServerService)_serverService).AppService = appService;
        ((SsoServerService)_serverService).TokenService = tokenService;

        // 设置令牌服务的依赖属性
        ((Services.Sso.TokenService)_tokenService).AppService = appService;
    }

    /// <summary>首页</summary>
    /// <returns></returns>
    [AllowAnonymous]
    public virtual ActionResult Index() => Redirect("~/");

    #region 单点登录客户端
    private String GetUserAgent() => Request.Headers.UserAgent + "";

    /// <summary>第三方登录</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [AllowAnonymous]
    public virtual ActionResult Login(String name)
    {
        var rurl = _clientService.GetReturnUrl(Request, true);

        try
        {
            var client = _clientService.GetClient(TenantContext.CurrentId, name);
            client.Init(GetUserAgent());

            return base.Redirect(OnLogin(client, null, rurl, null));
        }
        catch (InvalidOperationException)
        {
            var retUrl = "~/Admin/User/Login".AppendReturn(rurl);
            return Redirect(retUrl);
        }
    }

    private String OnLogin(OAuthClient client, String state, String returnUrl, OAuthLog log)
    {
        var redirect = _clientService.GetRedirect(Request, "~/Sso/LoginInfo/" + client.Name, client.Name);
        // 请求来源，前后端分离时传front-end，重定向会带上token放到锚点
        var source = GetRequest("source");

        if (log == null)
        {
            log = new OAuthLog
            {
                Provider = client.Name,
                Action = "Login",
                Success = false,
                ResponseType = client.ResponseType,
                Scope = client.Scope,
                State = state,
                RedirectUri = returnUrl,
                Source = source,
                TraceId = DefaultSpan.Current?.TraceId,
                Remark = GetUserAgent(),
            };
            log.Insert();
        }

        return client.Authorize(redirect, log.Id + "");
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
        var client = _clientService.GetClient(TenantContext.CurrentId, name);
        client.Init(GetUserAgent());

        client.WriteLog("LoginInfo name={0} code={1} state={2} {3}", name, code, state, Request.GetRawUrl());

        var log = OAuthLog.FindById(state.ToLong());
        if (log == null) throw new InvalidOperationException("无效state=" + state);

        // 无法拿到code时，跳回去再来
        if (code.IsNullOrEmpty())
        {
            return Redirect(OnLogin(client, null, null, log));
        }
        // 短期内用过的code也跳回
        if (!_cache.Add(code, code, 600))
            return Redirect(OnLogin(client, null, null, log));

        // 构造redirect_uri，部分提供商（百度）要求获取AccessToken的时候也要传递
        var redirect = _clientService.GetRedirect(Request, "~/Sso/LoginInfo/" + client.Name, client.Name);
        client.Authorize(redirect);

        var returnUrl = log.RedirectUri;

        try
        {
            // 获取访问令牌
            if (!client.AccessUrl.IsNullOrEmpty())
            {
                var html = client.GetAccessToken(code);

                if (client.AccessToken.IsNullOrEmpty() && client.OpenID.IsNullOrEmpty() && client.UserID == 0 && client.UserName.IsNullOrEmpty())
                {
                    XTrace.WriteLine("[{2}]拿不到访问令牌，自动重跳SSO重新授权 code={0} state={1}", code, state, id);
                    XTrace.WriteLine(Request.GetRawUrl() + "");
                    if (!html.IsNullOrEmpty()) XTrace.WriteLine(html);

                    log.Success = false;
                    log.Remark = $"无法获取令牌，自动重新授权: {html}";
                    log.Update();

                    return Redirect(OnLogin(client, null, returnUrl, null));
                }

                log.AccessToken = client.AccessToken;
                log.RefreshToken = client.RefreshToken;
            }

            if (!client.OpenIDUrl.IsNullOrEmpty()) client.GetOpenID();

            var set = CubeSetting.Current;
            var uc = _bindingService.GetConnect(client);
            if (uc.UpdateTime.AddSeconds(set.RefreshUserPeriod) < DateTime.Now)
            {
                if (!client.UserUrl.IsNullOrEmpty()) client.GetUserInfo();
            }

            if (uc.ID == 0) uc = _bindingService.GetConnect(client);
            uc.Fill(client);

            var url = _clientService.OnLogin(client, HttpContext.RequestServices, uc, log.Action == "Bind", log.UserId);

            log.ConnectId = uc.ID;
            log.UserId = uc.UserID;
            log.Success = true;
            log.Update();

            Session["Cube_Sso"] = client.Name;

            if (HttpContext.Items["Cube_Online"] is UserOnline olt)
            {
                olt.OAuthProvider = client.Name;
                olt.SaveAsync();
            }
            var stat = UserStat.GetOrAdd(DateTime.Today);
            if (stat != null)
            {
                stat.Logins++;
                stat.OAuths++;
                stat.SaveAsync(5_000);
            }

            if (url.IsNullOrEmpty())
            {
                Session["Cube_OAuthId"] = log.Id;

                var oauthToken = Rand.NextString(32);
                var oauthPending = new OAuthPendingInfoModel
                {
                    Provider = client.Name,
                    Username = client.UserName,
                    Email = client.Mail,
                    Mobile = client.Mobile,
                    Avatar = client.Avatar,
                };
                _cache.Set($"{OAuthPendingPrefix}{oauthToken}", oauthPending, 600);

                return Redirect($"/register?oauthToken={oauthToken}".AppendReturn(returnUrl));
            }

            var logId = Session["Cube_OAuthId"].ToLong();
            if (logId > 0 && logId != log.Id)
            {
                Session["Cube_OAuthId"] = null;
                var log2 = _bindingService.BindAfterLogin(logId);
                if (log2 != null && log2.Success && !log2.RedirectUri.IsNullOrEmpty()) return Redirect(log2.RedirectUri);
            }

            if (!returnUrl.IsNullOrEmpty()) url = returnUrl;

            var user = ManageProvider.Provider.Current;
            if (log.Source == "front-end")
            {
                if (!_clientService.IsSafeDomain(url, set.SsoSafeDomains))
                {
                    XTrace.WriteLine("[安全] SSO Token 颁发被拦截，跨域目标不在 SsoSafeDomains 白名单: {0}", url);
                    url = _clientService.SuccessUrl;
                }
                else
                {
                    var token = HttpContext.IssueToken(user, TimeSpan.FromSeconds(set.TokenExpire));
                    url += $"#token={token}";
                }
            }

            HttpContext.ChooseTenant(user.ID);

            // 防御性加固：确保 .Cube.Session cookie 已发送，防止外部OAuth回调后 Session ID 漂移
            var sessionKey = ".Cube.Session";
            var sid = Request.Cookies[sessionKey];
            if (!sid.IsNullOrEmpty())
                Response.Cookies.Append(sessionKey, sid);

            return Redirect(url);
        }
        catch (Exception ex)
        {
            if (log.Remark.IsNullOrEmpty()) log.Remark = ex.ToString();
            log.Success = false;
            log.SaveAsync();

            XTrace.WriteException(ex);

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
        var name = GetRequest("name");
        if (name.IsNullOrEmpty()) name = Session["Cube_Sso"] as String;
        var client = _clientService.GetClient(TenantContext.CurrentId, name);
        client.Init(GetUserAgent());

        _bindingService.Logout();

        var url = "";

        // 准备跳转到验证中心
        var set = CubeSetting.Current;
        if (client != null && set.LogoutAll)
        {
            if (client.LogoutUrl.IsNullOrEmpty() && name.EqualIgnoreCase("NewLife"))
                client.LogoutUrl = "logout?client_id={key}&redirect_uri={redirect}&state={state}";
            if (!client.LogoutUrl.IsNullOrEmpty())
            {
                // 准备返回地址
                url = GetRequest("r");
                if (url.IsNullOrEmpty()) url = GetRequest("ReturnUrl");
                if (url.IsNullOrEmpty()) url = _clientService.SuccessUrl;

                var state = GetRequest("state");

                url = url.AsUri(Request.GetRawUrl()?.ToUri()) + "";

                // 跳转到验证中心注销地址
                url = client.Logout(url, state);

                return Redirect(url);
            }
        }

        url = _clientService.GetReturnUrl(Request, false);
        if (url.IsNullOrEmpty()) url = "~/";

        // 严格校验回跳地址，区分SSO模式和本地模式
        if (url.StartsWithIgnoreCase("http://", "https://"))
        {
            var clientId = GetRequest("client_id");
            if (!clientId.IsNullOrEmpty())
            {
                var app = _appService.Auth(clientId, null, UserHost);
                if (!app.ValidCallback(url)) throw new XException("回调地址不合法 {0}", url);
            }
            else
            {
                // 本地模式，只允许回到本站点
                url = "~/";
            }
        }

        return Redirect(url);
    }

    /// <summary>绑定</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual ActionResult Bind(String id)
    {
        var user = _clientService.Current;
        if (user == null)
        {
            var returnUrl = Request.GetEncodedPathAndQuery();
            var rurl = "~/Admin/User/Login".AppendReturn(returnUrl);
            return Redirect(rurl);
        }

        var url = _clientService.GetReturnUrl(Request, true);
        var client = _clientService.GetClient(TenantContext.CurrentId, id);
        client.Init(GetUserAgent());

        var redirect = _clientService.GetRedirect(Request, "~/Sso/LoginInfo/" + client.Name, client.Name);

        var log = new OAuthLog
        {
            Provider = client.Name,
            Action = "Bind",
            Success = false,
            ResponseType = client.ResponseType,
            Scope = client.Scope,
            State = null,
            RedirectUri = url,
            UserId = user.ID,
            TraceId = DefaultSpan.Current?.TraceId,
        };
        log.Insert();

        url = client.Authorize(redirect, log.Id + "");

        return Redirect(url);
    }

    /// <summary>取消绑定</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual ActionResult UnBind(String id)
    {
        var user = _clientService.Current;
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

        var url = _clientService.GetReturnUrl(Request, true);
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
    /// <param name="loginUrl">登录页。子系统请求SSO时，如果在SSO未登录则直接跳转的地址，该地址有可能属于子系统自身，适用于password模式登录等场景</param>
    /// <returns></returns>
    [AllowAnonymous]
    public virtual ActionResult Authorize(String client_id, String redirect_uri, String response_type = null, String scope = null, String state = null, String loginUrl = null)
    {
        // 参数不完整时，跳转到登录页面，避免爬虫抓取而导致误报告警
        if (client_id.IsNullOrEmpty()) return Redirect(loginUrl ?? _clientService.LoginUrl);

        //有些第三方客户端使用redirect_url作为回调地址参数名
        if (redirect_uri.IsNullOrEmpty()) redirect_uri = GetRequest("redirect_url");
        if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));
        if (response_type.IsNullOrEmpty()) response_type = "code";

        // 判断合法性，然后跳转到登录页面，登录完成后跳转回来
        var key = _serverService.Authorize(client_id, redirect_uri, response_type, scope, state, UserHost);

        var url = "";

        // 如果已经登录，直接返回。否则跳到登录页面
        var user = _clientService.Current;
        user ??= ManageProvider.Provider.TryLogin(HttpContext);
        if (user != null)
            url = _serverService.GetResult(key, user);
        else if (!loginUrl.IsNullOrEmpty())
            url = loginUrl;
        else
            url = _clientService.GetLoginUrl(key);

        return Redirect(url);
    }

    /// <summary>2，用户登录成功后返回这里</summary>
    [AllowAnonymous]
    public virtual ActionResult Auth2(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var user = _clientService.Current;
        // 如果Session未登录，尝试从Cookie/Token恢复，避免因Session丢失导致死循环重定向
        user ??= ManageProvider.Provider.TryLogin(HttpContext);
        if (user == null) return Redirect(_clientService.GetLoginUrl(id));

        var url = _serverService.GetResult(id, user);

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
    [HttpGet]
    [HttpPost]
    public virtual ActionResult Access_Token(String client_id, String client_secret, String code, String grant_type = null)
    {
        if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
        if (client_secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_secret));
        if (code.IsNullOrEmpty()) throw new ArgumentNullException(nameof(code));
        if (grant_type.IsNullOrEmpty()) grant_type = "authorization_code";

        if (!grant_type.EqualIgnoreCase("authorization_code")) throw new NotSupportedException(nameof(grant_type));

        try
        {
            var token = _tokenService.GetAccessToken(client_id, client_secret, code, UserHost);

            var rs = new
            {
                access_token = token.AccessToken,
                refresh_token = token.RefreshToken,
                expires_in = token.ExpireIn,
                scope = token.Scope,
            };
            return SsoJsonOK(rs);
        }
        catch (Exception ex)
        {
            XTrace.WriteLine($"Access_Token client_id={client_id} client_secret={client_secret} code={code}");
            XTrace.WriteException(ex);
            return SsoJsonError(ex);
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
    [HttpGet]
    [HttpPost]
    public virtual ActionResult Token(String client_id, String client_secret, String username, String password, String refresh_token, String grant_type = null)
    {
        if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
        if (grant_type.IsNullOrEmpty()) grant_type = "password";

        try
        {
            TokenModel token = null;
            switch (grant_type.ToLower())
            {
                case "password":
                    if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username));
                    if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password));

                    token = _tokenService.GetAccessTokenByPassword(client_id, username, password, UserHost);
                    break;

                case "client_credentials":
                    if (client_secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_secret));

                    token = _tokenService.GetAccessTokenByClientCredentials(client_id, client_secret, username, UserHost);
                    break;

                case "refresh_token":
                    if (refresh_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(refresh_token));

                    token = _tokenService.RefreshToken(client_id, refresh_token, UserHost);
                    break;
            }

            var rs = new
            {
                access_token = token.AccessToken,
                refresh_token = token.RefreshToken,
                expires_in = token.ExpireIn,
                scope = token.Scope,
            };
            return SsoJsonOK(rs);
        }
        catch (Exception ex)
        {
            XTrace.WriteLine($"Token client_id={client_id} username={username} grant_type={grant_type}");
            XTrace.WriteException(ex);
            return SsoJsonError(ex);
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
    [HttpGet]
    [HttpPost]
    public virtual ActionResult PasswordToken([FromBody] SsoTokenModel model)
    {
        if (model.client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.client_id));
        if (model.grant_type.IsNullOrEmpty()) model.grant_type = "password";

        try
        {
            TokenModel token = null;
            switch (model.grant_type.ToLower())
            {
                case "password":
                    if (model.UserName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.UserName));
                    if (model.Password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.Password));

                    token = _tokenService.GetAccessTokenByPassword(model.client_id, model.UserName, model.Password, UserHost);
                    break;

                case "client_credentials":
                    if (model.client_secret.IsNullOrEmpty()) throw new ArgumentNullException(nameof(model.client_secret));

                    token = _tokenService.GetAccessTokenByClientCredentials(model.client_id, model.client_secret, model.UserName, UserHost);
                    break;
            }

            var rs = new
            {
                access_token = token.AccessToken,
                refresh_token = token.RefreshToken,
                expires_in = token.ExpireIn,
                scope = token.Scope,
            };
            return SsoJsonOK(rs);
        }
        catch (Exception ex)
        {
            XTrace.WriteLine($"Token client_id={model.client_id} username={model.UserName} grant_type={model.grant_type}");
            XTrace.WriteException(ex);
            return SsoJsonError(ex);
        }
    }

    /// <summary>4，根据token获取用户信息</summary>
    /// <param name="access_token">访问令牌</param>
    /// <returns></returns>
    [AllowAnonymous]
    public virtual ActionResult UserInfo(String access_token)
    {
        if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

        IManageUser user = null;

        var msg = "";
        try
        {
            var username = _tokenService.Decode(access_token);
            user = _tokenService.GetUser(username);
            if (user == null) throw new XException("用户[{0}]不存在", username);

            var rs = _tokenService.GetUserInfo(access_token, user);
            return SsoJsonOK(rs);
        }
        catch (Exception ex)
        {
            msg = ex.GetTrue().Message;

            XTrace.WriteLine($"UserInfo {access_token}");
            XTrace.WriteException(ex);
            return SsoJsonError(ex);
        }
        finally
        {
            _tokenService.WriteLog("UserInfo {0} access_token={1} msg={2}", user, access_token, msg);
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
    [HttpGet]
    [HttpPost]
    public virtual ActionResult Refresh_Token(String client_id, String grant_type, String refresh_token)
    {
        if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
        if (grant_type.IsNullOrEmpty()) grant_type = "refresh_token";

        //!!! 由于访问令牌使用JWT，做不到微信的机制，刷新令牌时access_token不改变且续期。JWT想要续期，就必须重新颁发。

        if (refresh_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(refresh_token));

        try
        {
            var token = _tokenService.RefreshToken(client_id, refresh_token, UserHost);

            var rs = new
            {
                access_token = token.AccessToken,
                refresh_token = token.RefreshToken,
                expires_in = token.ExpireIn,
                scope = token.Scope,
            };

            return SsoJsonOK(rs);
        }
        catch (Exception ex)
        {
            XTrace.WriteLine($"RefreshToken client_id={client_id} grant_type={grant_type} refresh_token={refresh_token}");
            XTrace.WriteException(ex);
            return SsoJsonError(ex);
        }
    }

    /// <summary>验证令牌是否有效</summary>
    /// <param name="access_token">应用</param>
    /// <returns></returns>
    [AllowAnonymous]
    public ActionResult Auth(String access_token)
    {
        try
        {
            if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

            var prv = ManageProvider.Provider;
            var username = _tokenService.Decode(access_token);

            var user = prv.FindByName(username);
            prv.Current = user ?? throw new XException("用户[{0}]不存在", username);

            return SsoJsonOK(new { errcode = 0, error = "ok" });
        }
        catch (Exception ex)
        {
            return SsoJsonError(ex);
        }
    }

    [AllowAnonymous]
    public ActionResult GetKey(String client_id, String client_secret)
    {
        try
        {
            var app = _appService.Auth(client_id, client_secret + "", UserHost);
            if (app == null) throw new ArgumentException($"无效应用[{client_id}]");

            var prv = _appService.GetProvider();
            var dsa = new DSACryptoServiceProvider();
            dsa.FromXmlStringX(prv.Key);

            _ = dsa.ExportParameters(true);
            var key = dsa.ToXmlString(false);
            var rs = new { algorithm = "DSA", key };
            return SsoJsonOK(rs);
        }
        catch (Exception ex)
        {
            return SsoJsonError(ex);
        }
    }

    [AllowAnonymous]
    public ActionResult Verify(String access_token, String redirect_uri)
    {
        if (access_token.IsNullOrEmpty()) throw new ArgumentNullException(nameof(access_token));

        var prv = ManageProvider.Provider;
        var username = _tokenService.Decode(access_token);

        var user = prv.FindByName(username);
        prv.Current = user ?? throw new XException("用户[{0}]不存在", username);

        var set = CubeSetting.Current;
        var expire = TimeSpan.FromMinutes(0);
        if (set.SessionTimeout > 0)
            expire = TimeSpan.FromSeconds(set.SessionTimeout);

        prv.SaveCookie(user, expire, HttpContext);

        if (redirect_uri.IsNullOrEmpty()) return Content("ok");

        return Redirect(redirect_uri);
    }

    /// <summary>用户验证。借助OAuth密码式验证，并返回用户信息</summary>
    /// <param name="model">令牌模型</param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    [HttpPost]
    public virtual ActionResult UserAuth([FromBody] SsoTokenModel model)
    {
        var client_id = model.client_id;
        var username = model.UserName;
        var password = model.Password;
        if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));

        try
        {
            if (username.IsNullOrEmpty()) throw new ArgumentNullException(nameof(username));
            if (password.IsNullOrEmpty()) throw new ArgumentNullException(nameof(password));

            var token = _tokenService.GetAccessTokenByPassword(client_id, username, password, UserHost);
            var rs = new
            {
                access_token = token.AccessToken,
                refresh_token = token.RefreshToken,
                expires_in = token.ExpireIn,
                scope = token.Scope,
            };
            var dic = rs.ToDictionary();

            var user = _tokenService.GetUser(username);
            if (user == null) throw new XException("用户[{0}]不存在", username);

            var rs2 = _tokenService.GetUserInfo(token.AccessToken, user);
            dic.Merge(rs2);

            return Json(0, null, dic);
        }
        catch (Exception ex)
        {
            XTrace.WriteLine($"UserAuth client_id={client_id} username={username}");
            XTrace.WriteException(ex);
            return Json(0, null, ex);
        }
    }
    #endregion

    #region 辅助
    /// <summary>获取用户头像。头像文件不存在时根据昵称和性别自动生成 SVG 文字头像</summary>
    /// <param name="id">用户编号</param>
    /// <returns></returns>
    [AllowAnonymous]
    public virtual ActionResult Avatar(Int32 id)
    {
        if (id <= 0) throw new ArgumentNullException(nameof(id));

        var user = ManageProvider.Provider?.FindByID(id) as IUser;
        if (user == null) throw new Exception("用户不存在 " + id);

        var set = CubeSetting.Current;
        FileInfo? av = null;
        if (!user.Avatar.IsNullOrEmpty() && !user.Avatar.StartsWith("/"))
        {
            av = set.AvatarPath.CombinePath(user.Avatar).GetBasePath().AsFile();
            if (!av.Exists) av = null;
        }

        // 用于兼容旧代码：按扩展名优先级查找（.png/.svg/.jpg/.gif/.webp）
        if (av == null && !set.AvatarPath.IsNullOrEmpty())
        {
            var (found, _) = SvgAvatarService.FindAvatarFile(set.AvatarPath, user.ID);
            av = found != null ? found.AsFile() : null;
        }

        // 使用最后一个第三方头像
        if (av == null && user is IManageUser muser)
        {
            var list = UserConnect.FindAllByUserID(user.ID);
            foreach (var item in list.OrderByDescending(e => e.UpdateTime))
            {
                var url = item.Avatar;
                if (!url.IsNullOrEmpty() && url.StartsWithIgnoreCase("http://", "https://"))
                {
                    // 自动下载头像
                    var cfg = OAuthConfig.FindByName(item.Provider);
                    if (cfg != null && cfg.FetchAvatar)
                        Task.Run(() => _bindingService.FetchAvatar(muser, url, item.AccessToken));

                    return Redirect(url);
                }
            }
        }

        // 头像文件不存在时，根据昵称和性别生成 SVG 文字头像
        if (av == null || !av.Exists)
        {
            var svg = SvgAvatarService.Generate(user, set.AvatarChars);
            return Content(svg, "image/svg+xml");
        }

        var vs = av.ReadBytes();

        // 设置文件哈希相关响应头
        Response.SetFileHashHeaders(vs.MD5().ToHex());

        var ct = SvgAvatarService.GetContentType(av.Extension);
        return File(vs, ct);
    }

    private ActionResult SsoJsonOK(Object data) => Json(data);

    private ActionResult SsoJsonError(Exception ex)
    {
        var code = 500;
        if (ex is ApiException aex && code > 0) code = aex.Code;
        return Json(new { errcode = code, error = ex.GetTrue().Message });
    }
    #endregion
}