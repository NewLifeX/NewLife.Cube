using System.Text.Json;
using System.Text.Json.Nodes;
using JsonArray = System.Text.Json.Nodes.JsonArray;
using JsonObject = System.Text.Json.Nodes.JsonObject;
using JsonNode = System.Text.Json.Nodes.JsonNode;

namespace NewLife.Cube.Automation;

/// <summary>筛选条件（与 OSC-0015 ViewFilter 同构）</summary>
public class ViewFilterDto
{
    /// <summary>all=AND，any=OR</summary>
    public String Logic { get; set; } = "all";

    /// <summary>条件</summary>
    public List<ViewFilterConditionDto> Conditions { get; set; } = [];
}

/// <summary>单条筛选条件</summary>
public class ViewFilterConditionDto
{
    /// <summary>字段名</summary>
    public String Field { get; set; }

    /// <summary>操作符</summary>
    public String Op { get; set; }

    /// <summary>值</summary>
    public Object Value { get; set; }
}

/// <summary>表单动作草稿</summary>
public class ActionDraft
{
    /// <summary>节点类型</summary>
    public String Type { get; set; }

    /// <summary>节点 data</summary>
    public JsonNode Data { get; set; }
}

/// <summary>表单编译入参</summary>
public class AutomationCompileInput
{
    /// <summary>触发</summary>
    public String TriggerKind { get; set; }

    /// <summary>筛选</summary>
    public ViewFilterDto Filter { get; set; }

    /// <summary>动作</summary>
    public List<ActionDraft> Actions { get; set; }
}

