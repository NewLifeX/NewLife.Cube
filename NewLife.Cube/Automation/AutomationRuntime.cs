using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Automation;

/// <summary>自动化运行时开关与宿主服务</summary>
public static class AutomationRuntime
{
    /// <summary>测试用：入队后立刻同步执行</summary>
    public static Boolean Immediate { get; set; }

    /// <summary>宿主 DI</summary>
    public static IServiceProvider Services { get; set; }

    /// <summary>runAutomation 最大深度</summary>
    public const Int32 MaxDepth = 3;

    /// <summary>同规则同记录防抖毫秒</summary>
    public const Int32 DebounceMs = 3000;

    /// <summary>单次请求同一 TypePath 即时入队上限</summary>
    public const Int32 BatchLimit = 50;
}

/// <summary>执行器递归深度</summary>
public static class AutomationScope
{
    private static readonly AsyncLocal<Int32> _depth = new();
    private static readonly AsyncLocal<Boolean> _exec = new();
    private static readonly AsyncLocal<Int32> _batch = new();

    /// <summary>是否处于执行器写入</summary>
    public static Boolean IsExecuting => _exec.Value;

    /// <summary>当前深度</summary>
    public static Int32 Depth => _depth.Value;

    /// <summary>本请求已即时入队数</summary>
    public static Int32 BatchCount { get => _batch.Value; set => _batch.Value = value; }

    /// <summary>进入执行作用域</summary>
    public static IDisposable Enter(Int32 depth)
    {
        var oldD = _depth.Value;
        var oldE = _exec.Value;
        _depth.Value = depth;
        _exec.Value = true;
        return new Token(() => { _depth.Value = oldD; _exec.Value = oldE; });
    }

    sealed class Token(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}

/// <summary>解析实体 TypePath / 主键</summary>
public static class AutomationPaths
{
    /// <summary>
    /// 规范化 TypePath：去掉首尾空白、~、/，得到与菜单路由一致的 <c>Admin/User</c>。
    /// 前端路由常带前导 <c>/</c>，EntityPageRegistry.Url 也常为 <c>/Admin/User</c>，
    /// 若不统一则 FindEnabled 与持久化触发对不上。
    /// </summary>
    public static String NormalizeTypePath(String typePath)
    {
        if (typePath.IsNullOrEmpty()) return typePath;
        return typePath.Trim().TrimStart('~', '/').Trim('/');
    }

    /// <summary>实体类型 → Admin/User</summary>
    public static String ResolveTypePath(Type entityType)
    {
        if (entityType == null) return null;
        var info = EntityPageRegistry.Get(entityType);
        if (info != null && !info.Url.IsNullOrEmpty())
            return NormalizeTypePath(info.Url);
        return entityType.Name;
    }

    /// <summary>主键字符串</summary>
    public static String RecordKey(IEntity entity)
    {
        if (entity == null) return null;
        var fact = EntityFactory.CreateFactory(entity.GetType());
        var pk = fact?.Unique?.Name ?? fact?.Table?.PrimaryKeys?.FirstOrDefault()?.Name;
        if (pk.IsNullOrEmpty()) return null;
        return entity[pk] + "";
    }
}

/// <summary>配置/按钮/历史权限</summary>
public static class AutomationAuth
{
    /// <summary>系统管理员</summary>
    public static Boolean IsSystem(IUser user) =>
        user != null && user.Roles != null && user.Roles.Any(e => e.IsSystem);

    /// <summary>实体菜单</summary>
    public static IMenu FindMenu(String typePath)
    {
        if (typePath.IsNullOrEmpty()) return null;
        var path = AutomationPaths.NormalizeTypePath(typePath);
        var url = "/" + path;
        var mf = ManageProvider.Menu;
        return mf?.FindByUrl(url) ?? mf?.FindByUrl("~" + url) ?? mf?.FindByUrl(path);
    }

    /// <summary>可配置（Update 或系统管理员）</summary>
    public static Boolean CanConfigure(IUser user, String typePath)
    {
        if (user == null) return false;
        if (IsSystem(user)) return true;
        var menu = FindMenu(typePath);
        if (menu == null) return false;
        return user.Has(menu, PermissionFlags.Update);
    }

    /// <summary>菜单权限（系统管理员放行）</summary>
    public static Boolean HasPermission(IUser user, String typePath, PermissionFlags flag)
    {
        if (user == null) return false;
        if (IsSystem(user)) return true;
        var menu = FindMenu(typePath);
        return menu != null && user.Has(menu, flag);
    }

    /// <summary>可看运行（Detail 或可配置）</summary>
    public static Boolean CanViewRuns(IUser user, String typePath)
    {
        if (CanConfigure(user, typePath)) return true;
        var menu = FindMenu(typePath);
        return menu != null && user.Has(menu, PermissionFlags.Detail);
    }

    /// <summary>按钮权限</summary>
    public static Boolean CanPressButton(IUser user, String typePath, String requirePermission)
    {
        if (user == null) return false;
        if (IsSystem(user)) return true;
        var menu = FindMenu(typePath);
        if (menu == null) return false;
        var flag = requirePermission.EqualIgnoreCase("update") ? PermissionFlags.Update : PermissionFlags.Detail;
        return user.Has(menu, flag);
    }
}

/// <summary>Webhook 限流与 HMAC（OSC-260815fa86）</summary>
public static class AutomationHookRate
{
    static readonly ConcurrentDictionary<String, (Int64 Window, Int32 Count)> Hits = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>每令牌每分钟上限</summary>
    public static Int32 Limit { get; set; } = 60;

    /// <summary>字典上限；超出时淘汰过期窗口</summary>
    public static Int32 MaxKeys { get; set; } = 2048;

    /// <summary>测试重置</summary>
    public static void Reset() => Hits.Clear();

    /// <summary>尝试占用一次；超限返回 false</summary>
    public static Boolean TryAcquire(String token)
    {
        if (token.IsNullOrEmpty()) return false;
        if (Hits.Count > MaxKeys) TrimExpired();
        var now = DateTime.UtcNow.Ticks;
        var win = TimeSpan.FromMinutes(1).Ticks;
        var next = Hits.AddOrUpdate(token, _ => (now, 1), (_, s) =>
        {
            if (now - s.Window > win) return (now, 1);
            return (s.Window, s.Count + 1);
        });
        return next.Count <= Limit;
    }

    static void TrimExpired()
    {
        var now = DateTime.UtcNow.Ticks;
        var win = TimeSpan.FromMinutes(1).Ticks;
        foreach (var kv in Hits)
        {
            if (now - kv.Value.Window > win)
                Hits.TryRemove(kv.Key, out _);
        }
        // 仍过大：丢掉最旧一半
        if (Hits.Count <= MaxKeys) return;
        foreach (var key in Hits.OrderBy(x => x.Value.Window).Take(Hits.Count / 2).Select(x => x.Key).ToArray())
            Hits.TryRemove(key, out _);
    }

    /// <summary>HMAC-SHA256 hex（小写）</summary>
    public static String HmacHex(String key, String body)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(key ?? ""));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(body ?? ""))).ToLowerInvariant();
    }
}
