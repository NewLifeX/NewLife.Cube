using System;

namespace NewLife.Cube.Extensions
{
    /// <summary>星尘助手</summary>
    public static class StarHelper
    {
        /// <summary>
        /// 生成星尘调用链Url
        /// </summary>
        /// <param name="traceId"></param>
        /// <returns></returns>
        public static String BuildUrl(String traceId)
        {
            if (traceId.IsNullOrEmpty()) return null;

            var web = Setting.Current.StarWeb;
            if (web.IsNullOrEmpty()) return null;

            return $"{web}/trace?id={traceId}";
        }
    }
}