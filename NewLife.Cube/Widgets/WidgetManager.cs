using System.Reflection;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
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

    /// <summary>参数分类：组件排序（旧数据）。UserID=当前用户，Name=组件名，Value=排序值；新数据走 <see cref="LayoutCategory"/>，读取时回退兼容</summary>
    public const String OrderCategory = "Widget.Order";

    /// <summary>参数分类：用户布局。UserID=当前用户，Name=config，LongValue=JSON（排序+隐藏），每用户单行</summary>
    public const String LayoutCategory = "Widget.Layout";

    /// <summary>参数分类：组件分组配置。UserID=0 全局，Name=Order 是组顺序，Name={组名} 是该组内默认部件顺序</summary>
    public const String GroupCategory = "Widget.Group";
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

    /// <summary>获取全部组件。按角色、管理员标志与启停过滤，按用户布局排序并过滤用户隐藏，返回</summary>
    /// <param name="roleNames">当前用户角色名。null 表示不按角色权限过滤</param>
    /// <param name="isAdmin">是否系统角色。false 时隐藏 AdminOnly 组件</param>
    /// <param name="userId">当前用户编号。大于0时用用户布局（排序+隐藏）覆盖默认排序</param>
    /// <returns>排序后的组件列表</returns>
    public IList<WidgetAttribute> GetWidgets(ICollection<String> roleNames = null, Boolean isAdmin = false, Int32 userId = 0)
    {
        var list = Scan().Values.ToList();

        if (roleNames != null || isAdmin) list = list.Where(e => IsVisible(e, roleNames, isAdmin)).ToList();

        // 全局禁用过滤
        list = list.Where(e => IsEnabled(e)).ToList();

        // 用户布局（排序+隐藏）一次读取，避免逐组件查询 Parameter
        IDictionary<String, WidgetLayout> layout = null;
        if (userId > 0)
        {
            layout = GetLayout(userId);
            if (layout.Count > 0)
                list = list.Where(e => !IsHidden(layout, e.Name)).ToList();
        }

        // 组配置（组顺序 + 各组内默认顺序）一次读取
        var (groupOrder, groupItems) = LoadGroupConfig();

        // 组顺序 + 用户布局排序
        return OrderWidgets(list, layout, groupOrder, groupItems);
    }

    /// <summary>获取对当前用户可见但用户已隐藏的组件（供工作台恢复面板），按默认组内排序返回</summary>
    /// <param name="userId">当前用户编号</param>
    /// <param name="roleNames">当前用户角色名。null 表示不按角色权限过滤</param>
    /// <param name="isAdmin">是否系统角色。false 时隐藏 AdminOnly 组件</param>
    /// <returns>已隐藏组件列表</returns>
    public IList<WidgetAttribute> GetHiddenWidgets(Int32 userId, ICollection<String> roleNames = null, Boolean isAdmin = false)
    {
        if (userId <= 0) return new List<WidgetAttribute>();

        var layout = GetLayout(userId);
        if (layout.Count == 0) return new List<WidgetAttribute>();

        var rs = new List<WidgetAttribute>();
        foreach (var kv in Scan())
        {
            var e = kv.Value;
            if (!IsHidden(layout, e.Name)) continue;
            if (!IsVisible(e, roleNames, isAdmin)) continue;
            if (!IsEnabled(e)) continue;

            rs.Add(e);
        }

        var (groupOrder, groupItems) = LoadGroupConfig();
        return OrderWidgets(rs, null, groupOrder, groupItems);
    }

    /// <summary>创建组件实例并获取数据。KPI 组件数据统一归一化为 <see cref="KpiModel"/>，保证跨程序集渲染安全</summary>
    /// <param name="info">组件元数据</param>
    /// <returns>组件数据模型</returns>
    public Object GetData(WidgetAttribute info)
    {
        if (Activator.CreateInstance(info.Type) is IWidget widget)
        {
            var data = widget.GetData();

            // KPI 小卡渲染契约 { Value, Trend, Url }。具体应用程序集实现的组件若返回匿名类型（internal），
            // 魔方工作台视图跨程序集 dynamic 绑定会抛 RuntimeBinderException，这里统一转换为公开模型
            if (data != null && info.WidgetType == WidgetTypes.Kpi && data is not KpiModel) data = ToKpiModel(data);

            return data;
        }

        return null;
    }

    /// <summary>将组件原始数据转换为公开 <see cref="KpiModel"/>。反射读取 Value/Trend/Url，兼容匿名类型与各种公开模型</summary>
    /// <param name="data">组件原始数据</param>
    /// <returns></returns>
    static KpiModel ToKpiModel(Object data)
    {
        var type = data.GetType();

        return new KpiModel
        {
            Value = type.GetProperty("Value")?.GetValue(data)?.ToString(),
            Trend = type.GetProperty("Trend")?.GetValue(data)?.ToString(),
            Url = type.GetProperty("Url")?.GetValue(data)?.ToString(),
        };
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
    #endregion

    #region 布局
    /// <summary>读取用户布局。Parameter 单行 JSON（分类 Widget.Layout，Name=config，LongValue=JSON），
    /// 缺失时回退兼容旧的 Widget.Order 逐行数据</summary>
    /// <param name="userId">用户编号</param>
    /// <returns>组件名到布局项的字典</returns>
    public IDictionary<String, WidgetLayout> GetLayout(Int32 userId)
    {
        var dic = new Dictionary<String, WidgetLayout>(StringComparer.OrdinalIgnoreCase);
        if (userId <= 0) return dic;

        var p = Parameter.FindByUserIDAndCategoryAndName(userId, LayoutCategory, "config");
        if (p != null)
        {
            var json = p.Value ?? p.LongValue;
            if (!json.IsNullOrEmpty())
            {
                var models = json.ToJsonEntity<Dictionary<String, WidgetLayout>>();
                if (models != null)
                {
                    foreach (var item in models)
                        dic[item.Key] = item.Value ?? new WidgetLayout();

                    return dic;
                }
            }
        }

        // 兼容旧数据：逐行的 Widget.Order，读取后由首次保存统一切换为 JSON
        var ps = Parameter.FindAllByUserID(userId, OrderCategory);
        foreach (var item in ps)
            dic[item.Name] = new WidgetLayout { Sort = item.Value.ToInt() };

        return dic;
    }

    /// <summary>保存用户布局。合并为单行 Parameter（LongValue=JSON），排序与隐藏原子保存</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="layout">组件名到布局项的字典</param>
    public void SaveLayout(Int32 userId, IDictionary<String, WidgetLayout> layout)
    {
        if (userId <= 0 || layout == null) return;

        var p = Parameter.GetOrAdd(userId, LayoutCategory, "config");
        p.LongValue = layout.ToJson();
        p.Save();
    }

    /// <summary>重置用户布局为默认。删除布局 Parameter 行，恢复出厂排序与显示</summary>
    /// <param name="userId">用户编号</param>
    public void ResetLayout(Int32 userId)
    {
        if (userId <= 0) return;

        var p = Parameter.FindByUserIDAndCategoryAndName(userId, LayoutCategory, "config");
        p?.Delete();
    }

    /// <summary>隐藏或显示组件（用户级布局）</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="name">组件名</param>
    /// <param name="hide">是否隐藏</param>
    public void SetHidden(Int32 userId, String name, Boolean hide)
    {
        if (userId <= 0 || name.IsNullOrEmpty()) return;

        var layout = GetLayout(userId);
        if (hide)
        {
            layout.TryGetValue(name, out var old);
            layout[name] = new WidgetLayout { Sort = old?.Sort ?? 0, Hide = true };
        }
        else
        {
            layout.Remove(name);
        }

        SaveLayout(userId, layout);
    }

    /// <summary>判断布局中组件是否被用户隐藏</summary>
    /// <param name="layout">用户布局</param>
    /// <param name="name">组件名</param>
    /// <returns></returns>
    private static Boolean IsHidden(IDictionary<String, WidgetLayout> layout, String name)
        => layout != null && layout.TryGetValue(name, out var item) && item.Hide;
    #endregion

    #region 排序
    /// <summary>获取组件分组顺序。读取 Parameter 全局配置（分类 Widget.Group，Name=Order），未配置时用内置约定：系统、个人、通用、其它（按名称）</summary>
    /// <returns>组名列表，按显示顺序</returns>
    public IList<String> GetGroupOrder() => LoadGroupConfig().GroupOrder;

    /// <summary>设置组件分组顺序（逗号分隔组名，Parameter 全局配置，UserID=0）</summary>
    /// <param name="groups">组名列表，按显示顺序</param>
    public void SetGroupOrder(IEnumerable<String> groups)
    {
        var p = Parameter.GetOrAdd(0, GroupCategory, "Order");
        p.SetItem("Value", String.Join(",", groups ?? []));
        p.Save();
    }

    /// <summary>获取指定组内的默认部件顺序。读取 Parameter 全局配置（分类 Widget.Group，Name=组名），未配置返回 null（回退部件 Sort）</summary>
    /// <param name="group">组名</param>
    /// <returns>部件名列表，按默认顺序；未配置返回 null</returns>
    public IList<String> GetGroupItemOrder(String group)
    {
        if (group.IsNullOrEmpty()) return null;

        var p = Parameter.FindByUserIDAndCategoryAndName(0, GroupCategory, group);
        var vs = p?.Value;

        return vs.IsNullOrEmpty() ? null : vs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    /// <summary>设置指定组内的默认部件顺序（逗号分隔部件名，Parameter 全局配置，UserID=0）。未列出的新部件自动排到组内末尾</summary>
    /// <param name="group">组名</param>
    /// <param name="names">部件名列表，按默认顺序</param>
    public void SetGroupItemOrder(String group, IEnumerable<String> names)
    {
        if (group.IsNullOrEmpty()) return;

        var p = Parameter.GetOrAdd(0, GroupCategory, group);
        p.SetItem("Value", String.Join(",", names ?? []));
        p.Save();
    }

    /// <summary>加载组配置（组顺序 + 各组内默认顺序），一次查询</summary>
    /// <returns>组顺序与组名到部件顺序的字典</returns>
    private (IList<String> GroupOrder, IDictionary<String, IList<String>> GroupItems) LoadGroupConfig()
    {
        var ps = Parameter.FindAllByUserID(0, GroupCategory);

        IList<String> order = null;
        var items = new Dictionary<String, IList<String>>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in ps)
        {
            if (p.Name == "Order")
                order = p.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            else if (!p.Value.IsNullOrEmpty())
                items[p.Name] = p.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        // 内置约定：系统 < 个人 < 通用 < 其它
        if (order == null || order.Count == 0) order = new List<String> { "系统", "个人", "通用" };

        return (order, items);
    }

    /// <summary>判断组件是否来自外部程序集（非魔方内置）。下游业务系统注册的组件默认排在魔方内置组件之前</summary>
    /// <param name="info">组件元数据。未扫描（Type 为空）视为内置，保证手工构造与测试安全</param>
    internal static Boolean IsExternal(WidgetAttribute info) =>
        info.Type != null && info.Type.Assembly != typeof(WidgetAttribute).Assembly;

    /// <summary>按默认规则排序组件（不叠加用户布局）。KPI 小卡恒在内容卡前，同簇内外部业务组件在魔方内置前，再按组顺序与组内配置/Sort。</summary>
    /// <param name="widgets">组件集合</param>
    /// <returns>排序后的组件列表</returns>
    public IList<WidgetAttribute> SortByDefault(IEnumerable<WidgetAttribute> widgets)
    {
        var list = widgets == null ? new List<WidgetAttribute>() : widgets.ToList();
        if (list.Count == 0) return list;

        var (groupOrder, groupItems) = LoadGroupConfig();
        return OrderWidgets(list, null, groupOrder, groupItems);
    }

    /// <summary>按用户布局、管理员组内配置与组顺序排序组件。无布局时用默认（KPI 簇优先 → 外部业务在魔方前 → 组顺序，同组聚合）；
    /// 有布局时已知组件严格按布局排序值，未覆盖的新组件按默认位智能插入（插到第一个默认排在它后面的已知组件前），避免新卡一律沉底</summary>
    /// <param name="widgets">组件集合</param>
    /// <param name="layout">用户布局。null/空表示无自定义布局</param>
    /// <param name="groupOrder">组顺序。未配置的组排到已配置组之后并按名称排序</param>
    /// <param name="groupItems">各组内默认部件顺序（管理员配置）。null 或组未配置时组内按 <see cref="WidgetAttribute.Sort"/> 排序</param>
    /// <returns>排序后的组件列表</returns>
    internal static IList<WidgetAttribute> OrderWidgets(IEnumerable<WidgetAttribute> widgets, IDictionary<String, WidgetLayout> layout, IList<String> groupOrder, IDictionary<String, IList<String>> groupItems = null)
    {
        var groupIndex = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < groupOrder.Count; i++) groupIndex[groupOrder[i]] = i;

        // 组内位置：优先管理员配置列表；组无配置或部件未列出时回退 Sort（未列出的新部件排到配置列表之后）
        Int32 ItemPos(WidgetAttribute e, out Boolean configured)
        {
            if (groupItems != null && groupItems.TryGetValue(e.Category.IsNullOrEmpty() ? "通用" : e.Category, out var items))
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i].EqualIgnoreCase(e.Name))
                    {
                        configured = true;
                        return i;
                    }
                }
            }

            configured = false;
            return 0;
        }

        // 默认顺序：KPI 小卡簇在前 → 外部业务组件在魔方内置前 → 组（已配置在前按序号，未配置在后按名称）→ 组内（配置在前按位置，未配置按 Sort）
        IEnumerable<WidgetAttribute> DefaultOrder(IEnumerable<WidgetAttribute> src) =>
            src.Select(e => new { W = e, C = e.Category.IsNullOrEmpty() ? "通用" : e.Category, K = e.WidgetType == WidgetTypes.Kpi ? 0 : 1, O = IsExternal(e) ? 0 : 1 })
               .Select(x => new { x.W, x.C, x.K, x.O, P = ItemPos(x.W, out var cfg), Cfg = cfg })
               .OrderBy(x => x.K)
               .ThenBy(x => x.O)
               .ThenBy(x => groupIndex.ContainsKey(x.C) ? 0 : 1)
               .ThenBy(x => groupIndex.TryGetValue(x.C, out var idx) ? idx : 0)
               .ThenBy(x => x.C, StringComparer.OrdinalIgnoreCase)
               .ThenBy(x => x.Cfg ? 0 : 1)
               .ThenBy(x => x.P)
               .ThenBy(x => x.W.Sort)
               .Select(x => x.W);

        if (layout == null || layout.Count == 0) return DefaultOrder(widgets).ToList();

        // 用户已布局的组件严格按布局排序值；新增（未布局）组件按默认位智能插入
        var known = widgets.Where(e => layout.ContainsKey(e.Name)).OrderBy(e => layout[e.Name].Sort).ToList();
        var unknown = widgets.Where(e => !layout.ContainsKey(e.Name)).ToList();

        // 全部已布局：直接按用户序返回；全部未布局：退回默认序
        if (known.Count == 0) return DefaultOrder(widgets).ToList();
        if (unknown.Count == 0) return known;

        // 全量默认序作为秩参照：每个未布局组件插到第一个“默认应排在它之后”的已知组件前，无锚点则追加末尾
        var defaults = DefaultOrder(widgets).ToList();
        var rank = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < defaults.Count; i++) rank[defaults[i].Name] = i;

        var rs = new List<WidgetAttribute>(known.Count + unknown.Count);
        rs.AddRange(known);
        foreach (var u in DefaultOrder(unknown))
        {
            var idx = rs.FindIndex(k => rank.TryGetValue(k.Name, out var r) && r > rank[u.Name]);
            if (idx < 0) rs.Add(u); else rs.Insert(idx, u);
        }

        return rs;
    }

    /// <summary>获取用户自定义排序。读取布局中的排序值，未配置时用默认排序（兼容旧接口）</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="name">组件名</param>
    /// <param name="defaultSort">默认排序</param>
    /// <returns>排序值</returns>
    public Int32 GetOrder(Int32 userId, String name, Int32 defaultSort)
    {
        if (userId <= 0 || name.IsNullOrEmpty()) return defaultSort;

        var layout = GetLayout(userId);
        return layout.TryGetValue(name, out var item) ? item.Sort : defaultSort;
    }

    /// <summary>保存用户组件排序（兼容旧接口，转为布局 JSON 存储，保留已有隐藏状态）</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="orders">组件名到排序值的字典</param>
    public void SetOrders(Int32 userId, IDictionary<String, Int32> orders)
    {
        if (userId <= 0 || orders == null || orders.Count == 0) return;

        var layout = GetLayout(userId);
        foreach (var item in orders)
        {
            layout.TryGetValue(item.Key, out var old);
            layout[item.Key] = new WidgetLayout { Sort = item.Value, Hide = old?.Hide == true };
        }

        SaveLayout(userId, layout);
    }
    #endregion
}
