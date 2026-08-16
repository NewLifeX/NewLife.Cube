using System.Globalization;
using System.Text.Json;
using XCode;

namespace NewLife.Cube.Automation;

/// <summary>与前端 matchesViewFilter 同构的 C# 匹配</summary>
public static class AutomationFilter
{
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
        // 与前端 matchesViewFilter 对齐：未知字段 → 条件恒 false
        if (!TryGet(row, c.Field, out var raw)) return false;
        return op.ToLowerInvariant() switch
        {
            "eq" => Eq(raw, c.Value),
            "neq" => !Eq(raw, c.Value),
            "contains" => Contains(raw, c.Value, true),
            "notcontains" => !Contains(raw, c.Value, true),
            "isnull" => IsNull(raw),
            "notnull" => !IsNull(raw),
            "gt" => Cmp(raw, c.Value) is { } r1 && r1 > 0,
            "gte" => Cmp(raw, c.Value) is { } r2 && r2 >= 0,
            "lt" => Cmp(raw, c.Value) is { } r3 && r3 < 0,
            "lte" => Cmp(raw, c.Value) is { } r4 && r4 <= 0,
            "after" => CmpDate(raw, c.Value) > 0,
            "before" => CmpDate(raw, c.Value) < 0,
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
