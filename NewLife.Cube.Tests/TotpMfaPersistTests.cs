using System;
using NewLife.Caching;
using NewLife.Cube.Services;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>MFA 持久化：TotpMfaService 写入 User.Ex4/Ex5/Ex6，FindByID 后仍可 IsEnabled</summary>
public class TotpMfaPersistTests
{
    public TotpMfaPersistTests()
    {
        DAL.AddConnStr("Membership", "Data Source=OscMfaPersist;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Fact(DisplayName = "IsEnabled 在 Update+FindByID 后仍为 true（Ex5 落库）")]
    public void IsEnabled_Survives_Reload()
    {
        var user = new User
        {
            Name = "mfa_" + Guid.NewGuid().ToString("N")[..8],
            Enable = true,
        };
        user.Insert();

        var provider = new CacheProvider { Cache = new MemoryCache() };
        var mfa = new TotpMfaService(provider);
        mfa.SetupTotp(user, "CubeTest");
        Assert.False(user.Ex4.IsNullOrEmpty());

        // 绕过 TOTP：直接激活标记并落库
        user.Ex5 = "true";
        user.Update();

        var reloaded = User.FindByID(user.ID);
        Assert.NotNull(reloaded);
        Assert.False(reloaded!.Ex4.IsNullOrEmpty());
        Assert.Equal("true", reloaded.Ex5);
        Assert.True(mfa.IsEnabled(reloaded));
    }

    private sealed class CacheProvider : ICacheProvider
    {
        public ICache Cache { get; set; } = null!;
        public ICache InnerCache { get; set; } = null!;
        public IProducerConsumer<T> GetQueue<T>(String topic, String group = null) => Cache.GetQueue<T>(topic);
        public IProducerConsumer<T> GetInnerQueue<T>(String topic) => Cache.GetQueue<T>(topic);
        public IDisposable AcquireLock(String key, Int32 msTimeout) => Cache.AcquireLock(key, msTimeout);
    }
}
