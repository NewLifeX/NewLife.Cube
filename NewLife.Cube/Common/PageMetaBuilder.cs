using System.ComponentModel;
using System.Reflection;
using NewLife.Cube.ViewModels;
using NewLife.Serialization;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>页面元数据构建器。视图自描述，每个视图直接输出完整字段数组</summary>
/// <remarks>
/// 不下发字段注册表与 patch 差异：list/form/add/detail/search 各自完整可渲染，
/// 消费端无需合并。用户列偏好通过 list.order / list.hidden 表达。
/// </remarks>
internal static class PageMetaBuilder
{
    #region 视图
    /// <summary>构建自描述字段数组</summary>
    /// <param name="fields">字段集合</param>
    /// <returns>字段字典数组，含 name 与 virtual 标记</returns>
    public static List<IDictionary<String, Object>> BuildFields(IEnumerable<DataField> fields)
    {
        var list = new List<IDictionary<String, Object>>();
        if (fields == null) return list;

        foreach (var field in fields)
        {
            var dic = field.ToDictionary();
            // 非实体列（链接列、虚拟查询项）由前端只读渲染或交给渲染器
            if (field.Field == null) dic["virtual"] = true;
            // 列表默认隐藏但保留在可用列中，前端表格设置可重新打开
            if (field is ListField { Visible: false }) dic["visible"] = false;
            list.Add(dic);
        }

        return list;
    }

    /// <summary>为 TraceId 列表字段生成星尘追踪链接。控制器显式设置 Url 时不覆盖</summary>
    /// <param name="fields">列表字段集合（就地修改）</param>
    /// <param name="starWeb">星尘 Web 地址。为空时不生成链接</param>
    public static void ApplyTraceUrl(IEnumerable<DataField> fields, String starWeb)
    {
        if (fields == null || starWeb.IsNullOrEmpty()) return;

        // 只处理显式的 TraceId 列；控制器已自定义链接（如 LogController）时以控制器为准
        var field = fields.OfType<ListField>().FirstOrDefault(e => e.Name.EqualIgnoreCase("TraceId"));
        if (field == null || !field.Url.IsNullOrEmpty()) return;

        field.Url = starWeb.Contains("{traceId}", StringComparison.OrdinalIgnoreCase)
            ? starWeb.Replace("{traceId}", "{TraceId}", StringComparison.OrdinalIgnoreCase)
            : starWeb.TrimEnd('/') + "/trace?id={TraceId}";
        field.Target ??= "_blank";
        if (field.Text.IsNullOrEmpty()) field.Text = "追踪";
    }

    /// <summary>构建搜索区查询项</summary>
    /// <param name="fields">搜索字段集合</param>
    /// <param name="factory">实体工厂，用于识别主时间字段；为空时不生成时间区间</param>
    /// <returns>查询项数组。Q 收敛为 keyword，主时间字段收敛为 dtStart/dtEnd 区间</returns>
    public static List<IDictionary<String, Object>> BuildSearch(IEnumerable<DataField> fields, IEntityFactory factory = null)
    {
        var list = BuildFields(fields);
        foreach (var dic in list)
        {
            if (dic.TryGetValue("name", out var name) && String.Equals(name as String, "Q", StringComparison.OrdinalIgnoreCase))
            {
                dic["name"] = "q";
                dic["kind"] = "keyword";
            }
        }

        // 主时间字段统一收敛为 dtStart/dtEnd，后端默认 Search 已按这两个参数拼接区间
        var masterTime = factory?.MasterTime;
        if (masterTime != null)
        {
            String displayName = null;
            var masterField = list.FirstOrDefault(e => String.Equals(e.TryGetValue("name", out var n) ? n as String : null, masterTime.Name, StringComparison.OrdinalIgnoreCase));
            if (masterField != null && masterField.TryGetValue("displayName", out var dn)) displayName = dn as String;

            // MasterTime 可能是雪花主键（DataScale=time）：主键显示名（如“编号”）不适合当区间标题，
            // 且时间过滤实际由实体的创建/更新时间承担（控制器从 dtStart 换算雪花区间），
            // 因此借用该时间字段的显示名，并把它从搜索项里摘掉，避免搜索区出现两个含义相同的时间区间
            if (masterTime.Type != typeof(DateTime))
            {
                if (TryRemoveTimeField(list, "CreateTime", out var timeName) || TryRemoveTimeField(list, "UpdateTime", out timeName))
                    displayName = timeName;
                else
                {
                    // 搜索字段里没有时间字段时，退回实体字段的显示名（按请求语言翻译）
                    var timeField = factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase("CreateTime"))
                        ?? factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase("UpdateTime"));
                    if (timeField != null && !timeField.DisplayName.IsNullOrEmpty()) displayName = L.T(timeField.DisplayName);
                }
            }

