using NewLife;
using NewLife.Collections;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube.AI;

/// <summary>AI 洞察数据收集与提示词构建</summary>
public static class AiInsightHelper
{
    /// <summary>收集实体数据和元数据，构建 AI 分析上下文</summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="factory">实体工厂</param>
    /// <param name="pager">查询条件（已解码的 _query），用于提取查询上下文与排序</param>
    /// <param name="data">控制器按列表页逻辑（SearchData）查询得到的数据，已应用数据权限/搜索条件/排序</param>
    /// <param name="maxRows">最大数据行数</param>
    /// <returns>AI 洞察上下文数据</returns>
    public static AiInsightContext Collect<TEntity>(IEntityFactory factory, Pager pager, IList<TEntity> data, Int32 maxRows = 100) where TEntity : Entity<TEntity>, new()
    {
        var ctx = new AiInsightContext
        {
            EntityName = factory.EntityType.GetDisplayName() ?? factory.Table.DataTable.DisplayName ?? factory.EntityType.Name,
            TableName = factory.Table.DataTable.TableName,
            Description = factory.Table.DataTable.Description,
            TotalCount = factory.Session.Count,
        };

        var entityType = typeof(TEntity);

        // 1. 收集字段元数据（安全过滤后）
        var allFields = factory.AllFields;
        var safeFields = AiDataHelper.FilterSafeFields(allFields, entityType);
        ctx.Fields = safeFields.Select(f => new AiFieldMeta
        {
            Name = f.Name,
            DisplayName = f.DisplayName,
            Type = f.Type?.Name ?? "unknown",
            Description = f.Description,
        }).ToList();

        // 2. 记录查询上下文
        ctx.Filters = pager.Params
            .Where(kv => !kv.Key.EqualIgnoreCase("_query") && !kv.Key.EqualIgnoreCase("Sort") && !kv.Key.EqualIgnoreCase("Desc") && !kv.Key.EqualIgnoreCase("PageIndex") && !kv.Key.EqualIgnoreCase("PageSize"))
            .ToDictionary(kv => kv.Key, kv => kv.Value + "");
        ctx.SortField = pager.Sort;
        ctx.SortDesc = pager.Desc;

        // 3. 使用控制器查询结果（数据权限 + 搜索条件 + 排序已由 SearchData 应用）
        var entityList = data.Cast<IEntity>().ToList();
        ctx.ShownCount = entityList.Count;

        // 4. 预计算统计摘要
        ctx.Stats = ComputeStats(entityList, safeFields);

        // 5. 智能选取数据样本（首尾 + 异常 + 随机）
        ctx.Samples = SelectSamples(entityList, safeFields, ctx.Stats, maxRows);

        return ctx;
    }

    /// <summary>预计算统计摘要</summary>
    private static AiStatsSummary ComputeStats(IList<IEntity> data, IList<FieldItem> safeFields)
    {
        var stats = new AiStatsSummary();
        if (data.Count == 0) return stats;

        // 数值字段统计
        foreach (var field in safeFields)
        {
            var type = field.Type;
            if (type == null) continue;

            if (type == typeof(Int32) || type == typeof(Int64) || type == typeof(Decimal) || type == typeof(Double) || type == typeof(Single))
            {
                var values = data.Select(e => e[field.Name] is IConvertible c ? c.ToDouble(null) : (Double?)null)
                                 .Where(v => v.HasValue)
                                 .Select(v => v!.Value)
                                 .ToList();
                if (values.Count > 0)
                {
                    stats.NumericStats[field.Name] = new NumericStats
                    {
                        Count = values.Count,
                        NullCount = data.Count - values.Count,
                        Min = values.Min(),
                        Max = values.Max(),
                        Avg = Math.Round(values.Average(), 2),
                        Sum = Math.Round(values.Sum(), 2),
                    };
                }
            }

            // 分类字段分布（字符串/枚举，Top 10）
            if (type == typeof(String) || type == typeof(Boolean) || type.IsEnum)
            {
                var groups = data.GroupBy(e => e[field.Name]?.ToString() ?? "(空)")
                                 .OrderByDescending(g => g.Count())
                                 .Take(10)
                                 .ToDictionary(g => g.Key, g => g.Count());
                if (groups.Count > 0)
                    stats.Distribution[field.Name] = groups;
            }
        }

        // 时间范围
        var timeFields = safeFields.Where(f => f.Type == typeof(DateTime)).ToList();
        foreach (var tf in timeFields)
        {
            var values = data.Select(e => e[tf.Name] is DateTime dt ? dt : (DateTime?)null)
                             .Where(v => v.HasValue)
                             .Select(v => v!.Value)
                             .ToList();
            if (values.Count > 0)
            {
                stats.TimeRange[tf.Name] = new TimeRangeStat
                {
                    Earliest = values.Min(),
                    Latest = values.Max(),
                    Span = values.Max() - values.Min(),
                };
            }
        }

        // 空值率
        foreach (var field in safeFields)
        {
            var nullCount = data.Count(e => e[field.Name] == null || (e[field.Name] is String s && s.IsNullOrEmpty()));
            if (nullCount > 0)
                stats.NullRates[field.Name] = Math.Round((Double)nullCount / data.Count * 100, 1);
        }

        return stats;
    }

