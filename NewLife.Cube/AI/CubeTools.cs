using System.ComponentModel;
using System.Diagnostics;
using NewLife.AI.Tools;
using NewLife.Cube.ViewModels;
using NewLife.Serialization;
using NewLife.Web;
using XCode;

namespace NewLife.Cube.AI;

/// <summary>魔方 AI 工具集。封装当前实体上下文的 AI 可调用工具，供对话 Agent 使用</summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <remarks>实例化魔方工具集</remarks>
/// <param name="factory">实体工厂</param>
/// <param name="pager">当前查询条件（可为空）</param>
/// <param name="entityId">当前记录编号</param>
/// <param name="queryData">数据查询委托，应用列表页查询逻辑（SearchData）</param>
public class CubeTools<TEntity>(IEntityFactory factory, Pager? pager, Int64 entityId, Func<Pager, IList<TEntity>> queryData) where TEntity : Entity<TEntity>, new()
{
    #region 属性
    private readonly Pager? _pager = pager;

    /// <summary>添加表单字段</summary>
    public FieldCollection AddFields { get; } = new FieldCollection(factory, ViewKinds.AddForm);

    /// <summary>编辑表单字段</summary>
    public FieldCollection EditFields { get; } = new FieldCollection(factory, ViewKinds.EditForm);
    #endregion

    #region 工具
    /// <summary>获取当前实体基本信息，如名称、表名、说明、总记录数</summary>
    [ToolDescription("get_entity_info", IsSystem = true, ReadOnly = true)]
    public String GetEntityInfo()
    {
        var tb = factory.Table.DataTable;
        var name = factory.EntityType.GetDisplayName() ?? tb.DisplayName ?? factory.EntityType.Name;
        return new { entity = name, table = tb.TableName, description = tb.Description, total = factory.Session.Count }.ToJson();
    }

    /// <summary>获取当前实体的表单字段结构，包含字段名、类型、枚举值、必填、说明，供生成表单值使用</summary>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 add</param>
    [ToolDescription("get_form_schema", ReadOnly = true)]
    public String GetFormSchema([Description("表单模式：add 新增 / edit 编辑")] String mode = "add")
    {
        var fields = mode.EqualIgnoreCase("edit") ? EditFields : AddFields;
        var schema = AiFormHelper.BuildSchema(fields);
        return new { mode, fields = schema }.ToJson();
    }

    /// <summary>生成表单字段值并回填到前端表单。不写数据库，由用户确认后提交</summary>
    /// <param name="values">字段值字典，键为字段名，值为要填入的值</param>
    /// <param name="mode">表单模式：add 新增 / edit 编辑，默认 add</param>
    [ToolDescription("fill_form")]
    public IToolResult FillForm([Description("字段值字典，键为字段名，值为要填入的值")] IDictionary<String, Object> values, [Description("表单模式：add 新增 / edit 编辑")] String mode = "add")
    {
        var fields = mode.EqualIgnoreCase("edit") ? EditFields : AddFields;

        var rs = new Dictionary<String, Object?>();
        var skipped = new List<String>();
        var errors = new List<String>();
        foreach (var kv in values)
        {
            var field = fields.FirstOrDefault(f => f.Name.EqualIgnoreCase(kv.Key));
            if (field == null)
            {
                skipped.Add(kv.Key);
                continue;
            }
            // 敏感/自动维护/只读字段不允许 AI 填写
            if (AiFormHelper.IsAutoField(field.Name) || !AiDataHelper.IsSafeFieldName(field.Name) || field.ReadOnly)
            {
                skipped.Add(kv.Key);
                continue;
            }
            var val = AiFormHelper.CoerceValue(kv.Value, field);
            if (val == null)
            {
                errors.Add(kv.Key);
                continue;
            }
            rs[field.Name] = val;
        }

        var user = new { kind = "fill_form", values = rs, count = rs.Count, skipped, errors }.ToJson();
        var llm = $"已生成 {rs.Count} 个表单字段值（跳过 {skipped.Count} 个，失败 {errors.Count} 个），已预填到表单，请提示用户检查后提交。";
        return new ToolResult(ToolContent.ForUser(user), ToolContent.ForLlm(llm));
    }

    /// <summary>收集当前查询条件下的数据（仅安全字段）与字段元数据，供数据洞察分析使用</summary>
    [ToolDescription("get_data_insight_context", ReadOnly = true)]
    public String GetDataInsightContext()
    {
        // 克隆当前查询条件，避免污染缓存对象
        var pager = _pager == null ? new Pager() : new Pager(_pager);
        if (pager.PageSize <= 0) pager.PageSize = 20;
        pager.RetrieveTotalCount = false;

        var data = queryData(pager);
        var ctx = AiInsightHelper.Collect<TEntity>(factory, pager, data);

        var filters = pager.Params
            .Where(kv => !kv.Key.EqualIgnoreCase("_query") && !kv.Key.EqualIgnoreCase("Sort") && !kv.Key.EqualIgnoreCase("Desc") && !kv.Key.EqualIgnoreCase("PageIndex") && !kv.Key.EqualIgnoreCase("PageSize"))
            .ToDictionary(kv => kv.Key, kv => kv.Value + "");

        return new
        {
            entity = ctx.EntityName,
            table = ctx.TableName,
            description = ctx.Description,
            total = factory.Session.Count,
            shown = data.Count,
            filters,
            sort = pager.Sort + (pager.Desc ? " 降序" : ""),
            fields = ctx.Fields,
            data = ctx.Data,
        }.ToJson();
    }

    /// <summary>收集当前记录的值与字段元数据，供单条记录异常分析使用</summary>
    [ToolDescription("get_record_context", ReadOnly = true)]
    public String GetRecordContext()
    {
        if (entityId <= 0) return "{\"error\":\"当前无记录（新增模式），请先保存或切换到详情/编辑页\"}";

        var entity = Entity<TEntity>.FindByKey(entityId);
        if (entity == null) return "{\"error\":\"记录不存在\"}";

        var safeFields = AiDataHelper.FilterSafeFields(factory.AllFields, typeof(TEntity));
        var values = AiDataHelper.ToSafeDictionary(entity, safeFields);
        var tb = factory.Table.DataTable;
        var name = factory.EntityType.GetDisplayName() ?? tb.DisplayName ?? factory.EntityType.Name;

        return new
        {
            entity = name,
            table = tb.TableName,
            description = tb.Description,
            total = factory.Session.Count,
            id = entityId,
            fields = safeFields.Select(f => new AiFieldMeta
            {
                Name = f.Name,
                DisplayName = f.DisplayName,
                Type = f.Type?.Name ?? "unknown",
                Description = f.Description,
            }).ToList(),
            values,
        }.ToJson();
    }

    /// <summary>收集服务器运行指标，供系统健康诊断使用</summary>
    [ToolDescription("get_system_info", ReadOnly = true)]
    public String GetSystemInfo()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var process = Process.GetCurrentProcess();
        var sysInfo = new
        {
            cpu = $"{mi.CpuRate:P0}",
            temperature = mi.Temperature,
            memoryAvailable = $"{mi.AvailableMemory / 1024 / 1024:N0}M",
            memoryTotal = $"{mi.Memory / 1024 / 1024:N0}M",
            workingSet = $"{process.WorkingSet64 / 1024 / 1024:N0}M",
            openTime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            os = mi.OSName + " " + mi.OSVersion,
            machineName = Environment.MachineName,
        };
        return sysInfo.ToJson();
    }
    #endregion
}
