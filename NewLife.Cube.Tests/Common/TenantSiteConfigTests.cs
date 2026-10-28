using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NewLife;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using NewLife.Serialization;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// TEN-01/TEN-02 租户站点配置测试。覆盖租户默认语言、允许语言白名单、站点锁定语言，
/// 以及按语言覆盖的品牌名称与 Logo 合并优先级。
/// </summary>
[Collection("TenantAuth")]
public class TenantSiteConfigTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具</param>
    public TenantSiteConfigTests(TenantAuthFixture fixture) => _fx = fixture;

    private String _code => _fx.Tenant1.Code;

    private void Cleanup() => TenantSiteOptionsStore.Remove(_code);

    [Fact(DisplayName = "租户站点配置_默认语言与白名单生效")]
    public void TenantLanguage_DefaultAndWhitelist()
    {
        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            TenantSiteOptionsStore.Save(_code, new TenantSiteOptions
            {
                DefaultLanguage = "zh-Hans",
                Languages = ["zh-Hans", "en-US"],
            });

            var model = new SiteConfigModel(_fx.Tenant1);

            Assert.Equal("zh-CN", model.Language.Default);
            // 当前请求语言 de 不在白名单，回退到租户默认语言
            Assert.Equal("zh-CN", model.Language.Current);
            Assert.False(model.Language.Locked);
            Assert.Equal(2, model.Language.Options.Count);
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
            Cleanup();
        }
    }

    [Fact(DisplayName = "租户站点配置_按语言品牌覆盖")]
    public void TenantLanguage_BrandOverride()
    {
        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
            TenantSiteOptionsStore.Save(_code, new TenantSiteOptions
            {
                DefaultLanguage = "zh-Hans",
                Names = new Dictionary<String, String> { ["zh-Hans"] = "租户甲站点" },
                Logos = new Dictionary<String, String> { ["zh-Hans"] = "/logo-a.png" },
            });

            var model = new SiteConfigModel(_fx.Tenant1);

            // 未配置站点界面语言时锁定为 false，前端保留语言切换入口
            Assert.False(model.Language.Locked);
            Assert.Equal("租户甲站点", model.Name);
            Assert.Equal("/logo-a.png", model.Logo);
            Assert.Equal("zh-CN", model.Language.Current);
        }
        finally
        {
            CultureInfo.CurrentUICulture = prev;
            Cleanup();
        }
    }

    [Fact(DisplayName = "租户站点配置_站点配置界面语言后锁定")]
    public void TenantLanguage_SiteLanguageLocked()
    {
        var cfg = NewLife.Setting.Current;
        var oldLanguage = cfg.Language;
        try
        {
            cfg.Language = "zh-Hans";
            Cleanup();

            var model = new SiteConfigModel(_fx.Tenant1);

            Assert.True(model.Language.Locked);
        }
        finally
        {
            cfg.Language = oldLanguage;
            Cleanup();
        }
    }

    [Fact(DisplayName = "租户站点配置_旧版含锁定字段JSON仍可解析")]
    public void TenantOptions_LegacyJsonWithLocked()
    {
        const String json = """{"defaultLanguage":"zh-Hans","languages":["zh-Hans"],"languageLocked":true,"names":{"zh-Hans":"租户甲"}}""";

        var options = json.ToJsonEntity<TenantSiteOptions>();

        Assert.NotNull(options);
        Assert.Equal("zh-Hans", options.DefaultLanguage);
        Assert.Equal("租户甲", options.Names["zh-Hans"]);
    }

    [Fact(DisplayName = "租户站点配置_未配置时回退租户名称与站点默认")]
    public void TenantLanguage_NoOptions_FallsBack()
    {
        Cleanup();
        var model = new SiteConfigModel(_fx.Tenant1);

        Assert.Equal(_fx.Tenant1.Name, model.Name);
        Assert.False(model.Language.Locked);
        Assert.NotEmpty(model.Language.Options);
        Assert.False(model.Language.Default.IsNullOrEmpty());
    }
}
