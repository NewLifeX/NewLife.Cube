using System.Security.Cryptography;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Security;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>内置 TOTP MFA 服务。遵循 RFC 6238，与 Google/Microsoft Authenticator 兼容，零外部依赖</summary>
/// <remarks>
/// MFA 数据全部存储在 User.Extends 字典中，key 约定如下：
/// - <c>MfaTotpSecret</c>：Base32 格式的 TOTP 密钥（仅未激活时存在）
/// - <c>MfaEnabled</c>：Boolean，true 表示已激活
/// - <c>MfaBackupCodes</c>：JSON 数组，存储最多 10 个一次性备用码（明文，一经使用立即删除）
/// </remarks>
public class TotpMfaService : IMfaService
{
    private const String ExtSecret = "MfaTotpSecret";
    private const String ExtEnabled = "MfaEnabled";
    private const String ExtBackupCodes = "MfaBackupCodes";

    // MFA 挂起令牌的缓存前缀与 TTL（秒）
    private const String MfaPendingPrefix = "MfaPending:";
    private const Int32 MfaPendingTtl = 300;

    private readonly ICache _cache;

    /// <summary>初始化</summary>
    /// <param name="cacheProvider">缓存提供者</param>
    public TotpMfaService(ICacheProvider cacheProvider) => _cache = cacheProvider.Cache;

    #region 接口实现

    /// <inheritdoc/>
    public MfaSetupResult SetupTotp(IUser user, String issuer)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var secret = GenerateBase32Secret();
        // 写入 Extends，等待激活；激活前不设置 MfaEnabled
        SetExtend(user, ExtSecret, secret);
        SaveExtends(user);

        var label = Uri.EscapeDataString($"{issuer}:{user.Name}");
        var iss = Uri.EscapeDataString(issuer);
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={iss}&algorithm=SHA1&digits=6&period=30";

