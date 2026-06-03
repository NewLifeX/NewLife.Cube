using System.ComponentModel;
using System.Net;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.Services.Sso;
using NewLife.Cube.Web;
using NewLife.Cube.Web.Models;
using NewLife.Log;
using NewLife.Model;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Web;
using NewLife.Web.OAuth;
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

/// <summary>单点登录。OAuth2.0客户端与服务端</summary>
/// <remarks>
/// 魔方支持接入微信钉钉等多个第三方OAuth2.0服务。
/// 魔方自身也可以作为OAuth2.0服务端，支持密码式、凭证式、刷新令牌等多种授权模式。
/// </remarks>
[DisplayName("单点登录")]
[Description("""
    魔方支持接入微信钉钉等多个第三方OAuth2.0服务。
    魔方自身也可以作为OAuth2.0服务端，支持密码式、凭证式、刷新令牌等多种授权模式。
    """)]
//[ApiExplorerSettings(GroupName = "Cube")]
[Route("[controller]/[action]")]
public class SsoController : ControllerBaseX
{
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
    /// <param name="clientService">SSO客户端服务</param>
    /// <param name="serverService">SSO服务端服务</param>
    /// <param name="tokenService">令牌服务</param>
    /// <param name="appService">应用验证服务</param>
    /// <param name="bindingService">用户绑定服务</param>
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

