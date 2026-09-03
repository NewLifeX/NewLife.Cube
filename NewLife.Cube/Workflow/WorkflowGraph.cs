using System.Text.Json.Nodes;

namespace NewLife.Cube.Workflow;

/// <summary>流程定义图。解析 WorkflowDefinition.GraphJson / PublishedGraphJson，提供节点导航与发布校验</summary>
/// <remarks>
/// 与 OSC-260815fa86 自动化图同形 nodes/edges，但节点 type 命名空间为 oa.*（start/approve/cc/xor/end）。
/// 只读模型，运行时状态与写回均在此图之外；未知字段保留在 Raw。
/// </remarks>
public class WorkflowGraph
{
    #region 属性
    /// <summary>节点。按 nodes 数组顺序</summary>
    public List<WorkflowNode> Nodes { get; set; } = [];

    /// <summary>边。source → target</summary>
    public List<WorkflowEdge> Edges { get; set; } = [];

    /// <summary>原始图 JSON（保留未知字段）</summary>
    public JsonObject Raw { get; set; }
    #endregion

    #region 节点类型
    /// <summary>开始节点 type</summary>
    public const String StartType = "oa.start";
    /// <summary>审批节点 type</summary>
    public const String ApproveType = "oa.approve";
    /// <summary>知会节点 type</summary>
    public const String CcType = "oa.cc";
    /// <summary>条件网关 type</summary>
    public const String XorType = "oa.xor";
    /// <summary>结束节点 type</summary>
    public const String EndType = "oa.end";
    #endregion

