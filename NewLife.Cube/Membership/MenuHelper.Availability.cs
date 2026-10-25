using System.Collections.Concurrent;
using System.Reflection;
using NewLife;
using XCode.Membership;

namespace NewLife.Cube.Membership;

/// <summary>菜单可用性。判断落库菜单指向的控制器是否部署在当前站点</summary>
/// <remarks>
/// 多个宿主（WebAPI/SPA 与 MVC、不同业务应用）可能共用同一个 Membership 数据库，各自扫描控制器生成的菜单会互相可见，
/// 表现为“菜单能看到、点击打不开页面”。菜单 FullName 记录控制器类型全名（或控制器全名加方法名），
/// 当前进程加载不到该类型时说明页面不存在，应从导航中过滤。菜单行仍需保留在数据库中，权限校验依赖它。
/// </remarks>
public static partial class MenuHelper
{
    #region 可用性
    static readonly ConcurrentDictionary<String, Type> _types = [];
    static readonly ConcurrentDictionary<String, Boolean> _available = [];

    /// <summary>判断菜单在当前站点是否可用。菜单指向的控制器类型未部署时不可用</summary>
    /// <param name="menu">菜单</param>
    /// <returns>可用返回 true</returns>
    public static Boolean IsMenuAvailable(this IMenu menu) => menu != null && IsMenuAvailable(menu.FullName);

    /// <summary>判断菜单全名在当前站点是否可用</summary>
    /// <param name="fullName">菜单全名。控制器类型全名，或控制器全名加方法名</param>
    /// <returns>可用返回 true</returns>
    public static Boolean IsMenuAvailable(String fullName)
    {
        if (fullName.IsNullOrEmpty()) return true;

        // 不含命名空间分隔符，不是类路径，不校验
        if (!fullName.Contains('.')) return true;

        if (_available.TryGetValue(fullName, out var available)) return available;

        // 控制器类型全名
        if (ResolveMenuType(fullName) != null)
            available = true;
        // 区域根节点保存命名空间，是否可用由子菜单决定
        else if (fullName.EndsWith(".Controllers"))
            available = true;
        else
        {
            // 方法级菜单，去掉方法名后再解析控制器
            var p = fullName.LastIndexOf('.');
            available = p > 0 && ResolveMenuType(fullName[..p]) != null;
        }

        return _available[fullName] = available;
    }

    /// <summary>过滤当前站点不可用的菜单</summary>
    /// <param name="menus">菜单集合</param>
    /// <returns>可用菜单集合，永不返回 null</returns>
    public static IList<IMenu> FilterAvailable(this IList<IMenu> menus)
    {
        if (menus == null || menus.Count == 0) return [];

        var list = new List<IMenu>(menus.Count);
        foreach (var item in menus)
        {
            if (!item.IsMenuAvailable()) continue;

            // 区域根节点下没有任何可用子菜单时一并隐藏，避免出现空分组
            if (item.FullName != null && item.FullName.EndsWith(".Controllers"))
            {
                var childs = item.Childs;
                if (childs != null && childs.Count > 0 && !childs.Any(e => e.IsMenuAvailable())) continue;
            }

            list.Add(item);
        }

        return list;
    }

    /// <summary>按 FullName 解析菜单对应类型（含跨程序集回退与方法级菜单）</summary>
    /// <param name="fullName">类型全名，或控制器全名加方法名</param>
    /// <returns>解析到的类型，未找到返回 null</returns>
    internal static Type ResolveMenuType(String fullName)
    {
        if (fullName.IsNullOrEmpty()) return null;
        if (_types.TryGetValue(fullName, out var type)) return type;

        type = Type.GetType(fullName);
        if (type == null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null) break;
            }
        }

        // 方法级菜单：去掉方法名后解析控制器，方法本身必须存在
        if (type == null)
        {
            var p = fullName.LastIndexOf('.');
            if (p > 0)
            {
                var method = fullName[(p + 1)..];
                var controller = ResolveMenuType(fullName[..p]);
                if (controller != null && controller.GetMethod(method, BindingFlags.Public | BindingFlags.Instance) != null) type = controller;
            }
        }

        // 只缓存解析成功的结果，程序集延迟加载后仍可重新查找
        if (type != null) _types[fullName] = type;

        return type;
    }
    #endregion
}
