using System.Web;

namespace NewLife.Cube.Web;

/// <summary>开放验证助手</summary>
public static class OAuthHelper
{
    /// <summary>获取登录地址</summary>
    /// <param name="name"></param>
    /// <param name="returnUrl"></param>
    /// <returns></returns>
    public static String GetLoginUrl(String name, String returnUrl)
    {
        var url = "Sso/Login?name=" + name;
        if (!returnUrl.IsNullOrEmpty()) url += "&r=" + HttpUtility.UrlEncode(returnUrl);

        var req = NewLife.Web.HttpContext.Current.Request;

        // 拼接 Scheme://Host + PathBase + /Sso/Login。SSO入口与服务控制器均在 /Sso 路由，不使用API前缀
        url = req.Scheme + "://" + req.Host + req.PathBase + "/" + url;

        return url;
    }

    /// <summary>合并Url</summary>
    /// <param name="baseUrl"></param>
    /// <param name="returnUrl"></param>
    /// <returns></returns>
    public static String GetUrl(String baseUrl, String returnUrl = null)
    {
        var url = baseUrl;

        //if (returnUrl.IsNullOrEmpty()) returnUrl = Request["r"];

        if (!returnUrl.IsNullOrEmpty())
        {
            if (url.Contains("?"))
                url += "&";
            else
                url += "?";

            url += "r=" + HttpUtility.UrlEncode(returnUrl);
        }

        return url;
    }

    /// <summary>判断异常是否为授权码过期/无效（OAuth 授权码交换被拒绝）</summary>
    /// <remarks>
    /// 兼容魔方服务端（“Code已过期！”/“Code无效！”）与外部 OAuth 提供商（invalid code / code expired 等）的错误消息。
    /// 用于 SSO 回跳方在授权码失效时自动重新授权，而不是展示错误页。
    /// </remarks>
    /// <param name="ex">令牌交换抛出的异常</param>
    /// <returns>是否因授权码过期/无效导致</returns>
    public static Boolean IsCodeExpired(Exception ex)
    {
        var msg = ex?.Message;
        if (msg.IsNullOrEmpty()) return false;

        // 明确的过期/失效语义
        if (msg.Contains("过期") || msg.Contains("失效") || msg.Contains("expired", StringComparison.OrdinalIgnoreCase))
            return true;

        // “无效”语义需结合授权码上下文（code/grant/授权码），避免把 client_secret 等配置错误也判定为授权码问题
        return (msg.Contains("无效") || msg.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            && (msg.Contains("code", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("授权码")
                || msg.Contains("grant", StringComparison.OrdinalIgnoreCase));
    }
}