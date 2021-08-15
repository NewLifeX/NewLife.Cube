using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Web;

namespace NewLife.Cube
{
    /// <summary>页面助手</summary>
    public static class PagerHelper
    {
        #region 名称
        /// <summary>名称类。用户可根据需要修改Url参数名</summary>
        public class __
        {
            /// <summary>排序字段</summary>
            public String Sort = "Sort";

            /// <summary>是否降序</summary>
            public String Desc = "Desc";

            /// <summary>页面索引</summary>
            public String PageIndex = "PageIndex";

            /// <summary>页面大小</summary>
            public String PageSize = "PageSize";
        }

        /// <summary>名称类。用户可根据需要修改Url参数名</summary>
        [XmlIgnore, ScriptIgnore]
        public static __ _ = new __();
        #endregion

        /// <summary>获取表单提交的Url</summary>
        /// <param name="pager">页面</param>
        /// <param name="action">动作</param>
        /// <returns></returns>
        public static String GetFormAction(this Pager pager, String action = null)
        {
            var req = NewLife.Web.HttpContext.Current?.Request;
            if (req == null) return action;

            // 表单提交，不需要排序、分页，不需要表单提交上来的数据，只要请求字符串过来的数据
#if __CORE__
            var query = req.Query;
            var forms = new HashSet<String>();
            if (req.HasFormContentType)
            {
                forms = new HashSet<String>(req.Form.Select(s => s.Key), StringComparer.OrdinalIgnoreCase);
            }
            // 只排除分页序号，不排除页大小和排序
            var excludes = new HashSet<String>(new[] { _.PageIndex }, StringComparer.OrdinalIgnoreCase);

            var url = Pool.StringBuilder.Get();
            foreach (var item in query.Select(s => s.Key))
            {
                // 只要查询字符串，不要表单
                if (forms.Contains(item)) continue;

                // 排除掉排序和分页
                if (excludes.Contains(item)) continue;

                // 内容为空也不要
                var v = query[item];
                if (v.Count < 1) continue;

                url.UrlParam(item, v);
            }
#else
            var query = req.QueryString;

            var forms = new HashSet<String>(req.Form.AllKeys, StringComparer.OrdinalIgnoreCase);
            // 只排除分页序号，不排除页大小和排序
            var excludes = new HashSet<String>(new[] { _.PageIndex }, StringComparer.OrdinalIgnoreCase);

            var url = Pool.StringBuilder.Get();
            foreach (var item in query.AllKeys)
            {
                // 只要查询字符串，不要表单
                if (forms.Contains(item)) continue;

                // 排除掉排序和分页
                if (excludes.Contains(item)) continue;

                // 内容为空也不要
                var v = HttpUtility.UrlEncode(query[item]);
                if (v.IsNullOrEmpty()) continue;

                var key = HttpUtility.UrlEncode(item);
                url.UrlParam(key, v);
            }
#endif


            if (url.Length == 0) return action;
            if (action != null && !action.Contains('?')) action += '?';

            return action + url.Put(true);
        }

        /// <summary>过滤特殊字符，避免注入</summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static Dictionary<String, String> FilterSpecialChar(IDictionary<String, String> dic)
        {
            // 过滤部分特殊字符避免XSS
            var ndic = new Dictionary<String, String>();

            foreach (var kv in dic)
            {
                var value = HttpUtility.UrlEncode(kv.Value);
                var key = HttpUtility.UrlEncode(kv.Key);

                ndic.Add(key, value);
            }

            return ndic;
        }
    }
}