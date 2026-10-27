using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>定时作业后台调度测试。验证 JobService 真正按 Cron 触发已注册作业、跳过停用作业、单个作业配置错误不影响其它作业</summary>
[Collection("TenantAuth")]
public class CronJobServiceTests
{
    private static Int32 _ticks;
    private static Int32 _otherTicks;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public CronJobServiceTests(TenantAuthFixture fixture) { }

    #region 辅助
    private static String Unique(String prefix) => prefix + "_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>被调度的测试方法，累加执行次数</summary>
    /// <param name="argument">作业参数</param>
    public static void Tick(String argument) => Interlocked.Increment(ref _ticks);

    /// <summary>被调度的第二个测试方法，用于验证错误作业不影响其它作业</summary>
    /// <param name="argument">作业参数</param>
    public static void TickOther(String argument) => Interlocked.Increment(ref _otherTicks);

    private static CronJob AddJob(String name, String method, Boolean enable = true)
    {
        var job = new CronJob
        {
            Name = name,
            // 每秒触发一次，缩短测试等待
            Cron = "* * * * * ?",
            Method = method,
            Enable = enable,
            NextTime = DateTime.Now.AddSeconds(1),
        };
        job.Insert();

        return job;
    }

    private static JobService CreateService()
    {
        var provider = new ServiceCollection().BuildServiceProvider();

        return new JobService(provider, null);
    }

    /// <summary>轮询等待条件成立</summary>
    /// <param name="condition">判定条件</param>
    /// <param name="timeout">超时时间</param>
    /// <returns>条件成立返回 true，超时返回 false</returns>
    private static async Task<Boolean> WaitUntilAsync(Func<Boolean> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition()) return true;
            await Task.Delay(200);
        }

        return condition();
    }
    #endregion

    [Fact(DisplayName = "定时作业_后台调度按Cron真正触发启用作业")]
    public async Task JobService_EnabledJob_ExecutesByTimer()
    {
        Interlocked.Exchange(ref _ticks, 0);
        var job = AddJob(Unique("Tick"), $"{typeof(CronJobServiceTests).FullName}.{nameof(Tick)}");
        var svc = CreateService();
        try
        {
            await svc.StartAsync(CancellationToken.None);

            var fired = await WaitUntilAsync(() => Volatile.Read(ref _ticks) > 0, TimeSpan.FromSeconds(30));
            Assert.True(fired, "启用作业在 30 秒内未被后台调度执行");
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
            job.Delete();
        }
    }

    [Fact(DisplayName = "定时作业_停用作业不被后台调度执行")]
    public async Task JobService_DisabledJob_NotExecuted()
    {
        Interlocked.Exchange(ref _ticks, 0);
        var job = AddJob(Unique("Off"), $"{typeof(CronJobServiceTests).FullName}.{nameof(Tick)}", false);
        var svc = CreateService();
        try
        {
            await svc.StartAsync(CancellationToken.None);

            await Task.Delay(4_000);
            Assert.Equal(0, Volatile.Read(ref _ticks));
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
            job.Delete();
        }
    }

    [Fact(DisplayName = "定时作业_单个作业配置错误不影响其它作业调度")]
    public async Task JobService_BrokenJob_DoesNotBlockOthers()
    {
        Interlocked.Exchange(ref _ticks, 0);
        Interlocked.Exchange(ref _otherTicks, 0);
        var broken = AddJob(Unique("Broken"), "No.Such.JobType.Noop");
        var good = AddJob(Unique("Good"), $"{typeof(CronJobServiceTests).FullName}.{nameof(TickOther)}");
        var svc = CreateService();
        try
        {
            await svc.StartAsync(CancellationToken.None);

            var fired = await WaitUntilAsync(() => Volatile.Read(ref _otherTicks) > 0, TimeSpan.FromSeconds(30));
            Assert.True(fired, "错误作业存在时，正常作业仍未被执行");
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
            broken.Delete();
            good.Delete();
        }
    }
}
