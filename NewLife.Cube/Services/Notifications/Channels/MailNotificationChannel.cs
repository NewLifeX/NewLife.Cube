using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;

namespace NewLife.Cube.Services.Notifications.Channels;

/// <summary>邮件通知渠道</summary>
/// <param name="mailService">邮件配置服务</param>
public class MailNotificationChannel(MailService mailService) : INotificationChannel
{
    /// <summary>渠道名称</summary>
    public String Name => NotificationChannels.Mail;

    /// <summary>发送通知</summary>
    /// <param name="record">通知记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    public async Task<NotificationSendResult> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null) return NotificationSendResult.Fail("Record is null.");
        if (record.Target.IsNullOrEmpty()) return NotificationSendResult.Fail("Mail target is empty.");

        var config = mailService.GetConfig(record.TenantId, "notify");
        if (config == null) return NotificationSendResult.Skip("No enabled mail channel.");

        record.ConfigId = config.Id;
        record.ConfigName = config.Name;
        record.Provider = config.Provider;

        var result = await mailService.SendNotify(record.Target, record.Title, record.Content, config);

        return NotificationSendResult.Ok(result);
    }
}
