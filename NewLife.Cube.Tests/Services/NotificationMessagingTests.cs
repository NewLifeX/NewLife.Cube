using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// NOTI 消息通知测试。覆盖短信验证码生成、短信渠道动作路由与发送记录落库，
/// 以及邮件/短信渠道配置模型映射（渠道 CRUD 与选择逻辑未覆盖）。
/// </summary>
[Collection("TenantAuth")]
public class NotificationMessagingTests
{
    private readonly TestCacheProvider _cache = new();
    private readonly SmsService _sms;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（验证码记录表）</param>
    public NotificationMessagingTests(TenantAuthFixture fixture) => _sms = new SmsService(_cache);

    #region 假渠道
    private class RecordingSms : ISmsVerifyCode
    {
        public List<String> Calls { get; } = [];
        public List<String> Codes { get; } = [];
        public String Result { get; set; } = "OK";

        public Task<String> SendLogin(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null) => Record("login", code);

        public Task<String> SendReset(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null) => Record("reset", code);

        public Task<String> SendBind(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null) => Record("bind", code);

        public Task<String> SendActivate(String mobile, String code, Int32 expire, SmsVerifyCodeOptions options = null) => Record("activate", code);

        private Task<String> Record(String action, String code)
        {
            Calls.Add(action);
            Codes.Add(code);

            return Task.FromResult(Result);
        }
    }

    private static SmsConfigModel SmsConfig(Int32 id, String provider = "Fake") => new()
    {
        Id = id,
        TenantId = 0,
        Provider = provider,
        Name = "测试短信",
        AppKey = "key",
        AppSecret = "secret",
        SignName = "签名",
        CodeLength = 6,
        Expire = 300,
        Enable = true,
    };
    #endregion

    #region NOTI-3 短信发送
    [Theory(DisplayName = "短信验证码_长度正确且全为数字")]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(0)]
    [InlineData(-3)]
    public void SmsVerifyCode_LengthAndDigits(Int32 length)
    {
        var expect = length <= 0 ? SmsService.DefaultCodeLength : length;

        var code = SmsService.GenerateVerifyCode(length);

        Assert.Equal(expect, code.Length);
        Assert.All(code, c => Assert.True(c >= '0' && c <= '9', $"出现非数字字符[{c}]"));
    }

    [Theory(DisplayName = "短信验证码_按动作路由到对应渠道方法并记录")]
    [InlineData("login")]
    [InlineData("reset")]
    [InlineData("bind")]
    [InlineData("activate")]
    public async Task SendVerifyCode_RoutesByAction(String action)
    {
        var config = SmsConfig(4300 + action.Length);
        var fake = new RecordingSms();
        _cache.InnerCache.Set($"SmsVerify:{config.Provider}:{config.Id}", fake, 600);

        var record = await _sms.SendVerifyCode(action, "13800138000", "246813", config);

        Assert.Equal([action], fake.Calls);
        Assert.Equal(["246813"], fake.Codes);
        Assert.True(record.Success);
        Assert.Equal("Sms", record.Channel);
        Assert.Equal(action, record.Action);
        Assert.Equal("13800138000", record.Target);
        Assert.Equal("Fake", record.Provider);
        Assert.Equal(config.Id, record.ConfigId);
        Assert.True(record.Id > 0);
    }

    [Fact(DisplayName = "短信验证码_渠道异常仍写失败记录并保留错误")]
    public async Task SendVerifyCode_UnknownProvider_RecordsFailure()
    {
        var config = SmsConfig(4399, "Unknown");

        var record = await _sms.SendVerifyCode("login", "13900139000", "135790", config);

        Assert.False(record.Success);
        Assert.False(record.Result.IsNullOrEmpty());
        Assert.Equal("Sms", record.Channel);
        Assert.Equal("13900139000", record.Target);
        Assert.True(record.Id > 0);
    }
    #endregion

    #region NOTI-2 / NOTI-4 渠道配置
    [Fact(DisplayName = "邮件配置转模型_字段完整映射")]
    public void MailConfig_ToModel()
    {
        var entity = new MailConfig
        {
            Id = 12,
            TenantId = 3,
            Name = "主渠道",
            DisplayName = "主邮件渠道",
            Provider = "Smtp",
            Server = "smtp.example.com",
            Port = 465,
            EnableSsl = true,
            UserName = "sender@example.com",
            FromMail = "noreply@example.com",
            FromName = "魔方",
            CodeLength = 6,
            Expire = 600,
            Enable = true,
            Priority = 9,
        };

        var model = entity.ToModel();

        Assert.Equal(12, model.Id);
        Assert.Equal(3, model.TenantId);
        Assert.Equal("Smtp", model.Provider);
        Assert.Equal("smtp.example.com", model.Server);
        Assert.Equal(465, model.Port);
        Assert.True(model.EnableSsl);
        Assert.Equal("noreply@example.com", model.FromMail);
        Assert.Equal(6, model.CodeLength);
        Assert.Equal(600, model.Expire);
        Assert.True(model.Enable);
        Assert.Equal(9, model.Priority);
    }

    [Fact(DisplayName = "短信配置转模型_字段完整映射")]
    public void SmsConfig_ToModel()
    {
        var entity = new SmsConfig
        {
            Id = 21,
            TenantId = 2,
            Name = "阿里云",
            DisplayName = "阿里云短信",
            Provider = "Aliyun",
            Server = "https://dysmsapi.aliyuncs.com",
            AppKey = "appkey",
            AppSecret = "secret",
            SignName = "新生命",
            SchemaName = "SMS_001",
            CodeLength = 6,
            Expire = 300,
            Enable = true,
            Priority = 5,
        };

        var model = entity.ToModel();

        Assert.Equal(21, model.Id);
        Assert.Equal(2, model.TenantId);
        Assert.Equal("Aliyun", model.Provider);
        Assert.Equal("appkey", model.AppKey);
        Assert.Equal("新生命", model.SignName);
        Assert.Equal("SMS_001", model.SchemaName);
        Assert.Equal(6, model.CodeLength);
        Assert.Equal(300, model.Expire);
        Assert.True(model.Enable);
        Assert.Equal(5, model.Priority);
    }
    #endregion
}
