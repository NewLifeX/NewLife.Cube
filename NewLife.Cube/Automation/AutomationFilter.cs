using System.Globalization;
using System.Text.Json;
using XCode;

namespace NewLife.Cube.Automation;

/// <summary>与前端 matchesViewFilter 同构的 C# 匹配</summary>
public static class AutomationFilter
{
    /// <summary>尝试下推为 SQL Where；任一条件无法下推则返回 null（调用方改分页内存过滤）</summary>
    public static Expression TryBuildWhere(IEntityFactory fact, ViewFilterDto filter)
    {
        if (fact == null || filter?.Conditions == null || filter.Conditions.Count == 0) return null;
        var any = (filter.Logic + "").EqualIgnoreCase("any");
        Expression exp = null;
        foreach (var c in filter.Conditions)
        {
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
    public static Boolean Match(IEntity entity, ViewFilterDto filter)
    {
        if (entity == null) return false;
        var map = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        var fact = EntityFactory.CreateFactory(entity.GetType());
        if (fact != null)
        {
            foreach (var f in fact.Fields)
                map[f.Name] = entity[f.Name];
        }
        return Match(map, filter);
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
}
