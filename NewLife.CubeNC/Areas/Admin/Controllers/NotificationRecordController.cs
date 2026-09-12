using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
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

        // 行内“标记已读”，仅对当前用户可读的未读站内信显示
        {
            var df = ListFields.AddListField("op", null, "Remark");
            df.DisplayName = "标记已读";
            df.Url = "/Admin/NotificationRecord/NotifyMarkRead?id={Id}";
            df.DataAction = "action";
            df.DataVisible = e => CanRead(e as NotificationRecord);
        }
    }

    /// <summary>当前用户是否能标记该通知已读。未读站内信：广播(UserId=0)仅系统管理员，个人消息仅本人</summary>
    /// <param name="entity">通知记录</param>
    /// <returns>是否可标记已读</returns>
    private static Boolean CanRead(NotificationRecord entity)
    {
        if (entity == null || !entity.Channel.EqualIgnoreCase("InApp") || entity.Read) return false;

        var user = ManageProvider.User;
        if (user == null) return false;
        if (entity.UserId == 0) return user.Roles.Any(e => e.IsSystem);

        return entity.UserId == user.ID;
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

    #region 站内信未读
    /// <summary>当前用户未读站内信数。广播消息(UserId=0)仅系统管理员计入</summary>
    /// <returns>JSON 未读数</returns>
    public ActionResult NotifyCount()
    {
        var (userId, admin, _) = GetCurrentUser();
        var count = NotificationRecord.CountUnread(userId, admin);

        return Json(0, null, new { count });
    }

    /// <summary>当前用户最近的未读站内信，用于导航栏铃铛下拉</summary>
    /// <param name="size">条数。默认8</param>
    /// <returns>JSON 未读列表</returns>
    public ActionResult NotifyRecent(Int32 size = 8)
    {
        var (userId, admin, _) = GetCurrentUser();
        var list = NotificationRecord.GetRecentUnread(userId, admin, size);
        var rs = list.Select(e => new
        {
            e.Id,
            e.Title,
            e.Content,
            e.CreateTime,
            Broadcast = e.UserId == 0,
        });

        return Json(0, null, rs);
    }

    /// <summary>标记站内信已读</summary>
    /// <param name="id">通知编号</param>
    /// <returns>JSON 结果</returns>
    [HttpPost]
    public ActionResult NotifyMarkRead(Int64 id)
    {
        var (userId, admin, name) = GetCurrentUser();
        var entity = NotificationRecord.MarkRead(id, userId, name, admin);
        if (entity == null) return Json(500, "记录不存在或无权标记已读");

        return Json(0, "已读", new { entity.Id, entity.Read });
    }

    /// <summary>全部标记已读</summary>
    /// <returns>JSON 结果</returns>
    [HttpPost]
    public ActionResult NotifyMarkAllRead()
    {
        var (userId, admin, name) = GetCurrentUser();
        var count = NotificationRecord.MarkAllRead(userId, name, admin);

        return Json(0, $"已读{count}条");
    }

    /// <summary>获取当前登录用户编号、是否系统管理员与名称</summary>
    /// <returns>三元组</returns>
    private (Int32 userId, Boolean admin, String name) GetCurrentUser()
    {
        var user = ManageProvider.User;
        var userId = user?.ID ?? 0;
        var admin = user != null && user.Roles.Any(e => e.IsSystem);

        return (userId, admin, user + "");
    }
    #endregion
}