using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Log;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知投递调度器。定时兜底扫描待发送记录，收到总线事件时立即唤醒</summary>
/// <param name="provider">服务提供者</param>
/// <param name="bus">通知事件总线</param>
/// <param name="log">日志</param>
public class NotificationDispatcher(IServiceProvider provider, NotificationBus bus = null, ILog log = null) : BackgroundService
{
    private readonly ILog _log = log ?? XTrace.Log;
    private readonly Channel<Boolean> _signal = Channel.CreateBounded<Boolean>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
    });
    private static readonly SemaphoreSlim _processLock = new(1, 1);
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    /// <summary>后台循环投递。总线事件到来立即执行，否则每60秒兜底一次</summary>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>异步任务</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (bus != null)
        {
            bus.Dispatch += OnDispatch;
            bus.Start();
        }

        try
        {
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

                // 有事件立即唤醒，否则等待一个兜底周期
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                wait.CancelAfter(_interval);
                try
                {
                    await _signal.Reader.ReadAsync(wait.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }
        finally
        {
            if (bus != null) bus.Dispatch -= OnDispatch;
        }
    }

    private void OnDispatch(Object sender, NotificationEvent e) => _signal.Writer.TryWrite(true);

    /// <summary>执行一轮投递。单实例用进程锁，多实例用缓存提供者的分布式锁</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>投递数量</returns>
    public async Task<Int32> DispatchOnce(CancellationToken cancellationToken = default)
    {
        IDisposable? cacheLock = null;
        var processLocked = false;
        var cache = provider.GetService<ICacheProvider>()?.Cache ?? provider.GetService<ICache>();
        if (cache != null)
        {
            cacheLock = cache.AcquireLock("CubeNotificationDispatch", 0, 120_000, false);
            if (cacheLock == null) return 0;
        }
        else
        {
            if (!await _processLock.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return 0;
            processLocked = true;
        }

        try
        {
            var now = DateTime.Now;

            // 节点崩溃或重启后，Sending 超过 5 分钟视为遗留，重新入队
            NotificationRecord.ResetStaleSending(now);

            var list = NotificationRecord.FindDispatchable(now);
            if (list.Count == 0) return 0;

            var channels = new Dictionary<String, INotificationChannel>(StringComparer.OrdinalIgnoreCase);
            foreach (var channel in provider.GetServices<INotificationChannel>())
            {
                if (!channel.Name.IsNullOrEmpty()) channels[channel.Name] = channel;
            }

            var count = 0;
            foreach (var record in list)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // 抢占记录，避免多节点重复投递
                if (NotificationRecord.TryClaimForSending(record.Id, DateTime.Now) == 0) continue;

                record.Status = (Int32)NotificationStatus.Sending;

                // 任一渠道已读/已处理，取消尚未发送的后续渠道
                if (record.CancelOnRead && !record.BizId.IsNullOrEmpty())
                {
                    if (NotificationRecord.ExistsReadOrHandled(record.TenantId, record.BizId))
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
                    record.Status = (Int32)NotificationStatus.Pending;
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
        finally
        {
            cacheLock?.Dispose();
            if (processLocked) _processLock.Release();
        }
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
