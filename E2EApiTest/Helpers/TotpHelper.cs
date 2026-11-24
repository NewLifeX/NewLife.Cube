using System;
using System.Security.Cryptography;
using System.Text;

namespace E2EApiTest.Helpers;

/// <summary>RFC 6238 TOTP 生成器，用于在 E2E 中模拟 Authenticator App 的动态码</summary>
public static class TotpHelper
{
    private const String Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>按当前时间生成 6 位验证码</summary>
    /// <param name="base32Secret">Base32 密钥</param>
    /// <param name="offsetSteps">相对当前时间步长的偏移（±1 可模拟时钟偏差）</param>
    public static String Compute(String base32Secret, Int32 offsetSteps = 0)
    {
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30 + offsetSteps;
        return ComputeAt(base32Secret, step);
    }

    /// <summary>按指定时间步长生成 6 位验证码</summary>
    /// <param name="base32Secret">Base32 密钥</param>
    /// <param name="step">时间步长序号（Unix 秒 / 30）</param>
    public static String ComputeAt(String base32Secret, Int64 step)
    {
        var key = FromBase32(base32Secret);

        var bytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(bytes);

        var offset = hash[^1] & 0x0F;
        var code = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (code % 1_000_000).ToString("D6");
    }

    /// <summary>生成一个当前时间窗口内必定错误的 6 位码</summary>
    /// <param name="base32Secret">Base32 密钥</param>
    public static String WrongCode(String base32Secret)
    {
        var valid = Compute(base32Secret);
        for (var i = 0; i < 10; i++)
        {
            var candidate = Random.Shared.Next(0, 1_000_000).ToString("D6");
            if (candidate != valid) return candidate;
        }

        return valid == "000000" ? "111111" : "000000";
    }

    private static Byte[] FromBase32(String text)
    {
        var output = new Byte[text.Length * 5 / 8];
        var buffer = 0;
        var bitsLeft = 0;
        var index = 0;
        foreach (var ch in text.TrimEnd('=').ToUpperInvariant())
        {
            var value = Base32Chars.IndexOf(ch);
            if (value < 0) throw new FormatException($"非法的 Base32 字符：{ch}");

            buffer = (buffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output[index++] = (Byte)(buffer >> (bitsLeft - 8));
                bitsLeft -= 8;
            }
        }

        return output[..index];
    }
}
