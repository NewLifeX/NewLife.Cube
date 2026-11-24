using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Data;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知服务。负责发布、查询、已读、处理与偏好设置</summary>
/// <param name="resolver">收件人解析器</param>
/// <param name="templates">模板注册表</param>
/// <param name="bus">通知事件总线。为空时不广播实时事件，SSE 轮询兜底</param>
public class NotificationService(NotificationRecipientResolver resolver, NotificationTemplateRegistry templates, NotificationBus bus = null) : INotifier
{
    /// <summary>业务消息幂等窗口。相同租户 + BizId 在窗口内重复发布会被跳过</summary>
    public static readonly TimeSpan IdempotentWindow = TimeSpan.FromMinutes(5);

    /// <summary>发布通知。一个收件人一个渠道写一行，相同 BizId 共享已读状态</summary>
    /// <param name="request">通知请求</param>
    /// <returns>发布结果</returns>
    /// <exception cref="ArgumentNullException">请求为空</exception>
    /// <exception cref="ArgumentException">标题和内容为空，或共享已读缺少站内信</exception>
    /// <exception cref="InvalidOperationException">收件人超过上限，或广播通知使用共享已读</exception>
    public NotificationPublishResult Publish(NotificationRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.TenantId <= 0) request.TenantId = TenantContext.CurrentId;
        if (request.ToAdmins && request.All)
            throw new InvalidOperationException("ToAdmins and All cannot both be true.");
        if (request.All && request.Scope == NotificationScope.Any)
            throw new InvalidOperationException("Broadcast notifications cannot share read state. Use scope=Each.");

        var template = templates.Get(request.TenantId, request.TemplateCode);
        var data = request.Data;
        if (request.Items.Count > 0)
        {
            var dic = data as IDictionary<String, Object?>;
            dic = dic != null
                ? new Dictionary<String, Object?>(dic)
                : data?.ToJson().ToJsonEntity<Dictionary<String, Object?>>() ?? new Dictionary<String, Object?>();
            foreach (var item in request.Items) dic[item.Key] = item.Value;
            data = dic;
        }
        var title = ResolveText(request.Title ?? template?.Title, data);
        var content = ResolveText(request.Content ?? template?.Content, data);
        if (title.IsNullOrEmpty() && content.IsNullOrEmpty())
            throw new ArgumentException("Title or content is required.", nameof(request));
        if (title.IsNullOrEmpty()) title = content.Length > 80 ? content[..80] : content;
        if (content.IsNullOrEmpty()) content = title;

        var channels = ResolveChannels(request, template);
        if (request.Scope == NotificationScope.Any && !channels.Any(e => e.EqualIgnoreCase(NotificationChannels.InApp)))
            throw new ArgumentException("Shared read requires the InApp channel.", nameof(request));

        var now = DateTime.Now;
        var baseBizId = request.BizId.IsNullOrEmpty() ? NewBizId(request.Action) : request.BizId.Trim();
        if (baseBizId.Length > 50) baseBizId = baseBizId[..50];

        var recipients = resolver.Resolve(request);
        if (recipients.Count == 0) return new NotificationPublishResult { BizId = baseBizId, Skipped = true };

        // 调用方显式传入 BizId 时视为同一业务消息，窗口内重复发布直接跳过
        if (!request.BizId.IsNullOrEmpty() && ExistsBizId(request.TenantId, baseBizId, request.Scope == NotificationScope.Each && recipients.Count > 1))
            return new NotificationPublishResult { BizId = baseBizId, Skipped = true };

        var result = new NotificationPublishResult { BizId = baseBizId, Recipients = recipients.Count };
        var smsContent = ResolveText(template?.SmsContent, data);
        if (smsContent.IsNullOrEmpty()) smsContent = content;
        var pushContent = ResolveText(template?.PushContent, data);
        if (pushContent.IsNullOrEmpty()) pushContent = content;
        var link = request.Link ?? template?.Link;
        var dataJson = data?.ToJson();
        var smsParam = BuildSmsParam(data, smsContent);
        var hasPending = false;

