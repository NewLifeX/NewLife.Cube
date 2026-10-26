using System;
using System.Linq;
using NewLife.Cube.Entity;
using NewLife.Cube.Tests.Common;
using NewLife.Cube.Tests.Membership;
using NewLife.Data;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// OAUTH-4 OAuth 日志测试。覆盖登录日志落库、按服务商/用户/绑定编号查询与条件检索、按时间批量清理。
/// </summary>
[Collection("TenantAuth")]
public class OAuthLogTests
{
    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static OAuthLog Create(String provider, Int32 userId, Boolean success, Int32 connectId = 0, String remark = null)
    {
        var log = new OAuthLog
        {
            Provider = provider,
            UserId = userId,
            Success = success,
            ConnectId = connectId,
            Remark = remark,
            CreateTime = DateTime.Now,
        };
        log.Insert();

        return log;
    }

    [Fact(DisplayName = "OAuth日志_按服务商用户与绑定编号查询")]
    public void OAuthLog_FindByKeys()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var provider = Unique("GitHub");
        var userId = 7001;
        var connectId = 88;
        var log = Create(provider, userId, true, connectId, "登录成功");

        Assert.Equal(log.Id, OAuthLog.FindById(log.Id)?.Id);
        Assert.Contains(OAuthLog.FindAllByProvider(provider), e => e.Id == log.Id);
        Assert.Contains(OAuthLog.FindAllByUserId(userId), e => e.Id == log.Id);
        Assert.Contains(OAuthLog.FindAllByConnectId(connectId), e => e.Id == log.Id);
        Assert.DoesNotContain(OAuthLog.FindAllByProvider(Unique("missing")), e => e.Id == log.Id);
    }

    [Fact(DisplayName = "OAuth日志_条件检索按服务商用户与时间过滤")]
    public void OAuthLog_Search_Filters()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var provider = Unique("WeChat");
        var userId = 7002;
        var hit = Create(provider, userId, true, 0, "命中");
        Create(Unique("other"), userId, true, 0, "其它服务商");
        Create(provider, 7999, false, 0, "其它用户");

        var start = DateTime.Now.AddHours(-1);
        var end = DateTime.Now.AddHours(1);
        var list = OAuthLog.Search(provider, -1, userId, start, end, null, new PageParameter { PageIndex = 1, PageSize = 20 });

        Assert.Contains(list, e => e.Id == hit.Id);
        Assert.All(list, e => Assert.Equal(provider, e.Provider));
        Assert.All(list, e => Assert.Equal(userId, e.UserId));

        var outside = OAuthLog.Search(provider, -1, userId, DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-9), null, new PageParameter { PageIndex = 1, PageSize = 20 });
        Assert.DoesNotContain(outside, e => e.Id == hit.Id);
    }

    [Fact(DisplayName = "OAuth日志_按时间批量清理旧日志并保留最近记录")]
    public void OAuthLog_DeleteBefore_RemovesOldRows()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var provider = Unique("DingTalk");
        var now = DateTime.Now;
        var old = new OAuthLog { Id = OAuthLog.Meta.Factory.Snow.GetId(now.AddDays(-40)), Provider = provider, UserId = 0, Success = false, CreateTime = now.AddDays(-40) };
        old.Insert();
        var recent = new OAuthLog { Id = OAuthLog.Meta.Factory.Snow.GetId(now.AddDays(-2)), Provider = provider, UserId = 0, Success = true, CreateTime = now.AddDays(-2) };
        recent.Insert();

        var count = OAuthLog.DeleteBefore(now.AddDays(-10), 1000);

        Assert.True(count >= 1);
        Assert.Null(OAuthLog.FindById(old.Id));
        Assert.NotNull(OAuthLog.FindById(recent.Id));
    }
}

