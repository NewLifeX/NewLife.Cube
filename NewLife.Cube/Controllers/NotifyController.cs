using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;
using NewLife.Data;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Controllers;

/// <summary>消息通知。站内信、已读、处理、偏好与实时推送</summary>
/// <param name="notifyService">通知服务</param>
/// <param name="ticketService">SSE 票据服务</param>
[DisplayName("消息通知")]
[Route("[controller]/[action]")]
public class NotifyController(NotificationService notifyService, NotificationStreamTicketService ticketService) : ControllerBaseX
{
    /// <summary>未读站内信数量</summary>
    /// <returns>JSON 未读数</returns>
    [HttpGet]
    public ActionResult Count()
    {
        var (userId, admin, _, _) = GetCurrentUser();
        var count = notifyService.CountUnread(userId, admin);

        return Json(0, null, new { count });
    }

    /// <summary>最近未读站内信，用于铃铛下拉</summary>
    /// <param name="size">条数。默认10</param>
    /// <returns>JSON 未读列表</returns>
    [HttpGet]
    public ActionResult Recent(Int32 size = 10)
    {
        var (userId, admin, _, _) = GetCurrentUser();
        var list = notifyService.GetRecentUnread(userId, admin, size);

        return Json(0, null, list.Select(ToItem));
    }

    /// <summary>分页查询站内信</summary>
    /// <param name="page">分页。pageIndex/pageSize</param>
    /// <param name="category">分类</param>
    /// <param name="read">已读状态</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="key">关键字</param>
    /// <returns>JSON 分页列表</returns>
    [HttpGet]
    public ActionResult List(PageParameter page, String category = null, Boolean? read = null, DateTime start = default, DateTime end = default, String key = null)
    {
        var (userId, admin, _, _) = GetCurrentUser();
        var list = notifyService.GetPage(userId, admin, category, read, start, end, key, page);

        return Json(0, null, new
        {
            page.PageIndex,
            page.PageSize,
            page.TotalCount,
            items = list.Select(ToItem),
        });
    }

    /// <summary>站内信详情</summary>
    /// <param name="id">记录编号</param>
    /// <returns>JSON 详情</returns>
    [HttpGet]
    public ActionResult Detail(Int64 id)
    {
        var (userId, admin, _, tenantId) = GetCurrentUser();
        var entity = notifyService.GetDetail(id, userId, admin, tenantId);
        if (entity == null) return Json(404, "Notification not found");

        return Json(0, null, entity.ToModel());
    }

    /// <summary>标记已读。团队共享消息会同步全组</summary>
    /// <param name="request">已读请求</param>
    /// <returns>JSON 已读结果</returns>
    [HttpPost]
    public ActionResult Read([FromBody] NotificationReadRequest request)
    {
        if (request == null) return Json(400, "Request is required");

        var (userId, admin, name, tenantId) = GetCurrentUser();
        var entity = notifyService.MarkRead(request.BizId, request.Id, userId, name, admin, tenantId, request.Channel);
        if (entity == null) return Json(404, "Notification not found or no permission");

        return Json(0, "Read", new { entity.Id, entity.BizId, entity.Read, entity.ReadBy, entity.ReadChannel });
    }

    /// <summary>全部标记已读。仅个人消息，团队共享消息需要显式处理</summary>
    /// <returns>JSON 已读数量</returns>
    [HttpPost]
    public ActionResult ReadAll()
    {
        var (userId, admin, name, _) = GetCurrentUser();
        var count = notifyService.MarkAllRead(userId, name, admin);

        return Json(0, null, new { count });
    }

    /// <summary>处理通知。团队共享消息任一成员处理即闭环</summary>
    /// <param name="request">处理请求</param>
    /// <returns>JSON 处理结果</returns>
    [HttpPost]
    public ActionResult Handle([FromBody] NotificationHandleRequest request)
    {
        if (request == null) return Json(400, "Request is required");

        var (userId, _, name, tenantId) = GetCurrentUser();
        var entity = notifyService.Handle(request.BizId, userId, name, tenantId, request.Result);
        if (entity == null) return Json(404, "Notification not found or no permission");

        return Json(0, null, new { entity.Id, entity.BizId, entity.Handled, entity.HandledBy, entity.HandledTime });
    }

