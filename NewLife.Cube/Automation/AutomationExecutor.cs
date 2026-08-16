using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode;

namespace NewLife.Cube.Automation;

/// <summary>一次执行上下文</summary>
public class AutomationContext
{
    /// <summary>当前记录</summary>
    public IEntity Current { get; set; }
    /// <summary>当前工厂</summary>
    public IEntityFactory Factory { get; set; }
    /// <summary>规则</summary>
    public EntityAutomation Rule { get; set; }
    /// <summary>运行行</summary>
    public AutomationRun Run { get; set; }
    /// <summary>查找结果</summary>
    public IList<IEntity> Found { get; set; } = [];
    /// <summary>查找游标</summary>
    public IEntity FoundCurrent { get; set; }
    /// <summary>新创建</summary>
    public IEntity Created { get; set; }
    /// <summary>Webhook 体</summary>
    public JsonObject Webhook { get; set; }
    /// <summary>轨迹</summary>
    public JsonArray Trace { get; set; } = [];
    /// <summary>从该节点 id 之后继续（delay 续跑）</summary>
    public String ResumeAfter { get; set; }
}

/// <summary>顺序执行 GraphJson</summary>
public static class AutomationExecutor
{
    static readonly Regex Tpl = new(@"\{\{([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)?)\}\}", RegexOptions.Compiled);

