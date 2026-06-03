using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Web;
using XCode;
using XCode.Membership;
using IHttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>SSO 客户端服务实现</summary>
/// <remarks>
/// 负责 OAuth 客户端侧的全流程：获取 OAuth 客户端实例、返回地址安全校验、回调地址构建、登录成功后保存用户会话。
/// SSO 安全域名白名单（SsoSafeDomains）控制跨域重定向目标地址，安全域名支持通配符 *.domain.com。
/// 外部应用可配置 OAuthConfig.AppUrl 覆盖自动推断的回调地址（常用于前后端分离场景）。
/// </remarks>
public class SsoClientService : ISsoClientService
{
    #region 属性
    /// <summary>用户管理提供者</summary>
    public IManageProvider Provider { get; set; }

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }

    /// <summary>登录成功后跳转地址。~/Admin</summary>
    public String SuccessUrl { get; set; }

    /// <summary>本地登录检查地址。~/Admin/User/Login</summary>
    public String LoginUrl { get; set; }

    /// <summary>安全密钥。keyName$keyValue</summary>
    public String SecurityKey { get; set; }

    /// <summary>用户绑定服务</summary>
    public IUserBindingService BindingService { get; set; }

    /// <summary>已登录用户</summary>
    public IManageUser Current => Provider.Current;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public SsoClientService()
    {
        Provider = ManageProvider.Provider;
        SuccessUrl = "~/Admin";
        LoginUrl = "~/Admin/User/Login";
    }
    #endregion

    #region 方法
    /// <summary>获取OAuth客户端</summary>
    /// <param name="tenantId"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual OAuthClient GetClient(Int32 tenantId, String name) => OAuthClient.Create(tenantId, name);

    /// <summary>获取返回地址。跨域URL需在SsoSafeDomains白名单内</summary>
    /// <param name="request"></param>
    /// <param name="referr"></param>
    /// <returns></returns>
    public virtual String GetReturnUrl(IHttpRequest request, Boolean referr)
    {
        var url = request.Get("r");
        if (url.IsNullOrEmpty()) url = request.Get("redirect_uri");
        if (url.IsNullOrEmpty() && referr)
        {
            url = request.Headers["Referer"].FirstOrDefault() + "";
        }
        if (!url.IsNullOrEmpty() && url.StartsWithIgnoreCase("http"))
        {
            var baseUri = request.GetRawUrl();

            var uri = new Uri(url);
            if (uri != null && uri.Authority.EqualIgnoreCase(baseUri?.Authority))
            {
                url = uri.PathAndQuery;
            }
            else
            {
                var safeDomains = CubeSetting.Current.SsoSafeDomains;
                if (!IsSafeDomain(url, safeDomains))
                {
                    XTrace.WriteLine("[安全] SSO 重定向目标不在白名单，已拦截: {0} (来自 {1})", url, request.GetRawUrl());
                    url = null;
                }
            }
        }

        if (!url.IsNullOrEmpty() && url.StartsWithIgnoreCase("/Sso/Login/")) url = null;

        return url;
    }

    /// <summary>判断URL目标域名是否在SSO安全域名白名单内</summary>
    /// <param name="url">目标URL</param>
    /// <param name="safeDomains">白名单配置</param>
    /// <returns></returns>
    public virtual Boolean IsSafeDomain(String url, String safeDomains)
    {
        if (url.IsNullOrEmpty()) return true;
        if (!url.StartsWithIgnoreCase("http://", "https://")) return true;

        if (safeDomains.IsNullOrEmpty()) return false;

        Uri uri;
        try { uri = new Uri(url); }
        catch { return false; }

        var host = uri.Host.ToLower();
        var domains = safeDomains.Split(',', ';');
        foreach (var domain in domains)
        {
            var d = domain.Trim().ToLower();
            if (d.IsNullOrEmpty()) continue;

            if (d.StartsWith("*."))
            {
                var suffix = d[1..];
                if (host == suffix[1..] || host.EndsWith(suffix)) return true;
            }
            else
            {
                if (host == d) return true;
            }
        }

        return false;
    }

    /// <summary>获取登录回跳地址</summary>
    /// <param name="logId"></param>
    /// <returns></returns>
    public virtual String GetLoginUrl(String logId)
    {
        var url = LoginUrl;

        var log = AppLog.FindById(logId.ToLong());
        if (log != null)
        {
            url += url.Contains("?") ? "&" : "?";
            url += $"ssoAppId={log.AppId}";
        }

        return url.AppendReturn("/Sso/Auth2?id=" + logId);
    }

    /// <summary>获取回调地址</summary>
    /// <param name="request"></param>
    /// <param name="redirectUrl"></param>
    /// <param name="providerName"></param>
    /// <returns></returns>
    public virtual String GetRedirect(IHttpRequest request, String redirectUrl, String providerName = null)
    {
        var config = providerName != null ? OAuthConfig.FindByName(providerName) : null;

        if (config != null && !config.AppUrl.IsNullOrEmpty())
        {
            if (Uri.TryCreate(config.AppUrl, UriKind.Absolute, out var appUri))
            {
                return appUri.ToString();
            }
            return config.AppUrl.AsUri(request.GetRawUrl()?.ToUri()).ToString().Replace("{name}", providerName ?? "");
        }

        return redirectUrl.AsUri(request.GetRawUrl()?.ToUri()).ToString();
    }

    /// <summary>登录成功</summary>
    /// <param name="client">OAuth客户端</param>
    /// <param name="context">服务提供者</param>
    /// <param name="uc">用户链接</param>
    /// <param name="forceBind">强行绑定</param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public virtual String OnLogin(OAuthClient client, IServiceProvider context, UserConnect uc, Boolean forceBind, Int32 userId)
    {
        using var span = Tracer?.NewSpan("SsoProviderLogin", $"connectid={uc.ID} openid={uc.OpenID} username={uc.UserName}");

        var httpContext = ModelExtension.GetService<IHttpContextAccessor>(context).HttpContext;
        var req = httpContext.Request;
        var ip = httpContext.GetUserHost();

        var prv = Provider;
        prv ??= Provider = ManageProvider.Provider;

        var user = prv.FindByID(uc.UserID);
        if (forceBind || user == null || !uc.Enable) user = BindingService.OnBind(uc, client, userId);

        try
        {
            uc.UpdateTime = DateTime.Now;
            uc.Save();
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);
        }

        if (user == null) return null;

        BindingService.Fill(client, user, context);

        if (user is User userAv && userAv.Avatar.IsNullOrEmpty())
            userAv.Avatar = $"/Sso/Avatar?id={user.ID}";

        if (user is IAuthUser user3)
        {
            user3.Logins++;
            user3.LastLogin = DateTime.Now;
            user3.LastLoginIP = ip;
        }
        if (user is IUser user4) user4.Online = true;
        if (user is IEntity entity) entity.Update();

        (user as IEntity).Extends.Clear();

        var log = LogProvider.Provider;
        log?.WriteLog(typeof(User), "SSO登录", true, $"[{user}]从[{client.Name}]的[{client.UserName ?? client.NickName}]登录", user.ID, user + "");

        if (!user.Enable) throw new InvalidOperationException($"用户[{user}]已禁用！");

        if (prv is ManageProvider2 prv2) user = prv2.CheckAgent(user);
        prv.Current = user;

        var set = CubeSetting.Current;
        if (set.SessionTimeout > 0)
        {
            var expire = TimeSpan.FromSeconds(set.SessionTimeout);
            prv.SaveCookie(user, expire, httpContext);
        }

        return SuccessUrl;
    }

    /// <summary>获取密钥</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual String GetKey(String name) => BindingService.GetKey(name);
    #endregion
}