    #region 解析
    /// <summary>解析图 JSON。非法 JSON 返回 null</summary>
    /// <param name="json">图 JSON</param>
    /// <returns>图对象，解析失败返回 null</returns>
    public static WorkflowGraph Parse(String json)
    {
        if (json.IsNullOrEmpty()) return null;
        try
        {
            var root = JsonNode.Parse(json) as JsonObject;
            if (root == null) return null;

            var graph = new WorkflowGraph { Raw = root };
            var nodes = root["nodes"] as JsonArray;
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n is not JsonObject jo) continue;
                    var node = WorkflowNode.Parse(jo);
                    if (node != null) graph.Nodes.Add(node);
                }
            }
            var edges = root["edges"] as JsonArray;
            if (edges != null)
            {
                foreach (var e in edges)
                {
                    if (e is not JsonObject jo) continue;
                    var edge = new WorkflowEdge
                    {
                        Source = jo["source"]?.ToString(),
                        Target = jo["target"]?.ToString(),
                    };
                    if (!edge.Source.IsNullOrEmpty() && !edge.Target.IsNullOrEmpty())
                        graph.Edges.Add(edge);
                }
            }
            return graph;
        }
        catch
        {
            return null;
        }
    }
    #endregion

    #region 导航
    /// <summary>按 Id 查找节点</summary>
    /// <param name="id">节点 Id</param>
    /// <returns>节点，无则 null</returns>
    public WorkflowNode Find(String id)
    {
        if (id.IsNullOrEmpty()) return null;
        return Nodes.FirstOrDefault(e => e.Id == id);
    }

    /// <summary>开始节点</summary>
    public WorkflowNode Start => Nodes.FirstOrDefault(e => e.Type == StartType);

    /// <summary>结束节点</summary>
    public WorkflowNode End => Nodes.FirstOrDefault(e => e.Type == EndType);

    /// <summary>出边目标节点 Id 列表（保持 edges 顺序）</summary>
    /// <param name="nodeId">源节点 Id</param>
    /// <returns>目标节点 Id 列表</returns>
    public List<String> NextOf(String nodeId)
    {
        var list = new List<String>();
        if (nodeId.IsNullOrEmpty()) return list;
        foreach (var edge in Edges)
        {
            if (edge.Source == nodeId && !list.Contains(edge.Target)) list.Add(edge.Target);
        }
        return list;
    }

    /// <summary>可达下游节点集合（不含自身；BFS 至无出边）。用于回退/取消下游任务</summary>
    /// <param name="nodeId">起点节点 Id</param>
    /// <returns>下游节点 Id 集合</returns>
    public HashSet<String> Downstream(String nodeId)
    {
        var set = new HashSet<String>();
        if (nodeId.IsNullOrEmpty()) return set;
        var queue = new Queue<String>();
        foreach (var next in NextOf(nodeId))
        {
            if (set.Add(next)) queue.Enqueue(next);
        }
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var next in NextOf(cur))
            {
                if (set.Add(next)) queue.Enqueue(next);
            }
        }
        return set;
    }

    /// <summary>是否存在 start→nodeId→end 通路（连通性 + 无环简化校验：V1 禁止回边，用可达性判断）</summary>
    /// <returns>图是否有效</returns>
    public Boolean IsValidFlow()
    {
        var start = Start;
        if (start == null) return false;
        var end = End;
        if (end == null) return false;

        // start 可到达 end
        var reach = Downstream(start.Id);
        if (!reach.Contains(end.Id)) return false;

        // 每个节点都可从 start 到达 且 可到达 end（V1 简单图，禁止孤立/回边）
        foreach (var node in Nodes)
        {
            if (node == start || node == end) continue;
            if (node.Type == EndType) continue;
            if (!reach.Contains(node.Id)) return false;
        }
        return true;
    }
    #endregion

    #region 发布校验
    /// <summary>发布校验（design §5 穷尽）。错误列表为空即通过</summary>
    /// <returns>错误信息列表</returns>
    public List<String> Validate()
    {
        var errors = new List<String>();
        var starts = Nodes.Where(e => e.Type == StartType).ToList();
        if (starts.Count != 1) errors.Add($"必须恰好 1 个开始节点，实际 {starts.Count}");
        var ends = Nodes.Where(e => e.Type == EndType).ToList();
        if (ends.Count != 1) errors.Add($"必须恰好 1 个结束节点，实际 {ends.Count}");

        var allowed = new HashSet<String> { StartType, ApproveType, CcType, XorType, EndType };
        foreach (var node in Nodes)
        {
            if (!allowed.Contains(node.Type))
            {
                errors.Add($"节点[{node.Id}]类型[{node.Type}]非法，仅支持 oa.start/approve/cc/xor/end");
                continue;
            }
            switch (node.Type)
            {
                case StartType:
                    if (InDegree(node.Id) != 0) errors.Add($"开始节点[{node.Id}]不允许入边");
                    break;
                case EndType:
                    if (OutDegree(node.Id) != 0) errors.Add($"结束节点[{node.Id}]不允许出边");
                    break;
                case ApproveType:
                    ValidateApprove(node, errors);
                    break;
                case XorType:
                    ValidateXor(node, errors);
                    break;
                case CcType:
                    if (node.To == null) errors.Add($"知会节点[{node.Id}]缺少 to");
                    break;
            }
        }

        // 连通：start 能到 end；无孤立节点
        if (!IsValidFlow()) errors.Add("图不连通：必须能从开始到达结束且无孤立节点（V1 禁止回边）");

        return errors;
    }

    /// <summary>校验审批节点参数</summary>
    /// <param name="node">节点</param>
    /// <param name="errors">错误列表</param>
    static void ValidateApprove(WorkflowNode node, List<String> errors)
    {
        if (node.To == null)
        {
            errors.Add($"审批节点[{node.Id}]缺少接收人 to");
        }
        else
        {
            var ids = new List<Int32>();
            ids.AddRange(WorkflowHelper.ReadIntArray(node.To["users"]));
            ids.AddRange(WorkflowHelper.ReadIntArray(node.To["roles"]));
            ids.AddRange(WorkflowHelper.ReadIntArray(node.To["departments"]));
            if (ids.Count == 0) errors.Add($"审批节点[{node.Id}]接收人 to 为空");
        }
        if (node.Mode is not ("or" or "and" or "sequence"))
            errors.Add($"审批节点[{node.Id}]签核模式[{node.Mode}]非法，仅 or/and/sequence");
        var quorum = node.Quorum;
        if (quorum > 0)
        {
            // 合法形式：(0,1] 比例，或 ≥1 的正整数且 ≤ 候选人数
            var n = TotalUsers(node);
            var okRatio = quorum <= 1;
            var okCount = quorum >= 1 && (n == 0 || quorum <= n);
            if (!okRatio && !okCount)
                errors.Add($"审批节点[{node.Id}]quorum[{quorum}]非法：须为 (0,1] 比例或 ≤ 候选人数({n})的正整数");
        }
        if (node.TimeoutHours > 0 && node.TimeoutAction.IsNullOrEmpty())
            errors.Add($"审批节点[{node.Id}]设置了超时小时数但缺少超时动作");
        if (!node.TimeoutAction.IsNullOrEmpty() && node.TimeoutAction is not ("pass" or "reject" or "transfer"))
            errors.Add($"审批节点[{node.Id}]超时动作[{node.TimeoutAction}]非法，仅 pass/reject/transfer");
        if (node.TimeoutAction == "transfer" && node.TimeoutTransferTo == null)
            errors.Add($"审批节点[{node.Id}]超时转交缺少 timeoutTransferTo");
    }

    /// <summary>校验网关节点参数</summary>
    /// <param name="node">节点</param>
    /// <param name="errors">错误列表</param>
    void ValidateXor(WorkflowNode node, List<String> errors)
    {
        var targets = new HashSet<String>();
        if (node.Cases != null)
        {
            foreach (var c in node.Cases)
            {
                if (c.Target.IsNullOrEmpty() || Find(c.Target) == null) errors.Add($"网关[{node.Id}]条件目标[{c.Target}]不存在");
                if (!c.Target.IsNullOrEmpty()) targets.Add(c.Target);
            }
        }
        if (node.DefaultTarget.IsNullOrEmpty())
        {
            errors.Add($"网关[{node.Id}]缺少 defaultTarget（V1 要求必有默认分支）");
        }
        else if (!targets.Contains(node.DefaultTarget))
        {
            if (Find(node.DefaultTarget) == null) errors.Add($"网关[{node.Id}]默认目标[{node.DefaultTarget}]不存在");
        }
    }
    #endregion

    #region 辅助
    /// <summary>入边数量</summary>
    /// <param name="nodeId">节点 Id</param>
    /// <returns>数量</returns>
    public Int32 InDegree(String nodeId) => Edges.Count(e => e.Target == nodeId);

    /// <summary>出边数量</summary>
    /// <param name="nodeId">节点 Id</param>
    /// <returns>数量</returns>
    public Int32 OutDegree(String nodeId) => Edges.Count(e => e.Source == nodeId);

    /// <summary>统计候选人基数（users+roles 粗略；用于 quorum 校验）</summary>
    /// <param name="node">节点</param>
    /// <returns>数量</returns>
    static Int32 TotalUsers(WorkflowNode node)
    {
        var n = 0;
        if (node.To != null)
        {
            n += WorkflowHelper.ReadIntArray(node.To["users"]).Count;
            n += WorkflowHelper.ReadIntArray(node.To["roles"]).Count;
            n += WorkflowHelper.ReadIntArray(node.To["departments"]).Count;
        }
        return n;
    }
    #endregion
}

