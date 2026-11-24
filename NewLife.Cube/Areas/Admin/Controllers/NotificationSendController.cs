using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;
using NewLife.Data;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>通知发送与投递管理</summary>
/// <param name="notifyService">通知服务</param>
[DisplayName("通知发送")]
[EntityAuthorize(PermissionFlags.Insert)]
[AdminArea]
[Menu(0, false)]
public class NotificationSendController(NotificationService notifyService) : ControllerBaseX
{
    /// <summary>发送通知。一个收件人一个渠道一行，相同 BizId 共享已读状态</summary>
    /// <param name="request">通知请求</param>
    /// <returns>JSON 发布结果</returns>
    [HttpPost]
    public ActionResult Send([FromBody] NotificationRequest request)
    {
        if (request == null) return Json(400, "Request is required");

        var user = CurrentUser as IUser ?? ManageProvider.User;
        request.SenderId = user?.Id ?? 0;
        if (request.TenantId <= 0) request.TenantId = (CurrentTenant as TenantUser)?.TenantId ?? 0;

        try
        {
            var result = notifyService.Publish(request);

            return Json(0, null, result);
        }
        catch (Exception ex)
        {
            return Json(500, ex.Message);
        }
    }

    /// <summary>重试失败的外部渠道记录</summary>
    /// <param name="id">记录编号</param>
    /// <returns>JSON 重试结果</returns>
    [HttpPost]
    public ActionResult Retry(Int64 id)
    {
        var entity = NotificationRecord.FindById(id);
        if (entity == null || entity.Channel.EqualIgnoreCase(NotificationChannels.InApp)) return Json(404, "Record not found");

        entity.Status = (Int32)NotificationStatus.Pending;
        entity.Attempts = 0;
        entity.NextRetryTime = DateTime.Now;
        entity.Result = "Retry requested";
        entity.Update();

        return Json(0, "Retry scheduled");
    }

    /// <summary>查询通知记录</summary>
    /// <param name="page">分页</param>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="channel">渠道</param>
    /// <param name="userId">用户编号</param>
    /// <param name="category">分类</param>
    /// <param name="level">级别</param>
    /// <param name="read">已读</param>
    /// <param name="handled">已处理</param>
    /// <param name="success">成功</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="key">关键字</param>
    /// <returns>JSON 分页列表</returns>
    [HttpGet]
    public ActionResult Records([FromQuery] PageParameter page, String bizId = null, String channel = null, Int32 userId = -1,
        String category = null, String level = null, Boolean? read = null, Boolean? handled = null, Boolean? success = null,
        DateTime start = default, DateTime end = default, String key = null)
    {
        var tenantId = (CurrentTenant as TenantUser)?.TenantId ?? -1;
        var list = NotificationRecord.SearchBy(tenantId, bizId, channel, userId, category, level, read, handled, success, start, end, key, page);

        return Json(0, null, new
        {
            page.PageIndex,
            page.PageSize,
            page.TotalCount,
            items = list.Select(e => new
            {
                e.Id,
                e.BizId,
                e.Shared,
                e.CancelOnRead,
                e.Channel,
                e.UserId,
                e.Target,
                e.Title,
                e.Category,
                e.Level,
                e.Status,
                e.Success,
                e.Attempts,
                e.NextRetryTime,
                e.Read,
                e.ReadBy,
                e.ReadChannel,
                e.Handled,
                e.HandledBy,
                e.HandledTime,
                e.Result,
                e.CreateTime,
                e.SentTime,
            }),
        });
    }

    /// <summary>获取通知模板</summary>
    /// <returns>JSON 模板列表</returns>
    [HttpGet]
    public ActionResult Templates()
    {
        var tenantId = (CurrentTenant as TenantUser)?.TenantId ?? 0;

        return Json(0, null, notifyService.GetTemplates(tenantId));
    }

    /// <summary>保存通知模板</summary>
    /// <param name="template">模板</param>
    /// <returns>JSON 保存结果</returns>
    [HttpPost]
    public ActionResult SaveTemplate([FromBody] NotificationTemplate template)
    {
        if (template == null || template.Code.IsNullOrEmpty()) return Json(400, "Template code is required");

        var tenantId = (CurrentTenant as TenantUser)?.TenantId ?? 0;
        notifyService.SaveTemplate(tenantId, template);

        return Json(0, "Saved");
    }

    /// <summary>删除通知模板</summary>
    /// <param name="code">模板编码</param>
    /// <returns>JSON 删除结果</returns>
    [HttpPost]
    public ActionResult DeleteTemplate(String code)
    {
        if (code.IsNullOrEmpty()) return Json(400, "Template code is required");

        var tenantId = (CurrentTenant as TenantUser)?.TenantId ?? 0;
        var count = notifyService.DeleteTemplate(tenantId, code);

        return Json(0, null, new { count });
    }
}
