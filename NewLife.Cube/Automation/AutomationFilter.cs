using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using NewLife.Remoting;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube.Automation;

/// <summary>与前端 matchesViewFilter 同构的 C# 匹配</summary>
public static class AutomationFilter
{
    /// <summary>解析 viewFilter 查询参数（OSC-260819e483 P2）：空返回 null；长度 &gt;4096 或 JSON 损坏抛 ApiException(400)，不执行半截 AST</summary>
    /// <param name="viewFilter">查询参数原文</param>
    /// <returns>筛选条件；空为 null</returns>
    public static ViewFilterDto ParseViewFilter(String viewFilter)
    {
        if (viewFilter.IsNullOrEmpty()) return null;
        if (viewFilter.Length > 4096) throw new ApiException(400, "筛选条件过长");
        try
        {
            return JsonNode.Parse(viewFilter).DeserializeFilter();
        }
        catch
        {
            throw new ApiException(400, "筛选条件无效");
        }
    }

    /// <summary>尝试下推为 SQL Where；任一条件无法下推则返回 null（调用方改分页内存过滤）</summary>
    /// <param name="fact">实体工厂</param>
    /// <param name="filter">筛选条件</param>
    /// <param name="allowedField">字段白名单。列表 / Widget / 树必须传入非 null；null 仅兼容自动化内部调用</param>
    /// <returns>可下推的表达式；无法下推则 null</returns>
    public static Expression TryBuildWhere(IEntityFactory fact, ViewFilterDto filter, Func<String, Boolean>? allowedField = null)
    {
        if (fact == null || filter?.Conditions == null || filter.Conditions.Count == 0) return null;
        // 复杂度上限（OSC-260830a1b2）：与白名单无关，恒生效
        if (filter.Conditions.Count > 10) throw new ApiException(400, "筛选条件过多");
        var any = (filter.Logic + "").EqualIgnoreCase("any");
        if (any && filter.Conditions.Count > 5) throw new ApiException(400, "OR 条件过多");
        Expression exp = null;
        foreach (var c in filter.Conditions)
        {
            // 字段白名单（OSC-260830a1b2）：未下发字段 → 400，不静默放弃，防止布尔侧信道伪造
            if (allowedField != null && !allowedField(c.Field))
                throw new ApiException(400, "筛选条件含未授权字段");
            var piece = TryBuildCondition(fact, c);
            if (piece == null) return null;
            exp = exp == null ? piece : (any ? (exp | piece) : (exp & piece));
        }
        return exp;
    }