/// <summary>流程边</summary>
public class WorkflowEdge
{
    /// <summary>源节点 Id</summary>
    public String Source { get; set; }

    /// <summary>目标节点 Id</summary>
    public String Target { get; set; }
}

/// <summary>流程节点。解析 nodes 中一项，未知字段保留在 Raw</summary>
public class WorkflowNode
{
    #region 属性
    /// <summary>节点 Id</summary>
    public String Id { get; set; }

    /// <summary>节点类型。oa.start/oa.approve/oa.cc/oa.xor/oa.end</summary>
    public String Type { get; set; }

    /// <summary>原始 JSON</summary>
    public JsonObject Raw { get; set; }
    #endregion

    #region 便捷访问
    /// <summary>节点名称</summary>
    public String Name => Raw?["name"]?.ToString() ?? Id;

    /// <summary>签核模式。or/and/sequence，默认 or</summary>
    public String Mode => Raw?["mode"]?.ToString() ?? "or";

    /// <summary>会签通过数。空=全部；(0,1] 比例；正整数=人数</summary>
    public Double Quorum
    {
        get
        {
            var v = Raw?["quorum"];
            return v == null ? 0 : v.GetValue<Double>();
        }
    }

    /// <summary>接收人。kind=users|roles|departments</summary>
    public JsonObject To => Raw?["to"] as JsonObject;

