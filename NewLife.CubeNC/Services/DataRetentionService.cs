using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;
using XCode;
using XCode.Exceptions;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Services;

/// <summary>数据保留服务</summary>
public class DataRetentionService : IHostedService
{
    private readonly CubeSetting _setting;
    private readonly ITracer _tracer;
    private TimerX _timer;

    /// <summary>实例化数据保留服务</summary>
    /// <param name="setting"></param>
    /// <param name="tracer"></param>
    public DataRetentionService(CubeSetting setting, ITracer tracer)
    {
        _setting = setting;
        _tracer = tracer;
    }

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
        var set = _setting;
        if (set.DataRetention <= 0) return;

        // 保留数据的起点
        var time = DateTime.Now.AddDays(-set.DataRetention);

        using var span = _tracer?.NewSpan("DataRetention", new { time });
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
}