    static Expression TryBuildCondition(IEntityFactory fact, ViewFilterConditionDto c)
    {
        if (c == null || c.Field.IsNullOrEmpty()) return null;
        var fi = fact.Fields?.FirstOrDefault(f => f.Name.EqualIgnoreCase(c.Field));
        if (fi == null) return null;
        var op = (c.Op + "").Trim().ToLowerInvariant();
        var val = Unwrap(c.Value);
        try
        {
            return op switch
            {
                "eq" => fi.Equal(val),
                "neq" => fi.NotEqual(val),
                "isnull" => fi.IsNull(),
                "notnull" => fi.NotIsNull(),
                "gt" => fi > val,
                "gte" => fi >= val,
                "lt" => fi < val,
                "lte" => fi <= val,
                "contains" => fi.Contains("" + val),
                "notcontains" => fi.NotContains("" + val),
                "startswith" => fi.StartsWith("" + val),
                "endswith" => fi.EndsWith("" + val),
                "after" => fi > val,
                "before" => fi < val,
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    static Object Unwrap(Object value)
    {
        if (value is JsonElement je) return JsonValue(je);
        if (value is String s)
        {
            if (Decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) &&
                s.IndexOfAny(['.', 'e', 'E']) < 0 && n == Decimal.Truncate(n) && n <= Int64.MaxValue && n >= Int64.MinValue)
                return (Int64)n;
            return s;
        }
        return value;
    }

    /// <summary>空条件恒 true</summary>
    /// <param name="entity">实体</param>
    /// <param name="filter">筛选条件</param>
    /// <param name="allowedField">字段白名单。树控制器必须传入非 null；白名单失败抛 400</param>
    /// <returns>是否匹配</returns>
    public static Boolean Match(IEntity entity, ViewFilterDto filter, Func<String, Boolean>? allowedField = null)
    {
        if (entity == null) return false;
        ValidateFilter(filter, allowedField);
        var map = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        var fact = EntityFactory.CreateFactory(entity.GetType());
        if (fact != null)
        {
            foreach (var f in fact.Fields)
                map[f.Name] = entity[f.Name];
        }
        return Match(map, filter);
    }

    /// <summary>校验白名单与复杂度上限（OSC-260830a1b2）。树控制器内存路径同样适用</summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="allowedField">字段白名单</param>
    static void ValidateFilter(ViewFilterDto filter, Func<String, Boolean>? allowedField)
    {
        if (filter?.Conditions == null || filter.Conditions.Count == 0) return;
        if (filter.Conditions.Count > 10) throw new ApiException(400, "筛选条件过多");
        if ((filter.Logic + "").EqualIgnoreCase("any") && filter.Conditions.Count > 5) throw new ApiException(400, "OR 条件过多");
        if (allowedField == null) return;
        foreach (var c in filter.Conditions)
        {
            if (!allowedField(c.Field)) throw new ApiException(400, "筛选条件含未授权字段");
        }
    }

    /// <summary>字典匹配</summary>
    public static Boolean Match(IDictionary<String, Object> row, ViewFilterDto filter)
    {
        if (filter == null || filter.Conditions == null || filter.Conditions.Count == 0) return true;
        var results = filter.Conditions.Select(c => MatchCondition(row, c)).ToArray();
        var any = (filter.Logic + "").EqualIgnoreCase("any");
        return any ? results.Any(x => x) : results.All(x => x);
    }

    static Boolean MatchCondition(IDictionary<String, Object> row, ViewFilterConditionDto c)
    {
        if (c == null || c.Field.IsNullOrEmpty()) return false;
        var op = (c.Op + "").Trim();
        if (op.IsNullOrEmpty()) return false;
        // 与前端 matchesViewFilter：缺键时 raw==null → isNull 为 true；其它 op 未知字段恒 false
        if (!TryGet(row, c.Field, out var raw))
        {
            if (op.EqualIgnoreCase("isNull")) return true;
            if (op.EqualIgnoreCase("notNull")) return false;
            return false;
        }
        return op.ToLowerInvariant() switch
        {
            "eq" => Eq(raw, c.Value),
            "neq" => !Eq(raw, c.Value),
            // 与前端 String.includes：大小写敏感
            "contains" => Contains(raw, c.Value, false),
            "notcontains" => !Contains(raw, c.Value, false),
            // 与前端 String.startsWith/endsWith：大小写敏感
            "startswith" => StartsWith(raw, c.Value),
            "endswith" => EndsWith(raw, c.Value),
            "isnull" => IsNull(raw),
            "notnull" => !IsNull(raw),
            "gt" => Cmp(raw, c.Value) is { } r1 && r1 > 0,
            "gte" => Cmp(raw, c.Value) is { } r2 && r2 >= 0,
            "lt" => Cmp(raw, c.Value) is { } r3 && r3 < 0,
            "lte" => Cmp(raw, c.Value) is { } r4 && r4 <= 0,
            // after/before：优先日期，否则与前端 compareValues 一样走可解析比较
            "after" => CmpFlexible(raw, c.Value) > 0,
            "before" => CmpFlexible(raw, c.Value) < 0,
            _ => false,
        };
    }

    static Boolean TryGet(IDictionary<String, Object> row, String field, out Object value)
    {
        if (row.TryGetValue(field, out value)) return true;
        foreach (var kv in row)
        {
            if (kv.Key.EqualIgnoreCase(field)) { value = kv.Value; return true; }
        }
        value = null;
        return false;
    }

    static Boolean IsNull(Object raw)
    {
        if (raw == null) return true;
        if (raw is String s) return s.Length == 0;
        if (raw is Array a) return a.Length == 0;
        return false;
    }

    static Boolean Eq(Object raw, Object expected)
    {
        if (expected is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Array)
            {
                foreach (var x in je.EnumerateArray())
                    if (Eq(raw, JsonValue(x))) return true;
                return false;
            }
            expected = JsonValue(je);
        }
        if (expected is System.Collections.IEnumerable en and not String)
        {
            foreach (var x in en)
                if (Eq(raw, x)) return true;
            return false;
        }
        if (raw == null && expected == null) return true;
        if (raw == null || expected == null) return false;
        return String.Equals(raw + "", expected + "", StringComparison.OrdinalIgnoreCase)
            || (TryNum(raw, out var a) && TryNum(expected, out var b) && a == b);
    }

    static Boolean Contains(Object raw, Object expected, Boolean ignoreCase)
    {
        var s = raw + "";
        var t = expected + "";
        return ignoreCase ? s.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 : s.Contains(t);
    }

    /// <summary>与前端 String.startsWith 一致：大小写敏感</summary>
    static Boolean StartsWith(Object raw, Object expected) =>
        (raw + "").StartsWith(expected + "", StringComparison.Ordinal);

    /// <summary>与前端 String.endsWith 一致：大小写敏感</summary>
    static Boolean EndsWith(Object raw, Object expected) =>
        (raw + "").EndsWith(expected + "", StringComparison.Ordinal);

    static Int32? Cmp(Object raw, Object expected)
    {
        if (!TryNum(raw, out var a) || !TryNum(expected, out var b)) return null;
        return a.CompareTo(b);
    }

    static Int32 CmpDate(Object raw, Object expected)
    {
        if (!TryDate(raw, out var a) || !TryDate(expected, out var b)) return 0;
        return a.CompareTo(b);
    }

    /// <summary>日期优先；否则数字/字符串比较（对齐前端 compareValues）</summary>
    static Int32 CmpFlexible(Object raw, Object expected)
    {
        if (TryDate(raw, out var da) && TryDate(expected, out var db)) return da.CompareTo(db);
        if (Cmp(raw, expected) is { } n) return n;
        return String.Compare(raw + "", expected + "", StringComparison.Ordinal);
    }

    static Boolean TryNum(Object v, out Decimal n)
    {
        n = 0;
        if (v == null || v is String s && s.Length == 0) return false;
        if (v is JsonElement je) v = JsonValue(je);
        return Decimal.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out n);
    }

