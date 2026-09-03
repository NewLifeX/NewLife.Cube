using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NewLife.Cube.Automation;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>DashboardJson 解析与校验（OSC-2608280e9e / OSC-26082815a1）</summary>
public static class DashboardJson
{
    /// <summary>最大字节</summary>
    public const Int32 MaxBytes = 64 * 1024;

    /// <summary>洞察槽部件上限</summary>
    public const Int32 MaxWidgets = 12;

    /// <summary>工作台部件上限</summary>
    public const Int32 MaxWidgetsWorkbench = 16;

    /// <summary>表面：insight / workbench</summary>
    public const String SurfaceInsight = "insight";

    /// <summary>工作台表面</summary>
    public const String SurfaceWorkbench = "workbench";

    static readonly HashSet<Int32> WidthsInsight = [3, 4, 6, 12];
    static readonly HashSet<Int32> WidthsWorkbench = [2, 3, 4, 6, 8, 12];
    static readonly HashSet<String> PlatformKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "metricCard", "miniChart"
    };
    static readonly HashSet<String> Providers = new(StringComparer.OrdinalIgnoreCase)
    {
        "entity.aggregate", "entity.list", "named"
    };
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>是否工作台表面</summary>
    public static Boolean IsWorkbench(String surface) =>
        !surface.IsNullOrEmpty() && surface.EqualIgnoreCase(SurfaceWorkbench);

    /// <summary>规范化并校验。失败返回 false 与错误文案。默认 insight，保持 e9e 行为。</summary>
    public static Boolean TryNormalize(String json, IUser user, Boolean checkSources, out String normalized, out String error) =>
        TryNormalize(json, user, checkSources, SurfaceInsight, null, out normalized, out error);

    /// <summary>规范化并校验。surface=workbench 时放宽栅格、上限，并允许 miniKanban。</summary>
    public static Boolean TryNormalize(String json, IUser user, Boolean checkSources, String surface, out String normalized, out String error) =>
        TryNormalize(json, user, checkSources, surface, null, out normalized, out error);

    /// <summary>规范化并校验。surface=workbench 时放宽栅格、上限，并允许 miniKanban。hostTypePath=宿主实体 typePath（insight 传当前页；工作台域传 null，含 $host 宿主引用的配置被拒）</summary>
    /// <param name="json">仪表盘配置 JSON</param>
    /// <param name="user">当前用户</param>
    /// <param name="checkSources">是否校验数据源权限</param>
    /// <param name="surface">insight / workbench</param>
    /// <param name="hostTypePath">宿主实体 typePath；null 表示无宿主（工作台）</param>
    /// <param name="normalized">规范化后的 JSON</param>
    /// <param name="error">失败原因</param>
    public static Boolean TryNormalize(String json, IUser user, Boolean checkSources, String surface, String hostTypePath, out String normalized, out String error)
    {
        normalized = null;
        error = null;
        if (json.IsNullOrWhiteSpace())
        {
            normalized = "";
            return true;
        }
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes)
        {
            error = "仪表盘配置过大";
            return false;
        }

        JsonObject root;
        try
        {
            var node = JsonNode.Parse(json);
            root = node as JsonObject;
            if (root == null)
            {
                error = "仪表盘配置无效";
                return false;
            }
        }
        catch
        {
            error = "仪表盘配置无效";
            return false;
        }

        if (root["version"]?.GetValue<Int32>() is not 1)
        {
            error = "仪表盘 version 必须为 1";
            return false;
        }

        var arr = root["widgets"] as JsonArray;
        if (arr == null)
        {
            error = "widgets 必须为数组";
            return false;
        }
        var max = IsWorkbench(surface) ? MaxWidgetsWorkbench : MaxWidgets;
        if (arr.Count > max)
        {
            error = $"部件数量不能超过 {max}";
            return false;
        }

        var ids = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        var items = new List<JsonObject>();
        var order = 0;
        foreach (var n in arr)
        {
            if (n is not JsonObject w)
            {
                error = "部件必须为对象";
                return false;
            }
            if (!NormalizeWidget(w, user, checkSources, order, ids, surface, hostTypePath, out error))
                return false;
            items.Add(w);
            order++;
        }

        items.Sort((a, b) =>
        {
            var ao = a["layout"]?["order"]?.GetValue<Int32>() ?? 0;
            var bo = b["layout"]?["order"]?.GetValue<Int32>() ?? 0;
            return ao.CompareTo(bo);
        });
        for (var i = 0; i < items.Count; i++)
        {
            var layout = items[i]["layout"] as JsonObject ?? new JsonObject();
            layout["order"] = i;
            items[i]["layout"] = layout;
        }

        var outRoot = new JsonObject { ["version"] = 1 };
        var outArr = new JsonArray();
        foreach (var w in items)
            outArr.Add(JsonNode.Parse(w.ToJsonString()));
        outRoot["widgets"] = outArr;
        foreach (var kv in root)
        {
            if (kv.Key.EqualIgnoreCase("version", "widgets")) continue;
            outRoot[kv.Key] = kv.Value == null ? null : JsonNode.Parse(kv.Value.ToJsonString());
        }
        normalized = outRoot.ToJsonString(JsonOpts);
        return true;
    }

    static Boolean NormalizeWidget(JsonObject w, IUser user, Boolean checkSources, Int32 index, HashSet<String> ids, String surface, String hostTypePath, out String error)
    {
        error = null;
        w.Remove("data");
        w.Remove("value");
        w.Remove("items");
        w.Remove("rows");

        var id = w["id"]?.GetValue<String>()?.Trim();
        if (id.IsNullOrEmpty() || !ids.Add(id))
        {
            error = "部件 id 不能为空或重复";
            return false;
        }

        var kind = w["kind"]?.GetValue<String>()?.Trim() ?? "";
        if (kind.EqualIgnoreCase("legacyChart"))
        {
            error = "禁止保存 legacyChart";
            return false;
        }

        var source = w["source"] as JsonObject;
        if (source == null)
        {
            error = "部件缺少 source";
            return false;
        }
        var provider = source["provider"]?.GetValue<String>()?.Trim() ?? "";
        if (!Providers.Contains(provider))
        {
            error = "非法 provider";
            return false;
        }

        // 实体部件源 typePath（供部件查询条件字段白名单校验）
        var srcTypePath = "";
        if (kind.EqualIgnoreCase("metricCard") && !provider.EqualIgnoreCase("entity.aggregate", "named"))
        {
            error = "metricCard 仅允许 entity.aggregate 或 named";
            return false;
        }
        if (kind.EqualIgnoreCase("miniChart") && !provider.EqualIgnoreCase("entity.aggregate"))
        {
            error = "miniChart 仅允许 entity.aggregate";
            return false;
        }
        if (kind.EqualIgnoreCase("miniKanban") && !provider.EqualIgnoreCase("entity.list"))
        {
            error = "miniKanban 仅允许 entity.list";
            return false;
        }
        if (kind.EqualIgnoreCase("dataList") && !provider.EqualIgnoreCase("entity.list"))
        {
            error = "dataList 仅允许 entity.list";
            return false;
        }
        if (kind.EqualIgnoreCase("dataCard") && !provider.EqualIgnoreCase("entity.list"))
        {
            error = "dataCard 仅允许 entity.list";
            return false;
        }

        if (provider.StartsWithIgnoreCase("entity."))
        {
            var typePath = AutomationPaths.NormalizeTypePath(source["typePath"]?.GetValue<String>());
            if (typePath.IsNullOrEmpty())
            {
                error = "entity 部件必须指定 typePath";
                return false;
            }
            source["typePath"] = typePath;
            srcTypePath = typePath;
            if (checkSources && user != null && !AutomationAuth.HasPermission(user, typePath, PermissionFlags.Detail))
            {
                error = $"无权引用实体 {typePath}";
                return false;
            }
        }
        else if (provider.EqualIgnoreCase("named"))
        {
            var name = source["widgetName"]?.GetValue<String>()?.Trim();
            if (name.IsNullOrEmpty())
            {
                error = "named 部件必须指定 widgetName";
                return false;
            }
            var reg = CubeWidgetManager.Find(name);
            if (reg != null && !reg.Kind.IsNullOrEmpty())
                w["kind"] = reg.Kind;
            if (checkSources && user != null)
            {
                if (reg == null || !CubeWidgetManager.Visible(reg, user) || !CubeWidgetManager.MatchesSurface(reg, surface))
                {
                    error = $"无权引用部件 {name}";
                    return false;
                }
            }
        }

        var widths = IsWorkbench(surface) ? WidthsWorkbench : WidthsInsight;
        var layout = w["layout"] as JsonObject ?? new JsonObject();
        var ww = layout["w"]?.GetValue<Int32>() ?? 3;
        if (!widths.Contains(ww)) ww = 3;
        var order = layout["order"]?.GetValue<Int32>() ?? index;
        layout["w"] = ww;
        layout["order"] = order;
        if (layout["h"] != null)
        {
            var h = layout["h"]?.GetValue<Int32>() ?? 1;
            if (h < 1) h = 1;
            if (h > 4) h = 4;
            layout["h"] = h;
        }
        w["layout"] = layout;

        var title = w["title"]?.GetValue<String>() ?? "";
        if (title.Length > 40) w["title"] = title[..40];

        var query = w["query"] as JsonObject;
        if (query != null)
        {
            if (query["buckets"] != null)
            {
                var b = query["buckets"]!.GetValue<Int32>();
                if (b < 1) b = 12;
                if (b > 24) b = 24;
                query["buckets"] = b;
            }
            if (query["limit"] != null)
            {
                var lim = query["limit"]!.GetValue<Int32>();
                // -1 = 全部（不截断）；其余默认 30、上限 300
                if (lim != -1)
                {
                    if (lim < 1) lim = 30;
                    if (lim > 300) lim = 300;
                }
                query["limit"] = lim;
            }

            // 部件查询条件（OSC-260903e2a4）：extraFilter 结构/白名单/复杂度/$host 表面与宿主字段
            if (query["extraFilter"] != null && !ValidateWidgetFilter(query, srcTypePath, hostTypePath, out error))
                return false;
        }

        kind = w["kind"]?.GetValue<String>()?.Trim() ?? kind;
        if (kind.EqualIgnoreCase("miniChart"))
        {
            var chartType = w["style"]?["chartType"]?.GetValue<String>() ?? "bar";
            var q = w["query"] as JsonObject ?? new JsonObject();
            if (chartType.EqualIgnoreCase("bar", "hbar", "pie") && q["groupBy"]?.GetValue<String>().IsNullOrEmpty() != false)
            {
                error = "miniChart bar/hbar/pie 必须指定 groupBy";
                return false;
            }
            if (chartType.EqualIgnoreCase("sparkline", "line") && q["timeField"]?.GetValue<String>().IsNullOrEmpty() != false)
            {
                error = "miniChart line/sparkline 必须指定 timeField";
                return false;
            }
        }
        if (kind.EqualIgnoreCase("miniKanban") && !IsWorkbench(surface))
        {
            error = "页面仪表盘不支持数据看板";
            return false;
        }
        if (kind.EqualIgnoreCase("dataList") && !IsWorkbench(surface))
        {
            error = "页面仪表盘不支持数据列表";
            return false;
        }
        if (kind.EqualIgnoreCase("dataCard") && !IsWorkbench(surface))
        {
            error = "页面仪表盘不支持数据卡片";
            return false;
        }

        _ = PlatformKinds;
        return true;
    }

    /// <summary>查询条件操作符白名单（与 AutomationFilter SQL 下推一致，OSC-260903e2a4）</summary>
    static readonly HashSet<String> FilterOps = new(StringComparer.OrdinalIgnoreCase)
    {
        "eq", "neq", "contains", "notcontains", "startswith", "endswith",
        "isnull", "notnull", "gt", "gte", "lt", "lte", "after", "before"
    };

    /// <summary>校验部件查询条件 extraFilter：结构/复杂度/源字段白名单/操作符/$host 表面与宿主字段（OSC-260903e2a4）</summary>
    /// <param name="query">部件 query 节点</param>
    /// <param name="srcTypePath">源实体 typePath（实体部件）；named 为空</param>
    /// <param name="hostTypePath">宿主 typePath；null=无宿主（工作台域，$host 拒绝）</param>
    /// <param name="error">失败原因</param>
    static Boolean ValidateWidgetFilter(JsonObject query, String srcTypePath, String hostTypePath, out String error)
    {
        error = null;
        if (query?["extraFilter"] is not JsonObject f) return true;
        var logic = (f["logic"]?.ToString() ?? "all").Trim().ToLowerInvariant();
        if (logic != "all" && logic != "any")
        {
            error = "部件查询条件 logic 必须为 all 或 any";
            return false;
        }
        if (f["conditions"] is not JsonArray arr)
        {
            error = "部件查询条件 conditions 必须为数组";
            return false;
        }
        if (arr.Count > 10)
        {
            error = "筛选条件过多";
            return false;
        }
        if (logic == "any" && arr.Count > 5)
        {
            error = "OR 条件过多";
            return false;
        }
        if (arr.Count == 0) return true;

        // 源实体 search∪list 白名单（与查询端 WidgetQueryService 一致）；仅当有源实体时校验
        HashSet<String> allowed = null;
        if (!srcTypePath.IsNullOrEmpty())
        {
            var fact = FindFactory(srcTypePath);
            if (fact == null)
            {
                error = "未知实体";
                return false;
            }
            allowed = WidgetQueryService.GetAllowedNames(fact);
        }
        // 宿主字段集（$host 引用须属于宿主实体字段）
        HashSet<String> hostNames = null;
        if (!hostTypePath.IsNullOrEmpty())
        {
            var hf = FindFactory(hostTypePath);
            if (hf == null)
            {
                error = "未知宿主实体";
                return false;
            }
            hostNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (var fi in hf.Fields)
                hostNames.Add(fi.Name);
        }

        foreach (var node in arr)
        {
            if (node is not JsonObject cn) continue;
            var field = (cn["field"]?.ToString() ?? "").Trim();
            if (field.IsNullOrEmpty())
            {
                error = "部件查询条件缺少字段";
                return false;
            }
            if (allowed != null && !allowed.Contains(field))
            {
                error = $"部件查询条件含未授权字段 {field}";
                return false;
            }
            var op = (cn["op"]?.ToString() ?? "").Trim().ToLowerInvariant();
            if (op.Length == 0 || !FilterOps.Contains(op))
            {
                error = "部件查询条件操作符非法";
                return false;
            }
            // 值：对象仅允许 $host 宿主引用标记；其余字面量/数组/空放行
            if (cn["value"] is JsonObject vo)
            {
                if (vo.Count != 1 || !vo.TryGetPropertyValue("$host", out var hn) ||
                    hn is not JsonValue hj || !hj.TryGetValue<String>(out var hostRaw) ||
                    String.IsNullOrWhiteSpace(hostRaw))
                {
                    error = "部件查询条件值无效";
                    return false;
                }
                var hostField = hostRaw.Trim();
                if (hostTypePath.IsNullOrEmpty())
                {
                    error = "工作台不支持宿主引用";
                    return false;
                }
                if (hostNames != null && !hostNames.Contains(hostField))
                {
                    error = $"未知宿主字段 {hostField}";
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>按 typePath 解析实体工厂（实体部件查询条件校验用）</summary>
    static IEntityFactory FindFactory(String typePath)
    {
        var type = WidgetQueryService.FindEntityType(typePath);
        return type == null ? null : EntityFactory.CreateFactory(type);
    }
}
