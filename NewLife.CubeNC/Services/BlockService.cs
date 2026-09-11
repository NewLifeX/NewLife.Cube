using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Cube.Services;

/// <summary>封禁服务。维护自动封禁内存快照，提供封禁判定、写入、解封与阶梯时长计算</summary>
/// <remarks>
/// 快照每30秒从数据库刷新一次，封禁与解封后立即刷新；多实例部署时其它实例最迟30秒后生效。
/// 封禁记录以访问规则形式持久化（名称前缀"自动封禁"），重启不丢失，过期由定时任务自动清理。
/// </remarks>
public class BlockService : IHostedService
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ITracer _tracer;
    private TimerX? _timer;

    private DateTime _lastCleanup;
    private volatile Dictionary<String, AccessRule> _exact = new(StringComparer.OrdinalIgnoreCase);
    private volatile List<AccessRule> _wildcards = [];

    /// <summary>实例化封禁服务</summary>
    /// <param name="cacheProvider">缓存提供者</param>
    /// <param name="tracer">追踪器</param>
    public BlockService(ICacheProvider cacheProvider, ITracer tracer)
    {
        _cacheProvider = cacheProvider;
        _tracer = tracer;
    }

    #region 生命周期
    /// <summary>启动服务。加载初始快照并启动定时刷新</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Refresh();
        _timer = new TimerX(DoWork, null, 30_000, 30_000) { Async = true };

        return Task.CompletedTask;
    }

    /// <summary>停止服务</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();

        return Task.CompletedTask;
    }
    #endregion

    #region 快照
    /// <summary>刷新封禁快照。从数据库加载全部生效中的自动封禁</summary>
    /// <returns>生效封禁数量</returns>
    public Int32 Refresh()
    {
        using var span = _tracer?.NewSpan("security:block:refresh");
        try
        {
            var now = DateTime.Now;
            var exact = new Dictionary<String, AccessRule>(StringComparer.OrdinalIgnoreCase);
            var wildcards = new List<AccessRule>();

            foreach (var rule in AccessRule.FindAllAutoBlocks())
            {
                if (!rule.Enable) continue;
                if (rule.ExpireTime.Year <= 2000 || rule.ExpireTime <= now) continue;
                if (rule.IP.IsNullOrEmpty()) continue;

                // 含通配符的规则单独存放，匹配时逐个比对
                if (rule.IP.Contains('*') || rule.IP.Contains('?'))
                    wildcards.Add(rule);
                else
                    exact[rule.IP] = rule;
            }

            _exact = exact;
            _wildcards = wildcards;

            return exact.Count + wildcards.Count;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);
            return 0;
        }
    }

    /// <summary>查找IP命中的封禁规则</summary>
    /// <param name="ip">来源IP</param>
    /// <returns>封禁规则，未命中返回null</returns>
    public AccessRule? FindBlock(String? ip)
    {
        if (ip.IsNullOrEmpty()) return null;

        if (_exact.TryGetValue(ip, out var rule)) return rule;

        foreach (var item in _wildcards)
        {
            if (!item.IP.IsNullOrEmpty() && item.IP.IsMatch(ip)) return item;
        }

        return null;
    }

    /// <summary>判断IP是否被自动封禁</summary>
    /// <param name="ip">来源IP</param>
    /// <returns></returns>
    public Boolean IsBlocked(String? ip) => FindBlock(ip) != null;
    #endregion

    #region 封禁与解封
    /// <summary>封禁IP。按历史触发次数计算阶梯时长，写入访问规则并立即刷新快照</summary>
    /// <param name="ip">来源IP</param>
    /// <param name="reason">封禁原因</param>
    /// <returns>封禁规则</returns>
    public AccessRule Block(String ip, String reason)
    {
        if (ip.IsNullOrEmpty()) throw new ArgumentNullException(nameof(ip));

        using var span = _tracer?.NewSpan("security:block", new { ip, reason });

        var duration = NextDuration(ip);
        var expire = DateTime.Now.AddSeconds(duration);

        var rule = AccessRule.WriteAutoBlock(ip, reason, expire);
        Refresh();

        span?.AppendTag($"duration={duration}s expire={expire:yyyy-MM-dd HH:mm:ss}");

        return rule;
    }

    /// <summary>解除自动封禁</summary>
    /// <param name="ip">IP地址</param>
    /// <returns>受影响行数</returns>
    public Int32 Unblock(String ip)
    {
        var rs = AccessRule.RemoveAutoBlock(ip);
        if (rs > 0) Refresh();

        return rs;
    }

    /// <summary>计算阶梯封禁时长。统计窗口内触发次数决定档位，触发越多封禁越久</summary>
    /// <param name="ip">来源IP</param>
    /// <returns>封禁秒数</returns>
    public Int32 NextDuration(String ip)
    {
        var cache = _cacheProvider.Cache;
        var key = $"security:blockcount:{ip}";
        var hits = cache.Increment(key, 1);
        cache.SetExpire(key, TimeSpan.FromDays(7));

        var durations = GetDurations();
        var idx = (Int32)Math.Min(hits - 1, durations.Length - 1);
        if (idx < 0) idx = 0;

        return durations[idx];
    }
    #endregion

    #region 清理
    /// <summary>清理已过期的自动封禁，并刷新快照</summary>
    /// <returns>清理行数</returns>
    public Int32 Cleanup()
    {
        var rs = AccessRule.RemoveExpiredAutoBlocks(DateTime.Now);
        if (rs > 0) Refresh();

        return rs;
    }

    private void DoWork(Object state)
    {
        Refresh();

        // 每小时清理一次过期的封禁记录
        var now = DateTime.Now;
        if (now - _lastCleanup > TimeSpan.FromHours(1))
        {
            _lastCleanup = now;

            var rs = Cleanup();
            if (rs > 0) XTrace.WriteLine("清理过期封禁 {0:n0} 行", rs);
        }
    }
    #endregion

    #region 辅助
    /// <summary>获取封禁时长档位。优先读取配置，未配置时使用默认档位</summary>
    /// <returns>秒数档位数组</returns>
    private Int32[] GetDurations()
    {
        var txt = CubeSetting.Current.BlockDurations;
        if (!txt.IsNullOrEmpty())
        {
            var list = new List<Int32>();
            foreach (var item in txt.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var n = item.Trim().ToInt();
                if (n > 0) list.Add(n);
            }
            if (list.Count > 0) return [.. list];
        }

        return [60, 300, 1800, 7200, 86400];
    }
    #endregion
}
