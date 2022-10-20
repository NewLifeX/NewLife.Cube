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

        url = NewLife.Web.HttpContext.Current.Request.Host + NewLife.Web.HttpContext.Current.Request.PathBase + url;

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
}