    /// <summary>执行一条 Run</summary>
    public static void Execute(AutomationRun run, JsonObject webhook = null)
    {
        if (run == null) return;
        var rule = EntityAutomation.FindById(run.AutomationId);
        if (rule == null)
        {
            run.Status = "failed";
            run.Error = "规则不存在";
            run.Update();
            AutomationFlowLog.WriteTerminal(null, run);
            return;
        }

        run.Status = "running";
        run.Update();

        var ctx = new AutomationContext { Rule = rule, Run = run, Webhook = webhook };
        using (AutomationScope.Enter(run.Depth))
        {
            try
            {
                LoadCurrent(ctx);
                var graph = AutomationGraph.Parse(rule.GraphJson);
                var nodes = AutomationGraph.LinearNodes(graph);
                var skip = !ctx.ResumeAfter.IsNullOrEmpty();
                for (var i = 0; i < nodes.Count; )
                {
                    var (id, type, dataObj) = nodes[i];
                    var data = dataObj as JsonObject ?? new JsonObject();
                    if (skip)
                    {
                        if (id.EqualIgnoreCase(ctx.ResumeAfter)) skip = false;
                        i++;
                        continue;
                    }

                    // found 连续段：对每条 found 整段执行（design §4.4）；空 found 则跳过整段不失败
                    if (IsFoundTargetNode(type, data))
                    {
                        var seg = new List<(String id, String type, JsonObject data)>();
                        var j = i;
                        while (j < nodes.Count)
                        {
                            var (sid, stype, sdataObj) = nodes[j];
                            var sdata = sdataObj as JsonObject ?? new JsonObject();
                            if (!IsFoundTargetNode(stype, sdata)) break;
                            seg.Add((sid, stype, sdata));
                            j++;
                        }
                        if (ctx.Found is not { Count: > 0 })
                        {
                            foreach (var (sid, stype, _) in seg)
                            {
                                ctx.Trace.Add(new JsonObject
                                {
                                    ["nodeId"] = sid,
                                    ["type"] = stype,
                                    ["at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    ["ok"] = true,
                                    ["detail"] = "found empty skip",
                                });
                            }
                            i = j;
                            continue;
                        }
                        var segFailed = false;
                        String segFailDetail = null;
                        foreach (var rec in ctx.Found)
                        {
                            ctx.FoundCurrent = rec;
                            foreach (var (sid, stype, sdata) in seg)
                            {
                                var ok = AutomationActions.Run(ctx, stype, sdata, out var detail);
                                ctx.Trace.Add(new JsonObject
                                {
                                    ["nodeId"] = sid,
                                    ["type"] = stype,
                                    ["at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    ["ok"] = ok,
                                    ["detail"] = detail,
                                    ["foundKey"] = AutomationPaths.RecordKey(rec),
                                });
                                if (!ok && !stype.EqualIgnoreCase("notify"))
                                {
                                    segFailed = true;
                                    segFailDetail = detail;
                                    break;
                                }
                            }
                            if (segFailed) break;
                        }
                        ctx.FoundCurrent = null;
                        if (segFailed)
                        {
                            Finish(rule, run, "failed", (segFailDetail + "").Cut(500), ctx.Trace.ToJsonString());
                            return;
                        }
                        i = j;
                        continue;
                    }

                    var okNode = RunNode(ctx, id, type, data, out var delayMinutes, out var detailNode);
                    ctx.Trace.Add(new JsonObject
                    {
                        ["nodeId"] = id,
                        ["type"] = type,
                        ["at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ["ok"] = okNode,
                        ["detail"] = detailNode,
                    });
                    if (delayMinutes > 0)
                    {
                        run.Status = "waiting";
                        run.ResumeAt = DateTime.Now.AddMinutes(delayMinutes);
                        run.NodeTrace = WrapTrace(ctx.Trace, id);
                        run.Update();
                        return;
                    }
                    if (!okNode && type.EqualIgnoreCase("filter"))
                    {
                        Finish(rule, run, "succeeded", "条件未匹配", ctx.Trace.ToJsonString());
                        return;
                    }
                    if (!okNode && !type.EqualIgnoreCase("notify"))
                    {
                        Finish(rule, run, "failed", (detailNode + "").Cut(500), ctx.Trace.ToJsonString());
                        return;
                    }
                    i++;
                }
                Finish(rule, run, "succeeded", null, ctx.Trace.ToJsonString());
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                Finish(rule, run, "failed", ex.Message.Cut(500), ctx.Trace.ToJsonString());
            }
        }
    }

    static void Finish(EntityAutomation rule, AutomationRun run, String status, String error, String nodeTrace)
    {
        run.Status = status;
        run.Error = error;
        run.NodeTrace = nodeTrace;
        run.Update();
        AutomationFlowLog.WriteTerminal(rule, run);
    }

    static String WrapTrace(JsonArray items, String resumeId)
    {
        var o = new JsonObject { ["resume"] = resumeId, ["items"] = items };
        return o.ToJsonString();
    }

    /// <summary>从 NodeTrace 读取 resume</summary>
    public static String ReadResume(String nodeTrace)
    {
        if (nodeTrace.IsNullOrEmpty()) return null;
        try
        {
            var n = JsonNode.Parse(nodeTrace);
            return n?["resume"]?.ToString();
        }
        catch { return null; }
    }

    static void LoadCurrent(AutomationContext ctx)
    {
        ctx.ResumeAfter = ReadResume(ctx.Run.NodeTrace);
        if (ctx.Run.RecordKey.IsNullOrEmpty()) return;
        var type = ResolveEntityType(ctx.Rule.TypePath);
        if (type == null) return;
        ctx.Factory = EntityFactory.CreateFactory(type);
        ctx.Current = ctx.Factory?.FindByKey(ctx.Run.RecordKey);
    }

    /// <summary>TypePath → 实体类型</summary>
    public static Type ResolveEntityType(String typePath)
    {
        typePath = AutomationPaths.NormalizeTypePath(typePath);
        if (typePath.IsNullOrEmpty()) return null;
        foreach (var kv in EntityPageRegistry.GetAll())
        {
            var url = AutomationPaths.NormalizeTypePath(kv.Value?.Url);
            if (url.EqualIgnoreCase(typePath)) return kv.Key;
        }
        foreach (var kv in EntityFactory.Entities)
        {
            if (kv.Key.Name.EqualIgnoreCase(typePath) || kv.Key.FullName.EqualIgnoreCase(typePath))
                return kv.Key;
        }
        return null;
    }

    static Boolean RunNode(AutomationContext ctx, String id, String type, JsonObject data, out Int32 delayMinutes, out String detail)
    {
        delayMinutes = 0;
        detail = null;
        type = (type + "").Trim();
        if (type.EqualIgnoreCase("start", "end")) { detail = type; return true; }
        if (AutomationGraph.ReservedTypes.Contains(type) || !AutomationGraph.ImplementedTypes.Contains(type))
        {
            detail = "节点类型未实现：" + type;
            return false;
        }
        if (type.EqualIgnoreCase("filter"))
        {
            var filter = data["filter"].DeserializeFilter();
            var ok = AutomationFilter.Match(ctx.Current, filter);
            detail = ok ? "matched" : "skipped";
            return ok;
        }
        if (type.EqualIgnoreCase("delay"))
        {
            delayMinutes = data["minutes"]?.GetValue<Int32>() ?? 0;
            if (delayMinutes < 1) delayMinutes = 1;
            if (delayMinutes > 10080) delayMinutes = 10080;
            detail = "delay " + delayMinutes;
            return true;
        }

        // found 连续段已在 Execute 外层处理；此处仅单次执行
        return AutomationActions.Run(ctx, type, data, out detail);
    }

    static Boolean IsFoundTargetNode(String type, JsonObject data)
    {
        if (!type.EqualIgnoreCase("updateRecord", "notify", "addComment")) return false;
        return data?["target"]?.ToString().EqualIgnoreCase("found") == true;
    }

    /// <summary>模板替换</summary>
    public static String ApplyTemplate(String tpl, AutomationContext ctx)
    {
        if (tpl.IsNullOrEmpty()) return tpl;
        return Tpl.Replace(tpl, m =>
        {
            var path = m.Groups[1].Value;
            var parts = path.Split('.');
            if (parts.Length == 1) return ReadField(ctx.Current, parts[0]);
            var head = parts[0];
            var field = parts[1];
            if (head.EqualIgnoreCase("trigger")) return ReadField(ctx.Current, field);
            if (head.EqualIgnoreCase("found")) return ReadField(ctx.FoundCurrent ?? ctx.Found?.FirstOrDefault(), field);
            if (head.EqualIgnoreCase("webhook")) return ctx.Webhook?[field]?.ToString() ?? "";
            return "";
        });
    }

    static String ReadField(IEntity entity, String field)
    {
        if (entity == null || field.IsNullOrEmpty()) return "";
        try { return entity[field] + ""; }
        catch { return ""; }
    }
}

static class FilterJson
{
    public static ViewFilterDto DeserializeFilter(this JsonNode node)
    {
        var dto = new ViewFilterDto();
        if (node is not JsonObject o) return dto;
        dto.Logic = o["logic"]?.ToString() ?? "all";
        var arr = o["conditions"] as JsonArray;
        if (arr == null) return dto;
        foreach (var c in arr.OfType<JsonObject>())
        {
            dto.Conditions.Add(new ViewFilterConditionDto
            {
                Field = c["field"]?.ToString(),
                Op = c["op"]?.ToString(),
                Value = ReadFilterValue(c["value"]),
            });
        }
        return dto;
    }

    static Object ReadFilterValue(JsonNode node)
    {
        if (node is null) return null;
        if (node is JsonValue v)
        {
            try
            {
                if (v.TryGetValue<Int64>(out var l)) return l;
            }
            catch { /* */ }
            try
            {
                if (v.TryGetValue<Double>(out var d)) return d;
            }
            catch { /* */ }
            try
            {
                if (v.TryGetValue<Boolean>(out var b)) return b;
            }
            catch { /* */ }
            return v.ToString();
        }
        if (node is JsonArray arr) return arr.Select(ReadFilterValue).ToList();
        return node.ToString();
    }
}