    /// <summary>智能选取数据样本：首部 5 + 尾部 5 + 异常行 + 随机 5，总计 50 行内</summary>
    private static IList<IDictionary<String, Object?>> SelectSamples(IList<IEntity> data, IList<FieldItem> safeFields, AiStatsSummary stats, Int32 maxRows)
    {
        var samples = new List<IDictionary<String, Object?>>();
        if (data.Count == 0) return samples;

        var used = new HashSet<Int32>();
        var total = data.Count;

        // 首部 5 行
        for (var i = 0; i < Math.Min(5, total); i++)
        {
            used.Add(i);
            samples.Add(AiDataHelper.ToSafeDictionary(data[i], safeFields));
        }

        // 尾部 5 行
        for (var i = total - 1; i >= Math.Max(0, total - 5); i--)
        {
            if (used.Add(i))
                samples.Add(AiDataHelper.ToSafeDictionary(data[i], safeFields));
        }

        // 异常行：每个数值字段的 min/max 行（各取 1 行）
        foreach (var stat in stats.NumericStats)
        {
            if (used.Count >= 50) break;

            // Max 行
            var maxIdx = -1;
            Double maxVal = Double.MinValue;
            for (var i = 0; i < total; i++)
            {
                if (used.Contains(i)) continue;
                var v = ToDouble(data[i][stat.Key]);
                if (v.HasValue && v.Value > maxVal) { maxVal = v.Value; maxIdx = i; }
            }
            if (maxIdx >= 0) { used.Add(maxIdx); samples.Add(AiDataHelper.ToSafeDictionary(data[maxIdx], safeFields)); }

            // Min 行
            var minIdx = -1;
            Double minVal = Double.MaxValue;
            for (var i = 0; i < total; i++)
            {
                if (used.Contains(i)) continue;
                var v = ToDouble(data[i][stat.Key]);
                if (v.HasValue && v.Value < minVal) { minVal = v.Value; minIdx = i; }
            }
            if (minIdx >= 0) { used.Add(minIdx); samples.Add(AiDataHelper.ToSafeDictionary(data[minIdx], safeFields)); }
        }

        // 随机 5 行
        var rnd = new Random();
        var attempts = 0;
        while (samples.Count < Math.Min(50, total) && attempts < 100)
        {
            var idx = rnd.Next(total);
            if (used.Add(idx))
                samples.Add(AiDataHelper.ToSafeDictionary(data[idx], safeFields));
            attempts++;
        }

        return samples;
    }

    private static Double? ToDouble(Object? value)
    {
        if (value is IConvertible c)
        {
            try { return c.ToDouble(null); }
            catch { return null; }
        }
        return null;
    }