            list.RemoveAll(e => String.Equals(e.TryGetValue("name", out var n) ? n as String : null, masterTime.Name, StringComparison.OrdinalIgnoreCase));

            list.Add(new Dictionary<String, Object>
            {
                ["kind"] = "range",
                ["name"] = "dtStart",
                ["end"] = "dtEnd",
                ["field"] = masterTime.Name,
                ["displayName"] = displayName.IsNullOrEmpty() ? masterTime.DisplayName : displayName,
                ["typeName"] = "DateTime",
            });
        }

        return list;
    }

    /// <summary>从字段输出列表中摘除指定字段，并取出其显示名</summary>
    /// <param name="list">已构建的字段输出列表（就地修改）</param>
    /// <param name="name">字段名</param>
    /// <param name="displayName">被摘除字段的显示名，可能为空</param>
    /// <returns>是否找到并摘除</returns>
    private static Boolean TryRemoveTimeField(List<IDictionary<String, Object>> list, String name, out String displayName)
    {
        displayName = null;

        var index = list.FindIndex(e => String.Equals(e.TryGetValue("name", out var n) ? n as String : null, name, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;

        if (list[index].TryGetValue("displayName", out var dn) && dn is String str) displayName = str;
        list.RemoveAt(index);

        return true;
    }

    /// <summary>构建实体标识块</summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="factory">实体工厂，可为空（单对象页）</param>
    /// <returns>实体名称、显示名与主键名</returns>
    public static IDictionary<String, Object> BuildEntity(Type entityType, IEntityFactory factory)
    {
        var dic = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        if (entityType == null) return dic;

        var name = entityType.Name;
        var displayName = entityType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? factory?.Table?.DataTable?.DisplayName;
        var primaryKey = factory?.Unique?.Name
            ?? factory?.Table?.PrimaryKeys?.FirstOrDefault()?.Name;

        dic["name"] = name;
        if (!displayName.IsNullOrEmpty()) dic["displayName"] = L.T(displayName);
        if (!primaryKey.IsNullOrEmpty()) dic["primaryKey"] = primaryKey;

        return dic;
    }
    #endregion

    #region 比较
    /// <summary>判断两个字段集合的名称与属性是否完全一致</summary>
    /// <param name="source">基准字段集合</param>
    /// <param name="target">目标字段集合</param>
    /// <returns>名称顺序与属性均一致时返回 true</returns>
    public static Boolean SameFields(IList<DataField> source, IList<DataField> target)
    {
        if (ReferenceEquals(source, target)) return true;
        if (source == null || target == null) return false;
        if (source.Count != target.Count) return false;

        for (var i = 0; i < source.Count; i++)
        {
            var x = source[i];
            var y = target[i];
            if (!String.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (!DictionaryEquals(x.ToDictionary(), y.ToDictionary())) return false;
        }

        return true;
    }

    #endregion

    #region 辅助
    private static Boolean DictionaryEquals(IDictionary<String, Object> x, IDictionary<String, Object> y)
    {
        if (x == null || y == null) return x == y;
        if (x.Count != y.Count) return false;

        foreach (var kv in x)
        {
            if (!y.TryGetValue(kv.Key, out var v) || !ValueEquals(kv.Value, v)) return false;
        }

        return true;
    }

    private static Boolean ValueEquals(Object x, Object y)
    {
        if (Equals(x, y)) return true;
        if (x == null || y == null) return false;
        if (x is String || y is String) return x + "" == y + "";

        return x.ToJson() == y.ToJson();
    }
    #endregion
}