    static Boolean TryDate(Object v, out DateTime d)
    {
        d = default;
        if (v is DateTime dt) { d = dt; return true; }
        if (v is JsonElement je) v = JsonValue(je);
        return DateTime.TryParse(v + "", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d);
    }

    static Object JsonValue(JsonElement je) => je.ValueKind switch
    {
        JsonValueKind.String => je.GetString(),
        JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => je.ToString(),
    };

    /// <summary>确定时间窗命中的时间字段：分表字段，或 *Log* 实体的 UpdateTime/CreateTime（OSC-260830a1b2）</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>时间字段；无则不注入</returns>
    public static FieldItem ResolveFilterTimeField(IEntityFactory fact)
    {
        if (fact == null) return null;
        // 分表字段（DataScale）
        var shard = fact.ShardPolicy?.Field;
        if (shard != null) return shard;
        // *Log* 且含 UpdateTime/CreateTime 数据字段：类型名或表名任一匹配 *Log*（类名不含 Log 但表名含 Log 也命中）
        var tn = fact.EntityType?.Name ?? "";
        var tableName = fact.Table?.TableName ?? "";
        if (!tn.Contains("Log", StringComparison.OrdinalIgnoreCase) && !tableName.Contains("Log", StringComparison.OrdinalIgnoreCase)) return null;
        return fact.Fields?.FirstOrDefault(f => f.Name.EqualIgnoreCase("UpdateTime") || f.Name.EqualIgnoreCase("CreateTime"));
    }

    /// <summary>判断筛选是否已含该时间字段且有值条件（OSC-260830a1b2）。未含返回 false</summary>
    /// <param name="filter">筛选条件（可为 null）</param>
    /// <param name="fieldName">时间字段名</param>
    /// <returns>是否已含时间条件</returns>
    public static Boolean HasTimeCondition(ViewFilterDto filter, String fieldName)
    {
        if (filter?.Conditions == null) return false;
        return filter.Conditions.Any(c =>
            c != null && c.Field.EqualIgnoreCase(fieldName) &&
            (c.Op + "").Trim().ToLowerInvariant() is "after" or "before" or "eq" or "gt" or "gte" or "lt" or "lte");
    }

    /// <summary>获取时间窗配置（OSC-260830a1b2）：无命中返回 false；命中返回时间字段与天数。FilterWindowDays=0 关闭</summary>
    /// <param name="fact">实体工厂</param>
    /// <param name="filter">已解析的筛选条件（可为 null）</param>
    /// <param name="field">命中时间字段</param>
    /// <param name="days">实际注入的天数；0 表示未注入</param>
    /// <returns>是否命中</returns>
    public static Boolean TryGetTimeWindow(IEntityFactory fact, ViewFilterDto filter, out FieldItem field, out Int32 days)
    {
        field = null;
        days = 0;
        var cfg = CubeSetting.Current.FilterWindowDays;
        if (cfg <= 0) return false;
        var d = Math.Clamp(cfg, 0, 3650);
        if (d <= 0) return false;
        var fi = ResolveFilterTimeField(fact);
        if (fi == null) return false;
        var t = Nullable.GetUnderlyingType(fi.Type) ?? fi.Type;
        if (t != typeof(DateTime)) return false;
        if (HasTimeCondition(filter, fi.Name)) return false;
        field = fi;
        days = d;
        return true;
    }

    /// <summary>构建时间窗谓词（OSC-260830a1b2）：无命中返回 null；命中返回天数 out 参数。FilterWindowDays=0 关闭</summary>
    /// <param name="fact">实体工厂</param>
    /// <param name="filter">已解析的筛选条件（可为 null）</param>
    /// <param name="days">实际注入的天数；0 表示未注入</param>
    /// <returns>时间窗表达式；未注入返回 null</returns>
    public static Expression BuildTimeWindow(IEntityFactory fact, ViewFilterDto filter, out Int32 days)
    {
        days = 0;
        if (!TryGetTimeWindow(fact, filter, out var fi, out days)) return null;
        // 与既有 dtStart 惯例一致，用本地时间当日零点起算，避免 UTC 零点把本地今日 0:00–7:59 挤出默认窗口（+8 时区）
        return fi >= DateTime.Now.Date.AddDays(-days);
    }
}
