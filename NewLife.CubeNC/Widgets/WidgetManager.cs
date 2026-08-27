using System.Reflection;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>工作台组件管理器。扫描程序集中实现 <see cref="IWidget"/> 的类，按权限、启停过滤并提供组件数据。
/// 通过 DI 注入（AddCube 中注册为单例），控制器构造注入使用</summary>
public class WidgetManager
{
    #region 属性
    private IDictionary<String, WidgetAttribute> _widgets;

    /// <summary>参数分类：组件启用开关。UserID=0 全局，Name=组件名，Value=0/1</summary>
    public const String EnableCategory = "Widget.Enable";

    /// <summary>参数分类：组件排序。UserID=当前用户，Name=组件名，Value=排序值</summary>
    public const String OrderCategory = "Widget.Order";
    #endregion

    #region 扫描
    /// <summary>扫描所有程序集，发现工作台组件。结果缓存，仅首次扫描</summary>
    /// <returns>组件名称到元数据的字典</returns>
    public IDictionary<String, WidgetAttribute> Scan()
    {
        if (_widgets != null) return _widgets;

        var dic = new Dictionary<String, WidgetAttribute>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in AssemblyX.FindAllPlugins(typeof(IWidget), true, true))
        {
            try
            {
                var att = type.GetCustomAttribute<WidgetAttribute>();
                var name = att?.Name ?? type.Name.TrimSuffix("Widget");
                if (att == null) att = new WidgetAttribute(name, type.GetDisplayName() ?? name);

                att.Name = name;
                att.Type = type;

                dic[name] = att;
            }
            catch (Exception ex)
            {
                // 单个组件扫描失败不影响其它组件
                XTrace.WriteException(ex);
            }
        }

        return _widgets = dic;
    }

    /// <summary>获取全部组件。按角色、管理员标志与启停过滤，并按用户排序返回</summary>
    /// <param name="roleNames">当前用户角色名。null 表示不按角色权限过滤</param>
    /// <param name="isAdmin">是否系统角色。false 时隐藏 AdminOnly 组件</param>
    /// <param name="userId">当前用户编号。大于0时用用户自定义排序覆盖默认排序</param>
    /// <returns>排序后的组件列表</returns>
    public IList<WidgetAttribute> GetWidgets(ICollection<String> roleNames = null, Boolean isAdmin = false, Int32 userId = 0)
    {
        var list = Scan().Values.ToList();

        if (roleNames != null || isAdmin) list = list.Where(e => IsVisible(e, roleNames, isAdmin)).ToList();

        // 全局禁用过滤
        list = list.Where(e => IsEnabled(e)).ToList();

        // 用户自定义排序覆盖默认排序，不修改缓存的默认元数据
        if (userId > 0)
            list = list.OrderBy(e => GetOrder(userId, e.Name, e.Sort)).ToList();
        else
            list = list.OrderBy(e => e.Sort).ToList();

        return list;
    }

    /// <summary>创建组件实例并获取数据</summary>
    /// <param name="info">组件元数据</param>
    /// <returns>组件数据模型</returns>
    public Object GetData(WidgetAttribute info)
    {
        if (Activator.CreateInstance(info.Type) is IWidget widget) return widget.GetData();

        return null;
    }

    /// <summary>判断组件对指定角色与管理员标志是否可见</summary>
    /// <param name="info">组件元数据</param>
    /// <param name="roleNames">角色名集合</param>
    /// <param name="isAdmin">是否系统角色。false 时隐藏 <see cref="WidgetAttribute.AdminOnly"/> 组件</param>
    /// <returns></returns>
    public Boolean IsVisible(WidgetAttribute info, ICollection<String> roleNames, Boolean isAdmin = false)
    {
        if (info == null) return false;
        // 仅系统角色可见的组件，普通用户隐藏
        if (info.AdminOnly && !isAdmin) return false;
        if (info.Permission.IsNullOrEmpty()) return true;
        if (roleNames == null || roleNames.Count == 0) return false;

        return info.Permission.Split(',').Any(p => roleNames.Contains(p.Trim(), StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>判断组件是否全局启用（Parameter 表 UserID=0，分类 Widget.Enable），默认启用</summary>
    /// <param name="info">组件元数据</param>
    /// <returns></returns>
    public Boolean IsEnabled(WidgetAttribute info)
    {
        if (info == null) return false;

        var p = Parameter.FindByUserIDAndCategoryAndName(0, EnableCategory, info.Name);
        if (p == null) return true;

        return p.Value.ToInt() != 0;
    }

    /// <summary>启用或禁用组件（Parameter 表全局配置）</summary>
    /// <param name="name">组件名</param>
    /// <param name="enable">是否启用</param>
    public void SetEnabled(String name, Boolean enable)
    {
        var p = Parameter.GetOrAdd(0, EnableCategory, name);
        p.SetItem("Value", enable ? "1" : "0");
        p.Save();
    }

    /// <summary>获取用户自定义排序。覆盖组件默认排序</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="name">组件名</param>
    /// <param name="defaultSort">默认排序</param>
    /// <returns>排序值</returns>
    public Int32 GetOrder(Int32 userId, String name, Int32 defaultSort)
    {
        if (userId <= 0) return defaultSort;

        var p = Parameter.FindByUserIDAndCategoryAndName(userId, OrderCategory, name);
        if (p == null) return defaultSort;

        return p.Value.ToInt(defaultSort);
    }

    /// <summary>保存用户组件排序（Parameter 表按用户）</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="orders">组件名到排序值的字典</param>
    public void SetOrders(Int32 userId, IDictionary<String, Int32> orders)
    {
        if (userId <= 0 || orders == null || orders.Count == 0) return;

        foreach (var item in orders)
        {
            var p = Parameter.GetOrAdd(userId, OrderCategory, item.Key);
            p.SetItem("Value", item.Value.ToString());
            p.Save();
        }
    }
    #endregion
}
