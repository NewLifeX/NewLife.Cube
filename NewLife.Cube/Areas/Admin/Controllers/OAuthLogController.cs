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
    /// <summary>OAuth日志</summary>
    [DisplayName("OAuth日志")]
    [Area("Admin")]
    [Menu(0, false)]
    public class OAuthLogController : ReadOnlyEntityController<OAuthLog>
    {
        /// <summary>搜索</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<OAuthLog> Search(Pager p)
        {
            var provider = p["provider"];
            var connectId = p["connectId"].ToInt(-1);
            var userId = p["userId"].ToInt(-1);
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            if (p.Sort.IsNullOrEmpty()) p.Sort = OAuthLog._.Id.Desc();

            return OAuthLog.Search(provider, connectId, userId, start, end, key, p);
        }
    }
}