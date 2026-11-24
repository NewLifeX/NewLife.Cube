using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;

namespace NewLife.Cube.Services.Notifications.Channels;

/// <summary>站内信渠道。记录本身即收件箱，无需再次发送</summary>
public class InAppNotificationChannel : INotificationChannel
{
    /// <summary>渠道名称</summary>
    public String Name => NotificationChannels.InApp;

    /// <summary>发送通知</summary>
    /// <param name="record">通知记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功结果</returns>
    public Task<NotificationSendResult> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
        => Task.FromResult(NotificationSendResult.Ok());
}
