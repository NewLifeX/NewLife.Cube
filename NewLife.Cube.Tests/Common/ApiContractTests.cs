using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using NewLife.Cube;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Enums;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// API 契约测试。覆盖认证分类的字符串枚举契约（CON-01）、MFA 结构化业务码（CON-04），
/// 以及界面语言配置即锁定（I18N-01）。
/// </summary>
[Collection("TenantAuth")]
public class ApiContractTests
{
    private const String Password = "Test@12345";

    private static readonly JsonSerializerOptions _readOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions _writeOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具，复用其 SQLite 库与登录管线依赖</param>
    public ApiContractTests(TenantAuthFixture fixture) => _fx = fixture;

    #region CON-01 枚举契约
    [Fact(DisplayName = "认证分类_字符串入参可绑定")]
    public void AuthCategory_BindsFromString()
    {
        var model = JsonSerializer.Deserialize<LoginModel>("""{"category":"mobile","username":"13800000000"}""", _readOptions);
        Assert.NotNull(model);
        Assert.Equal(AuthCategory.Mobile, model!.Category);

        var register = JsonSerializer.Deserialize<AuthRegisterModel>("""{"category":"Mail","mobile":"a@b.com"}""", _readOptions);
        Assert.NotNull(register);
        Assert.Equal(AuthCategory.Mail, register!.Category);
    }

    [Fact(DisplayName = "认证分类_数字入参保持兼容")]
    public void AuthCategory_BindsFromNumber()
    {
        var model = JsonSerializer.Deserialize<LoginModel>("""{"category":1}""", _readOptions);
        Assert.NotNull(model);
        Assert.Equal(AuthCategory.Mobile, model!.Category);
    }

    [Fact(DisplayName = "认证分类_输出为字符串")]
    public void AuthCategory_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(new LoginModel { Category = AuthCategory.OAuth }, _writeOptions);
        Assert.Contains("\"OAuth\"", json);
    }
    #endregion

    #region CON-04 MFA 结构化
    [Fact(DisplayName = "MFA业务码_取值为负七且在保留段")]
    public void MfaCode_DefinedInReservedRange()
    {
        Assert.Equal(-7, CubeCode.MfaRequired.ToInt());

        var json = JsonSerializer.Serialize(new LoginTokenModel { MfaToken = "pending-1" }, _writeOptions);
        Assert.Contains("\"mfaToken\":\"pending-1\"", json);
    }

    [Fact(DisplayName = "登录_已开启MFA_返回挂起令牌")]
    public void Login_WithMfa_ReturnsPendingToken()
    {
        var set = CubeSetting.Current;
        var prev = set.EnableMfa;
        set.EnableMfa = true;
        try
        {
            var cache = new TestCacheProvider();
            var mfa = new TotpMfaService(cache);
            var svc = new UserService(new PasswordService(), cache, mfa, null!, null!, new TenantContextService(), null!);

            var user = new User
            {
                Name = "mfac_" + Guid.NewGuid().ToString("N")[..8],
                DisplayName = "MFA 契约测试用户",
                Password = _fx.Provider.PasswordProvider.Hash(Password),
                RoleID = _fx.LegacyUser.RoleID,
                Enable = true,
                RegisterTime = DateTime.Now,
            };
            user.Insert();

            var setup = mfa.SetupTotp(user, "NewLife.Cube");
            mfa.ActivateTotp(user, ComputeCode(setup.Secret));
            Assert.True(mfa.IsEnabled(user));

            // 回归（MFA 持久化）：重新加载的用户仍应识别为已开启，否则登录永远不会拦到二步验证
            var reloaded = User.FindByName(user.Name);
            Assert.NotNull(reloaded);
            Assert.True(mfa.IsEnabled(reloaded!));

            var ctx = _fx.CreateContext();
            var rs = svc.Login(new LoginModel { Username = user.Name, Password = Password }, ctx);

            // MFA 拦截：不下发正式令牌，返回挂起令牌，由控制器映射为 CubeCode.MfaRequired
            Assert.True(rs.IsSuccess);
            Assert.Equal("mfa_required", rs.Message);
            Assert.False(rs.MfaToken.IsNullOrEmpty());
            Assert.Null(rs.Data);
        }
        finally
        {
            set.EnableMfa = prev;
        }
    }
    #endregion

    #region I18N-01 语言锁定
    [Fact(DisplayName = "锁定语言_配置受支持语言即生效")]
    public void ForcedLanguage_WhenConfigured()
    {
        var cfg = NewLife.Setting.Current;
        var oldLanguage = cfg.Language;
        try
        {
            cfg.Language = null;
            Assert.Null(CubeLocalization.GetForcedLanguage());

            cfg.Language = "zh-Hans";
            Assert.Equal(CubeLocalization.NormalizeCulture("zh-Hans"), CubeLocalization.GetForcedLanguage());

            // 配置了不受支持的语言时视为自动，回到请求语言来源
            cfg.Language = "xx-Unknown";
            Assert.Null(CubeLocalization.GetForcedLanguage());
        }
        finally
        {
            cfg.Language = oldLanguage;
        }
    }
    #endregion

    #region 辅助
    private static readonly Char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    /// <summary>测试侧独立实现的 RFC 6238 TOTP 计算</summary>
    private static String ComputeCode(String base32Secret)
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

        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
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
    #endregion
}
