using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NewLife.Caching;
using NewLife.Cube.Services;
using Xunit;

namespace XUnitTest;

/// <summary>Challenge-Response 密码安全（RSA-OAEP）相关独立逻辑单元测试</summary>
public class AuthControllerTests
{
    #region RSA-OAEP 加解密往返

    [Fact(DisplayName = "RSA-OAEP 加密后可被对应私钥解密还原")]
    public void RsaOaepEncryptDecrypt_RoundTrip()
    {
        using var rsa = RSA.Create(2048);
        var pubKey = rsa.ExportSubjectPublicKeyInfo();
        var original = "P@ssw0rd!123";

        // 用公钥加密（模拟前端 Web Crypto API RSA-OAEP/SHA-256）
        var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(original), RSAEncryptionPadding.OaepSHA256);
        // 用私钥解密（模拟服务端 DecryptOAEP）
        var decrypted = rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);

        Assert.Equal(original, Encoding.UTF8.GetString(decrypted));
    }

    [Fact(DisplayName = "RSA-OAEP 每次加密不同密文（随机填充）")]
    public void RsaOaepEncrypt_NonDeterministic()
    {
        using var rsa = RSA.Create(2048);
        var plain = "same_password";

        var c1 = rsa.Encrypt(Encoding.UTF8.GetBytes(plain), RSAEncryptionPadding.OaepSHA256);
        var c2 = rsa.Encrypt(Encoding.UTF8.GetBytes(plain), RSAEncryptionPadding.OaepSHA256);

        // 每次加密结果不同，防止重放攻击检测
        Assert.NotEqual(c1, c2);
    }

    [Fact(DisplayName = "PEM SPKI 导出/导入公钥往返验证")]
    public void PemSpkiExportImport_RoundTrip()
    {
        using var rsa = RSA.Create(2048);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var pem = "-----BEGIN PUBLIC KEY-----\n"
            + Convert.ToBase64String(spki, Base64FormattingOptions.InsertLineBreaks)
            + "\n-----END PUBLIC KEY-----";

        // 模拟前端从 PEM 解析 SPKI
        var base64 = pem
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\n", "").Replace("\r", "");
        var der = Convert.FromBase64String(base64);

        using var rsa2 = RSA.Create();
        rsa2.ImportSubjectPublicKeyInfo(der, out var read);
        Assert.True(read > 0);

        // 加密后用原始私钥解密，验证公钥等价
        var plain = "验证公钥导入";
        var encrypted = rsa2.Encrypt(Encoding.UTF8.GetBytes(plain), RSAEncryptionPadding.OaepSHA256);
        var decrypted = rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
        Assert.Equal(plain, Encoding.UTF8.GetString(decrypted));
    }

    #endregion

    #region 账号格式校验（ResetPassword 前置逻辑的正则规则验证）

    [Theory(DisplayName = "邮箱格式校验")]
    [InlineData("user@example.com", true)]
    [InlineData("name.surname+tag@sub.domain.org", true)]
    [InlineData("notanemail", false)]
    [InlineData("missing@", false)]
    [InlineData("", false)]
    public void IsEmail_FormatValidation(String input, Boolean expected)
    {
        var result = !String.IsNullOrWhiteSpace(input)
            && Regex.IsMatch(input, @"\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}");
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "手机号格式校验（大陆11位）")]
    [InlineData("13800138000", true)]
    [InlineData("18612345678", true)]
    [InlineData("12345678901", false)]
    [InlineData("1381234567", false)]
    [InlineData("", false)]
    public void IsMobile_FormatValidation(String input, Boolean expected)
    {
        var result = !String.IsNullOrWhiteSpace(input)
            && Regex.IsMatch(input, @"^1[3-9]\d{9}$");
        Assert.Equal(expected, result);
    }

    #endregion

    #region PasswordStrength（ResetPassword 密码强度校验前置）

    [Theory(DisplayName = "密码强度：弱密码被拒绝")]
    [InlineData("123456")]
    [InlineData("password")]
    [InlineData("aaa")]
    public void WeakPassword_IsRejectedByRegex(String pwd)
    {
        // 简单强度规则：至少8位、含大小写和数字
        var ok = IsStrongPassword(pwd);
        Assert.False(ok);
    }

    [Theory(DisplayName = "密码强度：强密码通过")]
    [InlineData("P@ssw0rd!1")]
    [InlineData("Abc12345!")]
    [InlineData("NewLife#2025")]
    public void StrongPassword_Passes(String pwd)
    {
        var ok = IsStrongPassword(pwd);
        Assert.True(ok);
    }

    /// <summary>简化版密码强度校验（至少8位、含大写字母、小写字母、数字三类中的两类）</summary>
    private static Boolean IsStrongPassword(String pwd)
    {
        if (pwd == null || pwd.Length < 8) return false;
        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        foreach (var c in pwd)
        {
            if (Char.IsUpper(c)) hasUpper = true;
            else if (Char.IsLower(c)) hasLower = true;
            else if (Char.IsDigit(c)) hasDigit = true;
        }
        var score = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0);
        return score >= 2;
    }

    #endregion

    #region 登录挑战（Challenge）无效处理

    /// <summary>简单内存缓存提供者，用于单元测试隔离</summary>
    private class MemoryCacheProvider : ICacheProvider
    {
        public ICache Cache { get; set; }
        public ICache InnerCache { get; set; }

        public MemoryCacheProvider()
        {
            var cache = new MemoryCache();
            Cache = cache;
            InnerCache = cache;
        }

        public IProducerConsumer<T> GetQueue<T>(String name, String? topic = null)
            => throw new NotImplementedException();

        public IProducerConsumer<T> GetInnerQueue<T>(String name)
            => throw new NotImplementedException();

        public IDisposable AcquireLock(String name, Int32 msTimeout)
            => throw new NotImplementedException();
    }

    [Fact(DisplayName = "GetPublicKey：每次调用生成不同 challengeId 与 PEM 公钥（提交时动态获取保证新鲜密钥）")]
    public void GetPublicKey_GeneratesUniqueChallengeId()
    {
        var provider = new MemoryCacheProvider();
        var svc = new UserService(null!, provider, null!, null!, null!, null!);

        var (challengeId1, publicKey1) = svc.GetPublicKey();
        var (challengeId2, publicKey2) = svc.GetPublicKey();

        Assert.NotEqual(challengeId1, challengeId2);
        Assert.False(String.IsNullOrEmpty(publicKey1));
        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", publicKey1);
        Assert.NotEqual(publicKey1, publicKey2);
    }

    [Fact(DisplayName = "GetPublicKey：PEM 公钥 OAEP 加密后，缓存的 XML 私钥可解密还原（WebAPI 链路）")]
    public void GetPublicKey_OaepEncrypt_XmlPrivateKeyDecrypt_RoundTrip()
    {
        var provider = new MemoryCacheProvider();
        var svc = new UserService(null!, provider, null!, null!, null!, null!);

        // 1. 获取挑战：返回 PEM(SPKI) 公钥，缓存 XML 私钥
        var (challengeId, publicKey) = svc.GetPublicKey();

        // 2. 用 PEM 公钥以 RSA-OAEP/SHA-256 加密密码（模拟前端 Web Crypto）
        var pem = publicKey
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "");
        using var pubRsa = RSA.Create();
        pubRsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(pem), out _);
        var plain = "P@ssw0rd!123";
        var cipher = Convert.ToBase64String(
            pubRsa.Encrypt(Encoding.UTF8.GetBytes(plain), RSAEncryptionPadding.OaepSHA256));

        // 3. 从缓存取 XML 私钥，反射调 DecryptByPrivateKey 解密（应走 OAEP 分支）
        var tuple = provider.Cache.Get<Tuple<String, String>>(challengeId);
        Assert.NotNull(tuple);

        var method = typeof(UserService).GetMethod("DecryptByPrivateKey", BindingFlags.NonPublic | BindingFlags.Static)!;
        var decrypted = (String)method.Invoke(null, [tuple!.Item2, cipher])!;

        Assert.Equal(plain, decrypted);
    }

    [Fact(DisplayName = "DecryptByPrivateKey：PEM 私钥走 PKCS1v15 解密（MVC JSEncrypt 链路）")]
    public void DecryptByPrivateKey_PemPrivateKey_Pkcs1v15()
    {
        using var rsa = RSA.Create(2048);
        var pemPrivate = rsa.ExportPkcs8PrivateKeyPem(); // -----BEGIN PRIVATE KEY-----

        var plain = "P@ssw0rd!123";
        var cipher = Convert.ToBase64String(
            rsa.Encrypt(Encoding.UTF8.GetBytes(plain), RSAEncryptionPadding.Pkcs1));

        var method = typeof(UserService).GetMethod("DecryptByPrivateKey", BindingFlags.NonPublic | BindingFlags.Static)!;
        var decrypted = (String)method.Invoke(null, [pemPrivate, cipher])!;

        Assert.Equal(plain, decrypted);
    }

    #endregion
}