/// <summary>GraphJson 编译 / 校验 / 反编译</summary>
public static class AutomationGraph
{
    /// <summary>本号可执行节点</summary>
    public static readonly HashSet<String> ImplementedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "start", "filter", "notify", "updateRecord", "createRecord", "findRecords",
        "httpRequest", "delay", "runAutomation", "addComment", "aiText", "end"
    };

    /// <summary>预留未实现</summary>
    public static readonly HashSet<String> ReservedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "condition", "switch", "loop", "approval"
    };

    /// <summary>表单动作类型</summary>
    public static readonly HashSet<String> ActionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "notify", "updateRecord", "createRecord", "findRecords",
        "httpRequest", "delay", "runAutomation", "addComment", "aiText"
    };

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>表单 → 线性图。非法动作剔除；超过 20 截断。</summary>
    public static JsonObject Compile(AutomationCompileInput input)
    {
        input ??= new AutomationCompileInput();
        var kind = (input.TriggerKind + "").Trim().ToLowerInvariant();
        var nodes = new JsonArray();
        var edges = new JsonArray();
        var n = 0;
        String LastId() => "n" + (n - 1);

        void AddNode(String type, JsonNode data)
        {
            var id = "n" + n;
            var node = new JsonObject
            {
                ["id"] = id,
                ["type"] = type,
                ["data"] = data ?? new JsonObject(),
            };
            if (n > 0)
            {
                edges.Add(new JsonObject
                {
                    ["id"] = "e" + (n - 1),
                    ["source"] = LastId(),
                    ["target"] = id,
                });
            }
            nodes.Add(node);
            n++;
        }

        AddNode("start", new JsonObject { ["triggerKind"] = kind });
        var filter = input.Filter;
        if (filter?.Conditions != null && filter.Conditions.Count > 0)
        {
            var fjson = JsonSerializer.SerializeToNode(filter, JsonOpts);
            AddNode("filter", new JsonObject { ["filter"] = fjson });
        }

        var actions = input.Actions ?? [];
        var count = 0;
        foreach (var a in actions)
        {
            var t = (a.Type + "").Trim();
            if (!ActionTypes.Contains(t)) continue;
            if (count >= 20) break;
            AddNode(t, a.Data ?? new JsonObject());
            count++;
        }
        AddNode("end", new JsonObject());

        return new JsonObject
        {
            ["version"] = 1,
            ["nodes"] = nodes,
            ["edges"] = edges,
        };
    }

    /// <summary>保存校验：线性链、version≤1、无预留/未知 type</summary>
    public static String ValidateForSave(JsonNode graph)
    {
        if (graph is not JsonObject obj) return "图必须是对象";
        var ver = obj["version"]?.GetValue<Int32>() ?? 1;
        if (ver > 1) return "不支持的图版本";
        var nodes = obj["nodes"] as JsonArray;
        var edges = obj["edges"] as JsonArray;
        if (nodes == null || nodes.Count == 0) return "缺少节点";
        var ids = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nodes)
        {
            if (n is not JsonObject no) return "非法节点";
            var id = no["id"]?.ToString();
            var type = no["type"]?.ToString();
            if (id.IsNullOrEmpty() || !ids.Add(id)) return "节点 id 重复或为空";
            if (type.IsNullOrEmpty()) return "节点 type 为空";
            if (ReservedTypes.Contains(type) || !ImplementedTypes.Contains(type))
                return $"节点类型未实现或不允许保存：{type}";
        }
        // 线性：每个非 end 出度 1，无环
        var outgoing = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        if (edges != null)
        {
            foreach (var e in edges)
            {
                if (e is not JsonObject eo) continue;
                var s = eo["source"]?.ToString();
                var t = eo["target"]?.ToString();
                if (s.IsNullOrEmpty() || t.IsNullOrEmpty()) return "边缺少 source/target";
                if (outgoing.ContainsKey(s)) return "图必须是单链（禁止分叉）";
                outgoing[s] = t;
            }
        }
        var start = nodes.OfType<JsonObject>().FirstOrDefault(x => (x["type"] + "").EqualIgnoreCase("start"));
        if (start == null) return "缺少 start";
        var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        var cur = start["id"]?.ToString();
        while (!cur.IsNullOrEmpty())
        {
            if (!seen.Add(cur)) return "图存在环";
            if (!outgoing.TryGetValue(cur, out var next)) break;
            cur = next;
        }
        return null;
    }

    /// <summary>执行期校验：允许脏数据中的预留类型存在，由执行器失败闭合</summary>
    public static List<(String Id, String Type, JsonNode Data)> LinearNodes(JsonNode graph)
    {
        var obj = graph as JsonObject ?? [];
        var nodes = (obj["nodes"] as JsonArray) ?? [];
        var edges = (obj["edges"] as JsonArray) ?? [];
        var map = new Dictionary<String, JsonObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nodes.OfType<JsonObject>())
        {
            var id = n["id"]?.ToString();
            if (!id.IsNullOrEmpty()) map[id] = n;
        }
        var outgoing = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in edges.OfType<JsonObject>())
        {
            var s = e["source"]?.ToString();
            var t = e["target"]?.ToString();
            if (!s.IsNullOrEmpty() && !t.IsNullOrEmpty() && !outgoing.ContainsKey(s))
                outgoing[s] = t;
        }
        var start = nodes.OfType<JsonObject>().FirstOrDefault(x => (x["type"] + "").EqualIgnoreCase("start"));
        var list = new List<(String, String, JsonNode)>();
        var cur = start?["id"]?.ToString();
        var guard = 0;
        while (!cur.IsNullOrEmpty() && map.TryGetValue(cur, out var no) && guard++ < 64)
        {
            list.Add((cur, no["type"]?.ToString() ?? "", no["data"] ?? new JsonObject()));
            if (!outgoing.TryGetValue(cur, out cur)) break;
        }
        return list;
    }

    /// <summary>解析 JSON 文本，缺省 version=1</summary>
    public static JsonNode Parse(String json)
    {
        if (json.IsNullOrEmpty()) return new JsonObject { ["version"] = 1, ["nodes"] = new JsonArray(), ["edges"] = new JsonArray() };
        var node = JsonNode.Parse(json);
        if (node is JsonObject o && o["version"] == null) o["version"] = 1;
        return node;
    }

    /// <summary>序列化（保留未知字段）</summary>
    public static String ToJson(JsonNode node) => node?.ToJsonString(JsonOpts) ?? "{}";
}
