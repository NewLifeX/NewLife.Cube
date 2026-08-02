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
    /// <param name="pager">查询条件（已解码的 _query），携带分页、排序与参数</param>
    /// <param name="data">控制器按列表页逻辑（SearchData）查询得到的当前页数据</param>
    /// <returns>AI 洞察上下文数据</returns>
    public static AiInsightContext Collect<TEntity>(IEntityFactory factory, Pager pager, IList<TEntity> data) where TEntity : Entity<TEntity>, new()
    {
        var ctx = new AiInsightContext
        {
            EntityName = factory.EntityType.GetDisplayName() ?? factory.Table.DataTable.DisplayName ?? factory.EntityType.Name,
            TableName = factory.Table.DataTable.TableName,
            Description = factory.Table.DataTable.Description,
            Pager = pager,
        };

        var entityType = typeof(TEntity);

        // 1. 收集字段元数据（安全过滤后）
        var allFields = factory.AllFields;
        var safeFields = AiDataHelper.FilterSafeFields(allFields, entityType);

        // 映射字段替换：xxxId → xxxName（与列表页 FieldCollection.SetRelation 一致）
        // Map 扩展属性（如 RoleName 映射 RoleId）替代原始 Id 字段，LLM 更易理解
        foreach (var f in safeFields.ToArray())
        {
            if (f.Map != null && !f.Map.Name.IsNullOrEmpty())
            {
                for (var i = safeFields.Count - 1; i >= 0; i--)
                {
                    if (safeFields[i].Name.EqualIgnoreCase(f.Map.Name))
                        safeFields.RemoveAt(i);
                }
            }
        }

        ctx.Fields = safeFields.Select(f => new AiFieldMeta
        {
            Name = f.Name,
            DisplayName = f.DisplayName,
            Type = f.Type?.Name ?? "unknown",
            Description = f.Description,
        }).ToList();

        // 2. 当前页数据，仅保留安全字段，整页交给 LLM
        ctx.Data = data.Cast<IEntity>().Select(e => AiDataHelper.ToSafeDictionary(e, safeFields)).ToList();

        return ctx;
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
        sb.AppendLine("- 分析范围：当前列表页数据 " + ctx.Data.Count.ToString("N0") + " 行");
        sb.AppendLine();

        // 字段列表
        sb.AppendLine("## 字段列表");
        foreach (var f in ctx.Fields)
        {
            var desc = f.Description.IsNullOrEmpty() ? "" : $" — {f.Description}";
            sb.AppendLine($"- **{f.DisplayName}** ({f.Name}) [{f.Type}]{desc}");
        }
        sb.AppendLine();

        // 查询条件（来自 Pager）
        var filters = ctx.Pager.Params
            .Where(kv => !kv.Key.EqualIgnoreCase("_query") && !kv.Key.EqualIgnoreCase("Sort") && !kv.Key.EqualIgnoreCase("Desc") && !kv.Key.EqualIgnoreCase("PageIndex") && !kv.Key.EqualIgnoreCase("PageSize"))
            .ToList();
        if (filters.Count > 0 || !ctx.Pager.Sort.IsNullOrEmpty())
        {
            sb.AppendLine("## 当前查询条件");
            foreach (var kv in filters)
            {
                sb.AppendLine($"- {kv.Key}: {kv.Value}");
            }
            if (!ctx.Pager.Sort.IsNullOrEmpty())
                sb.AppendLine($"- 排序: {ctx.Pager.Sort}{(ctx.Pager.Desc ? " 降序" : " 升序")}");
            sb.AppendLine();
        }

        // 当前页数据（仅安全字段）
        if (ctx.Data.Count > 0)
        {
            sb.AppendLine("## 当前页数据");
            sb.AppendLine("```json");
            sb.AppendLine(ctx.Data.ToJson(true));
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

    /// <summary>查询上下文。分页、排序与参数等全部信息</summary>
    public Pager Pager { get; set; } = null!;

    /// <summary>安全字段列表</summary>
    public IList<AiFieldMeta> Fields { get; set; } = [];

    /// <summary>当前页数据（仅安全字段）</summary>
    public IList<IDictionary<String, Object?>> Data { get; set; } = [];
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
#endregion