    /// <summary>构建 AI 分析提示词</summary>
    /// <param name="ctx">洞察上下文</param>
    /// <returns>完整的 LLM 提示词</returns>
    public static String BuildPrompt(AiInsightContext ctx)
    {
        var sb = Pool.StringBuilder.Get();
        sb.AppendLine("你是专业的数据分析师。当前用户正在查看「" + ctx.EntityName + "」管理页面。");
        sb.AppendLine("你的任务是解读数据、发现规律、识别异常、给出可执行的建议。");
        sb.AppendLine();
        sb.AppendLine("分析原则：");
        sb.AppendLine("- 用中文输出，语言简洁专业");
        sb.AppendLine("- 关注业务含义，不只描述数字");
        sb.AppendLine("- 异常必须解释「为什么异常」和「建议怎么做」");
        sb.AppendLine("- 建议要具体可执行，不说空话");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## 实体信息");
        sb.AppendLine("- 名称：" + ctx.EntityName);
        sb.AppendLine("- 表名：" + ctx.TableName);
        if (!ctx.Description.IsNullOrEmpty())
            sb.AppendLine("- 说明：" + ctx.Description);
        sb.AppendLine("- 总记录数：" + ctx.TotalCount.ToString("N0"));
        sb.AppendLine("- 分析样本数：" + ctx.ShownCount.ToString("N0"));
        sb.AppendLine();

        // 字段列表
        sb.AppendLine("## 字段列表");
        foreach (var f in ctx.Fields)
        {
            var desc = f.Description.IsNullOrEmpty() ? "" : $" — {f.Description}";
            sb.AppendLine($"- **{f.DisplayName}** ({f.Name}) [{f.Type}]{desc}");
        }
        sb.AppendLine();

        // 查询条件
        if (ctx.Filters.Count > 0)
        {
            sb.AppendLine("## 当前查询条件");
            foreach (var kv in ctx.Filters)
            {
                sb.AppendLine($"- {kv.Key}: {kv.Value}");
            }
            if (!ctx.SortField.IsNullOrEmpty())
                sb.AppendLine($"- 排序: {ctx.SortField}{(ctx.SortDesc ? " 降序" : " 升序")}");
            sb.AppendLine();
        }

        // 统计摘要
        sb.AppendLine("## 数据统计摘要");
        if (ctx.Stats.NumericStats.Count > 0)
        {
            sb.AppendLine("### 数值字段");
            sb.AppendLine("| 字段 | 总数 | 空值 | 最小值 | 最大值 | 平均值 | 合计 |");
            sb.AppendLine("|------|------|------|--------|--------|--------|------|");
            foreach (var kv in ctx.Stats.NumericStats)
            {
                var ns = kv.Value;
                var fieldName = ctx.Fields.FirstOrDefault(f => f.Name == kv.Key)?.DisplayName ?? kv.Key;
                sb.AppendLine($"| {fieldName} | {ns.Count} | {ns.NullCount} | {ns.Min:N2} | {ns.Max:N2} | {ns.Avg:N2} | {ns.Sum:N2} |");
            }
            sb.AppendLine();
        }

        if (ctx.Stats.Distribution.Count > 0)
        {
            sb.AppendLine("### 分类分布");
            foreach (var kv in ctx.Stats.Distribution)
            {
                var fieldName = ctx.Fields.FirstOrDefault(f => f.Name == kv.Key)?.DisplayName ?? kv.Key;
                sb.AppendLine($"**{fieldName}**：");
                var items = kv.Value.OrderByDescending(x => x.Value).Take(10);
                foreach (var item in items)
                {
                    sb.AppendLine($"  - {item.Key}: {item.Value} 条");
                }
            }
            sb.AppendLine();
        }

        if (ctx.Stats.TimeRange.Count > 0)
        {
            sb.AppendLine("### 时间范围");
            foreach (var kv in ctx.Stats.TimeRange)
            {
                var fieldName = ctx.Fields.FirstOrDefault(f => f.Name == kv.Key)?.DisplayName ?? kv.Key;
                sb.AppendLine($"- **{fieldName}**: {kv.Value.Earliest:yyyy-MM-dd HH:mm} ~ {kv.Value.Latest:yyyy-MM-dd HH:mm}（跨度 {kv.Value.Span.TotalDays:N1} 天）");
            }
            sb.AppendLine();
        }

        if (ctx.Stats.NullRates.Count > 0)
        {
            sb.AppendLine("### 数据质量");
            foreach (var kv in ctx.Stats.NullRates.Where(x => x.Value > 0))
            {
                var fieldName = ctx.Fields.FirstOrDefault(f => f.Name == kv.Key)?.DisplayName ?? kv.Key;
                sb.AppendLine($"- **{fieldName}** 空值率: {kv.Value}%");
            }
            sb.AppendLine();
        }

        // 数据样本
        if (ctx.Samples.Count > 0)
        {
            sb.AppendLine($"## 数据样本（{ctx.Samples.Count} 行）");
            sb.AppendLine("```json");
            sb.AppendLine(ctx.Samples.ToJson(true));
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // 分析指令
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("请基于以上数据进行分析，按以下结构输出 Markdown 报告：");
        sb.AppendLine();
        sb.AppendLine("## 数据概览");
        sb.AppendLine("（整体规模、时间范围、关键数字一句话总结）");
        sb.AppendLine();
        sb.AppendLine("## 关键发现");
        sb.AppendLine("（3-5 个发现，每个包含：现象、数据支撑、业务解读）");
        sb.AppendLine();
        sb.AppendLine("## 异常检测");
        sb.AppendLine("（异常数据点，每个包含：异常描述、偏离程度、可能原因、建议）");
        sb.AppendLine();
        sb.AppendLine("## 可执行建议");
        sb.AppendLine("（3-5 条，按优先级排列，每条包含具体行动和预期效果）");

        return sb.Put(true);
    }
}

#region 数据模型
/// <summary>AI 洞察上下文</summary>
public class AiInsightContext
{
    /// <summary>实体显示名</summary>
    public String EntityName { get; set; } = null!;

