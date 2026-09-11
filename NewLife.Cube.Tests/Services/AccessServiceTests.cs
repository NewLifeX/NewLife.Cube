using System;
using System.IO;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Tests.Membership;
using NewLife.Cube.Web;
using XCode.DataAccessLayer;
using Xunit;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Tests.Services;

/// <summary>安全防御测试集合。共享全局配置与数据库连接，禁用并行</summary>
[CollectionDefinition("SecurityDefense", DisableParallelization = true)]
public class SecurityDefenseCollection { }

/// <summary>安全访问测试的SQLite夹具。类级注册 Cube/Log 连接，类结束后恢复全局连接，避免影响其它测试类</summary>
public class AccessServiceDbFixture : IDisposable
{
    private readonly String _oldCube;
    private readonly String _oldLog;
    private readonly Boolean _hadCube;
    private readonly Boolean _hadLog;

    /// <summary>实例化，注册临时SQLite并建表</summary>
    public AccessServiceDbFixture()
    {
        var dbFile = Path.Combine(Path.GetTempPath(), $"CubeSecDb_{Guid.NewGuid():N}.db");

        if (DAL.ConnStrs != null)
        {
            _hadCube = DAL.ConnStrs.TryGetValue("Cube", out _oldCube);
            _hadLog = DAL.ConnStrs.TryGetValue("Log", out _oldLog);
        }

        DAL.AddConnStr("Cube", $"Data Source={dbFile}", null, "SQLite");
        DAL.AddConnStr("Log", $"Data Source={dbFile}", null, "SQLite");

        // 触发表结构检查与自动建表
        _ = AccessRule.Meta.Count;
        _ = XLog.Meta.Count;
        _ = NotificationRecord.Meta.Count;
    }

    /// <summary>恢复全局连接并清缓存，避免其它测试类拿到本夹具的临时库</summary>
    public void Dispose()
    {
        if (DAL.ConnStrs != null)
        {
            if (_hadCube) DAL.ConnStrs["Cube"] = _oldCube;
            else DAL.ConnStrs.TryRemove("Cube", out _);

            if (_hadLog) DAL.ConnStrs["Log"] = _oldLog;
            else DAL.ConnStrs.TryRemove("Log", out _);
        }

        DAL.Create("Cube").Reset();
        DAL.Create("Log").Reset();
    }
}

/// <summary>安全访问服务测试。覆盖封禁快照、观察/拦截/自动模式、威胁检测决策与自动封禁闭环（SQLite集成）</summary>
[Collection("SecurityDefense")]
public class AccessServiceTests : IDisposable, IClassFixture<AccessServiceDbFixture>
{
    private readonly TestCacheProvider _cache = new();
    private readonly SecurityEventService _events;
    private readonly BlockService _blocks;
    private readonly AccessService _access;
    private readonly Int32 _oldMode;

    /// <summary>实例化，构建服务实例</summary>
    public AccessServiceTests()
    {
        _oldMode = CubeSetting.Current.SecurityMode;

        _events = new SecurityEventService(_cache, null!);
        _blocks = new BlockService(_cache, null!);
        _access = new AccessService(_cache, null!, _blocks, _events);
    }

    /// <summary>恢复全局配置</summary>
    public void Dispose() => CubeSetting.Current.SecurityMode = _oldMode;

    private static void CleanRules()
    {
        foreach (var rule in AccessRule.FindAll()) rule.Delete();
    }

    private static UserAgentParser CreateUa(String ua)
    {
        var parser = new UserAgentParser();
        parser.Parse(ua);

        return parser;
    }

    [Fact(DisplayName = "观察模式：扫描器请求放行并记录安全事件")]
    public void ObserveMode_Scanner_Allowed_And_Logged()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 1;

        var ip = "192.0.2.11";
        var rs = _access.Valid("/Admin/User", null, CreateUa("sqlmap/1.7"), ip, null, null);

        // 观察模式放行
        Assert.Null(rs);

