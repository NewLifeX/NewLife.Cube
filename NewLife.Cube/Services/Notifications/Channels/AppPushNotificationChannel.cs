using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;

namespace NewLife.Cube.Services.Notifications.Channels;

/// <summary>APP 推送通知渠道</summary>
/// <param name="provider">推送提供者</param>
public class AppPushNotificationChannel(IAppPushProvider provider) : INotificationChannel
{
    /// <summary>渠道名称</summary>
    public String Name => "AppPush";

    /// <summary>发送通知</summary>
    /// <param name="record">通知记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    public Task<NotificationSendResult> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null) return Task.FromResult(NotificationSendResult.Fail("Record is null."));

        var message = new AppPushMessage
        {
            TenantId = record.TenantId,
            UserId = record.UserId,
            BizId = record.BizId,
            Title = record.Title,
            Content = record.Content,
            Link = record.Link,
            Data = record.Data,
        };

        return provider.SendAsync(message, cancellationToken);
    }
}