    /// <summary>表名</summary>
    public String TableName { get; set; } = null!;

    /// <summary>表说明</summary>
    public String? Description { get; set; }

    /// <summary>总记录数</summary>
    public Int64 TotalCount { get; set; }

    /// <summary>分析样本数</summary>
    public Int32 ShownCount { get; set; }

    /// <summary>安全字段列表</summary>
    public IList<AiFieldMeta> Fields { get; set; } = [];

    /// <summary>当前查询条件</summary>
    public IDictionary<String, String> Filters { get; set; } = new Dictionary<String, String>();

    /// <summary>排序字段</summary>
    public String? SortField { get; set; }

    /// <summary>是否降序</summary>
    public Boolean SortDesc { get; set; }

    /// <summary>统计摘要</summary>
    public AiStatsSummary Stats { get; set; } = new();

    /// <summary>数据样本</summary>
    public IList<IDictionary<String, Object?>> Samples { get; set; } = [];
}

/// <summary>AI 字段元数据</summary>
public class AiFieldMeta
{
    /// <summary>字段名</summary>
    public String Name { get; set; } = null!;

    /// <summary>显示名</summary>
    public String DisplayName { get; set; } = null!;

    /// <summary>类型名</summary>
    public String Type { get; set; } = null!;

    /// <summary>字段说明</summary>
    public String? Description { get; set; }
}

/// <summary>统计摘要</summary>
public class AiStatsSummary
{
    /// <summary>数值字段统计</summary>
    public Dictionary<String, NumericStats> NumericStats { get; set; } = new();

    /// <summary>分类字段分布（Top 10）</summary>
    public Dictionary<String, Dictionary<String, Int32>> Distribution { get; set; } = new();

    /// <summary>时间字段范围</summary>
    public Dictionary<String, TimeRangeStat> TimeRange { get; set; } = new();

    /// <summary>字段空值率（%）</summary>
    public Dictionary<String, Double> NullRates { get; set; } = new();
}

/// <summary>数值统计</summary>
public class NumericStats
{
    /// <summary>有效值数量</summary>
    public Int32 Count { get; set; }

    /// <summary>空值数量</summary>
    public Int32 NullCount { get; set; }

    /// <summary>最小值</summary>
    public Double Min { get; set; }

    /// <summary>最大值</summary>
    public Double Max { get; set; }

    /// <summary>平均值</summary>
    public Double Avg { get; set; }

    /// <summary>合计</summary>
    public Double Sum { get; set; }
}

/// <summary>时间范围统计</summary>
public class TimeRangeStat
{
    /// <summary>最早时间</summary>
    public DateTime Earliest { get; set; }

    /// <summary>最晚时间</summary>
    public DateTime Latest { get; set; }

    /// <summary>时间跨度</summary>
    public TimeSpan Span { get; set; }
}
#endregion
