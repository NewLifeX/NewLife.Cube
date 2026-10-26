using System;
using System.Collections.Generic;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Common;
using NewLife.Cube.Tests.Membership;
using NewLife.Cube.Web;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// PERM-8 访问规则运行时链路测试。覆盖规则库读取与缓存匹配（拦截/放行/禁用/优先级）、
/// 自动封禁的写入、命中与解除，以及过期自动封禁清理。
/// </summary>
[Collection("TenantAuth")]
public class AccessRuleRuntimeTests
{
    private readonly TestCacheProvider _cache = new();
    private readonly BlockService _block;
    private readonly AccessService _access;

    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库（访问规则表）</param>
    public AccessRuleRuntimeTests(TenantAuthFixture fixture)
    {
        var tracer = default(NewLife.Log.ITracer);
        _block = new BlockService(_cache, tracer);
        _access = new AccessService(_cache, tracer, _block, new SecurityEventService(_cache, tracer));
    }

    #region 辅助
    private static String Unique(String prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static UserAgentParser Chrome
    {
        get
        {
            var ua = new UserAgentParser();
            ua.Parse("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            return ua;
        }
    }

    private static AccessRule CreateRule(String name, String url, AccessActionKinds kind, Int32 priority = 0, Boolean enable = true)
    {
        var rule = new AccessRule
        {
            Name = name,
            Url = url,
            ActionKind = kind,
            Priority = priority,
            Enable = enable,
            BlockCode = 403,
            BlockContent = "被拦截",
        };
        rule.Insert();

        return rule;
    }

    private AccessRule Valid(String url, String ip = "8.8.8.8")
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        AccessRule.Meta.Cache.Clear("访问规则测试写入", true);

        return _access.Valid(url, Chrome, ip, null, new Dictionary<String, Object>());
    }
    #endregion

    [Fact(DisplayName = "访问规则_拦截规则命中返回规则本体")]
    public void BlockRule_Matches()
    {
        var path = "/blocked/" + Unique("p");
        var rule = CreateRule(Unique("rule"), path + "*", AccessActionKinds.Block);

        var hit = Valid(path + "/index");

        Assert.NotNull(hit);
        Assert.Equal(rule.Id, hit.Id);
    }

    [Fact(DisplayName = "访问规则_未命中的地址放行")]
    public void UnmatchedUrl_Passes()
    {
        var path = "/open/" + Unique("p");
        CreateRule(Unique("rule"), path + "/secret*", AccessActionKinds.Block);

        Assert.Null(Valid("/public/" + Unique("x")));
    }

    [Fact(DisplayName = "访问规则_禁用规则不生效")]
    public void DisabledRule_Ignored()
    {
        var path = "/disabled/" + Unique("p");
        CreateRule(Unique("rule"), path + "*", AccessActionKinds.Block, enable: false);

        Assert.Null(Valid(path + "/x"));
    }

    [Fact(DisplayName = "访问规则_高优先级放行规则豁免低优先级拦截")]
    public void PassRule_OverridesBlockByPriority()
    {
        var path = "/mixed/" + Unique("p");
        CreateRule(Unique("allow"), path + "*", AccessActionKinds.Pass, priority: 100);
        CreateRule(Unique("deny"), path + "*", AccessActionKinds.Block, priority: 1);

        Assert.Null(Valid(path + "/x"));
    }

    [Fact(DisplayName = "访问规则_自动封禁IP命中并可解除")]
    public void AutoBlock_BlocksAndReleases()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var ip = "203.0.113." + NewLife.Security.Rand.Next(1, 250);

        // 走真实封禁链路：BlockService 写库并刷新内存快照，AccessService 从快照判定
        var rule = _block.Block(ip, "测试自动封禁");
        Assert.NotNull(rule);
        Assert.True(rule.IsAutoBlock);

        var hit = _access.Valid("/api/anything", Chrome, ip, null, new Dictionary<String, Object>());
        Assert.NotNull(hit);

        Assert.True(_block.Unblock(ip) >= 1);

        hit = _access.Valid("/api/anything", Chrome, ip, null, new Dictionary<String, Object>());
        Assert.Null(hit);
    }

    [Fact(DisplayName = "访问规则_过期自动封禁可批量清理")]
    public void AutoBlock_ExpiredCleanup()
    {
        using var _ = TestConnections.Use("Cube", "MapTo=Membership");
        var ip = "198.51.100." + NewLife.Security.Rand.Next(1, 250);

        var expired = AccessRule.WriteAutoBlock(ip, "过期封禁", DateTime.Now.AddMinutes(-1));
        Assert.NotNull(expired);

        var count = AccessRule.RemoveExpiredAutoBlocks(DateTime.Now);

        Assert.True(count >= 1);
        Assert.Null(AccessRule.FindById(expired.Id));
    }
}




