using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>OAuth日志</summary>
[DataPermission(null, "UserId={#userId}")]
[DisplayName("OAuth日志")]
[AdminArea]
[Menu(0, false)]
public class OAuthLogController : ReadOnlyEntityController<OAuthLog>
{
    static OAuthLogController()
    {
        ListFields.RemoveUpdateField();

        ListFields.TraceUrl("TraceId");
        //{
        //    var df = ListFields.GetField("TraceId") as ListField;
        //    df.DisplayName = "跟踪";
        //    df.Url = StarHelper.BuildUrl("{TraceId}");
        //    df.DataVisible = (e, f) => !(e as OAuthLog).TraceId.IsNullOrEmpty();
        //}
    }

    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        // 指定了用户
        var userid = GetRequest("userId").ToInt(-1);
        if (userid > 0)
        {
            PageSetting.NavView = "_User_Nav";
            PageSetting.EnableNavbar = false;
        }
    }

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