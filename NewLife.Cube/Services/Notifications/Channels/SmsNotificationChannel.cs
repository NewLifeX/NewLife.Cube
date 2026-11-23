using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;

namespace NewLife.Cube.Services.Notifications.Channels;

/// <summary>短信通知渠道</summary>
/// <param name="smsService">短信配置服务</param>
public class SmsNotificationChannel(SmsService smsService) : INotificationChannel
{
    /// <summary>渠道名称</summary>
    public String Name => "Sms";

    /// <summary>发送通知</summary>
    /// <param name="record">通知记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    public async Task<NotificationSendResult> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null) return NotificationSendResult.Fail("Record is null.");
        if (record.Target.IsNullOrEmpty()) return NotificationSendResult.Fail("SMS target is empty.");

        var config = smsService.GetConfig(record.TenantId, "notify");
        if (config == null) return NotificationSendResult.Skip("No enabled SMS channel.");

        record.ConfigId = config.Id;
        record.ConfigName = config.Name;
        record.Provider = config.Provider;

        var result = await smsService.SendNotify(record.Target, record.Content, config, record.Data);

        return NotificationSendResult.Ok(result);
    }
}
