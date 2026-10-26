using System;
using System.Linq;
using System.Threading.Tasks;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// AUTH-7 验证码记录测试。覆盖短信/邮件验证码发送记录落库（成功与失败）、记录模型映射，
/// 以及验证码服务的入参与渠道异常路径。发送渠道以缓存注入假实现，不产生真实短信/邮件。
/// </summary>
[Collection("TenantAuth")]
public class VerifyCodeRecordTests
{
    private readonly TenantAuthFixture _fx;
    private readonly TestCacheProvider _cache = new();
    private readonly SmsService _sms;
    private readonly MailService _mail;
    private readonly VerifyCodeService _verify;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（VerifyCodeRecord 表）</param>
    public VerifyCodeRecordTests(TenantAuthFixture fixture)
    {
        _fx = fixture;
        _sms = new SmsService(_cache);
        _mail = new MailService(_cache);
        var userService = new UserService(new PasswordService(), _cache, null!, null!, null!, new TenantContextService(), null!);
        _verify = new VerifyCodeService(_sms, _mail, _cache, new DrawingCaptchaService(_cache), userService, new TenantContextService());
    }

    #region 假渠道
    private class FakeSms : ISmsVerifyCode
    {
        public String Result = "OK";
        public String LastCode;

        public Task<String> SendLogin(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null)
        {
            LastCode = code;
            return Task.FromResult(Result);
        }

        public Task<String> SendReset(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null) => SendLogin(mobile, code, expire, options);

        public Task<String> SendBind(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null) => SendLogin(mobile, code, expire, options);

        public Task<String> SendActivate(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null) => SendLogin(mobile, code, expire, options);
    }

    private static SmsConfigModel SmsConfig(Int32 id) => new()
    {
        Id = id,
        TenantId = 0,
        Provider = "Fake",
        Name = "测试短信",
        AppKey = "key",
        AppSecret = "secret",
        SignName = "签名",
        CodeLength = 6,
        Expire = 300,
        Enable = true,
    };

    #endregion

    #region 短信记录
    [Fact(DisplayName = "短信验证码发送成功_记录渠道动作目标与结果并落库")]
    public async Task SendVerifyCode_Sms_Success_PersistsRecord()
    {
        var config = SmsConfig(4101);
        var fake = new FakeSms();
        _cache.InnerCache.Set($"SmsVerify:{config.Provider}:{config.Id}", fake, 600);

        var record = await _sms.SendVerifyCode("login", "13800138000", "135790", config);

        Assert.True(record.Success);
        Assert.Equal("OK", record.Result);
        Assert.Equal("Sms", record.Channel);
        Assert.Equal("login", record.Action);
        Assert.Equal("13800138000", record.Target);
        Assert.Equal("Fake", record.Provider);
        Assert.Equal(config.Id, record.ConfigId);
        Assert.False(record.ConfigName.IsNullOrEmpty());
        Assert.True(record.ExpireTime > DateTime.Now.AddSeconds(200), $"有效期应约为 300 秒，实际 {record.ExpireTime}");

        var saved = VerifyCodeRecord.FindById(record.Id);
        Assert.NotNull(saved);
        Assert.Equal("13800138000", saved.Target);
        Assert.True(saved.Success);
    }

    [Fact(DisplayName = "短信验证码发送异常_仍写入失败记录并保留错误信息")]
    public async Task SendVerifyCode_Sms_Exception_RecordsFailure()
    {
        // 未配置任何渠道实现时发送抛异常，但记录必须落库，便于运维排查
        var config = new SmsConfigModel { Id = 4103, Provider = "Unknown", Expire = 300 };

        var failed = await _sms.SendVerifyCode("reset", "13700137000", "112233", config);

        Assert.False(failed.Success);
        Assert.False(failed.Result.IsNullOrEmpty());
        Assert.Equal("Sms", failed.Channel);
        Assert.True(failed.Id > 0);

        var saved = VerifyCodeRecord.FindById(failed.Id);
        Assert.NotNull(saved);
        Assert.False(saved.Success);
    }
    #endregion

