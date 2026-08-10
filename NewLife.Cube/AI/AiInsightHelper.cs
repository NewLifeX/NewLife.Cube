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
