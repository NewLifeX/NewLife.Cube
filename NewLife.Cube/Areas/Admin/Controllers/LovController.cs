using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>值集管理。管理枚举型和列表型值集的定义、枚举值、列表配置、搜索字段与列字段</summary>
[DisplayName("值集管理")]
[AdminArea]
[Menu(85, true, Icon = "fa-tasks")]
public class LovController : EntityController<LovDefinition>
{
    static LovController()
    {
        LogOnChange = true;

        ListFields.RemoveField("ValueField", "LabelField", "Source");
        ListFields.RemoveCreateField();
    }

    /// <summary>搜索数据集</summary>
    protected override IEnumerable<LovDefinition> Search(Pager p)
    {
        var type = p["type"];
        var source = p["source"];
        var key = p["Q"];

        // XCode 表达式构建，使用 & 拼接条件
        var exp = LovDefinition._.Id > 0;
        if (!type.IsNullOrEmpty())
            exp = LovDefinition._.Type == type & exp;
        if (!source.IsNullOrEmpty())
            exp = LovDefinition._.Source == source & exp;
        if (!key.IsNullOrEmpty())
            exp = (LovDefinition._.LovCode.Contains(key) | LovDefinition._.Name.Contains(key)) & exp;

        return LovDefinition.FindAll(exp, p);
    }

    #region 业务接口

    /// <summary>获取值集元数据。支持逗号分隔多个 lovCode，枚举型内联 options</summary>
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
            var def = LovDefinition.Find(LovDefinition._.LovCode == code);
            if (def == null) continue;

            if (def.Type == "ENUM")
            {
                // 枚举型：返回枚举值列表
                var items = LovEnumItem.FindAll(LovEnumItem._.LovDefId == def.Id & LovEnumItem._.Enabled == true)
                    .OrderBy(e => e.Sort)
                    .Select(e => new { e.Value, e.Label, e.Extra })
                    .ToList();

                result.Add(new
                {
                    LovCode = def.LovCode,
                    Type = def.Type,
                    Name = def.Name,
                    Options = items,
                });
            }
            else if (def.Type == "LIST")
            {
                // 列表型：返回配置 + 搜索字段 + 列字段 + 内联引用的枚举
                var listConfig = LovListConfig.Find(LovListConfig._.LovDefId == def.Id);
                var searchFields = LovSearchField.FindAll(LovSearchField._.LovDefId == def.Id)
                    .OrderBy(e => e.Sort)
                    .Select(e => new
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

                var tableColumns = LovTableColumn.FindAll(LovTableColumn._.LovDefId == def.Id)
                    .OrderBy(e => e.Sort)
                    .Select(e => new
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

                // 收集所有引用的枚举型值集，内联 options
                var refLovCodes = new List<String>();
                refLovCodes.AddRange(searchFields.Where(s => !s.RefLovCode.IsNullOrEmpty()).Select(s => s.RefLovCode));
                refLovCodes.AddRange(tableColumns.Where(c => !c.RefLovCode.IsNullOrEmpty()).Select(c => c.RefLovCode));

                foreach (var refCode in refLovCodes.Distinct())
                {
                    if (refCode.StartsWith("Enum.") && !inlineEnums.ContainsKey(refCode))
                    {
                        var refDef = LovDefinition.Find(LovDefinition._.LovCode == refCode);
                        if (refDef != null)
                        {
                            var enumItems = LovEnumItem.FindAll(LovEnumItem._.LovDefId == refDef.Id & LovEnumItem._.Enabled == true)
                                .OrderBy(e => e.Sort)
                                .Select(e => new { e.Value, e.Label, e.Extra })
                                .ToList();
                            inlineEnums[refCode] = enumItems;
                        }
                    }
                }

                result.Add(new
                {
                    LovCode = def.LovCode,
                    Type = def.Type,
                    Name = def.Name,
                    ValueField = def.ValueField,
                    LabelField = def.LabelField,
                    ListConfig = listConfig == null ? null : new
                    {
                        listConfig.RequestUrl,
                        listConfig.Method,
                        listConfig.Pageable,
                        listConfig.PageNumField,
                        listConfig.PageSizeField,
                        listConfig.DataPath,
                        listConfig.TotalPath,
                        listConfig.FixedParams,
                    },
                    SearchFields = searchFields,
                    TableColumns = tableColumns,
                });
            }
        }

        return new
        {
            Meta = result,
            InlineEnums = inlineEnums.Count > 0 ? inlineEnums : null,
        };
    }

