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

    /// <summary>实例化安全访问服务</summary>
    /// <param name="cacheProvider"></param>
    /// <param name="tracer"></param>
    public AccessService(ICacheProvider cacheProvider, ITracer tracer)
    {
        _cacheProvider = cacheProvider;
        _tracer = tracer;
    }

    /// <summary>验证是否允许当前请求访问</summary>
    /// <param name="url"></param>
    /// <param name="ua"></param>
    /// <param name="ip"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public AccessRule Valid(String url, UserAgentParser ua, String ip, IUser user, IDictionary<String, Object> session)
    {
        var rules = AccessRule.FindAllWithCache()
            .Where(e => e.Enable)
            .OrderByDescending(e => e.Priority)
            .ThenByDescending(e => e.Id)
            .ToList();
        if (rules.Count == 0) return null;

        // 按优先级匹配规则
        foreach (var rule in rules)
        {
            if (IsMatch(rule, url, ua.UserAgent, ip, user))
            {
                if (rule.ActionKind == AccessActionKinds.Pass)
                {
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

                    return null;
                }
            }
        }

        return null;
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

        var cacheKey = $"access:{rule.Id}:{key}:{time}";
        if (session != null) session["_access_limit"] = cacheKey;

        var count = _cacheProvider.Cache.Increment(cacheKey, 1);
        if (count > rule.LimitTimes) return false;

        if (count <= 2)
        {
            _cacheProvider.Cache.SetExpire(cacheKey, TimeSpan.FromSeconds(rule.LimitCycle));
        }

        return true;
    }

    /// <summary>解除限制</summary>
    /// <param name="session"></param>
    public void ResetLimit(IDictionary<String, Object> session)
    {
        session?.Remove("_access_limit");
    }
}
