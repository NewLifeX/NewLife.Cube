using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知投递调度器。后台扫描待发送记录，按渠道投递并做有限重试</summary>
public class NotificationDispatcher : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILog _log;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    /// <summary>实例化调度器</summary>
    /// <param name="provider">服务提供者</param>
    /// <param name="log">日志</param>
    public NotificationDispatcher(IServiceProvider provider, ILog log = null)
    {
        _provider = provider;
        _log = log ?? XTrace.Log;
    }

    /// <summary>后台循环投递</summary>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>异步任务</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchOnce(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log?.Error("Notification dispatch failed: {0}", ex.Message);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>执行一轮投递</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>投递数量</returns>
    public async Task<Int32> DispatchOnce(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var pending = (Int32)NotificationStatus.Pending;
        var failed = (Int32)NotificationStatus.Failed;
        var sending = (Int32)NotificationStatus.Sending;

        // 节点崩溃或重启后，Sending 超过 5 分钟视为遗留，重新入队
        var db = NotificationRecord.Meta.Session.Dal.Db;
        var nowText = db.FormatDateTime(now);
        var stale = db.FormatDateTime(now.AddMinutes(-5));
        NotificationRecord.Update($"Status={pending}", $"Status={sending} AND UpdateTime < {stale}");

        var list = NotificationRecord.FindAll(
            NotificationRecord._.Channel != "InApp"
            & NotificationRecord._.Deleted == false
            & NotificationRecord._.Attempts < 3
            & (NotificationRecord._.Status == pending | NotificationRecord._.Status == failed)
            & (NotificationRecord._.NextRetryTime.IsNull() | NotificationRecord._.NextRetryTime <= now),
            NotificationRecord._.Id.Asc(), null, 0, 50);

        if (list.Count == 0) return 0;

        var channels = new Dictionary<String, INotificationChannel>(StringComparer.OrdinalIgnoreCase);
        foreach (var channel in _provider.GetServices<INotificationChannel>())
        {
            if (!channel.Name.IsNullOrEmpty()) channels[channel.Name] = channel;
        }

        var count = 0;
        foreach (var record in list)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // 抢占记录，避免多节点重复投递
            var rows = NotificationRecord.Update(
                $"Status={sending}, UpdateTime={nowText}",
                $"Id={record.Id} AND (Status={pending} OR Status={failed})");
            if (rows == 0) continue;

            record.Status = (Int32)NotificationStatus.Sending;

            // 任一渠道已读/已处理，取消尚未发送的后续渠道
            if (record.CancelOnRead && !record.BizId.IsNullOrEmpty())
            {
                var read = NotificationRecord.FindCount(NotificationRecord._.TenantId == record.TenantId
                    & NotificationRecord._.BizId == record.BizId
                    & (NotificationRecord._.Read == true | NotificationRecord._.Handled == true)) > 0;
                if (read)
                {
                    record.Status = (Int32)NotificationStatus.Skipped;
                    record.Success = true;
                    record.Result = "Cancelled because read or handled.";
                    record.Update();
                    count++;
                    continue;
                }
            }

            if (!channels.TryGetValue(record.Channel, out var channel))
            {
                await MarkFailed(record, $"Channel '{record.Channel}' is not registered.", cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                var result = await channel.SendAsync(record, cancellationToken).ConfigureAwait(false);
                if (result == null)
                    await MarkFailed(record, "Channel returned no result.", cancellationToken).ConfigureAwait(false);
                else if (!result.Success)
                    await MarkFailed(record, result.Message, cancellationToken).ConfigureAwait(false);
                else
                {
                    record.Status = result.Skipped ? (Int32)NotificationStatus.Skipped : (Int32)NotificationStatus.Success;
                    record.Success = true;
                    record.Result = result.Message;
                    record.SentTime = DateTime.Now;
                    record.Attempts++;
                    record.Update();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                record.Status = pending;
                record.Update();
                break;
            }
            catch (Exception ex)
            {
                await MarkFailed(record, ex.Message, cancellationToken).ConfigureAwait(false);
            }

            count++;
        }

        return count;
    }

    private static Task MarkFailed(NotificationRecord record, String message, CancellationToken cancellationToken)
    {
        record.Attempts++;
        record.Success = false;
        record.Result = message;

        if (record.Attempts >= 3)
        {
            record.Status = (Int32)NotificationStatus.Failed;
            record.NextRetryTime = DateTime.Now.AddDays(1);
        }
        else
        {
            record.Status = (Int32)NotificationStatus.Failed;
            record.NextRetryTime = record.Attempts switch
            {
                1 => DateTime.Now.AddMinutes(1),
                2 => DateTime.Now.AddMinutes(5),
                _ => DateTime.Now.AddMinutes(15),
            };
        }

        record.Update();

        return Task.CompletedTask;
    }
}