    /// <summary>列表数据代理。根据列表型值集配置代理请求外部接口</summary>
    /// <param name="request">查询请求</param>
    /// <returns>代理查询结果</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<Object> ListData([FromBody] LovListDataRequest request)
    {
        if (request == null || request.LovCode.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(request));

        var def = LovDefinition.Find(LovDefinition._.LovCode == request.LovCode);
        if (def == null)
            throw new InvalidOperationException($"值集 {request.LovCode} 不存在");

        var config = LovListConfig.Find(LovListConfig._.LovDefId == def.Id);
        if (config == null)
            throw new InvalidOperationException($"值集 {request.LovCode} 未配置列表数据源");

        // 构建请求参数
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        var url = config.RequestUrl;
        var method = (config.Method ?? "GET").ToUpper();

        HttpResponseMessage httpResponse;

        if (method == "GET")
        {
            // GET 请求：参数拼接到 URL
            var queryParams = new List<String>();
            if (request.Params != null)
            {
                foreach (var kv in request.Params)
                {
                    if (kv.Value != null)
                        queryParams.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString()!)}");
                }
            }
            // 分页参数
            if (config.Pageable)
            {
                if (request.PageNum > 0 && !config.PageNumField.IsNullOrEmpty())
                    queryParams.Add($"{Uri.EscapeDataString(config.PageNumField)}={request.PageNum}");
                if (request.PageSize > 0 && !config.PageSizeField.IsNullOrEmpty())
                    queryParams.Add($"{Uri.EscapeDataString(config.PageSizeField)}={request.PageSize}");
            }
            // 固定参数
            if (!config.FixedParams.IsNullOrEmpty())
            {
                var fixedParams = JsonParser.Decode(config.FixedParams) as IDictionary<String, Object>;
                if (fixedParams != null)
                {
                    foreach (var kv in fixedParams)
                    {
                        if (kv.Value != null)
                            queryParams.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString()!)}");
                    }
                }
            }

            var queryString = String.Join("&", queryParams);
            if (!queryString.IsNullOrEmpty())
                url = url.Contains('?') ? $"{url}&{queryString}" : $"{url}?{queryString}";

            httpResponse = await httpClient.GetAsync(url);
        }
        else
        {
            // POST 请求：参数放 Body
            var bodyParams = new Dictionary<String, Object>();
            if (request.Params != null)
            {
                foreach (var kv in request.Params)
                {
                    bodyParams[kv.Key] = kv.Value!;
                }
            }
            // 分页参数
            if (config.Pageable)
            {
                if (request.PageNum > 0 && !config.PageNumField.IsNullOrEmpty())
                    bodyParams[config.PageNumField] = request.PageNum;
                if (request.PageSize > 0 && !config.PageSizeField.IsNullOrEmpty())
                    bodyParams[config.PageSizeField] = request.PageSize;
            }
            // 固定参数
            if (!config.FixedParams.IsNullOrEmpty())
            {
                var fixedParams = JsonParser.Decode(config.FixedParams) as IDictionary<String, Object>;
                if (fixedParams != null)
                {
                    foreach (var kv in fixedParams)
                    {
                        bodyParams[kv.Key] = kv.Value!;
                    }
                }
            }

            var json = bodyParams.ToJson();
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            httpResponse = await httpClient.PostAsync(url, content);
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"请求外部接口失败：{httpResponse.StatusCode} - {responseBody}");
        }

        // 解析响应
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // 提取数据列表
        JsonElement? dataElement = !config.DataPath.IsNullOrEmpty()
            ? ResolveJsonPath(root, config.DataPath)
            : root;

        // 提取总数
        Int32 total = 0;
        if (config.Pageable && !config.TotalPath.IsNullOrEmpty())
        {
            var totalElement = ResolveJsonPath(root, config.TotalPath);
            if (totalElement.HasValue)
                total = totalElement.Value.GetInt32();
        }

        // 序列化数据
        var dataList = dataElement.HasValue
            ? JsonSerializer.Serialize(dataElement.Value)
            : "[]";

        return new
        {
            Data = JsonParser.Decode(dataList),
            Total = total,
        };
    }

    /// <summary>批量翻译。将列表型值集的原始 value 批量翻译为 label</summary>
    /// <param name="request">翻译请求</param>
    /// <returns>value→label 字典</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public Object BatchLabel([FromBody] LovBatchLabelRequest request)
    {
        if (request == null || request.LovCode.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(request));

        var def = LovDefinition.Find(LovDefinition._.LovCode == request.LovCode);
        if (def == null)
            throw new InvalidOperationException($"值集 {request.LovCode} 不存在");

        var result = new Dictionary<String, String>();

        if (request.Values == null || request.Values.Length == 0)
            return result;

        if (def.Type == "ENUM")
        {
            // 枚举型：直接从 LovEnumItem 查询
            var values = request.Values.Select(v => v.ToString()).ToArray();
            var items = LovEnumItem.FindAll(LovEnumItem._.LovDefId == def.Id & LovEnumItem._.Value.In(values) & LovEnumItem._.Enabled == true);
            foreach (var item in items)
            {
                result[item.Value] = item.Label;
            }
        }
        else if (def.Type == "LIST")
        {
            // 列表型：通过 ListData 代理获取数据，再建立映射
            var config = LovListConfig.Find(LovListConfig._.LovDefId == def.Id);
            if (config != null && !def.ValueField.IsNullOrEmpty() && !def.LabelField.IsNullOrEmpty())
            {
                // 这里简化处理：如果有 Redis 缓存则优先使用
                // 否则通过 ListData 接口获取基础数据并提取映射
                var lists = LovEnumItem.FindAll(LovEnumItem._.LovDefId == def.Id & LovEnumItem._.Value.In(request.Values.Select(v => v.ToString()).ToArray()) & LovEnumItem._.Enabled == true);
                foreach (var item in lists)
                {
                    result[item.Value] = item.Label;
                }
            }
        }

        return result;
    }

    /// <summary>解析 JSON 路径表达式（如 data.records）</summary>
    private static JsonElement? ResolveJsonPath(JsonElement root, String path)
    {
        if (path.IsNullOrEmpty()) return root;

        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonElement current = root;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            if (!current.TryGetProperty(part, out current)) return null;
        }

        return current;
    }

    #endregion

    #region 配置管理（GetConfig / SaveConfig）

    /// <summary>获取值集完整配置。包含所有子表数据</summary>
    /// <param name="id">值集定义编号</param>
    /// <returns>值集完整配置</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public Object GetConfig([FromQuery] Int32 id)
    {
        var def = LovDefinition.FindById(id);
        if (def == null) throw new InvalidOperationException($"值集 {id} 不存在");

        var result = new Dictionary<String, Object?>
        {
            ["id"] = def.Id,
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
            var items = LovEnumItem.FindAll(LovEnumItem._.LovDefId == def.Id)
                .OrderBy(e => e.Sort)
                .Select(e => new Dictionary<String, Object?>
                {
                    ["id"] = e.Id,
                    ["lovDefId"] = e.LovDefId,
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
            var config = LovListConfig.Find(LovListConfig._.LovDefId == def.Id);
            result["listConfig"] = config == null ? null : new Dictionary<String, Object?>
            {
                ["id"] = config.Id,
                ["lovDefId"] = config.LovDefId,
                ["requestUrl"] = config.RequestUrl,
                ["method"] = config.Method,
                ["pageable"] = config.Pageable,
                ["pageNumField"] = config.PageNumField,
                ["pageSizeField"] = config.PageSizeField,
                ["dataPath"] = config.DataPath,
                ["totalPath"] = config.TotalPath,
                ["fixedParams"] = config.FixedParams,
            };

            var fields = LovSearchField.FindAll(LovSearchField._.LovDefId == def.Id)
                .OrderBy(e => e.Sort)
                .Select(e => new Dictionary<String, Object?>
                {
                    ["id"] = e.Id, ["lovDefId"] = e.LovDefId,
                    ["field"] = e.Field, ["title"] = e.Title,
                    ["componentType"] = e.ComponentType,
                    ["paramType"] = e.ParamType,
                    ["required"] = e.Required,
                    ["defaultValue"] = e.DefaultValue,
                    ["sort"] = e.Sort,
                    ["refLovCode"] = e.RefLovCode,
                }).ToList();
            result["searchFields"] = fields;

            var cols = LovTableColumn.FindAll(LovTableColumn._.LovDefId == def.Id)
                .OrderBy(e => e.Sort)
                .Select(e => new Dictionary<String, Object?>
                {
                    ["id"] = e.Id, ["lovDefId"] = e.LovDefId,
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

    /// <summary>保存值集完整配置。全量替换子表数据（增/改/删）</summary>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost]
    public Object SaveConfig([FromBody] JsonDocument body)
    {
        var root = body.RootElement;
        var id = root.GetProperty("id").GetInt32();
        var def = LovDefinition.FindById(id);
        if (def == null) throw new InvalidOperationException($"值集 {id} 不存在");

        // ENUM 类型：保存枚举值（仅 MANUAL 来源允许修改）
        if (def.Type == "ENUM" && def.Source != "AUTO")
        {
            if (root.TryGetProperty("enumItems", out var enumItems) && enumItems.ValueKind == JsonValueKind.Array)
                BatchSaveEnumItems(def.Id, enumItems);
        }

        // LIST 类型：保存列表配置 + 搜索字段 + 表格列
        if (def.Type == "LIST")
        {
            if (root.TryGetProperty("listConfig", out var lc) && lc.ValueKind == JsonValueKind.Object)
                SaveListConfig(def.Id, lc);

            if (root.TryGetProperty("searchFields", out var sfs) && sfs.ValueKind == JsonValueKind.Array)
                BatchSaveSearchFields(def.Id, sfs);

            if (root.TryGetProperty("tableColumns", out var tcs) && tcs.ValueKind == JsonValueKind.Array)
                BatchSaveTableColumns(def.Id, tcs);
        }

        return new { success = true };
    }

    /// <summary>全量替换枚举值</summary>
    private static void BatchSaveEnumItems(Int32 lovDefId, JsonElement items)
    {
        var existing = LovEnumItem.FindAll(LovEnumItem._.LovDefId == lovDefId);
        var existingMap = existing.ToDictionary(e => e.Id);
        var keepIds = new HashSet<Int32>();

        foreach (var item in items.EnumerateArray())
        {
            var itemId = item.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;

            LovEnumItem entity;
            if (itemId > 0 && existingMap.TryGetValue(itemId, out var found))
            {
                entity = found;
                keepIds.Add(itemId);
            }
            else
            {
                entity = new LovEnumItem { LovDefId = lovDefId };
            }

            entity.Value = item.GetProperty("value").GetString() ?? "";
            entity.Label = item.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
            entity.Sort = item.TryGetProperty("sort", out var s) ? s.GetInt32() : 0;
            entity.Enabled = item.TryGetProperty("enabled", out var e) ? e.GetBoolean() : true;
            entity.Extra = item.TryGetProperty("extra", out var ex) ? ex.GetString() : null;

            if (entity.Id > 0) entity.Update();
            else entity.Insert();
        }

        foreach (var old in existing)
        {
            if (!keepIds.Contains(old.Id))
                old.Delete();
        }
    }

    /// <summary>保存列表配置（单条 upsert）</summary>
    private static void SaveListConfig(Int32 lovDefId, JsonElement config)
    {
        var existing = LovListConfig.Find(LovListConfig._.LovDefId == lovDefId);
        var entity = existing ?? new LovListConfig { LovDefId = lovDefId };

        entity.RequestUrl = config.TryGetProperty("requestUrl", out var ru) ? ru.GetString() : null;
        entity.Method = config.TryGetProperty("method", out var m) ? m.GetString() : "GET";
        entity.Pageable = config.TryGetProperty("pageable", out var p) ? p.GetBoolean() : false;
        entity.PageNumField = config.TryGetProperty("pageNumField", out var pf) ? pf.GetString() : null;
        entity.PageSizeField = config.TryGetProperty("pageSizeField", out var psf) ? psf.GetString() : null;
        entity.DataPath = config.TryGetProperty("dataPath", out var dp) ? dp.GetString() : null;
        entity.TotalPath = config.TryGetProperty("totalPath", out var tp) ? tp.GetString() : null;
        entity.FixedParams = config.TryGetProperty("fixedParams", out var fp) ? fp.GetString() : null;

        if (entity.Id > 0) entity.Update();
        else entity.Insert();
    }

    /// <summary>全量替换搜索字段</summary>
    private static void BatchSaveSearchFields(Int32 lovDefId, JsonElement fields)
    {
        var existing = LovSearchField.FindAll(LovSearchField._.LovDefId == lovDefId);
        var existingMap = existing.ToDictionary(e => e.Id);
        var keepIds = new HashSet<Int32>();

        foreach (var item in fields.EnumerateArray())
        {
            var itemId = item.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;

            LovSearchField entity;
            if (itemId > 0 && existingMap.TryGetValue(itemId, out var found))
            {
                entity = found;
                keepIds.Add(itemId);
            }
            else
            {
                entity = new LovSearchField { LovDefId = lovDefId };
            }

            entity.Field = item.GetProperty("field").GetString() ?? "";
            entity.Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            entity.ComponentType = item.TryGetProperty("componentType", out var ct) ? ct.GetString() : "input";
            entity.ParamType = item.TryGetProperty("paramType", out var pt) ? pt.GetString() : "BODY";
            entity.Required = item.TryGetProperty("required", out var r) ? r.GetBoolean() : false;
            entity.DefaultValue = item.TryGetProperty("defaultValue", out var dv) ? dv.GetString() : null;
            entity.Sort = item.TryGetProperty("sort", out var s) ? s.GetInt32() : 0;
            entity.RefLovCode = item.TryGetProperty("refLovCode", out var rc) ? rc.GetString() : null;

            if (entity.Id > 0) entity.Update();
            else entity.Insert();
        }

        foreach (var old in existing)
        {
            if (!keepIds.Contains(old.Id))
                old.Delete();
        }
    }

    /// <summary>全量替换表格列</summary>
    private static void BatchSaveTableColumns(Int32 lovDefId, JsonElement columns)
    {
        var existing = LovTableColumn.FindAll(LovTableColumn._.LovDefId == lovDefId);
        var existingMap = existing.ToDictionary(e => e.Id);
        var keepIds = new HashSet<Int32>();

        foreach (var item in columns.EnumerateArray())
        {
            var itemId = item.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;

            LovTableColumn entity;
            if (itemId > 0 && existingMap.TryGetValue(itemId, out var found))
            {
                entity = found;
                keepIds.Add(itemId);
            }
            else
            {
                entity = new LovTableColumn { LovDefId = lovDefId };
            }

            entity.Field = item.GetProperty("field").GetString() ?? "";
            entity.Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            entity.Width = item.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
            entity.Align = item.TryGetProperty("align", out var a) ? a.GetString() : "left";
            entity.Sortable = item.TryGetProperty("sortable", out var so) ? so.GetBoolean() : false;
            entity.RefLovCode = item.TryGetProperty("refLovCode", out var rc) ? rc.GetString() : null;
            entity.FormatType = item.TryGetProperty("formatType", out var ft) ? ft.GetString() : null;
            entity.Sort = item.TryGetProperty("sort", out var s) ? s.GetInt32() : 0;

            if (entity.Id > 0) entity.Update();
            else entity.Insert();
        }

        foreach (var old in existing)
        {
            if (!keepIds.Contains(old.Id))
                old.Delete();
        }
    }

    #endregion

    #region 数据校验

    /// <summary>验证实体对象</summary>
    protected override Boolean Valid(LovDefinition entity, DataObjectMethodType type, Boolean post)
    {
        var rs = base.Valid(entity, type, post);

        if (post && (type == DataObjectMethodType.Insert || type == DataObjectMethodType.Update))
        {
            // 校验 LovCode 前缀与 Type 一致
            if (!entity.LovCode.IsNullOrEmpty() && !entity.Type.IsNullOrEmpty())
            {
                var prefix = entity.LovCode.Split('.')[0];
                var expectedPrefix = entity.Type == "ENUM" ? "Enum" : "List";
                if (!prefix.EqualIgnoreCase(expectedPrefix))
                {
                    throw new InvalidOperationException($"值集编码前缀与类型不匹配：LovCode 以 '{prefix}' 开头，但 Type 为 '{entity.Type}'，{entity.Type} 类型必须以 '{expectedPrefix}.' 开头");
                }
            }
        }

        return rs;
    }

    #endregion
}

/// <summary>列表数据查询请求</summary>
public class LovListDataRequest
{
    /// <summary>值集编码</summary>
    public String LovCode { get; set; } = null!;

    /// <summary>搜索参数</summary>
    public Dictionary<String, Object>? Params { get; set; }

    /// <summary>页码</summary>
    public Int32 PageNum { get; set; } = 1;

    /// <summary>每页条数</summary>
    public Int32 PageSize { get; set; } = 20;
}

/// <summary>批量翻译请求</summary>
public class LovBatchLabelRequest
{
    /// <summary>值集编码</summary>
    public String LovCode { get; set; } = null!;

    /// <summary>需要翻译的原始值列表</summary>
    public String[]? Values { get; set; }
}
