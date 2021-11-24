using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>应用日志</summary>
    [DisplayName("应用日志")]
    [Area("Admin")]
    [Menu(0, false)]
    public class AppLogController : ReadOnlyEntityController<AppLog>
    {
        static AppLogController() => ListFields.RemoveField("ID");

        /// <summary>搜索</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<AppLog> Search(Pager p)
        {
            var appId = p["appId"].ToInt(-1);
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            if (p.Sort.IsNullOrEmpty()) p.Sort = AppLog._.Id.Desc();

            return AppLog.Search(appId, start, end, key, p);
        }
    }
}