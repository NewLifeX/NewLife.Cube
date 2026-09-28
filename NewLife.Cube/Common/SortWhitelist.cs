using System.Text.RegularExpressions;
using NewLife.Remoting;
using NewLife.Web;

namespace NewLife.Cube;

/// <summary>多级排序白名单（OSC-26092694a1）。只接受查询参数 sorts，不读取客户端 orderby</summary>
public static class SortWhitelist
{
    static readonly Regex FieldName = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    /// <summary>把 sorts 编译成 OrderBy。参数缺失（null）时不改 pager；非法则抛 400</summary>
    /// <param name="pager">分页参数</param>
    /// <param name="sorts">查询参数 sorts；null 表示参数不存在</param>
    /// <param name="factoryFields">实体工厂字段名</param>
    /// <param name="allowed">search∪list 白名单</param>
    public static void Apply(Pager pager, String sorts, IEnumerable<String> factoryFields, Func<String, Boolean> allowed)
    {
        if (sorts == null) return;
        pager.Sort = null;
        pager.OrderBy = Compile(sorts, factoryFields, allowed);
    }

    /// <summary>编译 sorts 为服务端 OrderBy 串，字段名按工厂名大小写不敏感匹配</summary>
    /// <param name="raw">sorts 原文</param>
    /// <param name="factoryFields">实体工厂字段名</param>
    /// <param name="allowed">search∪list 白名单</param>
    /// <returns>形如 Name asc, CreateTime desc</returns>
    public static String Compile(String raw, IEnumerable<String> factoryFields, Func<String, Boolean> allowed)
    {
        if (raw.IsNullOrEmpty()) throw new ApiException(400, "排序条件无效");
        var parts = raw.Split(',');
        if (parts.Length == 0 || parts.Length > 3) throw new ApiException(400, "排序条件无效");

        var canon = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        if (factoryFields != null)
        {
            foreach (var name in factoryFields)
            {
                if (!name.IsNullOrEmpty() && !canon.ContainsKey(name)) canon[name] = name;
            }
        }

        var clauses = new List<String>(parts.Length);
        foreach (var part in parts)
        {
            if (part.IsNullOrEmpty()) throw new ApiException(400, "排序条件无效");
            var desc = part[0] == '-';
            var token = desc ? part.Substring(1) : part;
            if (token.Length == 0 || !FieldName.IsMatch(token)) throw new ApiException(400, "排序条件无效");
            if (!canon.TryGetValue(token, out var name)) throw new ApiException(400, "排序字段不存在");
            if (allowed != null && !allowed(name)) throw new ApiException(400, "排序字段不存在");
            clauses.Add(name + (desc ? " desc" : " asc"));
        }
        return String.Join(", ", clauses);
    }
}
