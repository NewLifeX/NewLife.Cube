using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using NewLife.Collections;

namespace NewLife.Web
{
    /// <summary>网页工具类</summary>
    public static class WebHelper1
    {
        #region Http请求
        /// <summary>返回请求字符串和表单的名值字段，过滤空值和ViewState，同名时优先表单</summary>
        public static IDictionary<String, String> Params
        {
            get
            {
                var ctx = HttpContext.Current;
                if (ctx.Items["Params"] is IDictionary<String, String> dic) return dic;

                var req = ctx.Request;
                var nvss = new IEnumerable<KeyValuePair<string, StringValues>>[] { req.Query, req.Form };

                // 这里必须用可空字典，否则直接通过索引查不到数据时会抛出异常
                dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var nvs in nvss)
                {
                    foreach (var item in nvs)
                    {
                        if (item.Key.IsNullOrWhiteSpace()) continue;
                        if (item.Key.StartsWithIgnoreCase("__VIEWSTATE")) continue;

                        // 空值不需要
                        var value = item.Value.ToString();
                        if (value.IsNullOrWhiteSpace())
                        {
                            // 如果请求字符串里面有值而后面表单为空，则抹去
                            if (dic.ContainsKey(item.Key)) dic.Remove(item.Key);
                            continue;
                        }

                        // 同名时优先表单
                        dic[item.Key] = value.Trim();
                    }
                }
                ctx.Items["Params"] = dic;

                return dic;
            }
        }

        ///// <summary>获取原始请求Url，支持反向代理</summary>
        ///// <param name="req"></param>
        ///// <returns></returns>
        //public static Uri GetRawUrl(this HttpRequest req)
        //{
        //    var uri = req.Url;

        //    var str = req.RawUrl;
        //    if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

        //    str = req.ServerVariables["HTTP_X_REQUEST_URI"];
        //    if (str.IsNullOrEmpty()) str = req.ServerVariables["X-Request-Uri"];
        //    if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

        //    return uri;
        //}

        ///// <summary>获取原始请求Url，支持反向代理</summary>
        ///// <param name="req"></param>
        ///// <returns></returns>
        //public static Uri GetRawUrl(this HttpRequestBase req)
        //{
        //    var uri = req.Url;

        //    var str = req.RawUrl;
        //    if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

        //    str = req.ServerVariables["HTTP_X_REQUEST_URI"];
        //    if (str.IsNullOrEmpty()) str = req.ServerVariables["X-Request-Uri"];
        //    if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

        //    return uri;
        //}
        #endregion
    }
}