    /// <summary>删除站内信。团队共享消息需要管理员操作</summary>
    /// <param name="request">删除请求</param>
    /// <returns>JSON 删除结果</returns>
    [HttpPost]
    public ActionResult Delete([FromBody] NotificationDeleteRequest request)
    {
        if (request == null) return Json(400, "Request is required");

        var (userId, admin, _, _) = GetCurrentUser();
        var count = notifyService.Delete(request.Id, userId, admin);

        return count > 0 ? Json(0, null, new { count }) : Json(404, "Notification not found or no permission");
    }

    /// <summary>增量拉取站内信，用于 SSE 断线后的轮询兜底</summary>
    /// <param name="lastId">最后一条编号</param>
    /// <param name="size">条数。默认20</param>
    /// <returns>JSON 增量列表</returns>
    [HttpGet]
    public ActionResult Incremental(Int64 lastId = 0, Int32 size = 20)
    {
        var (userId, admin, _, _) = GetCurrentUser();
        var list = notifyService.GetIncremental(userId, admin, lastId, size);

        return Json(0, null, list.Select(ToItem));
    }

    /// <summary>签发 SSE 连接票据。访问令牌不放入 URL</summary>
    /// <returns>JSON 票据与过期时间</returns>
    [HttpPost]
    public ActionResult StreamTicket()
    {
        var (userId, admin, _, _) = GetCurrentUser();
        if (userId <= 0) return Json(403, "Authentication required");

        var ticket = ticketService.Issue(userId, UserHost, admin);

        return Json(0, null, new { ticket, expiresIn = 60 });
    }

