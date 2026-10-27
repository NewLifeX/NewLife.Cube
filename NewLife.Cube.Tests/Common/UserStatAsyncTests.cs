using System;
using System.Linq;
using System.Threading;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS-12 用户统计异步落库测试。覆盖登录路径使用的 UserStat 递增 + SaveAsync 异步保存能被读到，
/// 以及同一天重复取用返回同一行。
/// </summary>
[Collection("TenantAuth")]
public class UserStatAsyncTests
{
    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public UserStatAsyncTests(TenantAuthFixture fixture) { }

    #region 辅助
    /// <summary>轮询等待异步落库完成</summary>
    private static T WaitFor<T>(Func<T> read, Func<T, Boolean> ok, Int32 timeoutMs = 8000)
    {
        var end = Environment.TickCount64 + timeoutMs;
        var value = read();
        while (!ok(value) && Environment.TickCount64 < end)
        {
            Thread.Sleep(100);
            value = read();
        }

        return value;
    }
    #endregion

    [Fact(DisplayName = "用户统计_登录计数异步落库后可读到")]
    public void UserStat_SaveAsync_PersistsLogins()
    {
        var stat = UserStat.GetOrAdd(DateTime.Today);
        Assert.NotNull(stat);

        var before = stat!.Logins;
        stat.Logins = before + 1;
        stat.OAuths++;
        stat.SaveAsync(5_000);

        // SaveAsync 走后台队列，轮询到计数落库
        var found = WaitFor(() => UserStat.FindByDate(DateTime.Today), e => e != null && e.Logins >= before + 1);
        Assert.NotNull(found);
        Assert.True(found!.Logins >= before + 1);
    }

    [Fact(DisplayName = "用户统计_同一天重复取用返回同一行")]
    public void UserStat_GetOrAdd_SameDaySameRow()
    {
        var first = UserStat.GetOrAdd(DateTime.Today);
        var second = UserStat.GetOrAdd(DateTime.Today);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ID, second!.ID);
        Assert.Equal(1, UserStat.FindAllByDate(DateTime.Today).Count(e => e.ID == first.ID));
    }
}
