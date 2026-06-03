using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using IHttpRequest = Microsoft.AspNetCore.Http.HttpRequest;
using IManageUser = NewLife.Model.IManageUser;

namespace NewLife.Cube.Services.Sso;

/// <summary>SSO 客户端服务接口</summary>
public interface ISsoClientService
{
    /// <summary>获取第三方 OAuth 客户端</summary>
    /// <param name="tenantId">当前租户ID，0 表示默认租户</param>
    /// <param name="name">OAuth 提供商名称，如 WeChat、DingDing</param>
    /// <returns>封装好的 OAuthClient 实例</returns>
    OAuthClient GetClient(Int32 tenantId, String name);

    /// <summary>获取返回地址，跨域URL需在白名单内</summary>
    /// <param name="request">HTTP 请求对象</param>
    /// <param name="referr">是否从 Referer 头取返回地址（常用于登录入口）</param>
    /// <returns>安全的返回 URL；不在白名单内或属于 SSO 自身路径时返回 null</returns>
    String GetReturnUrl(IHttpRequest request, Boolean referr);

    /// <summary>判断URL目标域名是否在白名单内</summary>
    /// <param name="url"></param>
    /// <param name="safeDomains"></param>
    /// <returns></returns>
    Boolean IsSafeDomain(String url, String safeDomains);

    /// <summary>获取回调地址</summary>
    /// <param name="request"></param>
    /// <param name="redirectUrl"></param>
    /// <param name="providerName"></param>
    /// <returns></returns>
    String GetRedirect(IHttpRequest request, String redirectUrl, String providerName = null);

    /// <summary>获取登录回跳地址</summary>
    /// <param name="logId"></param>
    /// <returns></returns>
    String GetLoginUrl(String logId);

    /// <summary>登录成功后处理（保存用户状态到Cookie等）</summary>
    /// <param name="client"></param>
    /// <param name="context"></param>
    /// <param name="uc"></param>
    /// <param name="forceBind"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    String OnLogin(OAuthClient client, IServiceProvider context, UserConnect uc, Boolean forceBind, Int32 userId);

    /// <summary>获取密钥</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    String GetKey(String name);

    /// <summary>已登录用户</summary>
    IManageUser Current { get; }

    /// <summary>登录成功后跳转地址</summary>
    String SuccessUrl { get; set; }

    /// <summary>本地登录检查地址</summary>
    String LoginUrl { get; set; }
}
