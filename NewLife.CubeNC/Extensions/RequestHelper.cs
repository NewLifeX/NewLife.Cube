using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace NewLife.Cube.Extensions
{
    public static class RequestHelper
    {
        /// <summary>
        /// 从请求中获取值
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String GetRequestValue(this HttpRequest request, String key)
        {
            var value = new StringValues();

            if (request.HasFormContentType) value = request.Form[key];

            if (value.Count > 0) return value;

            value = request.Query[key];
            return value.Count > 0 ? value.ToString() : null;
        }

        /// <summary>
        /// 确定指定的HTTP请求是否是Ajax请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Boolean IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Headers != null)
                return request.Headers["X-Requested-With"] == "XMLHttpRequest";
            return false;
        }
    }
}
