using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services.Notifications;
using NewLife.Serialization;

namespace NewLife.Cube.Services.Notifications.Channels;

/// <summary>Webhook 通知渠道。支持通用 JSON、钉钉和企业微信文本格式</summary>
/// <param name="name">渠道名称</param>
public class WebhookNotificationChannel(String name = NotificationChannels.Webhook) : INotificationChannel
{
    private static readonly HttpClient _client = new();

    /// <summary>渠道名称</summary>
    public String Name => name;

    /// <summary>发送通知</summary>
    /// <param name="record">通知记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    public async Task<NotificationSendResult> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null) return NotificationSendResult.Fail("Record is null.");
        if (record.Target.IsNullOrEmpty()) return NotificationSendResult.Fail("Webhook target is empty.");

        var text = $"{record.Title}\n{record.Content}".Trim();
        if (!record.Link.IsNullOrEmpty()) text += $"\n{record.Link}";

        var body = name.EqualIgnoreCase(NotificationChannels.DingTalk, NotificationChannels.WeCom)
            ? new { msgtype = "text", text = new { content = text } }.ToJson()
            : new { title = record.Title, content = record.Content, link = record.Link, bizId = record.BizId }.ToJson();

        try
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await _client.PostAsync(record.Target, content, cancellationToken);
            var message = await response.Content.ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? NotificationSendResult.Ok(message)
                : NotificationSendResult.Fail($"{(Int32)response.StatusCode}: {message}");
        }
        catch (Exception ex)
        {
            return NotificationSendResult.Fail(ex.Message);
        }
    }
}