        foreach (var recipient in recipients)
        {
            var bizId = request.Scope == NotificationScope.Any
                ? baseBizId
                : recipients.Count == 1 ? baseBizId : AppendUser(baseBizId, recipient.UserId);
            if (!result.BizIds.Contains(bizId)) result.BizIds.Add(bizId);

            var preferences = GetPreferences(recipient.UserId, request.Category);

            foreach (var channel in channels)
            {
                if (!AllowChannel(preferences, channel, request.Scope == NotificationScope.Any)) continue;

                var target = ResolveTarget(channel, recipient, request.Target);
                var record = new NotificationRecord
                {
                    TenantId = request.TenantId,
                    BizId = bizId,
                    Shared = request.Scope == NotificationScope.Any,
                    CancelOnRead = request.CancelOnRead,
                    Action = request.Action,
                    Channel = channel,
                    UserId = recipient.UserId,
                    Target = target,
                    Title = channel.EqualIgnoreCase(NotificationChannels.Sms) ? smsContent : channel.EqualIgnoreCase(NotificationChannels.AppPush) ? pushContent : title,
                    Content = channel.EqualIgnoreCase(NotificationChannels.Sms) ? smsContent : channel.EqualIgnoreCase(NotificationChannels.AppPush) ? pushContent : content,
                    Category = request.Category,
                    Level = request.Level,
                    Link = link,
                    Data = channel.EqualIgnoreCase(NotificationChannels.Sms) ? smsParam : dataJson,
                    TemplateCode = request.TemplateCode,
                    SenderId = request.SenderId,
                    ExpireTime = request.ExpireTime == DateTime.MinValue ? DateTime.Now.AddYears(100) : request.ExpireTime,
                    CreateTime = now,
                    Read = false,
                    Handled = false,
                    Deleted = false,
                };

                if (channel.EqualIgnoreCase(NotificationChannels.InApp))
                {
                    record.Status = (Int32)NotificationStatus.Success;
                    record.Success = true;
                    record.SentTime = now;
                }
                else if (target.IsNullOrEmpty() && !channel.EqualIgnoreCase(NotificationChannels.AppPush))
                {
                    record.Status = (Int32)NotificationStatus.Skipped;
                    record.Success = true;
                    record.Result = "No target.";
                }
                else
                {
                    var delay = ResolveDelay(request.ChannelDelays, channels, channel);
                    record.Status = (Int32)NotificationStatus.Pending;
                    record.Success = false;
                    record.NextRetryTime = now.AddSeconds(delay);
                    hasPending = true;
                }

                record.Insert();
                result.Records++;

                // 站内信写入后广播实时事件；总线不可用时由 SSE 轮询兜底
                if (bus != null && channel.EqualIgnoreCase(NotificationChannels.InApp))
                {
                    _ = bus.PublishAsync(new NotificationEvent
                    {
                        Kind = NotificationEventKind.Created,
                        Id = record.Id,
                        UserId = record.UserId,
                    });
                }
            }
        }

        // 有外部渠道待投递时唤醒各节点调度器，避免等到下一轮定时兜底
        if (bus != null && hasPending)
        {
            _ = bus.PublishAsync(new NotificationEvent { Kind = NotificationEventKind.Dispatch });
        }

        result.Skipped = result.Records == 0;