        return new MfaSetupResult { TotpUri = uri, Secret = secret };
    }

    /// <inheritdoc/>
    public IReadOnlyList<String> ActivateTotp(IUser user, String code)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var secret = GetExtend(user, ExtSecret);
        if (secret.IsNullOrEmpty()) throw new InvalidOperationException("请先调用 Setup 初始化 MFA");

        if (!VerifyTotp(secret, code))
            throw new InvalidOperationException("验证码错误，请确认 Authenticator App 时间已同步");

        // 激活：生成备用码
        var backupCodes = GenerateBackupCodes(10);
        SetExtend(user, ExtEnabled, "true");
        SetExtend(user, ExtBackupCodes, backupCodes.Join(","));
        SaveExtends(user);

        return backupCodes;
    }

    /// <inheritdoc/>
    public void DisableMfa(IUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        RemoveExtend(user, ExtSecret);
        RemoveExtend(user, ExtEnabled);
        RemoveExtend(user, ExtBackupCodes);
        SaveExtends(user);
    }

    /// <inheritdoc/>
    public Boolean Verify(IUser user, String code)
    {
        if (user == null || code.IsNullOrEmpty()) return false;
        if (!IsEnabled(user)) return false;

        var secret = GetExtend(user, ExtSecret);
        if (secret.IsNullOrEmpty()) return false;

        // 优先尝试 TOTP 6 位码
        if (VerifyTotp(secret, code)) return true;

        // 尝试一次性备用码
        return VerifyBackupCode(user, code);
    }

    /// <inheritdoc/>
    public Boolean IsEnabled(IUser user)
    {
        if (user == null) return false;
        return GetExtend(user, ExtEnabled).EqualIgnoreCase("true");
    }

    #endregion

    #region MFA 挂起令牌（供 UserService 使用）

    /// <summary>颁发 MFA 挂起令牌（登录校验通过账密但 MFA 尚未验证时调用）</summary>
    /// <param name="userId">用户 ID</param>
    /// <returns>不透明令牌字符串，TTL 300 秒</returns>
    public String IssuePendingToken(Int32 userId)
    {
        var token = Guid.NewGuid().ToString("N");
        _cache.Set($"{MfaPendingPrefix}{token}", userId, MfaPendingTtl);
        return token;
    }

    /// <summary>从挂起令牌中解析用户 ID，解析成功后立即删除令牌（防重放）</summary>
    /// <param name="mfaToken">挂起令牌</param>
    /// <returns>用户 ID；令牌无效或已过期返回 0</returns>
    public Int32 ConsumePendingToken(String mfaToken)
    {
        if (mfaToken.IsNullOrEmpty()) return 0;
        var key = $"{MfaPendingPrefix}{mfaToken}";
        var userId = _cache.Get<Int32>(key);
        if (userId > 0) _cache.Remove(key);
        return userId;
    }

    #endregion

    #region 辅助

    /// <summary>生成 20 字节随机 Base32 密钥（160 bit，符合 RFC 6238 推荐强度）</summary>
    private static String GenerateBase32Secret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return ToBase32(bytes);
    }

    /// <summary>生成指定数量的一次性备用码（每个 10 位全大写数字）</summary>
    private static List<String> GenerateBackupCodes(Int32 count)
    {
        var codes = new List<String>(count);
        for (var i = 0; i < count; i++)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000_0).ToString("D7")
                + RandomNumberGenerator.GetInt32(0, 1000).ToString("D3");
            codes.Add(code);
        }
        return codes;
    }

    /// <summary>RFC 6238 TOTP 校验（时间窗口 ±1 步长，30 秒步长）</summary>
    private static Boolean VerifyTotp(String base32Secret, String code)
    {
        if (code.IsNullOrEmpty() || code.Length != 6) return false;
        if (!Int32.TryParse(code, out var inputCode)) return false;

        var keyBytes = FromBase32(base32Secret);
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // 允许前后各 1 步（±30 秒）偏差
        for (var drift = -1; drift <= 1; drift++)
        {
            if (ComputeTotp(keyBytes, step + drift) == inputCode) return true;
        }
        return false;
    }

    /// <summary>计算指定时间步长的 TOTP 值（RFC 6238 HMAC-SHA1）</summary>
    private static Int32 ComputeTotp(Byte[] key, Int64 step)
    {
        var stepBytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian) Array.Reverse(stepBytes);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(stepBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);
        return binary % 1_000_000;
    }

    /// <summary>校验并消费一次性备用码</summary>
    private Boolean VerifyBackupCode(IUser user, String code)
    {
        var stored = GetExtend(user, ExtBackupCodes);
        if (stored.IsNullOrEmpty()) return false;

        var codes = stored.Split(',').ToList();
        var idx = codes.FindIndex(c => c.Trim() == code.Trim());
        if (idx < 0) return false;

        // 删除已用备用码
        codes.RemoveAt(idx);
        SetExtend(user, ExtBackupCodes, codes.Join(","));
        SaveExtends(user);
        return true;
    }

    private static String GetExtend(IUser user, String key)
    {
        if (user is not User u) return null;
        return u[key] as String;
    }

    private static void SetExtend(IUser user, String key, String value)
    {
        if (user is User u) u[key] = value;
    }

    private static void RemoveExtend(IUser user, String key)
    {
        if (user is User u) u[key] = null;
    }

    private static void SaveExtends(IUser user)
    {
        if (user is User u) u.Update();
    }

    #region Base32 编解码（RFC 4648，无外部依赖）

    private static readonly Char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    private static String ToBase32(Byte[] data)
    {
        var sb = Pool.StringBuilder.Get();
        try
        {
            var buf = 0;
            var bitsLeft = 0;
            foreach (var b in data)
            {
                buf = (buf << 8) | b;
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    sb.Append(Base32Chars[(buf >> bitsLeft) & 0x1F]);
                }
            }
            if (bitsLeft > 0)
                sb.Append(Base32Chars[(buf << (5 - bitsLeft)) & 0x1F]);
            return sb.ToString();
        }
        finally
        {
            Pool.StringBuilder.Put(sb);
        }
    }

    private static Byte[] FromBase32(String s)
    {
        var lookup = new Int32[128];
        Array.Fill(lookup, -1);
        for (var i = 0; i < Base32Chars.Length; i++) lookup[Base32Chars[i]] = i;

        var result = new List<Byte>();
        var buf = 0;
        var bitsLeft = 0;
        foreach (var c in s.ToUpperInvariant())
        {
            if (c == '=' || c == ' ') continue;
            if (c >= 128 || lookup[c] < 0) throw new FormatException($"无效的 Base32 字符: {c}");
            buf = (buf << 5) | lookup[c];
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                result.Add((Byte)(buf >> bitsLeft));
            }
        }
        return [.. result];
    }

    #endregion

    #endregion
}
