using System;
#if __CORE__
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NewLife.Collections;
using NewLife.Cube.Extensions;
using NewLife.Serialization;
#else
using System.Web;
#endif

namespace NewLife.Cube
{
    /// <summary>Web助手</summary>
    public static class WebHelper2
    {
        #region 兼容处理
#if __CORE__
        /// <summary>获取请求值</summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String Get(this HttpRequest request, String key) => request.GetRequestValue(key);

        /// <summary>获取Session值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(this ISession session, String key) where T : class
        {
            if (!session.TryGetValue(key, out var buf)) return default(T);

            return buf.ToStr().ToJsonEntity<T>();
        }

        /// <summary>获取Session值</summary>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static Object Get(this ISession session, String key, Type targetType)
        {
            if (!session.TryGetValue(key, out var buf)) return null;

            return buf.ToStr().ToJsonEntity(targetType);
        }

        /// <summary>设置Session值</summary>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set(this ISession session, String key, Object value) => session.Set(key, value?.ToJson().GetBytes());

        /// <summary>获取原始请求Url，支持反向代理</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Uri GetRawUrl(this HttpRequest request)
        {
            var uri = new Uri(request.Headers["Url"] + "");

            var str = request.Headers["RawUrl"] + "";
            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            str = request.Headers["HTTP_X_REQUEST_URI"];
            if (str.IsNullOrEmpty()) str = request.Headers["X-Request-Uri"];
            if (!str.IsNullOrEmpty()) uri = new Uri(uri, str);

            return uri;
        }

        /// <summary>获取用户主机</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static String GetUserHost(this HttpRequest request) => request.Headers["UserHostAddress"] + "";

        /// <summary>返回请求字符串和表单的名值字段，过滤空值和ViewState，同名时优先表单</summary>
        public static IDictionary<String, String> Params
        {
            get
            {
                var ctx = NewLife.Web.HttpContext.Current;
                if (ctx.Items["Params"] is IDictionary<String, String> dic) return dic;

                var req = ctx.Request;
                var nvss = new[]
                {
                    req.Query,
                    req.HasFormContentType ? (IEnumerable<KeyValuePair<String, StringValues>>) req.Form : new List<KeyValuePair<String, StringValues>>()
                };

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
#else
        /// <summary>获取请求值</summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String Get(this HttpRequest request, String key) => request[key];

        /// <summary>获取请求值</summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String Get(this HttpRequestBase request, String key) => request[key];

        /// <summary>获取Session值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(this HttpSessionStateBase session, String key) => (T)session[key];

        /// <summary>设置Session值</summary>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set(this HttpSessionStateBase session, String key, Object value) => session[key] = value;

        /// <summary>获取用户主机</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static String GetUserHost(this HttpRequest request) => request.UserHostAddress;

        /// <summary>获取用户主机</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static String GetUserHost(this HttpRequestBase request) => request?.UserHostAddress;

        /// <summary>确定指定的 HTTP 请求是否为 AJAX 请求</summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Boolean IsAjaxRequest(this HttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (request["X-Requested-With"] == "XMLHttpRequest") return true;
            if (request.Headers?["X-Requested-With"] == "XMLHttpRequest") return true;

            return false;
        }
#endif
        #endregion
    }
}