        return result;
    }

    /// <summary>发送通知。模块级薄门面，内部复用 <see cref="Publish"/> 的收件人、模板与持久化逻辑</summary>
    /// <param name="request">模块级通知请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模块级通知结果</returns>
    Task<NotifyResult> INotifier.SendAsync(NotifyRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<NotifyResult>(cancellationToken);

        var rich = new NotificationRequest
        {
            BizId = request.BizId,
            UserIds = request.UserIds,
            Channels = request.Channels,
            Title = request.Title,
            Content = request.Content,
            Link = request.Link,
            Category = request.Category ?? "Business",
            Level = request.Level ?? "Info",
        };
        foreach (var item in request.Items) rich.Items[item.Key] = item.Value;

        var result = Publish(rich);

        return Task.FromResult(new NotifyResult
        {
            BizId = result.BizId,
            Recipients = result.Recipients,
            Records = result.Records,
            Skipped = result.Skipped,
        });
    }

    /// <summary>统计未读站内信</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="admin">是否系统管理员</param>
    /// <returns>未读数量</returns>
    public Int32 CountUnread(Int32 userId, Boolean admin) => NotificationRecord.CountUnread(userId, admin);

    /// <summary>获取最近未读站内信</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="admin">是否系统管理员</param>
    /// <param name="size">条数</param>
    /// <returns>站内信列表</returns>
    public IList<NotificationRecord> GetRecentUnread(Int32 userId, Boolean admin, Int32 size = 10) => NotificationRecord.GetRecentUnread(userId, admin, size);

    /// <summary>获取增量站内信</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="admin">是否系统管理员</param>
    /// <param name="sinceId">起始编号，不含</param>
    /// <param name="size">条数</param>
    /// <returns>站内信列表</returns>
    public IList<NotificationRecord> GetIncremental(Int32 userId, Boolean admin, Int64 sinceId, Int32 size = 20) => NotificationRecord.GetIncremental(userId, admin, sinceId, size);

    /// <summary>分页获取当前用户站内信</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="admin">是否系统管理员</param>
    /// <param name="category">分类</param>
    /// <param name="read">已读状态</param>
    /// <param name="start">开始时间</param>
    /// <param name="end">结束时间</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页</param>
    /// <returns>站内信列表</returns>
    public IList<NotificationRecord> GetPage(Int32 userId, Boolean admin, String category, Boolean? read, DateTime start, DateTime end, String key, PageParameter page)
        => NotificationRecord.GetInAppPage(userId, admin, category, read, start, end, key, page);

    /// <summary>获取站内信详情</summary>
    /// <param name="id">记录编号</param>
    /// <param name="userId">用户编号</param>
    /// <param name="admin">是否系统管理员</param>
    /// <param name="tenantId">租户编号</param>
    /// <returns>记录；不存在或无权时返回 null</returns>
    public NotificationRecord GetDetail(Int64 id, Int32 userId, Boolean admin, Int32 tenantId)
    {
        var entity = NotificationRecord.FindById(id);
        if (entity == null || !entity.Channel.EqualIgnoreCase(NotificationChannels.InApp) || entity.Deleted) return null;
        if (tenantId > 0 && entity.TenantId != tenantId) return null;
        if (entity.UserId == 0) return admin ? entity : null;

        return entity.UserId == userId ? entity : null;
    }

    /// <summary>标记已读。优先按 BizId 处理团队共享消息</summary>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="id">记录编号</param>
    /// <param name="userId">用户编号</param>
    /// <param name="userName">用户名称</param>
    /// <param name="admin">是否系统管理员</param>
    /// <param name="tenantId">租户编号</param>
    /// <param name="channel">触发已读的渠道</param>
    /// <returns>记录；不存在或无权时返回 null</returns>
    public NotificationRecord MarkRead(String bizId, Int64 id, Int32 userId, String userName, Boolean admin, Int32 tenantId, String channel)
    {
        if (!bizId.IsNullOrEmpty())
        {
            var record = NotificationRecord.MarkReadByBizId(bizId, userId, userName, tenantId, channel);
            if (record != null) return record;
        }

        return NotificationRecord.MarkRead(id, userId, userName, admin);
    }

    /// <summary>批量标记当前用户的个人站内信为已读</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="userName">用户名称</param>
    /// <param name="admin">是否系统管理员</param>
    /// <returns>已读数量</returns>
    public Int32 MarkAllRead(Int32 userId, String userName, Boolean admin) => NotificationRecord.MarkAllRead(userId, userName, admin);

    /// <summary>按 BizId 处理通知</summary>
    /// <param name="bizId">业务消息编号</param>
    /// <param name="userId">用户编号</param>
    /// <param name="userName">用户名称</param>
    /// <param name="tenantId">租户编号</param>
    /// <param name="result">处理结果</param>
    /// <returns>记录；不存在或无权时返回 null</returns>
    public NotificationRecord Handle(String bizId, Int32 userId, String userName, Int32 tenantId, String result)
        => NotificationRecord.HandleByBizId(bizId, userId, userName, tenantId, result);

    /// <summary>软删除站内信</summary>
    /// <param name="id">记录编号</param>
    /// <param name="userId">用户编号</param>
    /// <param name="admin">是否系统管理员</param>
    /// <returns>影响行数</returns>
    public Int32 Delete(Int64 id, Int32 userId, Boolean admin) => NotificationRecord.SoftDelete(id, userId, admin);

    /// <summary>获取租户通知模板</summary>
    /// <param name="tenantId">租户编号</param>
    /// <returns>模板列表</returns>
    public IList<NotificationTemplate> GetTemplates(Int32 tenantId) => templates.GetAll(tenantId);

    /// <summary>保存通知模板</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="template">模板</param>
    public void SaveTemplate(Int32 tenantId, NotificationTemplate template) => templates.Save(tenantId, template);

    /// <summary>删除通知模板</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="code">模板编码</param>
    /// <returns>删除行数</returns>
    public Int32 DeleteTemplate(Int32 tenantId, String code) => templates.Delete(tenantId, code);

    /// <summary>获取用户分类偏好。返回 null 表示未设置，使用全部渠道</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="category">通知分类</param>
    /// <returns>允许的渠道列表；未设置时返回 null</returns>
    public IList<String> GetPreferences(Int32 userId, String category)
    {
        if (userId <= 0 || category.IsNullOrEmpty()) return null;

        var parameter = Parameter.FindByUserIdAndCategoryAndName(userId, "Notify", category);
        if (parameter == null || parameter.Value.IsNullOrEmpty()) return null;

        return parameter.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>保存用户分类偏好</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="category">通知分类</param>
    /// <param name="channels">允许的渠道，逗号分隔</param>
    public void SavePreferences(Int32 userId, String category, String channels)
    {
        if (userId <= 0) throw new ArgumentException("User is required.", nameof(userId));
        if (category.IsNullOrEmpty()) throw new ArgumentException("Category is required.", nameof(category));

        var parameter = Parameter.GetOrAdd(userId, "Notify", category);
        parameter.Value = channels;
        parameter.Save();
    }

    /// <summary>注册移动端推送设备</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="token">设备令牌</param>
    /// <param name="platform">平台。Android/iOS/WeChat 等</param>
    public void RegisterDevice(Int32 userId, String token, String platform)
    {
        if (userId <= 0) throw new ArgumentException("User is required.", nameof(userId));
        if (token.IsNullOrEmpty()) throw new ArgumentException("Token is required.", nameof(token));

        var name = token.Length <= 50 ? token : "Dev_" + token.MD5();
        var parameter = Parameter.GetOrAdd(userId, "NotifyDevice", name);
        parameter.Value = platform;
        parameter.LongValue = token;
        parameter.Save();
    }

    /// <summary>注销移动端推送设备</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="token">设备令牌</param>
    /// <returns>删除行数</returns>
    public Int32 UnregisterDevice(Int32 userId, String token)
    {
        if (userId <= 0 || token.IsNullOrEmpty()) return 0;

        var name = token.Length <= 50 ? token : "Dev_" + token.MD5();

        var parameter = Parameter.FindByUserIdAndCategoryAndName(userId, "NotifyDevice", name);
        return parameter?.Delete() ?? 0;
    }

    private static Boolean AllowChannel(IList<String> preferences, String channel, Boolean forceInApp)
    {
        if (forceInApp && channel.EqualIgnoreCase(NotificationChannels.InApp)) return true;
        if (preferences == null || preferences.Count == 0) return true;

        return preferences.Any(e => e.EqualIgnoreCase(channel));
    }

    private static Boolean ExistsBizId(Int32 tenantId, String bizId, Boolean prefix)
        => NotificationRecord.ExistsBizId(tenantId, bizId, DateTime.Now - IdempotentWindow, prefix);

    private static Int32 ResolveDelay(Int32[] delays, IList<String> channels, String channel)
    {
        if (delays is not { Length: > 0 }) return 0;

        var index = channels.IndexOf(channel);
        if (index < 0 || index >= delays.Length) return 0;

        return Math.Max(0, delays[index]);
    }

    private String ResolveText(String text, Object data)
    {
        if (text.IsNullOrEmpty()) return text;

        return templates.Render(text, data);
    }

    private static String[] ResolveChannels(NotificationRequest request, NotificationTemplate template)
    {
        var channels = request.Channels;
        if (channels is not { Length: > 0 } && template?.Channels.IsNullOrEmpty() == false)
            channels = template.Channels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        channels ??= [NotificationChannels.InApp];

        var list = new List<String>();
        foreach (var item in channels)
        {
            if (item.IsNullOrEmpty()) continue;

            var name = item.Trim();
            name = NotificationChannels.BuiltIn.FirstOrDefault(e => e.EqualIgnoreCase(name)) ?? name;
            if (!list.Any(e => e.EqualIgnoreCase(name))) list.Add(name);
        }

        return list.Count == 0 ? [NotificationChannels.InApp] : list.ToArray();
    }

    private static String ResolveTarget(String channel, NotificationRecipient recipient, String fallback)
    {
        if (channel.EqualIgnoreCase(NotificationChannels.Mail)) return recipient.Mail.IsNullOrEmpty() ? fallback : recipient.Mail;
        if (channel.EqualIgnoreCase(NotificationChannels.Sms)) return recipient.Mobile.IsNullOrEmpty() ? fallback : recipient.Mobile;
        if (channel.EqualIgnoreCase(NotificationChannels.InApp) || channel.EqualIgnoreCase(NotificationChannels.AppPush)) return null;

        return fallback;
    }

    private static String BuildSmsParam(Object data, String content)
    {
        var dictionary = data?.ToDictionary() ?? new Dictionary<String, Object>();
        if (!dictionary.ContainsKey("content")) dictionary["content"] = content;

        return dictionary.ToJson();
    }

    private static String AppendUser(String bizId, Int32 userId)
    {
        var suffix = ":" + userId;
        if (bizId.Length + suffix.Length > 50) bizId = bizId[..(50 - suffix.Length)];

        return bizId + suffix;
    }

    private static String NewBizId(String action)
    {
        var prefix = action.IsNullOrEmpty() ? "notify" : action.ToLowerInvariant();
        var id = $"{prefix}:{DateTime.Now:yyyyMMddHHmmss}:{Guid.NewGuid():N}";

        return id.Length <= 50 ? id : id[..50];
    }
}
