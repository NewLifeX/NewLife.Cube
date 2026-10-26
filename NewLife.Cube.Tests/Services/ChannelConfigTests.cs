using System;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// NOTI-2 / NOTI-4 渠道配置选择测试。覆盖邮件与短信渠道按租户、启用状态、优先级挑选配置，
/// 以及租户无配置时回退到全局配置、全部不可用时返回空。
/// </summary>
[Collection("TenantAuth")]
public class ChannelConfigTests
{
    private readonly TestCacheProvider _cache = new();
    private readonly MailService _mail;
    private readonly SmsService _sms;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（渠道配置表）</param>
    public ChannelConfigTests(TenantAuthFixture fixture)
    {
        _mail = new MailService(_cache);
        _sms = new SmsService(_cache);
    }

    #region 邮件渠道
    [Fact(DisplayName = "邮件配置_优先租户内启用的最高优先级渠道并回退全局")]
    public void MailConfig_SelectsByTenantAndPriority()
    {
        var tenantId = 811;
        ResetMailConfigs();
        try
        {
            InsertMail(0, "全局渠道", 2, true);
            InsertMail(tenantId, "低优先级", 1, true);
            InsertMail(tenantId, "高优先级", 9, true);
            InsertMail(tenantId, "已禁用", 20, false);

            var config = _mail.GetConfig(tenantId, "login");
            Assert.NotNull(config);
            Assert.Equal("高优先级", config.Name);
            Assert.True(config.Enable);

            // 其它租户无配置 → 回退全局
            Assert.Equal("全局渠道", _mail.GetConfig(812, "login")?.Name);
        }
        finally
        {
            ResetMailConfigs();
        }
    }

    [Fact(DisplayName = "邮件配置_没有任何可用渠道时返回空")]
    public void MailConfig_NoEnabled_ReturnsNull()
    {
        var tenantId = 813;
        ResetMailConfigs();
        try
        {
            InsertMail(tenantId, "已禁用", 5, false);

            Assert.Null(_mail.GetConfig(tenantId, "login"));
            Assert.Null(_mail.GetConfig(tenantId + 1, "login"));
        }
        finally
        {
            ResetMailConfigs();
        }
    }
    #endregion

    #region 短信渠道
    [Fact(DisplayName = "短信配置_优先租户内启用的最高优先级渠道并回退全局")]
    public void SmsConfig_SelectsByTenantAndPriority()
    {
        var tenantId = 821;
        ResetSmsConfigs();
        try
        {
            InsertSms(0, "全局短信", 2, true);
            InsertSms(tenantId, "低优先级短信", 1, true);
            InsertSms(tenantId, "高优先级短信", 9, true);
            InsertSms(tenantId, "已禁用短信", 20, false);

            var config = _sms.GetConfig(tenantId, "login");
            Assert.NotNull(config);
            Assert.Equal("高优先级短信", config.Name);
            Assert.Equal("全局短信", _sms.GetConfig(822, "login")?.Name);
        }
        finally
        {
            ResetSmsConfigs();
        }
    }

    [Fact(DisplayName = "短信配置_没有任何可用渠道时返回空")]
    public void SmsConfig_NoEnabled_ReturnsNull()
    {
        var tenantId = 823;
        ResetSmsConfigs();
        try
        {
            InsertSms(tenantId, "已禁用短信", 5, false);

            Assert.Null(_sms.GetConfig(tenantId, "login"));
            Assert.Null(_sms.GetConfig(tenantId + 1, "login"));
        }
        finally
        {
            ResetSmsConfigs();
        }
    }
    #endregion

    #region 辅助
    private static void ResetMailConfigs()
    {
        MailConfig.Delete(MailConfig._.Id > 0);
        // 配置服务带进程内缓存，清空后重新加载
        MailConfig.Meta.Cache.Clear("渠道配置测试重置", true);
    }

    private static void ResetSmsConfigs()
    {
        SmsConfig.Delete(SmsConfig._.Id > 0);
        SmsConfig.Meta.Cache.Clear("渠道配置测试重置", true);
    }

    private static MailConfig InsertMail(Int32 tenantId, String name, Int32 priority, Boolean enable)
    {
        var config = new MailConfig
        {
            TenantId = tenantId,
            Name = name,
            DisplayName = name,
            Provider = "Smtp",
            Server = "smtp.example.com",
            Port = 25,
            FromMail = "noreply@example.com",
            CodeLength = 6,
            Expire = 300,
            Enable = enable,
            Priority = priority,
        };
        config.Insert();

        return config;
    }

    private static SmsConfig InsertSms(Int32 tenantId, String name, Int32 priority, Boolean enable)
    {
        var config = new SmsConfig
        {
            TenantId = tenantId,
            Name = name,
            DisplayName = name,
            Provider = "Aliyun",
            Server = "https://dysmsapi.aliyuncs.com",
            AppKey = "key",
            AppSecret = "secret",
            SignName = "签名",
            CodeLength = 4,
            Expire = 300,
            Enable = enable,
            // 短信渠道按操作类型分别开关，未开启时 GetConfig 不会选中
            EnableLogin = true,
            EnableReset = true,
            EnableBind = true,
            EnableNotify = true,
            Priority = priority,
        };
        config.Insert();

        return config;
    }
    #endregion
}

