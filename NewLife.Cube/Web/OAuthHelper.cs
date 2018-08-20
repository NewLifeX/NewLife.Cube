using System;
using System.Web;
#if __CORE__
using Microsoft.AspNetCore.Http.Extensions;
using NewLife.Web;
#endif

namespace NewLife.Cube.Web
{
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

#if __CORE__
            url = HttpContext.Current.Request.Host + HttpContext.Current.Request.PathBase + url;
#else
            url = HttpRuntime.AppDomainAppVirtualPath + url;
#endif

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

        //public static String GetRedirectUri(this HttpRequestBase req, String redirect)
        //{
        //    var buri = req.GetRawUrl();

        //    var returnUrl = req["returnUrl"];
        //    if (!returnUrl.IsNullOrEmpty() && returnUrl.StartsWithIgnoreCase("http"))
        //    {
        //        var uri = new Uri(returnUrl);
        //        if (uri != null && uri.Host.EqualIgnoreCase(buri.Host)) returnUrl = uri.PathAndQuery;
        //    }

        //    if (!returnUrl.IsNullOrEmpty())
        //    {
        //        if (redirect.Contains("?"))
        //            redirect += "&";
        //        else
        //            redirect += "?";

        //        redirect += "returnUrl=" + HttpUtility.UrlEncode(returnUrl);
        //    }

        //    // 如果是相对路径，自动加上前缀。需要考虑反向代理的可能，不能直接使用Request.Url
        //    if (redirect.StartsWith("~/")) redirect = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + redirect.Substring(2);
        //    if (redirect.StartsWith("/"))
        //    {
        //        //if (burl.IsNullOrEmpty()) throw new ArgumentNullException(nameof(BaseUrl), "使用相对跳转地址时，需要设置BaseUrl");
        //        // 从Http请求头中取出原始主机名和端口
        //        //var request = HttpContext.Current.Request;
        //        //var uri = request.GetRawUrl();

        //        var uri = new Uri(buri, redirect);
        //        redirect = uri.ToString();
        //    }

        //    return redirect;
        //}
    }
}