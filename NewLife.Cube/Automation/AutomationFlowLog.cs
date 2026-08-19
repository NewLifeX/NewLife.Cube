using System.Text.Json.Nodes;
using NewLife.Cube.Entity;
using NewLife.Data;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Automation;

/// <summary>
/// 实体自动化「流程日志」写入系统审计表 <see cref="Log"/>（不改 Log 表结构）。
/// 运行中队列仅内存；终态 Category=规范化 TypePath、Action=Automation、LinkID=记录主键。
/// GET /Runs 与编辑器「运行日志」读本表，不再使用独立 AutomationRun 实体。
/// </summary>
public static class AutomationFlowLog
{
    /// <summary>审计动作名（与 Insert/Update/Delete 并列）</summary>
    public const String ActionName = "Automation";

    /// <summary>Remark JSON 最大长度（沿用现有 Remark 列，不做 DDL）</summary>
    public const Int32 MaxRemarkLength = 1900;

    static readonly Dictionary<String, String> TriggerLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["insert"] = "添加新记录",
        ["update"] = "修改记录",
        ["delete"] = "删除记录",
        ["insertorupdateif"] = "新增或修改记录",
        ["fieldchange"] = "字段发生变更",
        ["datearrive"] = "到达记录中的时间",
        ["schedule"] = "定时触发",
        ["button"] = "点击按钮",
        ["webhook"] = "收到 Webhook",
    };

    static readonly Dictionary<String, String> ActionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["notify"] = "发送通知",
        ["updaterecord"] = "修改记录",
        ["createrecord"] = "创建记录",
        ["findrecords"] = "查找记录",
        ["httprequest"] = "发送 HTTP 请求",
        ["delay"] = "延时",
        ["runautomation"] = "运行自动化",
        ["addcomment"] = "添加评论",
        ["aitext"] = "AI 文本",
        ["filter"] = "条件筛选",
    };

    static readonly Dictionary<String, String> OpLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eq"] = "等于",
        ["neq"] = "不等于",
        ["gt"] = "大于",
        ["gte"] = "大于等于",
        ["lt"] = "小于",
        ["lte"] = "小于等于",
        ["contains"] = "包含",
        ["notcontains"] = "不包含",
        ["startswith"] = "开头是",
        ["endswith"] = "结尾是",
        ["isnull"] = "为空",
        ["notnull"] = "不为空",
        ["before"] = "早于",
        ["after"] = "晚于",
        ["in"] = "属于",
        ["notin"] = "不属于",
    };

    /// <summary>仅在终态（succeeded/failed）写入 Log；waiting/queued/running 仅在内存队列</summary>
    public static void WriteTerminal(EntityAutomation rule, AutomationRun run)
    {
        if (rule == null || run == null) return;
        var status = (run.Status + "").ToLowerInvariant();
        if (status is not ("succeeded" or "failed")) return;

        try
        {
            var category = AutomationPaths.NormalizeTypePath(rule.TypePath);
            if (category.IsNullOrEmpty()) category = "Automation";

            var success = status == "succeeded";
            var linkId = ParseLinkId(run.RecordKey);
            var remark = BuildRemark(rule, run);
            var provider = LogProvider.Provider;
            if (provider == null) return;

            var log = provider.CreateLog(category, ActionName, success, remark);
            if (log == null) return;
            if (linkId > 0) log.LinkID = linkId;
            log.SaveAsync();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
    }

    /// <summary>构造可解析、可人工阅读的 Remark（JSON，截断至 MaxRemarkLength）</summary>
    public static String BuildRemark(EntityAutomation rule, AutomationRun run)
    {
        var detail = BuildDetail(rule, run);
        var o = new JsonObject
        {
            ["v"] = 1,
            ["automationId"] = rule?.Id ?? 0,
            ["name"] = rule?.Name,
            ["triggerKind"] = run?.TriggerKind ?? rule?.TriggerKind,
            ["status"] = run?.Status,
            ["runId"] = run?.Id ?? 0,
            ["error"] = run?.Error,
            ["recordKey"] = run?.RecordKey,
            ["detail"] = detail,
        };
        var summary = SummarizeTrace(run?.NodeTrace);
        if (!summary.IsNullOrEmpty()) o["nodes"] = summary;

        var json = o.ToJsonString();
        return json.Length <= MaxRemarkLength ? json : json[..MaxRemarkLength];
    }

    /// <summary>
    /// 根据触发类型、条件、动作拼出人类可读说明（写入 Remark.detail，供运行日志 Timeline）。
    /// </summary>
    public static String BuildDetail(EntityAutomation rule, AutomationRun run)
    {
        var name = (rule?.Name + "").Trim();
        if (name.IsNullOrEmpty()) name = "未命名流程";
        var kind = (run?.TriggerKind ?? rule?.TriggerKind ?? "").Trim();
        var trigger = TriggerLabels.TryGetValue(kind, out var tl) ? tl : (kind.IsNullOrEmpty() ? "触发" : kind);
        var (condText, actionTexts) = ParseGraphHints(rule?.GraphJson);
        var actions = actionTexts.Count == 0 ? "指定操作" : String.Join("、", actionTexts);
        var condPart = condText.IsNullOrEmpty() ? "" : $"，在满足「{condText}」时";
        var record = run?.RecordKey.IsNullOrEmpty() == false ? $"（记录 #{run.RecordKey}）" : "";
        var status = (run?.Status + "").ToLowerInvariant();
        var err = (run?.Error + "").Trim();

        if (status == "failed")
            return $"流程「{name}」在{trigger}时{condPart}执行失败{record}" + (err.IsNullOrEmpty() ? "。" : $"：{err}");
        if (err.Contains("条件未匹配"))
            return $"流程「{name}」在{trigger}时{condPart}未匹配条件，已跳过后续动作{record}。";
        return $"流程「{name}」在{trigger}时{condPart}，已执行：{actions}{record}。";
    }

    /// <summary>dateArrive once：系统 Log 是否已有该规则+记录的成功终态</summary>
    public static Boolean HasSucceeded(Int64 automationId, String typePath, String recordKey)
    {
        if (automationId <= 0 || recordKey.IsNullOrEmpty()) return false;
        try
        {
            var page = new PageParameter { PageIndex = 1, PageSize = 80, RetrieveTotalCount = false };
            var logs = EntityAuditLog.Search(typePath, ActionName, ParseLinkId(recordKey), true, 0, DateTime.MinValue, DateTime.MinValue, null, page);
            foreach (var log in logs)
            {
                if (!TryParseRemark(log.Remark, out var row)) continue;
                if (row.AutomationId == automationId && (row.RecordKey + "").EqualIgnoreCase(recordKey)
                    && (row.Status + "").EqualIgnoreCase("succeeded"))
                    return true;
            }
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
        return false;
    }

    /// <summary>运行日志列表（读系统 Log）</summary>
    public static IList<(XCode.Membership.Log Log, AutomationFlowLogRow Row)> SearchRuns(String typePath, Int64 automationId, String recordKey, PageParameter page)
    {
        var pageSize = page?.PageSize > 0 ? page.PageSize : 20;
        var pageIndex = page?.PageIndex > 0 ? page.PageIndex : 1;
        var fetch = new PageParameter
        {
            PageIndex = 1,
            PageSize = Math.Min(Math.Max(pageSize * pageIndex + pageSize, 80), 400),
            RetrieveTotalCount = false,
        };
        var linkId = ParseLinkId(recordKey);
        var logs = EntityAuditLog.Search(typePath, ActionName, linkId, null, 0, DateTime.MinValue, DateTime.MinValue, null, fetch);
        var rows = new List<(XCode.Membership.Log, AutomationFlowLogRow)>();
        foreach (var log in logs)
        {
            if (!TryParseRemark(log.Remark, out var row)) continue;
            if (automationId > 0 && row.AutomationId != automationId) continue;
            if (!recordKey.IsNullOrEmpty()
                && !(row.RecordKey + "").EqualIgnoreCase(recordKey)
                && (linkId <= 0 || log.LinkID != linkId))
                continue;
            rows.Add((log, row));
        }
        if (page != null) page.TotalCount = rows.Count;
        var skip = (pageIndex - 1) * pageSize;
        if (skip < 0) skip = 0;
        return rows.Skip(skip).Take(pageSize).ToList();
    }

    /// <summary>从 Remark 解析 Runs API 行</summary>
    public static Boolean TryParseRemark(String remark, out AutomationFlowLogRow row)
    {
        row = null;
        if (remark.IsNullOrEmpty()) return false;
        try
        {
            var n = JsonNode.Parse(remark) as JsonObject;
            if (n == null) return false;
            row = new AutomationFlowLogRow
            {
                AutomationId = n["automationId"]?.GetValue<Int64>() ?? 0,
                Name = n["name"]?.ToString(),
                TriggerKind = n["triggerKind"]?.ToString(),
                Status = n["status"]?.ToString(),
                RunId = n["runId"]?.GetValue<Int64>() ?? 0,
                Error = n["error"]?.ToString(),
                RecordKey = n["recordKey"]?.ToString(),
                Nodes = n["nodes"]?.ToString(),
                Detail = n["detail"]?.ToString(),
            };
            return row.AutomationId > 0 || row.RunId > 0 || !row.Detail.IsNullOrEmpty();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>批量取各规则最近一次终态运行时间（读系统 Log）</summary>
    public static IDictionary<Int64, DateTime> FindLastRunTimes(IEnumerable<Int64> automationIds)
    {
        var map = new Dictionary<Int64, DateTime>();
        var ids = automationIds?.Where(e => e > 0).Distinct().ToHashSet() ?? [];
        if (ids.Count == 0) return map;
        try
        {
            var list = XCode.Membership.Log.FindAll(
                XCode.Membership.Log._.Action == ActionName,
                "ID desc", null, 0, Math.Min(ids.Count * 30, 800));
            foreach (var log in list)
            {
                if (!TryParseRemark(log.Remark, out var row) || row.AutomationId <= 0) continue;
                if (!ids.Contains(row.AutomationId) || map.ContainsKey(row.AutomationId)) continue;
                if (log.CreateTime.Year > 2000) map[row.AutomationId] = log.CreateTime;
            }
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
        return map;
    }

    static (String Cond, List<String> Actions) ParseGraphHints(String graphJson)
    {
        var actions = new List<String>();
        var condParts = new List<String>();
        if (graphJson.IsNullOrEmpty()) return (null, actions);
        try
        {
            var graph = AutomationGraph.Parse(graphJson);
            foreach (var (_, type, data) in AutomationGraph.LinearNodes(graph))
            {
                if (type.EqualIgnoreCase("filter"))
                {
                    var filter = data?["filter"].DeserializeFilter();
                    if (filter?.Conditions != null)
                    {
                        foreach (var c in filter.Conditions)
                        {
                            if (c?.Field.IsNullOrEmpty() != false) continue;
                            var op = OpLabels.TryGetValue(c.Op + "", out var ol) ? ol : (c.Op + "");
                            var needsVal = !op.Contains("空");
                            var piece = needsVal ? $"{c.Field} {op} {c.Value}" : $"{c.Field} {op}";
                            condParts.Add(piece);
                            if (condParts.Count >= 4) break;
                        }
                    }
                }
                else if (AutomationGraph.ActionTypes.Contains(type))
                {
                    var label = ActionLabels.TryGetValue(type, out var al) ? al : type;
                    if (type.EqualIgnoreCase("notify"))
                    {
                        var title = data?["title"]?.ToString();
                        if (!title.IsNullOrEmpty()) label += $"「{title.Cut(24)}」";
                    }
                    else if (type.EqualIgnoreCase("delay"))
                    {
                        var m = data?["minutes"]?.GetValue<Int32>() ?? 0;
                        if (m > 0) label += $"{m}分钟";
                    }
                    actions.Add(label);
                    if (actions.Count >= 6) break;
                }
            }
        }
        catch
        {
            /* 忽略非法图 */
        }
        var logic = "且";
        try
        {
            var g = JsonNode.Parse(graphJson);
            foreach (var node in g?["nodes"] as JsonArray ?? [])
            {
                if (!(node?["type"]?.ToString()).EqualIgnoreCase("filter")) continue;
                var lg = node?["data"]?["filter"]?["logic"]?.ToString();
                if (lg.EqualIgnoreCase("any")) logic = "或";
                break;
            }
        }
        catch { /* */ }
        var cond = condParts.Count == 0 ? null : String.Join(logic == "或" ? " 或 " : " 且 ", condParts);
        return (cond, actions);
    }

    static String SummarizeTrace(String nodeTrace)
    {
        if (nodeTrace.IsNullOrEmpty()) return null;
        try
        {
            var n = JsonNode.Parse(nodeTrace);
            var arr = n as JsonArray ?? n?["items"] as JsonArray;
            if (arr == null || arr.Count == 0) return null;
            var parts = new List<String>();
            foreach (var item in arr)
            {
                var t = item?["type"]?.ToString();
                var ok = item?["ok"]?.GetValue<Boolean>() ?? true;
                if (t.IsNullOrEmpty()) continue;
                var label = ActionLabels.TryGetValue(t, out var al) ? al
                    : TriggerLabels.TryGetValue(t, out var tl) ? tl
                    : t.EqualIgnoreCase("start") ? "开始"
                    : t.EqualIgnoreCase("end") ? "结束"
                    : t;
                parts.Add(ok ? label : label + "(失败)");
                if (parts.Count >= 24) break;
            }
            return parts.Count == 0 ? null : String.Join(" → ", parts);
        }
        catch
        {
            return null;
        }
    }

    static Int64 ParseLinkId(String recordKey)
    {
        if (recordKey.IsNullOrEmpty()) return 0;
        return Int64.TryParse(recordKey, out var id) ? id : 0;
    }
}

/// <summary>从 Log.Remark 解析出的自动化运行摘要</summary>
public class AutomationFlowLogRow
{
    /// <summary>规则 Id</summary>
    public Int64 AutomationId { get; set; }
    /// <summary>规则名</summary>
    public String Name { get; set; }
    /// <summary>触发类型</summary>
    public String TriggerKind { get; set; }
    /// <summary>状态</summary>
    public String Status { get; set; }
    /// <summary>AutomationRun.Id</summary>
    public Int64 RunId { get; set; }
    /// <summary>错误</summary>
    public String Error { get; set; }
    /// <summary>记录主键</summary>
    public String RecordKey { get; set; }
    /// <summary>节点摘要</summary>
    public String Nodes { get; set; }
    /// <summary>人类可读详情</summary>
    public String Detail { get; set; }
}
