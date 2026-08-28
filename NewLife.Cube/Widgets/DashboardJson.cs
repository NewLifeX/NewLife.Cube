using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NewLife.Cube.Automation;
using XCode.Membership;

namespace NewLife.Cube.Widgets;

/// <summary>DashboardJson 解析与校验（OSC-2608280e9e）</summary>
public static class DashboardJson
{
    /// <summary>最大字节</summary>
    public const Int32 MaxBytes = 64 * 1024;

    /// <summary>部件上限</summary>
    public const Int32 MaxWidgets = 12;

    static readonly HashSet<Int32> Widths = [3, 4, 6, 12];
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

    /// <summary>规范化并校验。失败返回 false 与错误文案。</summary>
    public static Boolean TryNormalize(String json, IUser user, Boolean checkSources, out String normalized, out String error)
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
        if (arr.Count > MaxWidgets)
        {
            error = "部件数量不能超过 12";
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
            if (!NormalizeWidget(w, user, checkSources, order, ids, out error))
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

    static Boolean NormalizeWidget(JsonObject w, IUser user, Boolean checkSources, Int32 index, HashSet<String> ids, out String error)
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

        if (provider.StartsWithIgnoreCase("entity."))
        {
            var typePath = AutomationPaths.NormalizeTypePath(source["typePath"]?.GetValue<String>());
            if (typePath.IsNullOrEmpty())
            {
                error = "entity 部件必须指定 typePath";
                return false;
            }
            source["typePath"] = typePath;
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
            if (checkSources && user != null)
            {
                var reg = CubeWidgetManager.Find(name);
                if (reg == null || !CubeWidgetManager.Visible(reg, user))
                {
                    error = $"无权引用部件 {name}";
                    return false;
                }
            }
        }

        var layout = w["layout"] as JsonObject ?? new JsonObject();
        var ww = layout["w"]?.GetValue<Int32>() ?? 3;
        if (!Widths.Contains(ww)) ww = 3;
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
                if (lim < 1) lim = 30;
                if (lim > 50) lim = 50;
                query["limit"] = lim;
            }
        }

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
        // 页面仪表盘（ViewProfile.DashboardJson）本号不允许迷你看板
        if (kind.EqualIgnoreCase("miniKanban"))
        {
            error = "页面仪表盘不支持迷你看板";
            return false;
        }

        _ = PlatformKinds;
        return true;
    }
}
