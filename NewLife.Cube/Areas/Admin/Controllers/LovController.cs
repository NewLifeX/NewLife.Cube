using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>值集管理。管理后台手工创建的"名值定义"，并提供代码声明值集（枚举/[LovList]）的运行时解析与数据代理。</summary>
/// <remarks>
/// 值集已全面代码优先：代码枚举与 [LovList] 列表型值集由 <see cref="LovRegistry"/> 反射直读、不落库；
/// 值集管理页平时为空，仅展示运行时手工创建的"名值定义"（存 <see cref="LovStore"/>，Parameter 承载）。
/// 手工定义优先于代码声明解析（LovCode 同时存在代码与手工定义时以手工为准）。
/// </remarks>
[DisplayName("值集管理")]
[AdminArea]
[Menu(85, true, Icon = "Operation")]
public class LovController : ControllerBaseX
{
    #region 定义管理（手工"名值定义" CRUD，兼容原 EntityController 端点契约）

    /// <summary>多行数据列表。仅列出手工定义（平时为空）</summary>
    /// <returns>分页列表，行 JSON 与旧 LovDefinition 列表兼容（id=LovCode）</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]")]
    public ApiListResponse<Object> Index()
    {
        var p = new Pager(WebHelper.Params);
        var type = p["type"];
        var source = p["source"];
        var key = p["Q"];

        IEnumerable<LovDefModel> query = LovStore.FindAllDefs();
        if (!type.IsNullOrEmpty()) query = query.Where(e => e.Type == type);
        if (!source.IsNullOrEmpty()) query = query.Where(e => e.Source == source);
        if (!key.IsNullOrEmpty())
            query = query.Where(e => e.LovCode.Contains(key, StringComparison.OrdinalIgnoreCase) || (e.Name ?? "").Contains(key, StringComparison.OrdinalIgnoreCase));

        var list = query.OrderBy(e => e.LovCode).ToList();
        p.TotalCount = list.Count;
        var rows = p.PageSize > 0
            ? list.Skip((p.PageIndex - 1) * p.PageSize).Take(p.PageSize).ToList()
            : list;

        return new ApiListResponse<Object>
        {
            Data = rows.Select(ToRow).ToList(),
            Page = p.ToModel(),
        };
    }

    /// <summary>查看单条定义</summary>
    /// <param name="id">值集编码（LovCode），可为空（兼容 ?id= 传参）</param>
    /// <returns>行数据</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ApiResponse<Object> Detail(String id)
    {
        if (id.IsNullOrEmpty()) id = Request.Query["id"].ToString();
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var def = LovStore.FindDef(id);
        if (def == null) throw new InvalidOperationException($"值集 {id} 不存在");

        return new ApiResponse<Object> { Data = ToRow(def) };
    }

    /// <summary>新增手工定义</summary>
    /// <param name="body">定义 JSON（lovCode/name/type/valueField/labelField/enabled/remark）</param>
    /// <returns>响应，data.id=LovCode</returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost("/api/[area]/[controller]")]
    public ApiResponse<Object> Insert([FromBody] JsonDocument body)
    {
        var model = ParseDef(body.RootElement);
        CheckCode(model);

        if (LovStore.FindDef(model.LovCode) != null)
            throw new InvalidOperationException($"值集 {model.LovCode} 已存在");

        LovStore.SaveDef(model);

        return new ApiResponse<Object> { Code = 0, Message = "添加成功", Data = new { id = model.LovCode } };
    }

    /// <summary>更新手工定义。body.id 为原 LovCode，lovCode 变更时级联清理旧编码数据</summary>
    /// <param name="body">定义 JSON（含 id=旧 LovCode）</param>
    /// <returns>响应，data.id=LovCode</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPut("/api/[area]/[controller]")]
    public ApiResponse<Object> Update([FromBody] JsonDocument body)
    {
        var root = body.RootElement;
        var oldCode = GetStr(root, "id");
        var model = ParseDef(root);
        CheckCode(model);

        if (oldCode.IsNullOrEmpty()) oldCode = model.LovCode;
        if (LovStore.FindDef(oldCode) == null)
            throw new InvalidOperationException($"值集 {oldCode} 不存在");

        // 编码变更：级联清理旧编码数据，以新编码重建（明细需在配置页重新维护）
        if (!oldCode.EqualIgnoreCase(model.LovCode))
            LovStore.DeleteDef(oldCode);

        LovStore.SaveDef(model);

        return new ApiResponse<Object> { Code = 0, Message = "更新成功", Data = new { id = model.LovCode } };
    }

