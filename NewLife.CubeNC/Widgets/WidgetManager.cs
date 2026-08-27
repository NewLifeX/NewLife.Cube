using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Cube.Widgets;

/// <summary>工作台组件管理器。扫描程序集中实现 <see cref="IWidget"/> 的类，按权限过滤并提供组件数据</summary>
public class WidgetManager
{
    #region 属性
    /// <summary>实例</summary>
    public static WidgetManager Instance { get; } = new WidgetManager();

    private IDictionary<String, WidgetInfo> _widgets;
    #endregion

    #region 扫描
    /// <summary>扫描所有程序集，发现工作台组件。结果缓存，仅首次扫描</summary>
    /// <returns>组件名称到元数据的字典</returns>
    public IDictionary<String, WidgetInfo> Scan()
    {
        if (_widgets != null) return _widgets;

        var dic = new Dictionary<String, WidgetInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in AssemblyX.FindAllPlugins(typeof(IWidget), true, true))
        {
            try
            {
                var att = type.GetCustomAttribute<WidgetAttribute>();
                var name = att?.Name ?? type.Name.TrimSuffix("Widget");

                var wi = new WidgetInfo
                {
                    Name = name,
                    Title = att?.Title ?? type.GetDisplayName() ?? name,
                    Icon = att?.Icon,
                    Cols = att?.Cols ?? 6,
                    Sort = att?.Sort ?? 0,
                    Category = att?.Category,
                    Permission = att?.Permission,
                    AdminOnly = att?.AdminOnly ?? false,
                    ViewName = att?.ViewName,
                    Type = type,
                };

                dic[name] = wi;
            }
            catch (Exception ex)
            {
                // 单个组件扫描失败不影响其它组件
                XTrace.WriteException(ex);
            }
        }

        return _widgets = dic;
    }

    /// <summary>获取全部组件。按角色与管理员标志过滤，并按排序返回</summary>
    /// <param name="roleNames">当前用户角色名。null 表示不按角色权限过滤</param>
    /// <param name="isAdmin">是否系统角色。false 时隐藏 AdminOnly 组件，null 角色名时也生效</param>
    /// <returns>排序后的组件列表</returns>
    public IList<WidgetInfo> GetWidgets(ICollection<String> roleNames = null, Boolean isAdmin = false)
    {
        var list = Scan().Values.ToList();

        if (roleNames != null || isAdmin) list = list.Where(e => IsVisible(e, roleNames, isAdmin)).ToList();

        return list.OrderBy(e => e.Sort).ToList();
    }

    /// <summary>创建组件实例并获取数据</summary>
    /// <param name="info">组件元数据</param>
    /// <returns>组件数据模型</returns>
    public Object GetData(WidgetInfo info)
    {
        if (Activator.CreateInstance(info.Type) is IWidget widget) return widget.GetData();

        return null;
    }

    /// <summary>判断组件对指定角色与管理员标志是否可见</summary>
    /// <param name="info">组件元数据</param>
    /// <param name="roleNames">角色名集合</param>
    /// <param name="isAdmin">是否系统角色。false 时隐藏 <see cref="WidgetInfo.AdminOnly"/> 组件</param>
    /// <returns></returns>
    public static Boolean IsVisible(WidgetInfo info, ICollection<String> roleNames, Boolean isAdmin = false)
    {
        if (info == null) return false;
        // 仅系统角色可见的组件，普通用户隐藏
        if (info.AdminOnly && !isAdmin) return false;
        if (info.Permission.IsNullOrEmpty()) return true;
        if (roleNames == null || roleNames.Count == 0) return false;

        return info.Permission.Split(',').Any(p => roleNames.Contains(p.Trim(), StringComparer.OrdinalIgnoreCase));
    }
    #endregion
}
