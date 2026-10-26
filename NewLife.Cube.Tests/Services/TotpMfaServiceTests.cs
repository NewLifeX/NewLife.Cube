using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// AUTH-10 多因素认证（TOTP）测试。覆盖开通/激活/校验/禁用与挂起令牌一次性消费，
/// TOTP 验证码由测试侧独立实现 RFC 6238 计算，避免用被测代码验证自己。
/// </summary>
[Collection("TenantAuth")]
public class TotpMfaServiceTests
{
    private readonly TenantAuthFixture _fx;
    private readonly TestCacheProvider _cache = new();
    private readonly TotpMfaService _mfa;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（User 表）</param>
    public TotpMfaServiceTests(TenantAuthFixture fixture)
    {
        _fx = fixture;
        _mfa = new TotpMfaService(_cache);
    }

    #region 辅助

    private User CreateUser()
    {
        var user = new User
        {
            Name = "mfa_" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "MFA 测试用户",
            Password = "x",
            RoleID = _fx.LegacyUser.RoleID,
            Enable = true,
            RegisterTime = DateTime.Now,
        };
        user.Insert();

        return user;
    }

    private static readonly Char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    /// <summary>测试侧独立实现的 RFC 6238 TOTP 计算</summary>
    private static String ComputeCode(String base32Secret, Int32 driftSteps = 0)
    {
        var keyBytes = new List<Byte>();
        var buf = 0;
        var bitsLeft = 0;
        foreach (var c in base32Secret.ToUpperInvariant())
        {
            var idx = Array.IndexOf(Base32Chars, c);
            Assert.True(idx >= 0, $"非法 Base32 字符 {c}");
            buf = (buf << 5) | idx;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                keyBytes.Add((Byte)(buf >> bitsLeft));
            }
        }

        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30 + driftSteps;
        var stepBytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian) Array.Reverse(stepBytes);

        using var hmac = new HMACSHA1([.. keyBytes]);
        var hash = hmac.ComputeHash(stepBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D6");
    }

    /// <summary>取一个当前时间窗口内必定错误的验证码</summary>
    private static String WrongCode(String secret)
    {
        var valid = Enumerable.Range(-1, 3).Select(e => ComputeCode(secret, e)).ToHashSet();
        foreach (var code in new[] { "000000", "111111", "222222", "333333" })
        {
            if (!valid.Contains(code)) return code;
        }

        throw new InvalidOperationException("无法构造错误验证码");
    }

    #endregion

    #region 开通与激活
    [Fact(DisplayName = "开通TOTP_返回可扫描密钥且激活前不生效")]
    public void SetupTotp_ReturnsSecret_NotEnabledYet()
    {
        var user = CreateUser();

        var result = _mfa.SetupTotp(user, "NewLife.Cube");

        Assert.False(result.Secret.IsNullOrEmpty());
        Assert.StartsWith("otpauth://totp/", result.TotpUri);
        Assert.Contains($"secret={result.Secret}", result.TotpUri);
        Assert.Contains("digits=6", result.TotpUri);
        Assert.Contains("period=30", result.TotpUri);
        Assert.Contains("NewLife.Cube", Uri.UnescapeDataString(result.TotpUri));
        Assert.False(_mfa.IsEnabled(user));
        Assert.False(_mfa.Verify(user, ComputeCode(result.Secret)));
    }

    [Fact(DisplayName = "开通TOTP_未开通时激活被拒绝")]
    public void ActivateTotp_WithoutSetup_Throws()
    {
        var user = CreateUser();

        var ex = Assert.Throws<InvalidOperationException>(() => _mfa.ActivateTotp(user, "123456"));
        Assert.Contains("Setup", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "激活TOTP_错误验证码被拒绝")]
    public void ActivateTotp_WrongCode_Throws()
    {
        var user = CreateUser();
        var setup = _mfa.SetupTotp(user, "NewLife.Cube");

        Assert.Throws<InvalidOperationException>(() => _mfa.ActivateTotp(user, WrongCode(setup.Secret)));
        Assert.False(_mfa.IsEnabled(user));
    }

    [Fact(DisplayName = "激活TOTP_正确验证码启用并返回十个唯一备用码")]
    public void ActivateTotp_ValidCode_EnablesAndReturnsBackupCodes()
    {
        var user = CreateUser();
        var setup = _mfa.SetupTotp(user, "NewLife.Cube");

        var backupCodes = _mfa.ActivateTotp(user, ComputeCode(setup.Secret));

        Assert.True(_mfa.IsEnabled(user));
        Assert.Equal(10, backupCodes.Count);
        Assert.Equal(10, backupCodes.Distinct().Count());
        Assert.All(backupCodes, e => Assert.Equal(10, e.Length));
        Assert.True(_mfa.Verify(user, ComputeCode(setup.Secret)));
    }
    #endregion

    #region 校验
    [Fact(DisplayName = "校验TOTP_允许前后一个时间步的时钟漂移")]
    public void Verify_TotpCode_AllowsClockDrift()
    {
        var user = CreateUser();
        var setup = _mfa.SetupTotp(user, "NewLife.Cube");
        _mfa.ActivateTotp(user, ComputeCode(setup.Secret));

        Assert.True(_mfa.Verify(user, ComputeCode(setup.Secret, 1)));
        Assert.True(_mfa.Verify(user, ComputeCode(setup.Secret, -1)));
        Assert.False(_mfa.Verify(user, WrongCode(setup.Secret)));
        Assert.False(_mfa.Verify(user, "12345"));
        Assert.False(_mfa.Verify(user, null));
    }

    [Fact(DisplayName = "校验备用码_一次性使用后立即失效")]
    public void Verify_BackupCode_SingleUse()
    {
        var user = CreateUser();
        var setup = _mfa.SetupTotp(user, "NewLife.Cube");
        var backupCodes = _mfa.ActivateTotp(user, ComputeCode(setup.Secret));
        var code = backupCodes[3];

        Assert.True(_mfa.Verify(user, code));
        Assert.False(_mfa.Verify(user, code));

        // 其余备用码仍可用
        Assert.True(_mfa.Verify(user, backupCodes[4]));
    }

    [Fact(DisplayName = "禁用MFA_清除密钥备用码且校验不再通过")]
    public void DisableMfa_ClearsState()
    {
        var user = CreateUser();
        var setup = _mfa.SetupTotp(user, "NewLife.Cube");
        var backupCodes = _mfa.ActivateTotp(user, ComputeCode(setup.Secret));

        _mfa.DisableMfa(user);

        Assert.False(_mfa.IsEnabled(user));
        Assert.False(_mfa.Verify(user, ComputeCode(setup.Secret)));
        Assert.False(_mfa.Verify(user, backupCodes[0]));
        Assert.False(_mfa.IsEnabled(null));
        Assert.False(_mfa.Verify(null, "123456"));
    }
    #endregion

    #region 挂起令牌
    [Fact(DisplayName = "MFA挂起令牌_一次性消费且未知令牌返回零")]
    public void PendingToken_OneTimeUse()
    {
        var token = _mfa.IssuePendingToken(1234);

        Assert.False(token.IsNullOrEmpty());
        Assert.Equal(1234, _mfa.ConsumePendingToken(token));
        Assert.Equal(0, _mfa.ConsumePendingToken(token));
        Assert.Equal(0, _mfa.ConsumePendingToken("not-exists"));
        Assert.Equal(0, _mfa.ConsumePendingToken(null));
    }
    #endregion
}