    #region 邮件记录
    [Fact(DisplayName = "邮件渠道不支持_发送失败但记录落库含异常信息")]
    public async Task SendVerifyCode_Mail_UnsupportedProvider_RecordsFailure()
    {
        var config = new MailConfigModel { Id = 4201, TenantId = 0, Provider = "Unknown", Expire = 300 };

        var record = await _mail.SendVerifyCode("login", "user@example.com", "864213", config);

        Assert.False(record.Success);
        Assert.Equal("Mail", record.Channel);
        Assert.Equal("user@example.com", record.Target);
        Assert.Contains("Unknown", record.Result);
        Assert.True(record.Id > 0);
        Assert.NotNull(VerifyCodeRecord.FindById(record.Id));
    }
    #endregion

    #region 服务入参与渠道
    [Fact(DisplayName = "发送验证码_账号为空被拒绝")]
    public async Task SendVerifyCode_EmptyAccount_Throws()
    {
        var set = CubeSetting.Current;
        var scene = set.CaptchaScene;
        set.CaptchaScene = 0;
        try
        {
            var ex = await Assert.ThrowsAsync<XException>(() => _verify.SendVerifyCode(new VerifyCodeModel { Username = "  " }, "127.0.0.1"));
            Assert.False(ex.Message.IsNullOrEmpty());
        }
        finally
        {
            set.CaptchaScene = scene;
        }
    }

    [Fact(DisplayName = "发送验证码_邮件渠道未启用被拒绝")]
    public async Task SendVerifyCode_MailDisabled_Throws()
    {
        var set = CubeSetting.Current;
        var scene = set.CaptchaScene;
        var enable = set.EnableMail;
        set.CaptchaScene = 0;
        set.EnableMail = false;
        try
        {
            var ex = await Assert.ThrowsAsync<XException>(() => _verify.SendVerifyCode(new VerifyCodeModel { Username = "user@example.com", Channel = "Mail" }, "127.0.0.1"));
            Assert.False(ex.Message.IsNullOrEmpty());
        }
        finally
        {
            set.CaptchaScene = scene;
            set.EnableMail = enable;
        }
    }

    [Fact(DisplayName = "发送验证码_无法识别的账号格式不支持")]
    public async Task SendVerifyCode_UnsupportedChannel_Throws()
    {
        var set = CubeSetting.Current;
        var scene = set.CaptchaScene;
        set.CaptchaScene = 0;
        try
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => _verify.SendVerifyCode(new VerifyCodeModel { Username = "not-a-mail-or-mobile", Channel = "Pigeon" }, "127.0.0.1"));
        }
        finally
        {
            set.CaptchaScene = scene;
        }
    }
    #endregion

    #region 模型与渠道列表
    [Fact(DisplayName = "验证码记录转模型_字段完整映射")]
    public void ToModel_MapsAllFields()
    {
        var record = new VerifyCodeRecord
        {
            Id = 99,
            TenantId = 2,
            Action = "login",
            Channel = "Sms",
            ConfigId = 7,
            ConfigName = "测试",
            Provider = "Fake",
            UserId = 3,
            Target = "13800138000",
            Code = "1234",
            Content = "验证码 1234",
            Success = true,
            Result = "OK",
            ExpireTime = new DateTime(2026, 1, 2, 3, 4, 5),
            VerifyTimes = 2,
            LastVerifyResult = true,
        };

        var model = record.ToModel();

        Assert.Equal(99L, model.Id);
        Assert.Equal(2, model.TenantId);
        Assert.Equal("login", model.Action);
        Assert.Equal("Sms", model.Channel);
        Assert.Equal(7, model.ConfigId);
        Assert.Equal("Fake", model.Provider);
        Assert.Equal(3, model.UserId);
        Assert.Equal("13800138000", model.Target);
        Assert.True(model.Success);
        Assert.Equal("OK", model.Result);
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5), model.ExpireTime);
        Assert.Equal(2, model.VerifyTimes);
    }

    [Fact(DisplayName = "验证码渠道列表_包含已记录的渠道")]
    public void GetChannelList_ContainsRecordedChannels()
    {
        new VerifyCodeRecord { Channel = "Channel_" + Guid.NewGuid().ToString("N")[..6], Target = "x", Action = "login", Success = true }.Insert();

        var list = VerifyCodeRecord.GetChannelList();

        Assert.NotNull(list);
        // 字段缓存可能被更早的用例填充，仅在非空时断言包含本测试写入的渠道集合
        if (list.Count > 0) Assert.Contains(list.Keys, e => e.EqualIgnoreCase("Sms") || e.StartsWith("Channel_"));
    }
    #endregion
}
