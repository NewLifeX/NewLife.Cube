using NewLife.Caching;
using NewLife.Serialization;
using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>快捷入口。最近访问优先，新上菜单补足，全部按当前用户权限过滤</summary>
[Widget("QuickLink", "快捷入口", Icon = "fa-th-large", Cols = 4, Sort = 80, Category = "通用", WidgetType = WidgetTypes.Content)]
public class QuickLinkWidget : IWidget
{
    /// <summary>最近访问缓存。进程内缓存按用户隔离，600 秒无访问自动清理</summary>
    private static readonly ICache _cache = MemoryCache.Instance;

    /// <summary>获取组件数据。最近访问优先，新上菜单补足至 12 个，全部按当前用户权限过滤</summary>
    /// <returns>快捷链接列表匿名对象</returns>
    public Object GetData()
    {
        // 未登录（如测试环境）返回空结构
        var user = ManageProvider.User;
        if (user == null) return new { Links = new List<Object>() };

        var links = new List<Object>();
        var urls = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        // 最近访问优先：菜单需存在、可见且有权限才展示，最多 10 个
        foreach (var item in GetRecent(user.ID))
        {
            var menu = ManageProvider.Menu?.FindByUrl(item.Url);
            if (!CanAccess(user, menu)) continue;
            if (!urls.Add(menu.Url)) continue;

            links.Add(new { Name = menu.DisplayName ?? menu.Name, menu.Url, menu.Icon });
        }

        // 新上菜单补足剩余位置：按更新时间倒序，权限过滤与 URL 去重
        if (links.Count < 12)
        {
            foreach (var menu in GetNewMenus(user))
            {
                if (links.Count >= 12) break;
                if (!urls.Add(menu.Url)) continue;

                links.Add(new { Name = menu.DisplayName ?? menu.Name, menu.Url, menu.Icon });
            }
        }

        return new { Links = links };
    }

    #region 最近访问
    /// <summary>最近访问缓存项</summary>
    private sealed class VisitCache
    {
        /// <summary>最近访问列表</summary>
        public List<RecentVisit> List { get; set; } = [];
    }

    /// <summary>记录页面访问。菜单需存在、可见且可用；新增页面立即落库，重复访问延迟 600 秒保存；失败不影响主流程</summary>
    /// <param name="userId">用户编号。小于等于0忽略</param>
    /// <param name="menu">页面菜单</param>
    public static void RecordVisit(Int32 userId, IMenu menu)
    {
        if (userId <= 0 || menu == null || !menu.Visible || menu.Url.IsNullOrEmpty()) return;

        try
        {
            var url = menu.Url;
            var name = menu.DisplayName ?? menu.Name;
            var icon = menu.Icon;

            var item = GetOrLoad(userId);
            Boolean changed = false, added = false;
            String json = null;
            lock (item)
            {
                var list = item.List;
                var p = list.FirstOrDefault(e => e.Url == url);
                if (p != null)
                {
                    // 已存在：更新名称/图标并置顶；已在顶部且无变化时不产生脏数据
                    if (list[0] != p || p.Name != name || p.Icon != icon)
                    {
                        p.Name = name;
                        p.Icon = icon;
                        list.Remove(p);
                        list.Insert(0, p);
                        changed = true;
                    }
                }
                else
                {
                    // 新增页面
                    list.Insert(0, new RecentVisit { Name = name, Url = url, Icon = icon });
                    if (list.Count > 10) list.RemoveRange(10, list.Count - 10);
                    changed = true;
                    added = true;
                }

                if (changed) json = item.List.ToJson();
            }

            // 无论是否变化都刷新缓存过期时间，保持活跃
            _cache.Set("QuickLink:Visit:" + userId, item, 600);

            if (!changed || json == null) return;

            var para = Parameter.GetOrAdd(userId, "Visit", "Recent_Pages");
            para.LongValue = json;
            // 新增页面立即落库；重复访问延迟 600 秒批量保存，降低写压力
            if (added)
                para.Save();
            else
                para.SaveAsync(600_000);
        }
        catch
        {
            // 最近访问数据不重要，记录失败忽略，不影响主流程
        }
    }

    /// <summary>获取最近访问列表。列表顺序即最近访问顺序（最新在前），优先内存缓存，无数据返回空列表</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="max">最大条数。小于等于0返回全部</param>
    /// <returns>最近访问列表</returns>
    public static IList<RecentVisit> GetRecent(Int32 userId, Int32 max = 10)
    {
        if (userId <= 0) return [];

        try
        {
            var item = GetOrLoad(userId);
            lock (item)
            {
                var list = item.List ?? [];
                if (max > 0 && list.Count > max) return list.Take(max).ToList();

                return list;
            }
        }
        catch
        {
            // 最近访问数据不重要，读取失败返回空，不影响主流程
            return [];
        }
    }

    /// <summary>获取或加载用户缓存。缓存缺失时从数据库加载并写入缓存</summary>
    /// <param name="userId">用户编号</param>
    /// <returns>缓存项</returns>
    private static VisitCache GetOrLoad(Int32 userId)
    {
        var key = "QuickLink:Visit:" + userId;
        var item = _cache.Get<VisitCache>(key);
        if (item != null) return item;

        item = new VisitCache { List = Load(userId) };
        _cache.Set(key, item, 600);

        return item;
    }

    /// <summary>从数据库加载最近访问列表</summary>
    /// <param name="userId">用户编号</param>
    /// <returns>最近访问列表</returns>
    private static List<RecentVisit> Load(Int32 userId)
    {
        try
        {
            var p = Parameter.FindByUserIDAndCategoryAndName(userId, "Visit", "Recent_Pages");
            var json = p?.Value ?? p?.LongValue;
            if (json.IsNullOrEmpty()) return [];

            return json.ToJsonEntity<List<RecentVisit>>() ?? [];
        }
        catch
        {
            return [];
        }
    }
    #endregion

    #region 权限过滤
    /// <summary>判断当前用户能否访问该菜单。需存在、可见、有地址且有查看权限</summary>
    /// <param name="user">当前用户</param>
    /// <param name="menu">菜单</param>
    /// <returns></returns>
    private static Boolean CanAccess(IUser user, IMenu menu)
    {
        if (user == null || menu == null) return false;
        if (!menu.Visible || menu.Url.IsNullOrEmpty()) return false;

        try
        {
            return user.Has(menu, PermissionFlags.Detail);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>获取新上菜单：可见、有地址、当前用户有权访问，按更新时间倒序再按编号倒序。排除工作台首页</summary>
    /// <param name="user">当前用户</param>
    /// <returns>菜单列表</returns>
    private static IEnumerable<IMenu> GetNewMenus(IUser user)
    {
        if (user == null) return [];

        try
        {
            return Menu.FindAllWithCache()
                .Where(e => e.Visible && !e.Url.IsNullOrEmpty() && !e.Url.EqualIgnoreCase("/Admin/Index/Dashboard"))
                .Where(e => user.Has(e, PermissionFlags.Detail))
                .OrderByDescending(e => e.UpdateTime)
                .ThenByDescending(e => e.ID);
        }
        catch
        {
            return [];
        }
    }
    #endregion
}