        // 设置应用验证服务的CubeSetting
        ((OAuthAppService)_appService).Setting = _setting;
    }

    #region 单点登录客户端
    private String GetUserAgent() => Request.Headers.UserAgent + "";

    /// <summary>第三方登录</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public virtual ActionResult Login(String name)
    {
        var client = _clientService.GetClient(TenantContext.CurrentId, name);
        client.Init(GetUserAgent());

        var rurl = _clientService.GetReturnUrl(Request, true);

        return base.Redirect(OnLogin(client, null, rurl, null));
    }

    #region MyRegion

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
    [HttpGet]
    [HttpGet("{id}")]
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

                // 如果拿不到访问令牌或用户信息，则重新跳转
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

            // 获取OpenID。部分提供商不需要
            if (!client.OpenIDUrl.IsNullOrEmpty()) client.GetOpenID();

            // 短时间内不要重复拉取用户信息
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

            // 标记登录提供商
            Session["Cube_Sso"] = client.Name;

            // 记录在线统计
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

            // 如果验证成功但登录失败，直接跳走
            if (url.IsNullOrEmpty())
            {
                Session["Cube_OAuthId"] = log.Id;
                return Redirect("/Admin/User/Login?autologin=0".AppendReturn(returnUrl));
            }

            // 登录后自动绑定
            var logId = Session.ContainsKey("Cube_OAuthId") ? Session["Cube_OAuthId"].ToLong() : 0;
            if (logId > 0 && logId != log.Id)
            {
                Session["Cube_OAuthId"] = null;
                var log2 = _bindingService.BindAfterLogin(logId);
                if (log2 != null && log2.Success && !log2.RedirectUri.IsNullOrEmpty()) return Redirect(log2.RedirectUri);
            }

            if (!returnUrl.IsNullOrEmpty()) url = returnUrl;

            // 子系统颁发token给前端（防御纵深：再次校验目标URL是否在安全域名白名单内）
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

            // 设置租户
            HttpContext.ChooseTenant(user.ID);

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
    [HttpGet]
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
                url = GetRequest("r");
                if (url.IsNullOrEmpty()) url = GetRequest("ReturnUrl");
                if (url.IsNullOrEmpty()) url = _clientService.SuccessUrl;

                var logoutState = GetRequest("state");

                url = url.AsUri(Request.GetRawUrl()?.ToUri()) + "";

                url = client.Logout(url, logoutState);

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
                url = "~/";
            }
        }

        return Redirect(url);
    }

    /// <summary>绑定</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
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
    [HttpGet]
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

        return Json(0, "ok");
    }

    #endregion
    #endregion

    #region 微信登录
    /// <summary>微信小程序登录</summary>
    [AllowAnonymous]
    [HttpPost]
    public virtual ApiResponse<TokenModel> WxMiniLogin(WxLoginModel model) => WxLogin(model.Code, model.AppId, "WxOpen").ToOkApiResponse();

    /// <summary>微信APP登录</summary>
    [AllowAnonymous]
    [HttpPost]
    public virtual ApiResponse<TokenModel> WxAppLogin(WxLoginModel model) => WxLogin(model.Code, model.AppId, "WxApp").ToOkApiResponse();

    /// <summary>微信登录核心方法</summary>
    protected virtual TokenModel WxLogin(String code, String appId, String wxLoginType)
    {
        if (String.IsNullOrWhiteSpace(code))
            throw new ApiException(CubeCode.ParamError.ToInt(), $"参数错误：{nameof(code)}");
        if (String.IsNullOrWhiteSpace(appId))
            throw new ApiException(CubeCode.ParamError.ToInt(), $"参数错误：{nameof(appId)}");

        var client = CreateWxOpenClient(appId, wxLoginType);
        var tokenModel = ProcessWxLoginCore(client, code, $"{wxLoginType}", true);
        return tokenModel;
    }

    /// <summary>创建微信OAuth客户端</summary>
    protected virtual OAuthClient CreateWxOpenClient(String appId, String wxLoginType)
    {
        var client = _clientService.GetClient(TenantContext.CurrentId, wxLoginType);
        client.Init(GetUserAgent());

        OAuthConfig config = null;
        if (!String.IsNullOrWhiteSpace(appId))
            config = OAuthConfig.FindByAppId(appId);
        if (config == null)
            throw new ApiException(CubeCode.Exception.ToInt(),
                $"应用{nameof(OAuthConfig.AppId)}未配置");
        if (String.IsNullOrWhiteSpace(config.Secret))
            throw new ApiException(CubeCode.Exception.ToInt(),
                $"应用{nameof(OAuthConfig.Secret)}未配置");

        client.Key = config.AppId;
        client.Secret = config.Secret;
        client.TenantId = config.TenantId;
        return client;
    }

    /// <summary>处理微信登录的公共逻辑</summary>
    protected virtual TokenModel ProcessWxLoginCore(OAuthClient client, String code, String action, Boolean includeAvatar)
    {
        client.Log = XTrace.Log;

        var log = new OAuthLog
        {
            Provider = client.Name,
            Action = action,
            Success = false,
            ResponseType = client.ResponseType,
            Scope = client.Scope,
            TraceId = DefaultSpan.Current?.TraceId,
            Remark = GetUserAgent(),
        };
        log.Insert();

        try
        {
            client.GetAccessToken(code);

            if (client.OpenID.IsNullOrEmpty())
                throw new ApiException(CubeCode.RemotingError.ToInt(), "获取 OpenID 失败");

            log.AccessToken = client.AccessToken;
            log.RefreshToken = client.RefreshToken;

            if (!client.UserUrl.IsNullOrEmpty()) client.GetUserInfo();

            var uc = _bindingService.GetConnect(client);
            uc.Fill(client);

            _clientService.OnLogin(client, HttpContext.RequestServices, uc, false, 0);

            var user = ManageProvider.Provider.Current;
            if (user == null)
                throw new ApiException(CubeCode.Failed.ToInt(), "登录失败，用户不存在");

            log.ConnectId = uc.ID;
            log.UserId = uc.UserID;
            log.Success = true;
            log.Update();

            var set = CubeSetting.Current;
            var token = HttpContext.IssueTokenAndRefreshToken(user, TimeSpan.FromSeconds(set.TokenExpire));

            return token as TokenModel;
        }
        catch (Exception ex)
        {
            if (log.Remark.IsNullOrEmpty()) log.Remark = ex.Message;
            log.Success = false;
            log.SaveAsync();

            throw;
        }
    }
    #endregion

    #region 单点登录服务端
    /// <summary>1，验证用户身份</summary>
    [AllowAnonymous]
    [HttpGet]
    public virtual ActionResult Authorize(String client_id, String redirect_uri, String response_type = null, String scope = null, String state = null, String loginUrl = null)
    {
        if (client_id.IsNullOrEmpty()) return Redirect(loginUrl ?? _clientService.LoginUrl);

        if (redirect_uri.IsNullOrEmpty()) redirect_uri = GetRequest("redirect_url");
        if (redirect_uri.IsNullOrEmpty()) throw new ArgumentNullException(nameof(redirect_uri));
        if (response_type.IsNullOrEmpty()) response_type = "code";

        var key = _serverService.Authorize(client_id, redirect_uri, response_type, scope, state, UserHost);

        var url = "";

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
    [HttpGet]
    public virtual ActionResult Auth2(String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var user = _clientService.Current;
        if (user == null) return Redirect(_clientService.GetLoginUrl(id));

        var url = _serverService.GetResult(id, user);

        return Redirect(url);
    }

    /// <summary>3，根据code获取令牌</summary>
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
    [AllowAnonymous]
    [HttpGet]
    [HttpPost]
    public new virtual ActionResult Token(String client_id, String client_secret, String username, String password, String refresh_token, String grant_type = null)
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

    /// <summary>3，根据password/client_credentials获取令牌（JSON Body）</summary>
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
    [AllowAnonymous]
    [HttpGet]
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
    [AllowAnonymous]
    [HttpGet]
    [HttpPost]
    public virtual ActionResult Refresh_Token(String client_id, String grant_type, String refresh_token)
    {
        if (client_id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(client_id));
        if (grant_type.IsNullOrEmpty()) grant_type = "refresh_token";

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
    [AllowAnonymous]
    [HttpGet]
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

    /// <summary>获取应用公钥，用于验证令牌</summary>
    [AllowAnonymous]
    [HttpGet]
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

    /// <summary>验证令牌，回写cookie</summary>
    [AllowAnonymous]
    [HttpGet]
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
    [HttpGet]
    public virtual ActionResult Avatar(Int32 id)
    {
        if (id <= 0) throw new ArgumentNullException(nameof(id));

        var user = ManageProvider.Provider?.FindByID(id) as IUser;
        if (user == null) throw new Exception("用户不存在 " + id);

        var set = CubeSetting.Current;
        var av = "";
        if (!user.Avatar.IsNullOrEmpty() && !user.Avatar.StartsWith("/"))
        {
            av = set.AvatarPath.CombinePath(user.Avatar).GetBasePath();
            if (!System.IO.File.Exists(av)) av = null;
        }

        if (av.IsNullOrEmpty() && !set.AvatarPath.IsNullOrEmpty())
        {
            var (found, _) = SvgAvatarService.FindAvatarFile(set.AvatarPath, user.ID);
            av = found;
        }

        if (av.IsNullOrEmpty() || !System.IO.File.Exists(av))
        {
            var svg = SvgAvatarService.Generate(user, set.AvatarChars);
            return Content(svg, "image/svg+xml");
        }

        var vs = System.IO.File.ReadAllBytes(av);
        var ct = SvgAvatarService.GetContentType(Path.GetExtension(av));
        return File(vs, ct);
    }

    private ActionResult SsoJsonOK(Object data) => Json(0, null, data);

    private ActionResult SsoJsonError(Exception ex)
    {
        var code = 500;
        if (ex is ApiException aex && code > 0) code = aex.Code;
        return Json(code, ex.GetTrue().Message);
    }
    #endregion
}