    /// <summary>节点可写字段名；仅审批人经流程通道可写</summary>
    public List<String> WritableFields
    {
        get
        {
            var list = new List<String>();
            var f = Raw?["fields"];
            if (f is JsonObject jo)
            {
                var w = jo["writable"] as JsonArray;
                if (w != null)
                {
                    foreach (var item in w)
                    {
                        var s = item?.ToString();
                        if (!s.IsNullOrEmpty()) list.Add(s);
                    }
                }
            }
            return list;
        }
    }

    /// <summary>超时小时数。0=不限</summary>
    public Int32 TimeoutHours
    {
        get
        {
            var v = Raw?["timeoutHours"];
            return v == null ? 0 : v.GetValue<Int32>();
        }
    }

    /// <summary>超时动作。pass/reject/transfer</summary>
    public String TimeoutAction => Raw?["timeoutAction"]?.ToString();

    /// <summary>超时转交接收人（复用 to schema）</summary>
    public JsonObject TimeoutTransferTo => Raw?["timeoutTransferTo"] as JsonObject;

    /// <summary>是否允许加签</summary>
    public Boolean AllowAddSign => Raw?["allowAddSign"]?.GetValue<Boolean>() ?? false;

    /// <summary>是否允许回退</summary>
    public Boolean AllowRollback => Raw?["allowRollback"]?.GetValue<Boolean>() ?? false;

    /// <summary>网关条件列表</summary>
    public List<WorkflowGraphCase> Cases
    {
        get
        {
            var list = new List<WorkflowGraphCase>();
            var arr = Raw?["cases"] as JsonArray;
            if (arr != null)
            {
                foreach (var item in arr)
                {
                    if (item is not JsonObject jo) continue;
                    var c = new WorkflowGraphCase
                    {
                        Target = jo["target"]?.ToString(),
                        Filter = jo["filter"] as JsonObject,
                    };
                    list.Add(c);
                }
            }
            return list;
        }
    }

    /// <summary>网关默认目标</summary>
    public String DefaultTarget => Raw?["defaultTarget"]?.ToString();
    #endregion

    #region 解析
    /// <summary>解析节点</summary>
    /// <param name="jo">节点 JSON</param>
    /// <returns>节点对象</returns>
    public static WorkflowNode Parse(JsonObject jo)
    {
        var id = jo["id"]?.ToString();
        var type = jo["type"]?.ToString();
        if (id.IsNullOrEmpty() || type.IsNullOrEmpty()) return null;

        return new WorkflowNode
        {
            Id = id,
            Type = type,
            Raw = jo["data"] as JsonObject ?? new JsonObject(),
        };
    }
    #endregion
}

/// <summary>网关条件</summary>
public class WorkflowGraphCase
{
    /// <summary>条件命中的目标节点</summary>
    public String Target { get; set; }

    /// <summary>条件（ViewFilter 同构 JSON）</summary>
    public JsonObject Filter { get; set; }
}

/// <summary>Workflow 图/JSON 工具</summary>
public static class WorkflowHelper
{
    /// <summary>读取 JSON 整数数组，兼容逗号分隔字符串</summary>
    /// <param name="node">JSON 节点</param>
    /// <returns>整数序列</returns>
    public static List<Int32> ReadIntArray(JsonNode node)
    {
        var list = new List<Int32>();
        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item == null) continue;
                try { list.Add(item.GetValue<Int32>()); }
                catch
                {
                    if (Int32.TryParse(item.ToString(), out var id)) list.Add(id);
                }
            }
            return list;
        }
        if (node != null)
        {
            var s = node.ToString();
            if (!s.IsNullOrEmpty())
            {
                foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Int32.TryParse(part.Trim().Trim('"'), out var id)) list.Add(id);
                }
            }
        }
        return list;
    }

    /// <summary>把 JsonNode 序列化为字符串（未知字段保留）</summary>
    /// <param name="node">节点</param>
    /// <returns>JSON 文本</returns>
    public static String ToJson(JsonNode node) => node?.ToJsonString() ?? "";
}
