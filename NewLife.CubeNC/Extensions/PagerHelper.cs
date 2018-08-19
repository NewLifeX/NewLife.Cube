using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Web;

namespace NewLife.CubeNC.Extensions
{
    public static class PagerHelper
    {
        /// <summary>获取表单提交的Url</summary>
        /// <param name="action">动作</param>
        /// <returns></returns>
        public static String GetFormAction(this Pager pager, String action = null)
        {
            var req = HttpContext.Current?.Request;
            if (req == null) return action;

            // 表单提交，不需要排序、分页，不需要表单提交上来的数据，只要请求字符串过来的数据
            var query = req.Query;
            var forms = new HashSet<String>();
            if (req.HasFormContentType)
            {
                forms = new HashSet<String>(req.Form.Select(s => s.Key), StringComparer.OrdinalIgnoreCase);
            }
            var excludes = new HashSet<String>(new[] { pager._.Sort, pager._.Desc, pager._.PageIndex, pager._.PageSize }, StringComparer.OrdinalIgnoreCase);

            var url = Pool.StringBuilder.Get();
            foreach (var item in query.Select(s=>s.Key))
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

            if (url.Length == 0) return action;
            if (action != null && !action.Contains('?')) action += '?';

            return action + url.Put(true);
        }
    }
}