    /// <summary>SSE 实时通知流</summary>
    /// <param name="ticket">连接票据</param>
    /// <param name="lastId">最后一条编号</param>
    /// <returns>异步任务</returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task Stream(String ticket, Int64 lastId = 0)
    {
        // EventSource 无法附加 Authorization 头，使用一次性票据鉴权；票据绑定用户与 IP
        var info = ticketService.Consume(ticket, UserHost);
        if (info == null || info.UserId <= 0)
        {
            Response.StatusCode = 403;
            return;
        }

        var userId = info.UserId;
        var admin = info.Admin;

        var last = Request.Headers["Last-Event-ID"].ToString().ToLong();
        if (last > 0) lastId = last;

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        await Response.WriteAsync("retry: 3000\n\n");
        await Response.Body.FlushAsync();

        var token = HttpContext.RequestAborted;
        var ping = Runtime.TickCount64;
        var countAt = 0L;
        var lastCount = -1;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var list = notifyService.GetIncremental(userId, admin, lastId, 20);
                foreach (var item in list)
                {
                    lastId = item.Id;
                    var json = item.ToModel().ToJson();
                    await Response.WriteAsync($"id: {item.Id}\nevent: notification\ndata: {json}\n\n", token);
                }

                // 未读数变化事件，让其它终端的已读/新消息在 10 秒内同步到铃铛
                if (Runtime.TickCount64 - countAt >= 10_000)
                {
                    countAt = Runtime.TickCount64;
                    var count = notifyService.CountUnread(userId, admin);
                    if (count != lastCount)
                    {
                        lastCount = count;
                        await Response.WriteAsync($"event: count\ndata: {new { count }.ToJson()}\n\n", token);
                    }
                }

                await Response.Body.FlushAsync(token);

                // 心跳保持代理连接，避免 20 秒空闲被断开
                if (Runtime.TickCount64 - ping >= 20_000)
                {
                    ping = Runtime.TickCount64;
                    await Response.WriteAsync(": ping\n\n", token);
                    await Response.Body.FlushAsync(token);
                }

                await Task.Delay(3000, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>获取当前用户的分类偏好</summary>
    /// <returns>JSON 偏好列表</returns>
    [HttpGet]
    public ActionResult Preferences()
    {
        var (userId, _, _, _) = GetCurrentUser();
        var categories = new[] { "System", "Business", "Security", "Task", "Marketing" };
        var items = categories.Select(e => new
        {
            category = e,
            channels = notifyService.GetPreferences(userId, e) ?? ["InApp", "Mail", "Sms", "AppPush"],
        });

        return Json(0, null, items);
    }

    /// <summary>保存当前用户的分类偏好</summary>
    /// <param name="request">偏好请求</param>
    /// <returns>JSON 保存结果</returns>
    [HttpPost]
    public ActionResult SavePreferences([FromBody] NotificationPreferenceRequest request)
    {
        if (request == null) return Json(400, "Request is required");

        var (userId, _, _, _) = GetCurrentUser();
        notifyService.SavePreferences(userId, request.Category, request.Channels);

        return Json(0, "Saved");
    }

    /// <summary>注册移动端推送设备</summary>
    /// <param name="request">设备请求</param>
    /// <returns>JSON 注册结果</returns>
    [HttpPost]
    public ActionResult RegisterDevice([FromBody] NotificationDeviceRequest request)
    {
        if (request == null) return Json(400, "Request is required");

        var (userId, _, _, _) = GetCurrentUser();
        notifyService.RegisterDevice(userId, request.Token, request.Platform);

        return Json(0, "Registered");
    }

    /// <summary>注销移动端推送设备</summary>
    /// <param name="request">设备请求</param>
    /// <returns>JSON 注销结果</returns>
    [HttpPost]
    public ActionResult UnregisterDevice([FromBody] NotificationDeviceRequest request)
    {
        if (request == null) return Json(400, "Request is required");

        var (userId, _, _, _) = GetCurrentUser();
        var count = notifyService.UnregisterDevice(userId, request.Token);

        return Json(0, null, new { count });
    }

    private (Int32 userId, Boolean admin, String name, Int32 tenantId) GetCurrentUser()
    {
        var user = CurrentUser as IUser ?? ManageProvider.User;
        var userId = user?.Id ?? 0;
        var admin = user != null && user.Roles.Any(e => e.IsSystem);
        var tenantId = (CurrentTenant as TenantUser)?.TenantId ?? 0;

        return (userId, admin, user + "", tenantId);
    }

    private static Object ToItem(NotificationRecord entity) => new
    {
        entity.Id,
        entity.BizId,
        entity.Shared,
        entity.Title,
        entity.Content,
        entity.Category,
        entity.Level,
        entity.Link,
        entity.Read,
        entity.ReadTime,
        entity.ReadChannel,
        entity.Handled,
        entity.HandledBy,
        entity.HandledTime,
        entity.CreateTime,
    };
}

/// <summary>标记已读请求</summary>
public class NotificationReadRequest
{
    /// <summary>业务消息编号</summary>
    public String BizId { get; set; }

    /// <summary>记录编号。BizId 为空时使用</summary>
    public Int64 Id { get; set; }

    /// <summary>触发已读的渠道</summary>
    public String Channel { get; set; } = "InApp";
}

/// <summary>处理通知请求</summary>
public class NotificationHandleRequest
{
    /// <summary>业务消息编号</summary>
    public String BizId { get; set; }

    /// <summary>处理结果</summary>
    public String Result { get; set; }
}

/// <summary>删除通知请求</summary>
public class NotificationDeleteRequest
{
    /// <summary>记录编号</summary>
    public Int64 Id { get; set; }
}

/// <summary>通知偏好请求</summary>
public class NotificationPreferenceRequest
{
    /// <summary>分类</summary>
    public String Category { get; set; }

    /// <summary>允许的渠道，逗号分隔</summary>
    public String Channels { get; set; }
}

/// <summary>移动端设备请求</summary>
public class NotificationDeviceRequest
{
    /// <summary>设备令牌</summary>
    public String Token { get; set; }

    /// <summary>平台</summary>
    public String Platform { get; set; }
}
