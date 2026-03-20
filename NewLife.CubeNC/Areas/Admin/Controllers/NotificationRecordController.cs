using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>通知记录</summary>
[DataPermission(null, "UserId={#userId}")]
[DisplayName("通知记录")]
[AdminArea]
[Menu(0, false)]
public class NotificationRecordController : ReadOnlyEntityController<NotificationRecord>
{
    static NotificationRecordController()
    {
        ListFields.RemoveField("Id");
        ListFields.RemoveUpdateField();

        ListFields.TraceUrl("TraceId");
        //{
        //    var df = ListFields.GetField("TraceId") as ListField;
        //    df.DisplayName = "跟踪";
        //    df.Url = StarHelper.BuildUrl("{TraceId}");
        //    df.DataVisible = (e, f) => !(e as NotificationRecord).TraceId.IsNullOrEmpty();
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
    protected override IEnumerable<NotificationRecord> Search(Pager p)
    {
        var tenantId = p["tenantId"].ToInt(-1);
        var channel = p["channel"];
        var userId = p["userId"].ToInt(-1);
        var read = p["read"]?.ToBoolean();
        var success = p["success"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        var key = p["Q"];

        if (p.Sort.IsNullOrEmpty()) p.Sort = NotificationRecord._.Id.Desc();

        return NotificationRecord.Search(tenantId, channel, userId, null, read, success, start, end, key, p);
    }
}