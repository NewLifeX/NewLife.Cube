using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知渠道</summary>
public interface INotificationChannel
{
    /// <summary>渠道名称。InApp/Mail/Sms/AppPush/Webhook 等</summary>
    String Name { get; }

    /// <summary>发送通知</summary>
    /// <param name="record">通知记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<NotificationSendResult> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default);
}
