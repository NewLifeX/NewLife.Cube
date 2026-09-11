using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Cube.Web;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>安全访问规则服务</summary>
public class AccessService
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ITracer _tracer;
    private readonly BlockService _blockService;
    private readonly SecurityEventService _eventService;

    /// <summary>实例化安全访问服务</summary>
    /// <param name="cacheProvider">缓存提供者</param>
    /// <param name="tracer">追踪器</param>
    /// <param name="blockService">封禁服务</param>
    /// <param name="eventService">安全事件服务</param>
    public AccessService(ICacheProvider cacheProvider, ITracer tracer, BlockService blockService, SecurityEventService eventService)
    {
        _cacheProvider = cacheProvider;
        _tracer = tracer;
        _blockService = blockService;
        _eventService = eventService;
    }

    /// <summary>验证是否允许当前请求访问</summary>
    /// <param name="url">请求地址</param>
    /// <param name="ua">用户代理</param>
    /// <param name="ip">来源IP</param>
    /// <param name="user">当前用户</param>
    /// <param name="session">会话集合</param>
    /// <returns>需要拦截时返回规则，放行返回null</returns>
    public AccessRule Valid(String url, UserAgentParser ua, String ip, IUser user, IDictionary<String, Object> session) => Valid(url, null, ua, ip, user, session);

    /// <summary>验证是否允许当前请求访问</summary>
    /// <param name="url">请求地址</param>
    /// <param name="body">请求体，用于威胁检测，可为空</param>
    /// <param name="ua">用户代理</param>
    /// <param name="ip">来源IP</param>
    /// <param name="user">当前用户</param>
    /// <param name="session">会话集合</param>
    /// <returns>需要拦截时返回规则，放行返回null</returns>
    public AccessRule Valid(String url, String body, UserAgentParser ua, String ip, IUser user, IDictionary<String, Object> session)
    {
        // 检查IP是否被自动封禁（持久化封禁的内存快照）
        if (!ip.IsNullOrEmpty())
        {
            var blockRule = _blockService.FindBlock(ip);
            if (blockRule != null)
            {
                using var span = _tracer?.NewSpan($"security:blocked:{blockRule.Name}", new { url, ip });
                return blockRule;
            }
        }

        var rules = AccessRule.FindAllWithCache()
            .Where(e => e.Enable && !e.IsAutoBlock)
            .OrderByDescending(e => e.Priority)
            .ThenByDescending(e => e.Id)
            .ToList();
        if (rules.Count > 0)
        {
            // 按优先级匹配规则
            foreach (var rule in rules)
            {
                if (!IsMatch(rule, url, ua.UserAgent, ip, user)) continue;

                if (rule.ActionKind == AccessActionKinds.Pass)
                {
                    // 放行规则命中，豁免后续规则与威胁检测
                    return null;
                }
                else if (rule.ActionKind == AccessActionKinds.Block)
                {
                    using var span = _tracer?.NewSpan($"access:{rule.Name}", new { url, ua.UserAgent, ip, user?.Name });
                    return rule;
                }
                else if (rule.ActionKind == AccessActionKinds.Limit)
                {
                    // 验证限流未通过时，返回规则，让外部限制访问
                    if (!ValidLimit(rule, url, ua.UserAgent, ip, user, session))
                    {
                        using var span = _tracer?.NewSpan($"access:{rule.Name}", new { url, ua.UserAgent, ip, user?.Name });
                        return rule;
                    }

                    // 未超限时不阻断，继续评估后续规则与威胁检测
                }
            }
        }

        // 内置威胁检测
        return DetectThreat(url, body, ua.UserAgent, ip, user);
    }

    private Boolean IsMatch(AccessRule rule, String url, String userAgent, String ip, IUser user)
    {
        if (!IsMatch(rule.Url, url)) return false;
        if (!IsMatch(rule.UserAgent, userAgent)) return false;
        if (!IsMatch(rule.IP, ip)) return false;
        if (!IsMatch(rule.LoginedUser, user?.Name)) return false;

        return true;
    }

    private Boolean IsMatch(String rule, String txt)
    {
        // 没有规则要求，直接通过
        if (rule.IsNullOrEmpty()) return true;

        // 有规则，没有目标输入，不通过
        if (txt.IsNullOrEmpty()) return false;

        if (rule[0] == '!')
        {
            // 任意匹配不通过
            var ss = rule.Split(",").Select(e => e.TrimStart('!')).ToArray();
            return !ss.Any(e => e.IsMatch(txt));
        }
        else
        {
            // 任意匹配通过
            var ss = rule.Split(",");
            return ss.Any(e => e.IsMatch(txt));
        }
    }

    private Boolean ValidLimit(AccessRule rule, String url, String userAgent, String ip, IUser user, IDictionary<String, Object> session)
    {
        // 未设置限流周期和次数，不限制
        if (rule.LimitCycle <= 0 || rule.LimitTimes <= 0) return true;

        var key = rule.LimitDimension switch
        {
            LimitDimensions.User => user?.Name,
            _ => ip,
        };
        // 没有关键字，不限制
        if (key.IsNullOrEmpty()) return true;

        // 时间因子，今天总秒数除以周期
        var now = DateTime.Now;
        var sec = (Int32)(now - now.Date).TotalSeconds;
        var time = sec / rule.LimitCycle;

        // 限流缓存键
        var cacheKey = $"access:{rule.Id}:{key}:{time}";
        if (session != null) session["_access_limit"] = cacheKey;

        // 递增并设置过期时间
        var hits = _cacheProvider.Cache.Increment(cacheKey, 1);
        if (hits <= 2)
            _cacheProvider.Cache.SetExpire(cacheKey, TimeSpan.FromSeconds(rule.LimitCycle));

        DefaultSpan.Current?.AppendTag($"cacheKey={cacheKey} startTime={TimeSpan.FromSeconds(time * rule.LimitCycle)} hits={hits}");

        if (hits > rule.LimitTimes) return false;

        return true;
    }

    /// <summary>解除限制</summary>
    /// <param name="session"></param>
    public void ResetLimit(IDictionary<String, Object> session)
    {
        session?.Remove("_access_limit");
    }

    /// <summary>追踪HTTP响应码，检测爬虫或web扫描攻击</summary>
    /// <param name="statusCode">HTTP响应状态码</param>
    /// <param name="url">请求URL</param>
    /// <param name="ip">来源IP</param>
    /// <param name="user">当前用户</param>
    /// <param name="session">会话</param>
    public void TrackResponse(Int32 statusCode, String url, String ip, IUser user, IDictionary<String, Object> session)
    {
        if (ip.IsNullOrEmpty()) return;

        var rules = AccessRule.FindAllWithCache()
            .Where(e => e.Enable && !e.ResponseCodes.IsNullOrEmpty())
            .OrderByDescending(e => e.Priority)
            .ThenByDescending(e => e.Id)
            .ToList();
        if (rules.Count == 0) return;

        foreach (var rule in rules)
        {
            // 检查响应码是否匹配
            var codes = rule.ResponseCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (!codes.Any(c => c.Trim() == statusCode.ToString())) continue;

            // 检查URL是否匹配（用于过滤静态资源等不需小心的请求）
            if (!IsMatch(rule.Url, url)) continue;

            // 匹配IP静态过滤（可限制部分IP段检测）
            if (!rule.IP.IsNullOrEmpty() && !IsMatch(rule.IP, ip)) continue;

            // 未设置限流周期和次数，跳过
            if (rule.LimitCycle <= 0 || rule.LimitTimes <= 0) continue;

            var key = rule.LimitDimension switch
            {
                LimitDimensions.User => user?.Name,
                _ => ip,
            };
            if (key.IsNullOrEmpty()) continue;

            // 时间因子，今天总秒数除以周期
            var now = DateTime.Now;
            var sec = (Int32)(now - now.Date).TotalSeconds;
            var time = sec / rule.LimitCycle;

            // 响应码计数缓存键
            var cacheKey = $"access:resp:{rule.Id}:{key}:{time}";

            var hits = _cacheProvider.Cache.Increment(cacheKey, 1);
            if (hits <= 2)
                _cacheProvider.Cache.SetExpire(cacheKey, TimeSpan.FromSeconds(rule.LimitCycle));

            DefaultSpan.Current?.AppendTag($"responseTrack cacheKey={cacheKey} statusCode={statusCode} hits={hits}");

            if (hits > rule.LimitTimes)
            {
                // 超过阈值，自动封禁IP（持久化+快照），并记录安全事件与告警
                ApplyBlock(ip, $"{rule.Name}：{statusCode}响应超{rule.LimitTimes}次", user?.Name);

                using var span = _tracer?.NewSpan($"security:blocked:{rule.Name}", new { ip, statusCode, hits });
            }
        }
    }

    #region 威胁检测
    /// <summary>威胁拦截累计窗口。同一IP在该窗口内多次触发威胁时自动封禁，单位秒</summary>
    public const Int32 ThreatBlockWindow = 300;

    /// <summary>威胁拦截累计阈值。窗口内达到该次数时自动封禁</summary>
    public const Int32 ThreatBlockTimes = 3;

    /// <summary>内置威胁检测。观察模式仅记录事件；拦截模式返回拦截规则，累计达标时自动封禁</summary>
    /// <param name="url">请求地址</param>
    /// <param name="body">请求体</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="ip">来源IP</param>
    /// <param name="user">当前用户</param>
    /// <returns>需要拦截时返回拦截规则，否则返回null</returns>
    private AccessRule DetectThreat(String url, String body, String userAgent, String ip, IUser user)
    {
        var set = CubeSetting.Current;
        if (set.SecurityMode <= 0) return null;

        ThreatResult threat;
        try
        {
            threat = ThreatDetector.Detect(url, body, userAgent);
        }
        catch (Exception ex)
        {
            // 检测异常不影响正常请求
            XTrace.WriteException(ex);
            return null;
        }
        if (threat == null) return null;

        var blocked = set.SecurityMode >= 2 && threat.Level >= 2;
        var message = $"{threat.Pattern} @{threat.Target}；片段：{threat.Snippet}；URL：{url}；UA：{userAgent}";
        _eventService.Write(threat.Category, message, ip, user?.Name, blocked);

        if (!blocked) return null;

        // 拦截模式下累计威胁次数，短时间连续触发时自动封禁
        if (CheckThreatBlock(ip)) ApplyBlock(ip, $"{threat.Category}攻击", user?.Name);

        return new AccessRule
        {
            Name = $"威胁拦截 {threat.Category}",
            ActionKind = AccessActionKinds.Block,
            BlockCode = 403,
            BlockContent = "<h1>访问被拒绝</h1><p>您的请求存在安全风险，已被系统拦截！</p>",
        };
    }

    /// <summary>累计威胁触发次数，达到阈值时返回true</summary>
    /// <param name="ip">来源IP</param>
    /// <returns>是否达到自动封禁阈值</returns>
    private Boolean CheckThreatBlock(String ip)
    {
        if (ip.IsNullOrEmpty()) return false;

        var cache = _cacheProvider.Cache;
        var key = $"security:threat:{ip}";
        var hits = cache.Increment(key, 1);
        if (hits <= 2) cache.SetExpire(key, TimeSpan.FromSeconds(ThreatBlockWindow));

        return hits >= ThreatBlockTimes;
    }

    /// <summary>执行自动封禁，记录安全事件并发送告警</summary>
    /// <param name="ip">来源IP</param>
    /// <param name="reason">封禁原因</param>
    /// <param name="user">当前用户</param>
    private void ApplyBlock(String ip, String reason, String user)
    {
        if (ip.IsNullOrEmpty()) return;

        try
        {
            var rule = _blockService.Block(ip, reason);
            _eventService.Write("自动封禁", $"{reason}；解封时间 {rule.ExpireTime:yyyy-MM-dd HH:mm:ss}", ip, user, blocked: true, linkId: rule.Id);
            _eventService.NotifyBlocked(ip, reason, rule.ExpireTime);

            // 封禁后清除威胁计数，解封后重新累计
            _cacheProvider.Cache.Remove($"security:threat:{ip}");
        }
        catch (Exception ex)
        {
            // 封禁失败不影响请求处理
            XTrace.WriteException(ex);
        }
    }
    #endregion
}
