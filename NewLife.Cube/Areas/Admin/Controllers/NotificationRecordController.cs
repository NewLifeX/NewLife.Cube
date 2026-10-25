using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>通知记录</summary>
/// <remarks>
/// 与 MVC 版保持一致：只读列表 + 行内「标记已读」。站内信（InApp）未读记录才可操作，
/// 广播消息（UserId=0）仅系统管理员可标记，个人消息仅本人可标记。
/// </remarks>
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

        // 行内「标记已读」，仅对当前用户可读的未读站内信显示
        {
            var df = ListFields.AddListField("op", null, "Remark");
            df.DisplayName = "Mark as read";
            df.Url = "/api/Admin/NotificationRecord/NotifyMarkRead?id={Id}";
            df.DataAction = "post";
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

    /// <summary>搜索</summary>
    /// <param name="p">分页参数</param>
    /// <returns>通知记录列表</returns>
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

    /// <summary>标记站内信已读</summary>
    /// <param name="id">通知编号</param>
    /// <returns>标记结果</returns>
    [HttpPost("/api/[area]/[controller]/NotifyMarkRead")]
    public ActionResult NotifyMarkRead(Int64 id)
    {
        var (userId, admin, name) = GetCurrentUser();
        var entity = NotificationRecord.MarkRead(id, userId, name, admin);
        if (entity == null) return Json(500, "Record not found or no permission to mark as read");

        return Json(0, "Read", new { entity.Id, entity.Read });
    }

    /// <summary>全部标记已读</summary>
    /// <returns>已读条数</returns>
    [HttpPost("/api/[area]/[controller]/NotifyMarkAllRead")]
    public ActionResult NotifyMarkAllRead()
    {
        var (userId, admin, name) = GetCurrentUser();
        var count = NotificationRecord.MarkAllRead(userId, name, admin);

        return Json(0, L.T("Marked {0} as read", count), new { count });
    }

    /// <summary>获取当前登录用户编号、是否系统管理员与名称</summary>
    /// <returns>三元组</returns>
    private static (Int32 userId, Boolean admin, String name) GetCurrentUser()
    {
        var user = ManageProvider.User;
        var userId = user?.ID ?? 0;
        var admin = user != null && user.Roles.Any(e => e.IsSystem);

        return (userId, admin, user + "");
    }
}