    /// <summary>删除手工定义（级联清理明细）</summary>
    /// <param name="id">值集编码</param>
    /// <returns>响应</returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [HttpDelete("/api/[area]/[controller]")]
    public ApiResponse<Object> Delete(String id)
    {
        if (id.IsNullOrEmpty()) id = Request.Query["id"].ToString();
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        LovStore.DeleteDef(id);

        return new ApiResponse<Object> { Code = 0, Message = "删除成功" };
    }

    /// <summary>批量删除选中数据。支持重复参数 id=a&amp;id=b、逗号分隔 id=a,b</summary>
    /// <param name="id">值集编码集合</param>
    /// <returns>响应</returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [HttpDelete("/api/[area]/[controller]/DeleteSelect")]
    public ApiResponse<String> DeleteSelect([FromQuery] String[] id)
    {
        if (id == null || id.Length == 0)
            throw new InvalidOperationException("未指定要删除的数据！");

        var n = 0;
        foreach (var item in id)
        {
            var parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (LovStore.DeleteDef(part) > 0) n++;
            }
        }

        return new ApiResponse<String> { Code = 0, Message = $"删除成功！共{n}条", Data = n.ToString() };
    }
    #endregion

    #region 业务接口

    /// <summary>获取值集元数据。支持逗号分隔多个 lovCode，枚举型内联 options。</summary>
    /// <remarks>解析规则：手工定义（Parameter Lov.Def）优先 → 代码声明（Enum.* 反射 / List.* 读 [LovList] 描述符）。</remarks>
    /// <param name="lovCode">值集编码，逗号分隔</param>
    /// <returns>值集元数据集合</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public Object Meta(String lovCode)
    {
        if (lovCode.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(lovCode));

        var codes = lovCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (codes.Length == 0)
            throw new ArgumentNullException(nameof(lovCode));

        var result = new List<Object>();
        var inlineEnums = new Dictionary<String, Object>();

        foreach (var code in codes)
        {
            // 1) 手工定义（名值定义）优先
            var def = LovStore.FindDef(code);
            if (def != null)
            {
                if (def.Type == "ENUM")
                {
                    var options = BuildEnumOptions(code);
                    if (options == null) continue;

                    result.Add(new
                    {
                        LovCode = def.LovCode,
                        Type = "ENUM",
                        Name = def.Name,
                        Options = options,
                    });
                }
                else if (def.Type == "LIST")
                {
                    var m = ResolveListMeta(def.LovCode, def.Name, def.ValueField, def.LabelField,
                        LovStore.FindListConfig(code), LovStore.FindSearchFields(code), LovStore.FindTableColumns(code), inlineEnums);
                    if (m != null) result.Add(m);
                }
                continue;
            }

            // 2) 代码声明：枚举型反射 / [LovList] 列表型描述符
            if (code.StartsWith("Enum."))
            {
                var type = LovRegistry.FindEnumType(code[5..]);
                if (type == null) continue;

                var options = LovRegistry.GetEnumOptions(type)
                    .Select(e => (Object)new { e.Value, e.Label })
                    .ToList();
                result.Add(new
                {
                    LovCode = code,
                    Type = "ENUM",
                    Name = LovRegistry.GetEnumName(type),
                    Options = options,
                });
            }
            else if (code.StartsWith("List."))
            {
                var desc = LovRegistry.FindList(code);
                if (desc == null) continue;

                var m = ResolveListMeta(code, desc.Name, desc.ValueField, desc.LabelField,
                    desc.Config, desc.SearchFields, desc.TableColumns, inlineEnums);
                if (m != null) result.Add(m);
            }
        }

        return new
        {
            Meta = result,
            InlineEnums = inlineEnums.Count > 0 ? inlineEnums : null,
        };
    }

    /// <summary>列表数据代理。根据列表型值集配置代理请求外部接口（手工配置或 [LovList] 描述符）</summary>
    /// <param name="request">查询请求</param>
    /// <returns>代理查询结果</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<Object> ListData([FromBody] LovListDataRequest request)
    {
        if (request == null || request.LovCode.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(request));

        var config = ResolveListConfig(request.LovCode);
        if (config == null)
            throw new InvalidOperationException($"值集 {request.LovCode} 不存在或未配置列表数据源");

        // 通过 IOC 获取列表数据代理实现（默认 DefaultLovListDataProxy，可被使用者覆盖）
        var proxy = HttpContext.RequestServices.GetRequiredService<ILovListDataProxy>();
        var result = await proxy.FetchAsync(config, request);

        return new
        {
            Data = result.Data,
            Total = result.Total,
        };
    }
    #endregion

    #region 配置管理（GetConfig / SaveConfig，仅手工定义）

    /// <summary>获取值集完整配置。包含枚举值/列表配置/搜索字段/表格列</summary>
    /// <param name="id">值集编码（LovCode）</param>
    /// <returns>值集完整配置</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public Object GetConfig([FromQuery] String id)
    {
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var def = LovStore.FindDef(id);
        if (def == null) throw new InvalidOperationException($"值集 {id} 不存在");

        var result = new Dictionary<String, Object?>
        {
            ["id"] = def.LovCode,
            ["lovCode"] = def.LovCode,
            ["name"] = def.Name,
            ["type"] = def.Type,
            ["source"] = def.Source,
            ["valueField"] = def.ValueField,
            ["labelField"] = def.LabelField,
            ["enabled"] = def.Enabled,
        };

        if (def.Type == "ENUM")
        {
            var items = LovStore.FindEnumItems(id)
                .OrderBy(e => e.Sort)
                .Select(e => (Object)new Dictionary<String, Object?>
                {
                    ["id"] = e.Id,
                    ["lovDefId"] = def.LovCode,
                    ["value"] = e.Value,
                    ["label"] = e.Label,
                    ["sort"] = e.Sort,
                    ["enabled"] = e.Enabled,
                    ["extra"] = e.Extra,
                }).ToList();
            result["enumItems"] = items;
        }
        else if (def.Type == "LIST")
        {
            var config = LovStore.FindListConfig(id);
            result["listConfig"] = config == null ? null : new Dictionary<String, Object?>
            {
                ["id"] = config.Id,
                ["lovDefId"] = def.LovCode,
                ["requestUrl"] = config.RequestUrl,
                ["method"] = config.Method,
                ["pageable"] = config.Pageable,
                ["pageNumField"] = config.PageNumField,
                ["pageSizeField"] = config.PageSizeField,
                ["dataPath"] = config.DataPath,
                ["totalPath"] = config.TotalPath,
                ["fixedParams"] = config.FixedParams,
                ["proxyRequest"] = config.ProxyRequest,
            };

            var fields = LovStore.FindSearchFields(id)
                .OrderBy(e => e.Sort)
                .Select(e => (Object)new Dictionary<String, Object?>
                {
                    ["id"] = e.Id, ["lovDefId"] = def.LovCode,
                    ["field"] = e.Field, ["title"] = e.Title,
                    ["componentType"] = e.ComponentType,
                    ["paramType"] = e.ParamType,
                    ["required"] = e.Required,
                    ["defaultValue"] = e.DefaultValue,
                    ["sort"] = e.Sort,
                    ["refLovCode"] = e.RefLovCode,
                }).ToList();
            result["searchFields"] = fields;

            var cols = LovStore.FindTableColumns(id)
                .OrderBy(e => e.Sort)
                .Select(e => (Object)new Dictionary<String, Object?>
                {
                    ["id"] = e.Id, ["lovDefId"] = def.LovCode,
                    ["field"] = e.Field, ["title"] = e.Title,
                    ["width"] = e.Width, ["align"] = e.Align,
                    ["sortable"] = e.Sortable,
                    ["refLovCode"] = e.RefLovCode,
                    ["formatType"] = e.FormatType,
                    ["sort"] = e.Sort,
                }).ToList();
            result["tableColumns"] = cols;
        }

        return result;
    }

    /// <summary>保存值集完整配置。全量替换子表数据（增/改/删），仅手工定义可写</summary>
    /// <param name="body">配置 JSON（含 id=LovCode）</param>
    /// <returns>保存结果</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost]
    public Object SaveConfig([FromBody] JsonDocument body)
    {
        var root = body.RootElement;
        var id = GetStr(root, "id") ?? GetStr(root, "lovCode");
        if (id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(id));

        var def = LovStore.FindDef(id);
        if (def == null) throw new InvalidOperationException($"值集 {id} 不存在");

        // ENUM 类型：保存枚举值（名值对）
        if (def.Type == "ENUM")
        {
            if (root.TryGetProperty("enumItems", out var enumItems) && enumItems.ValueKind == JsonValueKind.Array)
                BatchSaveEnumItems(id, enumItems);
        }
        // LIST 类型：保存列表配置 + 搜索字段 + 表格列
        else if (def.Type == "LIST")
        {
            if (root.TryGetProperty("listConfig", out var lc) && lc.ValueKind == JsonValueKind.Object)
                SaveListConfig(id, lc);

            if (root.TryGetProperty("searchFields", out var sfs) && sfs.ValueKind == JsonValueKind.Array)
                BatchSaveSearchFields(id, sfs);

            if (root.TryGetProperty("tableColumns", out var tcs) && tcs.ValueKind == JsonValueKind.Array)
                BatchSaveTableColumns(id, tcs);
        }

        return new { success = true };
    }

    /// <summary>全量替换枚举值。整表覆盖到 Parameter（按 LovCode 聚合一条）</summary>
    private static void BatchSaveEnumItems(String lovCode, JsonElement items)
    {
        var list = new List<LovEnumItemModel>();
        foreach (var item in items.EnumerateArray())
        {
            var model = new LovEnumItemModel();
            model.Value = item.GetProperty("value").GetString() ?? "";
            model.Label = item.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
            model.Sort = item.TryGetProperty("sort", out var s) ? s.GetInt32() : 0;
            model.Enabled = item.TryGetProperty("enabled", out var e) ? e.GetBoolean() : true;
            model.Extra = item.TryGetProperty("extra", out var ex) ? ex.GetString() : null;
            list.Add(model);
        }
        LovStore.SaveEnumItems(lovCode, list);
    }

    /// <summary>保存列表配置（单条）。整表覆盖到 Parameter</summary>
    private static void SaveListConfig(String lovCode, JsonElement config)
    {
        var model = new LovListConfigModel();
        model.RequestUrl = config.TryGetProperty("requestUrl", out var ru) ? ru.GetString() : null;
        model.Method = config.TryGetProperty("method", out var m) ? m.GetString() : "GET";
        model.Pageable = config.TryGetProperty("pageable", out var p) ? p.GetBoolean() : false;
        model.PageNumField = config.TryGetProperty("pageNumField", out var pf) ? pf.GetString() : null;
        model.PageSizeField = config.TryGetProperty("pageSizeField", out var psf) ? psf.GetString() : null;
        model.DataPath = config.TryGetProperty("dataPath", out var dp) ? dp.GetString() : null;
        model.TotalPath = config.TryGetProperty("totalPath", out var tp) ? tp.GetString() : null;
        model.FixedParams = config.TryGetProperty("fixedParams", out var fp) ? fp.GetString() : null;
        model.ProxyRequest = config.TryGetProperty("proxyRequest", out var pr) ? pr.GetBoolean() : false;
        LovStore.SaveListConfig(lovCode, model);
    }

    /// <summary>全量替换搜索字段。整表覆盖到 Parameter</summary>
    private static void BatchSaveSearchFields(String lovCode, JsonElement fields)
    {
        var list = new List<LovSearchFieldModel>();
        foreach (var item in fields.EnumerateArray())
        {
            var model = new LovSearchFieldModel();
            model.Field = item.GetProperty("field").GetString() ?? "";
            model.Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            model.ComponentType = item.TryGetProperty("componentType", out var ct) ? ct.GetString() : "input";
            model.ParamType = item.TryGetProperty("paramType", out var pt) ? pt.GetString() : "BODY";
            model.Required = item.TryGetProperty("required", out var r) ? r.GetBoolean() : false;
            model.DefaultValue = item.TryGetProperty("defaultValue", out var dv) ? dv.GetString() : null;
            model.Sort = item.TryGetProperty("sort", out var s) ? s.GetInt32() : 0;
            model.RefLovCode = item.TryGetProperty("refLovCode", out var rc) ? rc.GetString() : null;
            list.Add(model);
        }
        LovStore.SaveSearchFields(lovCode, list);
    }

    /// <summary>全量替换表格列。整表覆盖到 Parameter</summary>
    private static void BatchSaveTableColumns(String lovCode, JsonElement columns)
    {
        var list = new List<LovTableColumnModel>();
        foreach (var item in columns.EnumerateArray())
        {
            var model = new LovTableColumnModel();
            model.Field = item.GetProperty("field").GetString() ?? "";
            model.Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            model.Width = item.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
            model.Align = item.TryGetProperty("align", out var a) ? a.GetString() : "left";
            model.Sortable = item.TryGetProperty("sortable", out var so) ? so.GetBoolean() : false;
            model.RefLovCode = item.TryGetProperty("refLovCode", out var rc) ? rc.GetString() : null;
            model.FormatType = item.TryGetProperty("formatType", out var ft) ? ft.GetString() : null;
            model.Sort = item.TryGetProperty("sort", out var s) ? s.GetInt32() : 0;
            list.Add(model);
        }
        LovStore.SaveTableColumns(lovCode, list);
    }
    #endregion

    #region 辅助

    /// <summary>构建定义列表行 JSON（兼容旧 LovDefinition 列表字段，id=LovCode）</summary>
    private static Object ToRow(LovDefModel def) => new
    {
        id = def.LovCode,
        lovCode = def.LovCode,
        name = def.Name,
        type = def.Type,
        valueField = def.ValueField,
        labelField = def.LabelField,
        source = def.Source,
        enabled = def.Enabled,
        remark = def.Remark,
        createTime = def.CreateTime <= DateTime.MinValue ? (DateTime?)null : def.CreateTime,
        updateTime = def.UpdateTime <= DateTime.MinValue ? (DateTime?)null : def.UpdateTime,
    };

    /// <summary>解析定义 JSON 为模型（键为小写：lovCode/name/type/valueField/labelField/source/enabled/remark）</summary>
    private static LovDefModel ParseDef(JsonElement root)
    {
        var model = new LovDefModel();
        model.LovCode = GetStr(root, "lovCode") ?? "";
        model.Name = GetStr(root, "name") ?? "";
        model.Type = GetStr(root, "type") ?? "ENUM";
        model.ValueField = GetStr(root, "valueField") ?? "id";
        model.LabelField = GetStr(root, "labelField") ?? "name";
        model.Source = GetStr(root, "source") ?? "MANUAL";
        model.Enabled = GetBool(root, "enabled", true);
        model.Remark = GetStr(root, "remark");
        return model;
    }

    /// <summary>校验值集编码前缀与类型一致</summary>
    private static void CheckCode(LovDefModel def)
    {
        if (def.LovCode.IsNullOrEmpty())
            throw new InvalidOperationException("值集编码不能为空");
        if (def.Type != "ENUM" && def.Type != "LIST")
            throw new InvalidOperationException($"值集类型 {def.Type} 无效，仅支持 ENUM/LIST");

        var prefix = def.LovCode.Split('.')[0];
        var expectedPrefix = def.Type == "ENUM" ? "Enum" : "List";
        if (!prefix.EqualIgnoreCase(expectedPrefix))
            throw new InvalidOperationException($"值集编码前缀与类型不匹配：LovCode 以 '{prefix}' 开头，但 Type 为 '{def.Type}'，{def.Type} 类型必须以 '{expectedPrefix}.' 开头");
    }

    /// <summary>读取字符串属性（支持字符串/数字/布尔），不存在返回 null</summary>
    private static String? GetStr(JsonElement root, String name)
    {
        if (!root.TryGetProperty(name, out var v)) return null;

        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => v.ToString(),
            _ => null,
        };
    }

    /// <summary>读取布尔属性，不存在或非布尔返回默认值</summary>
    private static Boolean GetBool(JsonElement root, String name, Boolean def)
        => root.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : def;

    /// <summary>解析列表型值集的数据源配置。手工定义（LIST）优先读 Parameter；否则读 [LovList] 描述符</summary>
    private static LovListConfigModel? ResolveListConfig(String lovCode)
    {
        var def = LovStore.FindDef(lovCode);
        if (def != null)
        {
            if (def.Type != "LIST") return null;

            return LovStore.FindListConfig(lovCode);
        }

        return LovRegistry.FindList(lovCode)?.Config;
    }

    /// <summary>解析枚举型选项。手工"名值定义"优先，否则反射代码枚举</summary>
    private static IList<Object>? BuildEnumOptions(String lovCode)
    {
        // 手工定义优先
        var def = LovStore.FindDef(lovCode);
        if (def != null && def.Type == "ENUM")
        {
            return LovStore.FindEnumItems(lovCode)
                .Where(e => e.Enabled)
                .OrderBy(e => e.Sort)
                .Select(e => (Object)new { e.Value, e.Label, e.Extra })
                .ToList();
        }

        // 代码枚举反射
        if (lovCode.StartsWith("Enum."))
        {
            var type = LovRegistry.FindEnumType(lovCode[5..]);
            if (type == null) return null;

            return LovRegistry.GetEnumOptions(type)
                .Select(e => (Object)new { e.Value, e.Label, Extra = (String?)null })
                .ToList();
        }

        return null;
    }

    /// <summary>构建列表型值集元数据（含内联引用的枚举），供 Meta 使用</summary>
    private static Object? ResolveListMeta(String lovCode, String name, String valueField, String labelField,
        LovListConfigModel? config, IList<LovSearchFieldModel> searchFields, IList<LovTableColumnModel> tableColumns,
        Dictionary<String, Object> inlineEnums)
    {
        var sfs = searchFields.OrderBy(e => e.Sort)
            .Select(e => (Object)new
            {
                e.Field,
                e.Title,
                ComponentType = e.ComponentType,
                ParamType = e.ParamType,
                e.Required,
                DefaultValue = e.DefaultValue,
                RefLovCode = e.RefLovCode,
            })
            .ToList();

        var tcs = tableColumns.OrderBy(e => e.Sort)
            .Select(e => (Object)new
            {
                e.Field,
                e.Title,
                e.Width,
                Align = e.Align ?? "left",
                e.Sortable,
                RefLovCode = e.RefLovCode,
                FormatType = e.FormatType,
            })
            .ToList();

        // 收集所有引用的枚举型值集，内联 options（代码枚举或手工名值均可）
        var refLovCodes = searchFields.Where(s => !s.RefLovCode.IsNullOrEmpty()).Select(s => s.RefLovCode)
            .Concat(tableColumns.Where(c => !c.RefLovCode.IsNullOrEmpty()).Select(c => c.RefLovCode));
        foreach (var refCode in refLovCodes.Distinct())
        {
            if (refCode.StartsWith("Enum.") && !inlineEnums.ContainsKey(refCode))
            {
                var items = BuildEnumOptions(refCode);
                if (items != null) inlineEnums[refCode] = items;
            }
        }

        return new
        {
            LovCode = lovCode,
            Type = "LIST",
            Name = name,
            ValueField = valueField,
            LabelField = labelField,
            ListConfig = config == null ? null : new
            {
                config.RequestUrl,
                config.Method,
                config.Pageable,
                config.PageNumField,
                config.PageSizeField,
                config.DataPath,
                config.TotalPath,
                config.FixedParams,
                config.ProxyRequest,
            },
            SearchFields = sfs,
            TableColumns = tcs,
        };
    }
    #endregion
}

/// <summary>列表数据查询请求（类型转发至 NewLife.Cube.Services.LovListDataRequest，便于兼容旧引用）</summary>
public class LovListDataRequest : NewLife.Cube.Services.LovListDataRequest;
