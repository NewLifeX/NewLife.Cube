using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.DataAccessLayer;
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
        // 首次打开管理页时补齐样例（幂等；与 UseCube 种子互补，避免未重启宿主时列表仍空）
        LovSampleSeeds.Ensure();

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

            // 2) 代码声明：内部实体值集（Entity.{Type}）→ 按实体工厂即时合成，不落库
            if (ResolveEntityFactory(code) is { } entityFact)
            {
                var m = ResolveListMeta(code, entityFact.EntityType?.Name ?? code, ValueFieldOf(entityFact), LabelFieldOf(entityFact),
                    BuildEntityListConfig(entityFact), BuildEntitySearchFields(), BuildEntityColumns(entityFact), inlineEnums);
                if (m != null) result.Add(m);
                continue;
            }

            // 3) 代码声明：枚举型反射 / [LovList] 列表型描述符
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

        // 内部实体值集（Entity.{Type}）：entity: 协议在控制器内直接查询实体工厂，不外发 HTTP，也不依赖 DI 代理
        if (!config.RequestUrl.IsNullOrEmpty() && config.RequestUrl.StartsWith("entity:", StringComparison.OrdinalIgnoreCase))
        {
            var fact = ResolveEntityFactoryByUrl(config.RequestUrl);
            if (fact == null)
                throw new InvalidOperationException($"内部实体数据源 {config.RequestUrl["entity:".Length..].Trim()} 未注册");

            // 行权（OSC-260926c2b8）：目标实体菜单 Detail 失败关闭 → 403（空数据不下发）
            if (!LovEntityGuard.CheckMenu(fact))
            {
                Response.StatusCode = 403;
                return new JsonResult(new { code = 403, message = $"无权访问[{fact.EntityType?.Name}]值集数据" });
            }

            var (rows, total) = FetchEntityList(fact, request.Params, request.PageNum, request.PageSize);
            return new
            {
                Data = rows,
                Total = total,
            };
        }

        // 通过 IOC 获取列表数据代理实现（默认 DefaultLovListDataProxy，可被使用者覆盖）
        var proxy = HttpContext.RequestServices.GetRequiredService<ILovListDataProxy>();
        var result = await proxy.FetchAsync(config, request);

        return new
        {
            Data = result.Data,
            Total = result.Total,
        };
    }

    /// <summary>批量翻译。将列表型值集的原始 value 批量翻译为 label</summary>
    /// <param name="request">翻译请求</param>
    /// <returns>value→label 字典</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpPost]
    public async Task<Object> BatchLabel([FromBody] LovBatchLabelRequest request)
    {
        if (request == null || request.LovCode.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(request));

        var result = new Dictionary<String, String>();

        if (request.Values == null || request.Values.Length == 0)
            return result;

        // 枚举型：LovCode 形如 Enum.{Type.FullName}，由 LovRegistry 反射直读（代码优先，不落库）
        if (request.LovCode.StartsWith("Enum.", StringComparison.OrdinalIgnoreCase))
        {
            var enumType = LovRegistry.FindEnumType(request.LovCode[5..]);
            if (enumType == null)
                throw new InvalidOperationException($"值集 {request.LovCode} 不存在");

            var map = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var opt in LovRegistry.GetEnumOptions(enumType))
            {
                map[opt.Value] = opt.Label;
            }

            foreach (var v in request.Values)
            {
                var key = v?.ToString();
                if (!key.IsNullOrEmpty() && map.TryGetValue(key, out var label))
                    result[key] = label;
            }

            return result;
        }

        // 内部实体值集（Entity.{Type}）：按主键集合逐键取数 + 行权判定，不可见键省略（OSC-260926c2b8，不再扫全表防枚举）
        if (ResolveEntityFactory(request.LovCode) is { } entityFact)
        {
            AppendEntityLabels(entityFact, request.Values, result);
            return result;
        }

        // [LovList] 声明式值集（List.*）：由 LovRegistry 反射直读配置，分页权威反查（行为不变）
        var desc = LovRegistry.FindList(request.LovCode);
        if (desc == null)
            throw new InvalidOperationException($"值集 {request.LovCode} 不存在");

        LovListConfigModel config = desc.Config;
        String valueField = desc.ValueField;
        String labelField = desc.LabelField;

        {
            if (config != null && !valueField.IsNullOrEmpty() && !labelField.IsNullOrEmpty())
            {
                var pending = request.Values
                    .Select(v => v?.ToString())
                    .Where(v => !v.IsNullOrEmpty())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(LovLabelQuery.ListLabelValueCap)
                    .ToList();
                var pendingSet = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var v in pending)
                {
                    if (!pendingSet.ContainsKey(v)) pendingSet[v] = v;
                }

                var searchNames = desc.SearchFields?.Select(e => e.Field);
                var plan = LovLabelQuery.PlanListLabelQuery(valueField, searchNames, pending.Count, config.Pageable);
                if (plan.MaxPages > 0 && plan.Mode != "none")
                {
                    Dictionary<String, Object>? extra = null;
                    if (plan.Mode == "keyed")
                    {
                        extra = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
                        {
                            [valueField] = pending.Join(","),
                        };
                    }

                    var pageSize = plan.PageSize;
                    for (var pageNum = 1; pageNum <= plan.MaxPages && pendingSet.Count > 0; pageNum++)
                    {
                        var (rows, _) = await FetchRemoteList(config, extra, pageNum, pageSize);
                        if (rows.Count == 0) break;

                        foreach (var row in rows)
                        {
                            if (pendingSet.Count == 0) break;
                            var v = GetRowValue(row, valueField);
                            if (v == null) continue;
                            var key = v.ToString()!;
                            if (pendingSet.TryGetValue(key, out var original) && !result.ContainsKey(original))
                            {
                                var label = GetRowValue(row, labelField)?.ToString();
                                if (!label.IsNullOrEmpty()) result[original] = label!;
                                pendingSet.Remove(key);
                            }
                        }

                        if (pendingSet.Count == 0 || rows.Count < pageSize) break;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>从行数据中按字段名大小写不敏感取值（兼容远端 camelCase / PascalCase）</summary>
    private static Object? GetRowValue(IDictionary<String, Object> row, String field)
    {
        if (row == null || field.IsNullOrEmpty()) return null;
        if (row.TryGetValue(field, out var v)) return v;
        foreach (var kv in row)
        {
            if (kv.Key.EqualIgnoreCase(field)) return kv.Value;
        }
        return null;
    }

    /// <summary>
    /// 代理远端列表接口，返回解析后的行数据与总数。
    /// </summary>
    private static async Task<(List<Dictionary<String, Object>> Rows, Int32 Total)> FetchRemoteList(
        LovListConfigModel config,
        Dictionary<String, Object>? extraParams,
        Int32 pageNum,
        Int32 pageSize)
    {
        var url = config.RequestUrl;

        if (url.StartsWith("entity:"))
        {
            var fact = ResolveEntityFactoryByUrl(url) ?? throw new InvalidOperationException($"内部实体数据源 {url["entity:".Length..].Trim()} 未注册");

            // 菜单 Detail 失败关闭（OSC-260926c2b8 审查 🟡1）：声明式值集把数据源配成 entity: 时同样受目标实体菜单约束
            if (!LovEntityGuard.CheckMenu(fact)) return ([], 0);

            return FetchEntityList(fact, extraParams, pageNum, pageSize);
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        var method = (config.Method ?? "GET").ToUpper();

        HttpResponseMessage httpResponse;

        if (method == "GET")
        {
            var queryParams = new List<String>();
            if (extraParams != null)
            {
                foreach (var kv in extraParams)
                {
                    if (kv.Value != null)
                        queryParams.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString()!)}");
                }
            }
            if (config.Pageable)
            {
                if (pageNum > 0 && !config.PageNumField.IsNullOrEmpty())
                    queryParams.Add($"{Uri.EscapeDataString(config.PageNumField)}={pageNum}");
                if (pageSize > 0 && !config.PageSizeField.IsNullOrEmpty())
                    queryParams.Add($"{Uri.EscapeDataString(config.PageSizeField)}={pageSize}");
            }
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
            var bodyParams = new Dictionary<String, Object>();
            if (extraParams != null)
            {
                foreach (var kv in extraParams)
                    bodyParams[kv.Key] = kv.Value!;
            }
            if (config.Pageable)
            {
                if (pageNum > 0 && !config.PageNumField.IsNullOrEmpty())
                    bodyParams[config.PageNumField] = pageNum;
                if (pageSize > 0 && !config.PageSizeField.IsNullOrEmpty())
                    bodyParams[config.PageSizeField] = pageSize;
            }
            if (!config.FixedParams.IsNullOrEmpty())
            {
                var fixedParams = JsonParser.Decode(config.FixedParams) as IDictionary<String, Object>;
                if (fixedParams != null)
                {
                    foreach (var kv in fixedParams)
                        bodyParams[kv.Key] = kv.Value!;
                }
            }

            var json = bodyParams.ToJson();
            var content = new StringContent(json, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            httpResponse = await httpClient.PostAsync(url, content);
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"请求外部接口失败：{httpResponse.StatusCode} - {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        JsonElement? dataElement = !config.DataPath.IsNullOrEmpty()
            ? DefaultLovListDataProxy.ResolveJsonPath(root, config.DataPath)
            : root;

        Int32 total = 0;
        if (config.Pageable && !config.TotalPath.IsNullOrEmpty())
        {
            var totalElement = DefaultLovListDataProxy.ResolveJsonPath(root, config.TotalPath);
            if (totalElement.HasValue)
                total = totalElement.Value.GetInt32();
        }

        var rows = new List<Dictionary<String, Object>>();
        if (dataElement.HasValue && dataElement.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in dataElement.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var row = JsonSerializer.Deserialize<Dictionary<String, Object>>(item.GetRawText());
                if (row != null) rows.Add(row);
            }
        }

        return (rows, total);
    }

    /// <summary>内部实体值集查询（entity: 协议，OSC-0016；行权 OSC-260926c2b8）。Q 模糊 + 分页 + 租户/行权表达式，行输出 {值字段: 主键值, 标签字段: ToString}</summary>
    /// <param name="fact">目标实体工厂（调用方已按 entity: 地址解析并完成菜单裁决）</param>
    /// <param name="extraParams">查询参数（含 Q 关键字）</param>
    /// <param name="pageNum">页码，从 1 起</param>
    /// <param name="pageSize">每页条数，缺省 20、上限 500</param>
    /// <returns>行数据列表与总数</returns>
    private static (List<Dictionary<String, Object>> Rows, Int32 Total) FetchEntityList(
        IEntityFactory fact,
        Dictionary<String, Object>? extraParams,
        Int32 pageNum,
        Int32 pageSize)
    {
        // Q 关键字（extraParams 兼容大小写）
        var q = "";
        if (extraParams != null)
        {
            foreach (var kv in extraParams)
            {
                if (kv.Value != null && kv.Key.EqualIgnoreCase("Q")) { q = kv.Value.ToString(); break; }
            }
        }

        // pageNum 从 1 起，上限 100000 防 Int32 分页偏移溢出；pageSize 缺省 20、上限 500
        if (pageNum < 1) pageNum = 1;
        if (pageNum > 100_000) pageNum = 100_000;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 500) pageSize = 500;

        // Q 模糊表达式（等价 Entity{T}.SearchWhereByKeys，保证与后端 Search 语义一致）
        Expression exp = null;
        if (!q.IsNullOrEmpty())
        {
            var method = GetSearchWhereByKeys(fact.EntityType);
            if (method == null)
                XTrace.WriteLine("entity: 值集 {0} 未找到 SearchWhereByKeys 方法，Q 过滤失效", fact.EntityType.Name);
            else
            {
                try
                {
                    exp = method.Invoke(null, [q, null, null]) as Expression;
                }
                catch (TargetInvocationException ex)
                {
                    // 反射包装：解包内部异常并降级为无 Q 过滤，避免整个查询 500
                    XTrace.WriteException(ex.InnerException ?? ex);
                }
            }
        }

        // 行权（OSC-260926c2b8）：关键字条件之后 AND 租户 + DataScope 表达式；菜单 Detail 由调用方先裁决
        var guard = LovEntityGuard.GetFilter(fact);
        if (guard != null) exp = exp == null ? guard : exp & guard;

        var unique = fact.Unique;
        var valueField = unique?.Name ?? "Id";
        var labelField = fact.Master?.Name ?? valueField;
        var order = unique?.Name;

        var startRow = (pageNum - 1) * pageSize;
        var list = fact.FindAll(exp, order, null, startRow, pageSize);
        var total = exp == null ? fact.Session.Count : (Int32)fact.FindCount(exp);

        var rows = new List<Dictionary<String, Object>>();
        foreach (var entity in list)
        {
            var row = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
            {
                [valueField] = entity[valueField],
            };
            // 标签字段与值字段不同名时输出 ToString 显示串；同名时前端回退值字段
            if (!labelField.EqualIgnoreCase(valueField))
                row[labelField] = entity.ToString();
            rows.Add(row);
        }

        return (rows, total);
    }

    /// <summary>SearchWhereByKeys 方法缓存，避免每请求反射（GetMethods 仅执行一次）</summary>
    private static readonly ConcurrentDictionary<Type, MethodInfo> _searchWhereByKeysCache = new();

    /// <summary>获取实体类型对应的 SearchWhereByKeys 静态方法（三参数签名，缓存）</summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>方法信息；不存在返回 null</returns>
    private static MethodInfo? GetSearchWhereByKeys(Type entityType)
    {
        return _searchWhereByKeysCache.GetOrAdd(entityType, static t =>
        {
            var genType = typeof(Entity<>).MakeGenericType(t);
            return genType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "SearchWhereByKeys" && m.GetParameters().Length == 3);
        });
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

    /// <summary>内部实体值集编码前缀。Entity.{Type.FullName} 表示按唯一键/主字段翻译的内部实体值集（代码优先，不落库）</summary>
    private const String EntityPrefix = "Entity.";

    /// <summary>解析内部实体值集对应的实体工厂。lovCode 形如 Entity.{Type.FullName}，兼容短类型名</summary>
    /// <param name="lovCode">值集编码</param>
    /// <returns>实体工厂；非内部实体值集或未注册时返回 null</returns>
    private static IEntityFactory? ResolveEntityFactory(String lovCode)
    {
        if (lovCode.IsNullOrEmpty() || !lovCode.StartsWith(EntityPrefix, StringComparison.OrdinalIgnoreCase)) return null;

        var typeName = lovCode[EntityPrefix.Length..].Trim();
        if (typeName.IsNullOrEmpty()) return null;

        foreach (var fact in EntityFactory.Entities.Values)
        {
            var type = fact.EntityType;
            if (type == null) continue;
            if (type.FullName.EqualIgnoreCase(typeName) || type.Name.EqualIgnoreCase(typeName)) return fact;
        }

        return null;
    }

    /// <summary>解析 entity: 数据源地址中的实体工厂（大小写不敏感，短类型名）</summary>
    /// <param name="url">entity:{EntityTypeName} 数据源地址</param>
    /// <returns>实体工厂；地址为空、缺实体名或未注册时返回 null</returns>
    private static IEntityFactory? ResolveEntityFactoryByUrl(String url)
    {
        if (url.IsNullOrEmpty() || !url.StartsWith("entity:", StringComparison.OrdinalIgnoreCase)) return null;

        var typeName = url["entity:".Length..].Trim();
        if (typeName.IsNullOrEmpty()) return null;

        return EntityFactory.Entities.Values.FirstOrDefault(e => e.EntityType?.Name.EqualIgnoreCase(typeName) == true);
    }

    /// <summary>实体值集按主键集合翻译标签（OSC-260926c2b8）。单次按主键集合取数 + 菜单/行权判定，不可见键省略，防枚举探测</summary>
    /// <param name="fact">目标实体工厂</param>
    /// <param name="values">待翻译的原始值集合</param>
    /// <param name="result">value→label 结果字典（就地追加）</param>
    private static void AppendEntityLabels(IEntityFactory fact, Object[] values, Dictionary<String, String> result)
    {
        // 菜单 Detail 失败关闭：找不到菜单或无权限时全部省略（不 500）
        if (!LovEntityGuard.CheckMenu(fact)) return;

        var unique = fact.Unique;
        if (unique == null) return;

        var valueField = ValueFieldOf(fact);
        var labelField = LabelFieldOf(fact);

        // 去重并限制单次翻译量（与值集分页上限对齐，防构造超大数组触发大量查询）
        var pending = values
            .Select(v => v?.ToString())
            .Where(v => !v.IsNullOrEmpty())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .ToList();
        if (pending.Count == 0) return;

        // 主键类型转换（非法格式的键静默跳过，不整体失败）
        var keys = new List<Object>();
        foreach (var key in pending)
        {
            var value = ConvertKey(key, unique.Type);
            if (value != null) keys.Add(value);
        }
        if (keys.Count == 0) return;

        // 单次查询按主键集合取数，再逐行行权判定（DataScope 归属 + 租户，与列表 FindData 同序；范围外键省略）
        var list = fact.FindAll(unique.In(keys), null, null, 0, 0);
        if (list == null || list.Count == 0) return;

        // 行键 → 请求原始键（保留请求大小写与格式）
        var original = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in pending)
        {
            original[key] = key;
        }

        foreach (var entity in list)
        {
            if (entity == null) continue;
            if (!LovEntityGuard.CanAccess(fact, entity)) continue;

            var key = entity[valueField]?.ToString();
            if (key.IsNullOrEmpty() || !original.TryGetValue(key, out var raw)) continue;

            result[raw] = labelField.EqualIgnoreCase(valueField) ? raw : entity.ToString();
        }
    }

    /// <summary>按主键字段类型转换字符串键；非法格式返回 null（跳过该键）</summary>
    /// <param name="key">请求中的字符串键</param>
    /// <param name="type">主键字段类型</param>
    /// <returns>转换后的键值；无法转换返回 null</returns>
    private static Object? ConvertKey(String key, Type? type)
    {
        if (type == null || type == typeof(String)) return key;

        try
        {
            return Convert.ChangeType(key, type);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>内部实体值集的值字段：唯一键名，缺省 Id</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>字段名</returns>
    private static String ValueFieldOf(IEntityFactory fact) => fact.Unique?.Name ?? "Id";

    /// <summary>内部实体值集的标签字段：主字段名，缺省与值字段相同</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>字段名</returns>
    private static String LabelFieldOf(IEntityFactory fact)
    {
        var value = ValueFieldOf(fact);
        var label = fact.Master?.Name;
        return label.IsNullOrEmpty() ? value : label;
    }

    /// <summary>构建内部实体值集的列表数据源配置（entity: 协议，分页参数 pageNum/pageSize）</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>列表数据源配置</returns>
    private static LovListConfigModel BuildEntityListConfig(IEntityFactory fact) => new()
    {
        RequestUrl = "entity:" + (fact.EntityType?.Name ?? ""),
        Method = "GET",
        Pageable = true,
        PageNumField = "pageNum",
        PageSizeField = "pageSize",
    };

    /// <summary>内部实体值集的搜索字段：Q 关键字（entity: 协议按 Q 做键值模糊）</summary>
    /// <returns>搜索字段列表</returns>
    private static IList<LovSearchFieldModel> BuildEntitySearchFields() =>
    [
        new() { Field = "Q", Title = "关键字", ComponentType = "input", ParamType = "BODY", Sort = 0 },
    ];

    /// <summary>内部实体值集的表格列：值字段 + 标签字段</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>表格列列表</returns>
    private static IList<LovTableColumnModel> BuildEntityColumns(IEntityFactory fact)
    {
        var value = ValueFieldOf(fact);
        var label = LabelFieldOf(fact);
        var list = new List<LovTableColumnModel>
        {
            new() { Field = value, Title = "编号", Sort = 0 },
        };
        if (!label.EqualIgnoreCase(value)) list.Add(new() { Field = label, Title = "名称", Sort = 1 });
        return list;
    }

    /// <summary>解析列表型值集的数据源配置。手工定义（LIST）优先读 Parameter；其次内部实体值集；否则读 [LovList] 描述符</summary>
    private static LovListConfigModel? ResolveListConfig(String lovCode)
    {
        var def = LovStore.FindDef(lovCode);
        if (def != null)
        {
            if (def.Type != "LIST") return null;

            return LovStore.FindListConfig(lovCode);
        }

        // 内部实体值集：代码优先，按实体工厂即时合成 entity: 数据源（不落库）
        if (ResolveEntityFactory(lovCode) is { } fact) return BuildEntityListConfig(fact);

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
