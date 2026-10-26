using System;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Membership;
using NewLife.Data;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// OAUTH-3 / OAUTH-5 测试。覆盖 OAuth 服务商配置（新增、按名称/AppId 查找、可用与可见列表的启用/租户/授权类型过滤与排序）
/// 与用户第三方链接（按 Provider+OpenID、用户、UnionID、设备查重与条件检索）。
/// </summary>
[Collection("TenantAuth")]
public class OAuthManagementTests
{
    private readonly TenantAuthFixture _fx;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public OAuthManagementTests(TenantAuthFixture fixture) => _fx = fixture;

    #region 辅助
    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static OAuthConfig CreateConfig(Int32 tenantId, String name, Int32 sort, Boolean enable = true, Boolean visible = true, GrantTypes grantType = GrantTypes.AuthorizationCode)
    {
        var config = new OAuthConfig
        {
            TenantId = tenantId,
            Name = name,
            NickName = name,
            AppId = Unique("appid"),
            Secret = Unique("secret"),
            Server = "https://oauth.example.com",
            GrantType = grantType,
            Sort = sort,
            Enable = enable,
            Visible = visible,
        };
        config.Insert();

        return config;
    }

    private UserConnect CreateConnect(String provider, Int32 userId, String openId, String unionId = null, String deviceId = null)
    {
        var connect = new UserConnect
        {
            Provider = provider,
            UserID = userId,
            OpenID = openId,
            UnionID = unionId,
            DeviceId = deviceId,
            NickName = "第三方昵称",
            Enable = true,
        };
        connect.Insert();

        return connect;
    }
    #endregion

    #region OAUTH-3 配置管理
    [Fact(DisplayName = "OAuth配置_新增后可按名称与AppId查找")]
    public void OAuthConfig_Add_ThenFind()
    {
        var name = Unique("wechat");
        var config = OAuthConfig.Add(name, "微信登录", "/logo.png", "测试配置");

        Assert.NotNull(config);
        Assert.True(config.ID > 0);
        Assert.Equal(name, config.Name);
        Assert.Equal("微信登录", config.NickName);
        Assert.Equal("测试配置", config.Remark);
        Assert.True(config.Visible, "新增配置默认可见");
        Assert.True(config.AutoRegister, "新增配置默认允许自动注册");

        // Add 只创建基础信息，AppId/Secret/启用状态由后台填写后生效
        config.AppId = Unique("appid");
        config.Secret = Unique("secret");
        config.Enable = true;
        config.Update();

        Assert.Equal(config.ID, OAuthConfig.FindByID(config.ID)?.ID);
        Assert.Equal(config.ID, OAuthConfig.FindByName(name)?.ID);
        Assert.Equal(config.ID, OAuthConfig.FindByAppId(config.AppId)?.ID);
        Assert.Null(OAuthConfig.FindByID(0));
        Assert.Null(OAuthConfig.FindByName(Unique("missing")));

        // 禁用后的配置不得再被 AppId 命中（防止禁用渠道继续生效）
        config.Enable = false;
        config.Update();
        OAuthConfig.Meta.Cache.Clear("OAuth 配置测试禁用", true);
        Assert.Null(OAuthConfig.FindByAppId(config.AppId));
    }

    [Fact(DisplayName = "OAuth配置_可用列表按启用租户与授权类型过滤并按排序倒序")]
    public void OAuthConfig_GetValids_FiltersAndSorts()
    {
        var tenantId = 931;
        var low = CreateConfig(tenantId, Unique("low"), 1);
        var high = CreateConfig(tenantId, Unique("high"), 9);
        CreateConfig(tenantId, Unique("disabled"), 99, enable: false);
        CreateConfig(tenantId + 1, Unique("otherTenant"), 99);
        var password = CreateConfig(tenantId, Unique("pwd"), 5, grantType: GrantTypes.Password);
        OAuthConfig.Meta.Cache.Clear("OAuth 配置测试写入", true);

        var valids = OAuthConfig.GetValids(tenantId);
        Assert.Contains(valids, e => e.ID == low.ID);
        Assert.Contains(valids, e => e.ID == high.ID);
        Assert.DoesNotContain(valids, e => e.Name == "disabled");
        Assert.DoesNotContain(valids, e => e.TenantId == tenantId + 1);
        Assert.True(valids.First().ID == high.ID, "应按 Sort 倒序，最高优先级在最前");

        var passwordOnly = OAuthConfig.GetValids(tenantId, GrantTypes.Password);
        Assert.Contains(passwordOnly, e => e.ID == password.ID);
        Assert.DoesNotContain(passwordOnly, e => e.ID == high.ID);

        var visibles = OAuthConfig.GetVisibles(tenantId);
        Assert.Contains(visibles, e => e.ID == high.ID);
        Assert.DoesNotContain(visibles, e => e.Name == "disabled");
    }

    [Fact(DisplayName = "OAuth配置_条件检索支持名称过滤")]
    public void OAuthConfig_Search_Filters()
    {
        var name = Unique("github");
        var config = CreateConfig(0, name, 0);

        var page = new PageParameter { PageIndex = 1, PageSize = 10 };
        var list = OAuthConfig.Search(name, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), null, page);
        Assert.Contains(list, e => e.ID == config.ID);

        var miss = OAuthConfig.Search(Unique("missing"), DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), null, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.DoesNotContain(miss, e => e.ID == config.ID);
    }
    #endregion

    #region OAUTH-5 用户链接
    [Fact(DisplayName = "用户链接_按Provider与OpenID以及用户与UnionID查找")]
    public void UserConnect_FindByProviderAndUser()
    {
        var userId = _fx.LegacyUser.ID;
        var provider = Unique("WeChat");
        var openId = Unique("open");
        var unionId = Unique("union");
        var connect = CreateConnect(provider, userId, openId, unionId, Unique("dev"));

        Assert.Equal(connect.ID, UserConnect.FindByProviderAndOpenID(provider, openId)?.ID);
        Assert.Null(UserConnect.FindByProviderAndOpenID(provider, Unique("missing")));
        Assert.Null(UserConnect.FindByProviderAndOpenID(Unique("missing"), openId));

        var byUser = UserConnect.FindAllByUserID(userId);
        Assert.Contains(byUser, e => e.ID == connect.ID);

        Assert.Contains(UserConnect.FindAllByUnionId(unionId), e => e.ID == connect.ID);
        Assert.Contains(UserConnect.FindAllByOpenID(openId), e => e.ID == connect.ID);
        Assert.Contains(UserConnect.FindAllByDeviceId(connect.DeviceId), e => e.ID == connect.ID);
    }

    [Fact(DisplayName = "用户链接_按服务商与时间区间检索")]
    public void UserConnect_Search_Filters()
    {
        var userId = _fx.LegacyUser.ID;
        var provider = Unique("DingTalk");
        var connect = CreateConnect(provider, userId, Unique("open"));

        var start = DateTime.Now.AddHours(-1);
        var end = DateTime.Now.AddHours(1);
        var hit = UserConnect.Search(provider, userId, start, end, null, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.Contains(hit, e => e.ID == connect.ID);

        var otherProvider = UserConnect.Search(Unique("missing"), userId, start, end, null, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.DoesNotContain(otherProvider, e => e.ID == connect.ID);

        var outside = UserConnect.Search(provider, userId, DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-9), null, new PageParameter { PageIndex = 1, PageSize = 10 });
        Assert.DoesNotContain(outside, e => e.ID == connect.ID);
    }
    #endregion
}
