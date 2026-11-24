using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知发送范围</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationScope
{
    /// <summary>每人独立已读</summary>
    Each,

    /// <summary>同组任一收件人已读即全员已读</summary>
    Any,
}

/// <summary>通知状态</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationStatus
{
    /// <summary>待发送</summary>
    Pending = 0,

    /// <summary>发送中</summary>
    Sending = 1,

    /// <summary>成功</summary>
    Success = 2,

    /// <summary>失败</summary>
    Failed = 3,

    /// <summary>跳过</summary>
    Skipped = 4,
}

/// <summary>通知发布请求</summary>
public class NotificationRequest : IExtend
{
    /// <summary>租户编号</summary>
    public Int32 TenantId { get; set; }

    /// <summary>业务消息编号。相同 BizId 共享已读/处理状态</summary>
    public String BizId { get; set; }

    /// <summary>发送范围。Any 表示同组任一已读，Each 表示各自已读</summary>
    public NotificationScope Scope { get; set; } = NotificationScope.Each;

    /// <summary>收件人用户编号</summary>
    public Int32[] UserIds { get; set; }

    /// <summary>收件人角色编号</summary>
    public Int32[] RoleIds { get; set; }

    /// <summary>收件人部门编号</summary>
    public Int32[] DepartmentIds { get; set; }

    /// <summary>是否发送给系统管理员。与 All 互斥</summary>
    public Boolean ToAdmins { get; set; }

    /// <summary>是否发送给全部用户。受 1000 人上限保护</summary>
    public Boolean All { get; set; }

    /// <summary>操作类型。Notify/Alert/Task 等</summary>
    public String Action { get; set; } = "Notify";

    /// <summary>分类。System/Business/Security/Marketing/Todo 等</summary>
    public String Category { get; set; } = "Business";

    /// <summary>级别。Info/Success/Warning/Error/Urgent</summary>
    public String Level { get; set; } = "Info";

    /// <summary>模板编码。可选；为空时直接使用 Title/Content，非空时从 Parameter(Category=NotifyTemplate) 读取默认文案</summary>
    public String TemplateCode { get; set; }

    /// <summary>标题。未指定模板时直接使用</summary>
    public String Title { get; set; }

    /// <summary>内容。未指定模板时直接使用</summary>
    public String Content { get; set; }

    /// <summary>点击跳转地址</summary>
    public String Link { get; set; }

    /// <summary>业务载荷。模板中的 {key} 会用此对象属性替换</summary>
    public Object Data { get; set; }

    /// <summary>投递渠道。默认 InApp；需要进消息中心时必须包含 InApp</summary>
    public String[] Channels { get; set; }

    /// <summary>渠道延迟秒数，与 Channels 一一对应。InApp 始终立即，其余渠道按延迟排队发送</summary>
    public Int32[] ChannelDelays { get; set; }

    /// <summary>已读后是否取消尚未发送的后续渠道。默认 true</summary>
    public Boolean CancelOnRead { get; set; } = true;

    /// <summary>外部渠道目标。邮件/短信为空时取收件人邮箱/手机号，Webhook 使用此地址</summary>
    public String Target { get; set; }

    /// <summary>过期时间</summary>
    public DateTime ExpireTime { get; set; }

    /// <summary>发送人。0 表示系统</summary>
    public Int32 SenderId { get; set; }

    /// <summary>扩展数据。下游可写入自定义字段，参与模板渲染与业务载荷</summary>
    [JsonIgnore]
    public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>设置 或 获取 扩展数据项</summary>
    /// <param name="key">键</param>
    /// <returns>扩展数据</returns>
    [JsonIgnore]
    public Object? this[String key]
    {
        get => Items.TryGetValue(key, out var value) ? value : null;
        set => Items[key] = value;
    }
}

/// <summary>通知发布结果</summary>
public class NotificationPublishResult
{
    /// <summary>基础业务消息编号</summary>
    public String BizId { get; set; }

    /// <summary>收件人数量</summary>
    public Int32 Recipients { get; set; }

    /// <summary>写入记录数量</summary>
    public Int32 Records { get; set; }

    /// <summary>本次涉及的业务消息编号。Each 模式下每人一个</summary>
    public IList<String> BizIds { get; set; } = [];

    /// <summary>是否因去重或无收件人而跳过</summary>
    public Boolean Skipped { get; set; }
}

/// <summary>通知收件人</summary>
public class NotificationRecipient
{
    /// <summary>用户编号</summary>
    public Int32 UserId { get; set; }

    /// <summary>用户名称</summary>
    public String Name { get; set; }

    /// <summary>邮箱</summary>
    public String Mail { get; set; }

    /// <summary>手机号</summary>
    public String Mobile { get; set; }
}

/// <summary>通知发送结果</summary>
public class NotificationSendResult
{
    /// <summary>是否成功</summary>
    public Boolean Success { get; set; }

    /// <summary>结果消息</summary>
    public String Message { get; set; }

    /// <summary>是否因未配置而跳过</summary>
    public Boolean Skipped { get; set; }

    /// <summary>创建成功结果</summary>
    /// <param name="message">结果消息</param>
    /// <returns>发送结果</returns>
    public static NotificationSendResult Ok(String message = null) => new() { Success = true, Message = message };

    /// <summary>创建失败结果</summary>
    /// <param name="message">错误消息</param>
    /// <returns>发送结果</returns>
    public static NotificationSendResult Fail(String message) => new() { Success = false, Message = message };

    /// <summary>创建跳过结果</summary>
    /// <param name="message">跳过原因</param>
    /// <returns>发送结果</returns>
    public static NotificationSendResult Skip(String message = null) => new() { Success = true, Skipped = true, Message = message };
}

/// <summary>APP 推送消息</summary>
public class AppPushMessage
{
    /// <summary>租户编号</summary>
    public Int32 TenantId { get; set; }

    /// <summary>用户编号</summary>
    public Int32 UserId { get; set; }

    /// <summary>业务消息编号</summary>
    public String BizId { get; set; }

    /// <summary>标题</summary>
    public String Title { get; set; }

    /// <summary>内容摘要</summary>
    public String Content { get; set; }

    /// <summary>点击跳转地址</summary>
    public String Link { get; set; }

    /// <summary>业务载荷</summary>
    public Object Data { get; set; }
}

/// <summary>APP 推送提供者。宿主接入极光、个推、FCM、APNs 等具体通道</summary>
public interface IAppPushProvider
{
    /// <summary>发送 APP 推送</summary>
    /// <param name="message">推送消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<NotificationSendResult> SendAsync(AppPushMessage message, CancellationToken cancellationToken = default);
}

/// <summary>空 APP 推送提供者。默认不发送，宿主可注册自己的实现覆盖</summary>
public class NoopAppPushProvider : IAppPushProvider
{
    /// <summary>发送 APP 推送</summary>
    /// <param name="message">推送消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>跳过结果</returns>
    public Task<NotificationSendResult> SendAsync(AppPushMessage message, CancellationToken cancellationToken = default)
        => Task.FromResult(NotificationSendResult.Skip("APP push provider is not configured."));
}
