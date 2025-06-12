﻿using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;
using XCode;
using XCode.Exceptions;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Services;

/// <summary>数据保留服务</summary>
/// <remarks>实例化数据保留服务</remarks>
/// <param name="setting"></param>
/// <param name="tracer"></param>
public class DataRetentionService(CubeSetting setting, ITracer tracer) : IHostedService
{
    private TimerX _timer;

    /// <summary>
    /// 开始
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new TimerX(DoWork, null, DateTime.Today.AddMinutes(Rand.Next(60)), 3600 * 1000) { Async = true };

        // 临时来一次
        TimerX.Delay(DoWork, 10_000);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();

        return Task.CompletedTask;
    }

    private void DoWork(Object state)
    {
        TrimData();
        TrimFile();
    }

    private void TrimData()
    {
        var set = setting;
        if (set.DataRetention <= 0) return;

        // 保留数据的起点
        var time = DateTime.Now.AddDays(-set.DataRetention);

        using var span = tracer?.NewSpan("DataRetention", new { time });
        try
        {
            var rs = OAuthLog.DeleteBefore(time);
            XTrace.WriteLine("删除[{0}]之前的 OAuthLog 共：{1:n0}", time.ToFullString(), rs);

            rs = AppLog.DeleteBefore(time);
            XTrace.WriteLine("删除[{0}]之前的 AppLog 共：{1:n0}", time.ToFullString(), rs);

            rs = DeleteLogBefore(time);
            XTrace.WriteLine("删除[{0}]之前的 Log 共：{1:n0}", time.ToFullString(), rs);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
        }
    }

    static Int32 DeleteLogBefore(DateTime date)
    {
        // 删除指定日期之前的日志
        // SQLite下日志表较大时，删除可能报错，可以查询出来逐个删除
        var where = XLog._.ID < XLog.Meta.Factory.Snow.GetId(date) & XLog._.CreateUserID == 0;
        try
        {
            return XLog.Delete(where);
        }
        catch (XSqlException)
        {
            var rs = 0;
            for (var i = 0; i < 100; i++)
            {
                var list = XLog.FindAll(where, null, null, 0, 10000);
                if (list.Count == 0) break;

                rs += list.Delete();
            }
            return rs;
        }
    }

    private void TrimFile()
    {
        var set = setting;
        if (set.FileRetention <= 0) return;

        var di = NewLife.Setting.Current.BackupPath.AsDirectory();
        if (!di.Exists) return;

        // 小于该大小的文件将不会被删除，即使超过保留时间
        var minSize = set.FileRetentionSize * 1024;

        // 保留数据的起点
        var time = DateTime.Now.AddDays(-set.FileRetention);

        using var span = tracer?.NewSpan("FileRetention", new { time });
        try
        {
            foreach (var fi in di.GetAllFiles("*.*", false))
            {
                if (minSize > 0 && fi.Length < minSize) continue;

                var name = fi.Name;
                var p = name.LastIndexOf('_');
                if (p > 0)
                {
                    var p2 = name.LastIndexOf('.');
                    if (p2 > 0)
                    {
                        var dt = name.Substring(p + 1, p2 - p - 1).ToDateTime();
                        if (dt.Year > 2000 && dt < time)
                        {
                            var msg = $"删除[{time.ToFullString()}]之前的备份文件：{fi.Name}";
                            XTrace.WriteLine(msg);
                            LogProvider.Provider?.WriteLog("FileRetention", "Delete", true, msg);

                            try
                            {
                                fi.Delete();
                            }
                            catch (Exception ex)
                            {
                                XTrace.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
        }
    }
}