        // 安全事件已写入且标记为未拦截
        var log = XLog.Find(XLog._.Category == SecurityEventService.CategoryName & XLog._.Action == "扫描器" & XLog._.CreateIP == ip);
        Assert.NotNull(log);
        Assert.False(log!.Success);
    }

    [Fact(DisplayName = "拦截模式：扫描器拦截并累计达阈值自动封禁")]
    public void BlockMode_Scanner_Blocked_And_AutoBlock()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 2;

        var ip = "192.0.2.12";
        var ua = CreateUa("sqlmap/1.7");

        // 前两次：拦截但未达封禁阈值
        var rs1 = _access.Valid("/Admin/User", null, ua, ip, null, null);
        Assert.NotNull(rs1);
        Assert.Equal(AccessActionKinds.Block, rs1!.ActionKind);
        Assert.False(_blocks.IsBlocked(ip));

        var rs2 = _access.Valid("/Admin/User", null, ua, ip, null, null);
        Assert.NotNull(rs2);
        Assert.False(_blocks.IsBlocked(ip));

        // 第三次：达到阈值自动封禁
        var rs3 = _access.Valid("/Admin/User", null, ua, ip, null, null);
        Assert.NotNull(rs3);
        Assert.True(_blocks.IsBlocked(ip));

        // 后续请求直接被自动封禁规则拦截
        var rs4 = _access.Valid("/Admin/User", null, ua, ip, null, null);
        Assert.NotNull(rs4);
        Assert.True(rs4!.IsAutoBlock);
    }

    [Fact(DisplayName = "封禁快照：封禁后请求被拦截，解封后恢复")]
    public void Block_Unblock_RoundTrip()
    {
        CleanRules();

        var ip = "192.0.2.13";
        _blocks.Block(ip, "测试封禁");
        Assert.True(_blocks.IsBlocked(ip));

        var rs = _access.Valid("/Admin/Index", null, CreateUa("Mozilla/5.0 Chrome/126.0"), ip, null, null);
        Assert.NotNull(rs);
        Assert.True(rs!.IsAutoBlock);

        var n = _blocks.Unblock(ip);
        Assert.True(n > 0);
        Assert.False(_blocks.IsBlocked(ip));
    }

    [Fact(DisplayName = "Pass规则命中：豁免威胁检测")]
    public void PassRule_ExemptsThreat()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 2;

        var ip = "192.0.2.14";
        new AccessRule
        {
            Name = "白名单-" + ip,
            Enable = true,
            Priority = 10000,
            IP = ip,
            ActionKind = AccessActionKinds.Pass,
        }.Insert();

        var rs = _access.Valid("/Admin/User", null, CreateUa("sqlmap/1.7"), ip, null, null);

        Assert.Null(rs);
    }

    [Fact(DisplayName = "限流未超限：不阻断后续威胁检测")]
    public void LimitNotExceeded_DoesNotExemptThreat()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 2;

        var ip = "192.0.2.15";
        new AccessRule
        {
            Name = "全局限流-" + ip,
            Enable = true,
            Url = "*",
            ActionKind = AccessActionKinds.Limit,
            LimitDimension = LimitDimensions.IP,
            LimitCycle = 600,
            LimitTimes = 1000,
        }.Insert();

        var rs = _access.Valid("/Admin/User", null, CreateUa("sqlmap/1.7"), ip, null, null);

        // 限流未超限不应豁免威胁检测
        Assert.NotNull(rs);
    }

    [Fact(DisplayName = "响应码超阈值：自动封禁并记录事件")]
    public void TrackResponse_ExceedThreshold_AutoBlock()
    {
        CleanRules();

        var ip = "192.0.2.16";
        new AccessRule
        {
            Name = "404扫描测试",
            Enable = true,
            ResponseCodes = "404",
            ActionKind = AccessActionKinds.Block,
            BlockCode = 403,
            LimitDimension = LimitDimensions.IP,
            LimitCycle = 60,
            LimitTimes = 2,
        }.Insert();

        for (var i = 0; i < 3; i++) _access.TrackResponse(404, "/not-exist-" + i, ip, null, null);

        Assert.True(_blocks.IsBlocked(ip));
    }

    [Fact(DisplayName = "阶梯封禁：连续封禁时长按档位递增")]
    public void NextDuration_Escalates()
    {
        CubeSetting.Current.BlockDurations = null;

        var ip = "192.0.2.17";
        _cache.Cache.Remove($"security:blockcount:{ip}");

        var d1 = _blocks.NextDuration(ip);
        var d2 = _blocks.NextDuration(ip);
        var d3 = _blocks.NextDuration(ip);

        Assert.Equal(60, d1);
        Assert.Equal(300, d2);
        Assert.Equal(1800, d3);
    }

    [Fact(DisplayName = "自动模式：单次威胁仅记录不拦截")]
    public void AutoMode_SingleThreat_ObserveOnly()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 3;

        var ip = "192.0.2.21";
        _cache.Cache.Remove($"security:threat:{ip}");

        var rs = _access.Valid("/Admin/User", null, CreateUa("sqlmap/1.7"), ip, null, null);

        // 自动模式单次威胁放行，事件已记录且标记为未拦截
        Assert.Null(rs);

        var log = XLog.Find(XLog._.Category == SecurityEventService.CategoryName & XLog._.Action == "扫描器" & XLog._.CreateIP == ip);
        Assert.NotNull(log);
        Assert.False(log!.Success);
    }

    [Fact(DisplayName = "自动模式：连续威胁达阈值拦截并自动封禁")]
    public void AutoMode_RepeatedThreat_BlockAndBan()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 3;

        var ip = "192.0.2.22";
        var ua = CreateUa("sqlmap/1.7");
        _cache.Cache.Remove($"security:threat:{ip}");

        // 前两次观察放行
        Assert.Null(_access.Valid("/Admin/User", null, ua, ip, null, null));
        Assert.Null(_access.Valid("/Admin/User", null, ua, ip, null, null));

        // 第三次达到累计阈值，拦截并自动封禁
        var rs = _access.Valid("/Admin/User", null, ua, ip, null, null);
        Assert.NotNull(rs);
        Assert.Equal(AccessActionKinds.Block, rs!.ActionKind);
        Assert.True(_blocks.IsBlocked(ip));
    }

    [Fact(DisplayName = "自动模式：全局大范围攻击进入临时拦截")]
    public void AutoMode_GlobalEscalation_TemporaryBlock()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 3;

        var ip = "192.0.2.23";
        _cache.Cache.Remove($"security:threat:{ip}");

        // 预置全局攻击计数，使下一次威胁触发升级
        _cache.Cache.Set("security:auto:global", AccessService.AutoEscalateTimes - 1);
        _cache.Cache.SetExpire("security:auto:global", TimeSpan.FromSeconds(60));

        var rs1 = _access.Valid("/Admin/User", null, CreateUa("sqlmap/1.7"), ip, null, null);

        // 达到全局阈值：进入临时拦截，本次直接拦截
        Assert.NotNull(rs1);
        Assert.True(_cache.Cache.ContainsKey("security:auto:escalate"));

        // 临时拦截期内，其它IP的威胁同样被拦截
        var rs2 = _access.Valid("/Admin/User", null, CreateUa("sqlmap/1.7"), "192.0.2.24", null, null);
        Assert.NotNull(rs2);
    }

    [Fact(DisplayName = "自动模式：临时拦截到期自动回落观察")]
    public void AutoMode_Escalation_ExpiresToObserve()
    {
        CleanRules();
        CubeSetting.Current.SecurityMode = 3;

        var ip = "192.0.2.25";
        _cache.Cache.Remove($"security:threat:{ip}");

        // 模拟曾经升级且临时拦截已到期
        _cache.Cache.Set("security:auto:escalated", 1, 3600);

        var rs = _access.Valid("/Admin/User", null, CreateUa("sqlmap/1.7"), ip, null, null);

        // 自动回落：标记被清除并记录恢复事件，本次恢复观察放行
        Assert.Null(rs);
        Assert.False(_cache.Cache.ContainsKey("security:auto:escalated"));

        var log = XLog.Find(XLog._.Category == SecurityEventService.CategoryName & XLog._.Action == "自动恢复" & XLog._.CreateIP == ip);
        Assert.NotNull(log);
    }
}
