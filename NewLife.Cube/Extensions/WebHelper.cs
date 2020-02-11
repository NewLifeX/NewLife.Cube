using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using NewLife.Collections;
using NewLife.Log;
using XCode.Membership;
#if __CORE__
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
#endif

namespace NewLife.Web
{
    /// <summary>网页工具类</summary>
    public static class WebHelper
    {
        #region 用户主机
        /// <summary>用户主机。支持非Web</summary>
        [Obsolete("=>ManageProvider.UserHost")]
        public static String UserHost { get => ManageProvider.UserHost; set => ManageProvider.UserHost = value; }
        #endregion

        #region Http请求
#if !__CORE__
        /// <summary>返回请求字符串和表单的名值字段，过滤空值和ViewState，同名时优先表单</summary>
        public static IDictionary<String, String> Params
        {
            get
            {
                var ctx = HttpContext.Current;
                if (ctx.Items["Params"] is IDictionary<String, String> dic) return dic;

                var req = ctx.Request;
                var nvss = new NameValueCollection[] { req.QueryString, req.Form };

                // 这里必须用可空字典，否则直接通过索引查不到数据时会抛出异常
                dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var nvs in nvss)
                {
                    foreach (var item in nvs.AllKeys)
                    {
                        if (item.IsNullOrWhiteSpace()) continue;
                        if (item.StartsWithIgnoreCase("__VIEWSTATE")) continue;

                        // 空值不需要
                        var value = nvs[item];
                        if (value.IsNullOrWhiteSpace())
                        {
                            // 如果请求字符串里面有值而后面表单为空，则抹去
                            if (dic.ContainsKey(item)) dic.Remove(item);
                            continue;
                        }

                        // 同名时优先表单
                        dic[item] = value.Trim();
                    }
                }
                ctx.Items["Params"] = dic;

                return dic;
            }
        }

        /// <summary>获取原始请求Url，支持反向代理</summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static Uri GetRawUrl(this HttpRequest req)
        {
            var uri = req.Url;

            var str = req.RawUrl;
            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            return GetRawUrl(uri, k => req.ServerVariables[k]);
        }

        /// <summary>获取原始请求Url，支持反向代理</summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static Uri GetRawUrl(this HttpRequestBase req)
        {
            Uri uri = null;

            // 配置
            var set = OAuthConfig.Current;
            if (!set.AppUrl.IsNullOrEmpty()) uri = new Uri(set.AppUrl);

            // 取请求头
            if (uri == null && !req.RawUrl.IsNullOrEmpty()) uri = new Uri(uri, req.RawUrl);

            if (uri == null) uri = req.Url;

            return GetRawUrl(uri, k => req.ServerVariables[k]);
        }
#else
        /// <summary>返回请求字符串和表单的名值字段，过滤空值和ViewState，同名时优先表单</summary>
        public static IDictionary<String, String> Params
        {
            get
            {
                var ctx = HttpContext.Current;
                if (ctx.Items["Params"] is IDictionary<String, String> dic) return dic;

                var req = ctx.Request;
                IEnumerable<KeyValuePair<String, StringValues>>[] nvss;
                nvss = req.HasFormContentType ?
                    new IEnumerable<KeyValuePair<String, StringValues>>[] { req.Query, req.Form } :
                    new IEnumerable<KeyValuePair<String, StringValues>>[] { req.Query };


                // 这里必须用可空字典，否则直接通过索引查不到数据时会抛出异常
                dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var nvs in nvss)
                {
                    foreach (var kv in nvs)
                    {
                        var item = kv.Key;
                        if (item.IsNullOrWhiteSpace()) continue;
                        if (item.StartsWithIgnoreCase("__VIEWSTATE")) continue;

                        // 空值不需要
                        var value = kv.Value;
                        if (value.Count == 0)
                        {
                            // 如果请求字符串里面有值而后面表单为空，则抹去
                            if (dic.ContainsKey(item)) dic.Remove(item);
                            continue;
                        }

                        // 同名时优先表单
                        dic[item] = value.ToString().Trim();
                    }
                }
                ctx.Items["Params"] = dic;

                return dic;
            }
        }

        /// <summary>获取原始请求Url，支持反向代理</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Uri GetRawUrl(this HttpRequest request)
        {
            Uri uri = null;

            // 配置
            var set = OAuthConfig.Current;
            if (!set.AppUrl.IsNullOrEmpty()) uri = new Uri(set.AppUrl);

            // 取请求头
            if (uri == null)
            {
                var url = request.GetEncodedUrl();
                uri = new Uri(url);
            }

            return GetRawUrl(uri, k => request.Headers[k]);
        }
#endif

        private static Uri GetRawUrl(Uri uri, Func<String, String> headers)
        {
            var str = headers("HTTP_X_REQUEST_URI");
            if (str.IsNullOrEmpty()) str = headers("X-Request-Uri");

            if (str.IsNullOrEmpty())
            {
                // 阿里云CDN默认支持 X-Client-Scheme: https
                var scheme = headers("HTTP_X_CLIENT_SCHEME");
                if (scheme.IsNullOrEmpty()) scheme = headers("X-Client-Scheme");

                // nginx
                if (scheme.IsNullOrEmpty()) scheme = headers("HTTP_X_FORWARDED_PROTO");
                if (scheme.IsNullOrEmpty()) scheme = headers("X-Forwarded-Proto");

                if (!scheme.IsNullOrEmpty()) str = scheme + "://" + uri.ToString().Substring("://");
            }

            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            return uri;
        }
        #endregion

        #region Url扩展
        /// <summary>追加Url参数，不为空时加与符号</summary>
        /// <param name="sb"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static StringBuilder UrlParam(this StringBuilder sb, String str)
        {
            if (str.IsNullOrWhiteSpace()) return sb;

            if (sb.Length > 0)
                sb.Append("&");
            //else
            //    sb.Append("?");

            sb.Append(str);

            return sb;
        }

        /// <summary>追加Url参数，不为空时加与符号</summary>
        /// <param name="sb">字符串构建</param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static StringBuilder UrlParam(this StringBuilder sb, String name, Object value)
        {
            if (name.IsNullOrWhiteSpace()) return sb;

            // 必须注意，value可能是时间类型
            return UrlParam(sb, "{0}={1}".F(HttpUtility.UrlEncode(name), HttpUtility.UrlEncode("{0}".F(value))));
        }

        /// <summary>把一个参数字典追加Url参数，指定包含的参数</summary>
        /// <param name="sb">字符串构建</param>
        /// <param name="pms">参数字典</param>
        /// <param name="includes">包含的参数</param>
        /// <returns></returns>
        public static StringBuilder UrlParams(this StringBuilder sb, IDictionary<String, String> pms, params String[] includes)
        {
            foreach (var item in pms)
            {
                if (!item.Value.IsNullOrEmpty() && item.Key.EqualIgnoreCase(includes))
                    sb.UrlParam(item.Key, item.Value);
            }
            return sb;
        }

        /// <summary>把一个参数字典追加Url参数，排除一些参数</summary>
        /// <param name="sb">字符串构建</param>
        /// <param name="pms">参数字典</param>
        /// <param name="excludes">要排除的参数</param>
        /// <returns></returns>
        public static StringBuilder UrlParamsExcept(this StringBuilder sb, IDictionary<String, String> pms, params String[] excludes)
        {
            foreach (var item in pms)
            {
                if (!item.Value.IsNullOrEmpty() && !item.Key.EqualIgnoreCase(excludes))
                    sb.UrlParam(item.Key, item.Value);
            }
            return sb;
        }

        /// <summary>相对路径转Uri</summary>
        /// <param name="url">相对路径</param>
        /// <param name="baseUri">基础</param>
        /// <returns></returns>
        public static Uri AsUri(this String url, Uri baseUri = null)
        {
            if (url.IsNullOrEmpty()) return null;

#if !__CORE__
            // 虚拟路径
            if (url.StartsWith("~/")) url = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + url.Substring(2);
#else
            if (url.StartsWith("~/")) url = "/" + url.Substring(2);
#endif

            // 绝对路径
            if (!url.StartsWith("/")) return new Uri(url);

            // 相对路径
            if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
            return new Uri(baseUri, url);
        }

        /// <summary>打包返回地址</summary>
        /// <param name="uri"></param>
        /// <param name="returnUrl"></param>
        /// <param name="returnKey"></param>
        /// <returns></returns>
        public static Uri AppendReturn(this Uri uri, String returnUrl, String returnKey = null)
        {
            if (uri == null || returnUrl.IsNullOrEmpty()) return uri;

            if (returnKey.IsNullOrEmpty()) returnKey = "r";

            // 如果协议和主机相同，则削减为只要路径查询部分
            if (returnUrl.StartsWithIgnoreCase("http"))
            {
                var ruri = new Uri(returnUrl);
                if (ruri.Scheme.EqualIgnoreCase(uri.Scheme) && ruri.Host.EqualIgnoreCase(uri.Host)) returnUrl = ruri.PathAndQuery;
            }
#if !__CORE__
            else if (returnUrl.StartsWith("~/"))
                returnUrl = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + returnUrl.Substring(2);
#endif

            var url = uri + "";
            if (url.Contains("?"))
                url += "&";
            else
                url += "?";
            url += returnKey + "=" + HttpUtility.UrlEncode(returnUrl);

            return new Uri(url);
        }

        /// <summary>打包返回地址</summary>
        /// <param name="url"></param>
        /// <param name="returnUrl"></param>
        /// <param name="returnKey"></param>
        /// <returns></returns>
        public static String AppendReturn(this String url, String returnUrl, String returnKey = null)
        {
            if (url.IsNullOrEmpty() || returnUrl.IsNullOrEmpty()) return url;

            if (returnKey.IsNullOrEmpty()) returnKey = "r";

            // 如果协议和主机相同，则削减为只要路径查询部分
            if (url.StartsWithIgnoreCase("http") && returnUrl.StartsWithIgnoreCase("http"))
            {
                var uri = new Uri(url);
                var ruri = new Uri(returnUrl);
                if (ruri.Scheme.EqualIgnoreCase(uri.Scheme) && ruri.Host.EqualIgnoreCase(uri.Host)) returnUrl = ruri.PathAndQuery;
            }
#if !__CORE__
            else if (returnUrl.StartsWith("~/"))
                returnUrl = HttpRuntime.AppDomainAppVirtualPath.EnsureEnd("/") + returnUrl.Substring(2);
#endif

            if (url.Contains("?"))
                url += "&";
            else
                url += "?";
            //url += returnKey + "=" + returnUrl;
            url += returnKey + "=" + HttpUtility.UrlEncode(returnUrl);

            return url;
        }
        #endregion
    }
}