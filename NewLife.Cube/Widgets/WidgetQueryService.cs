using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using NewLife;
using NewLife.Cube.Automation;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Model;

namespace NewLife.Cube.Widgets;

/// <summary>Widget 查询请求</summary>
/// <remarks>
/// 除 Mode/TypePath 外均为可选项，必须声明为可空：项目开启 Nullable 标注且 MVC 默认
/// SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=false，非空引用类型属性会被隐式推断为 [Required]，
/// 而前端 buildQueryBody 会丢弃 undefined 字段（如 count 度量不带 field），导致 POST /Cube/Widget/Query 被模型校验拦下 400。
/// </remarks>
public class WidgetQueryRequest
{
    /// <summary>aggregate | list</summary>
    public String Mode { get; set; } = "aggregate";

    /// <summary>源实体</summary>
    public String TypePath { get; set; }

    /// <summary>度量</summary>
    public WidgetMeasure? Measure { get; set; }

    /// <summary>多统计度量（OSC-260920）；非空时优先于 Measure。最多 5 个</summary>
    public List<WidgetMeasure>? Measures { get; set; }

    /// <summary>排序依据：x=横轴列 / y=首度量（默认） / record=记录顺序（不排序）；仅分组聚合生效</summary>
    public String? SortBy { get; set; }

    /// <summary>排序方向：asc/desc（x 缺省正序，y 缺省倒序）</summary>
    public String? SortOrder { get; set; }

    /// <summary>分组字段</summary>
    public String? GroupBy { get; set; }

    /// <summary>时间字段</summary>
    public String? TimeField { get; set; }

    /// <summary>时间桶数</summary>
    public Int32 Buckets { get; set; } = 12;

    /// <summary>条数上限</summary>
    public Int32 Limit { get; set; } = 30;

    /// <summary>部件自有筛选</summary>
    public ViewFilterDto? ExtraFilter { get; set; }

    /// <summary>宿主 typePath</summary>
    public String? HostTypePath { get; set; }

    /// <summary>宿主筛选</summary>
    public ViewFilterDto? HostFilter { get; set; }

    /// <summary>跨实体字段映射</summary>
    public List<WidgetLinkFilter>? LinkFilter { get; set; }

    /// <summary>宿主字段当前值</summary>
    public Dictionary<String, Object>? HostValues { get; set; }

    /// <summary>拒绝 sql/script/join</summary>
    [JsonExtensionData]
    public Dictionary<String, JsonElement>? Extra { get; set; }
}

/// <summary>度量</summary>
public class WidgetMeasure
{
    /// <summary>count/sum/avg/min/max</summary>
    public String Fn { get; set; } = "count";

    /// <summary>字段。count 度量可空（=记录总数 Count(*)）</summary>
    public String? Field { get; set; }

    /// <summary>序列显示名（图例/提示用；仅透传，不参与 SQL）</summary>
    public String? Label { get; set; }
}

/// <summary>跨实体映射</summary>
public class WidgetLinkFilter
{
    /// <summary>宿主字段</summary>
    public String? HostField { get; set; }

    /// <summary>源字段</summary>
    public String? SourceField { get; set; }
}

/// <summary>查询结果</summary>
public class WidgetQueryResult
{
    /// <summary>主值（无分组）</summary>
    public Object Value { get; set; }

    /// <summary>分组/时间序列</summary>
    public List<WidgetQueryItem> Items { get; set; } = [];

    /// <summary>列表行（list 模式）</summary>
    public List<IDictionary<String, Object>> Rows { get; set; }

    /// <summary>是否应用了宿主筛选</summary>
    public Boolean HostFilterApplied { get; set; }

    /// <summary>时间窗收窄标记（OSC-260830a1b2）。形如 "30d"；未收窄则省略</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("filterNarrowed")]
    public String FilterNarrowed { get; set; }
}

/// <summary>分组项</summary>
public class WidgetQueryItem
{
    /// <summary>键</summary>
    public String Key { get; set; }

    /// <summary>标签</summary>
    public String Label { get; set; }

    /// <summary>值（兼容：首个度量）</summary>
    public Object Value { get; set; }

    /// <summary>多度量值（与请求 measures 顺序对齐，OSC-260920）</summary>
    public List<Object> Values { get; set; }
}

/// <summary>带鉴权的只读聚合/列表查询</summary>
public static class WidgetQueryService
{
    /// <summary>执行查询。无权限抛 ApiException 403；非法参数 400。</summary>
    public static WidgetQueryResult Execute(IUser user, WidgetQueryRequest? req)
    {
        if (req == null) throw new ApiException(400, "body 不能为空");
        RejectForbiddenKeys(req);
        var typePath = AutomationPaths.NormalizeTypePath(req.TypePath);
        if (typePath.IsNullOrEmpty()) throw new ApiException(400, "typePath 不能为空");
        if (user == null) throw new ApiException(401, "未授权");
        if (!AutomationAuth.HasPermission(user, typePath, PermissionFlags.Detail))
            throw new ApiException(403, "无权查看");

        var entityType = FindEntityType(typePath);
        if (entityType == null) throw new ApiException(400, "未知实体");
        var fact = EntityFactory.CreateFactory(entityType);
        if (fact == null) throw new ApiException(400, "未知实体");

        var ctrlType = FindControllerType(entityType);
        if (ctrlType == null) throw new ApiException(403, "无法解析实体控制器");

        var hostApplied = false;
        var where = BuildWhere(user, fact, ctrlType, entityType, req, typePath, ref hostApplied);
        var mode = (req.Mode + "").Trim().ToLowerInvariant();
        if (mode.IsNullOrEmpty()) mode = "aggregate";

        // 时间窗（OSC-260830a1b2）：分表/日志实体无时间条件时收窄近 N 天。宿主同源且已含时间条件时不注入
        var filterNarrowed = "";
        var hostPath = AutomationPaths.NormalizeTypePath(req.HostTypePath);
        if (AutomationFilter.TryGetTimeWindow(fact, req.ExtraFilter, out var timeFi, out var narrowedDays) &&
            !(hostPath.EqualIgnoreCase(typePath) && AutomationFilter.HasTimeCondition(req.HostFilter, timeFi.Name)))
        {
            var win = timeFi >= DateTime.Now.Date.AddDays(-narrowedDays);
            where = where == null ? win : where & win;
            filterNarrowed = $"{narrowedDays}d";
        }
        // limit=-1：全部（XCode maximumRows=0 表示不截断）；其余默认 30、上限 300
        Int32 limit;
        Int64 fetchRows;
        if (req.Limit == -1)
        {
            limit = -1;
            fetchRows = 0;
        }
        else
        {
            limit = req.Limit <= 0 ? 30 : req.Limit;
            if (limit > 300) limit = 300;
            fetchRows = limit;
        }

        if (mode == "list")
        {
            var list = fact.FindAll(where, null, null, 0, fetchRows);
            var project = ListProjectionFields(fact);
            var rows = new List<IDictionary<String, Object>>();
            foreach (var e in list)
            {
                var map = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
                foreach (var fi in project)
                    map[fi.Name] = e[fi.Name];
                rows.Add(map);
            }
            return new WidgetQueryResult { Rows = rows, HostFilterApplied = hostApplied, FilterNarrowed = filterNarrowed };
        }

        if (!req.GroupBy.IsNullOrEmpty())
            // 聚合分组仍封顶；「全部」按上限 300 参与分组
            return GroupAggregate(fact, where, req, limit == -1 ? 300 : limit, hostApplied, filterNarrowed);
        if (!req.TimeField.IsNullOrEmpty())
            return TimeBucketAggregate(fact, where, req, hostApplied, filterNarrowed);

        // 无分组：仅支持单值，取首个度量（多度量由前端 miniChart 使用）
        var measures = ResolveMeasures(fact, req);
        var value = ScalarAggregate(fact, where, measures[0]);
        return new WidgetQueryResult { Value = value, HostFilterApplied = hostApplied, FilterNarrowed = filterNarrowed };
    }

    static void RejectForbiddenKeys(WidgetQueryRequest req)
    {
        if (req.Extra == null) return;
        foreach (var key in req.Extra.Keys)
        {
            if (key.EqualIgnoreCase("sql", "script", "join"))
                throw new ApiException(400, "禁止 SQL/脚本/JOIN");
        }
    }

    /// <summary>按 typePath 反查已注册实体类型（DashboardJson 校验与查询共用）</summary>
    internal static Type FindEntityType(String typePath)
    {
        foreach (var kv in EntityPageRegistry.GetAll())
        {
            var url = AutomationPaths.NormalizeTypePath(kv.Value?.Url);
            if (url.EqualIgnoreCase(typePath)) return kv.Key;
        }
        return null;
    }

    static Type FindControllerType(Type entityType)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }
            foreach (var t in types)
            {
                if (t.IsAbstract) continue;
                var bt = t;
                while (bt != null && bt != typeof(Object))
                {
                    if (bt.IsGenericType)
                    {
                        var args = bt.GetGenericArguments();
                        if (args.Length >= 1 && args[0] == entityType)
                        {
                            var name = bt.GetGenericTypeDefinition().Name;
                            if (name.StartsWith("ReadOnlyEntityController") || name.StartsWith("EntityController"))
                                return t;
                        }
                    }
                    bt = bt.BaseType;
                }
            }
        }
        return null;
    }

    static Expression BuildWhere(IUser user, IEntityFactory fact, Type ctrlType, Type entityType, WidgetQueryRequest req, String typePath, ref Boolean hostApplied)
    {
        Expression exp = null;
        var allowed = BuildAllowedField(fact);

        var att = ctrlType.GetCustomAttribute<DataPermissionAttribute>(true);
        if (att != null && (user.Roles == null || !user.Roles.Any(e => e.IsSystem) && !att.Valid(user.Roles)))
        {
            var builder = new WhereBuilder { Factory = fact, Expression = att.Expression };
            try { exp = builder.GetExpression(); }
            catch { throw new ApiException(400, "数据权限表达式无法解析"); }
        }

        // 行权（OSC-2608273d95）：聚合/列表与页面列表同一助手合并 DataScope 四档；
        // 上下文由当前用户显式构造，不写 DataScopeContext.Current（实体层拦截器继续休眠）
        var scopeCtx = DataScopeContext.Create(user);
        var scope = DataScopeHelper.GetFilter(fact, scopeCtx);
        if (scope != null) exp = exp == null ? scope : exp & scope;

        // 与 ReadOnlyEntityController2.CreateWhere 同等：无上下文 fail-closed；租户模式 AND TenantId；管理后台不加
        if (CubeSetting.Current.EnableTenant && typeof(ITenantScope).IsAssignableFrom(entityType))
        {
            var ctx = TenantContext.Current;
            var mode = ctx.GetTenantMode();
            if (mode == TenantMode.None)
            {
                if (CubeSetting.Current.TenantEnforceMode != TenantEnforceModes.Shadow)
                {
                    var uk = fact.Unique;
                    if (uk != null)
                    {
                        var closed = uk.Equal(-1);
                        exp = exp == null ? closed : exp & closed;
                    }
                }
            }
            else if (mode == TenantMode.Tenant)
            {
                var tenant = ctx.Tenant ?? Tenant.FindById(ctx.TenantId);
                if (tenant == null || !tenant.Enable)
                {
                    var uk = fact.Unique;
                    if (uk != null)
                    {
                        var closed = uk.Equal(-1);
                        exp = exp == null ? closed : exp & closed;
                    }
                }
                else
                {
                    FieldItem tenantFi;
                    if (entityType == typeof(Tenant))
                        tenantFi = fact.Table.FindByName("Id") ?? fact.Unique;
                    else
                        tenantFi = fact.Table.FindByName("TenantId");
                    if (tenantFi != null)
                    {
                        var te = tenantFi.Equal(tenant.Id);
                        exp = exp == null ? te : exp & te;
                    }
                }
            }
            // AdminBackend：不加租户过滤
        }

        var hostPath = AutomationPaths.NormalizeTypePath(req.HostTypePath);

        // 部件查询条件（OSC-260903e2a4）：先解析 $host 宿主引用（取宿主筛选等值条件值），非法对象值 400
        var extraFilter = req.ExtraFilter;
        var hostRefApplied = false;
        if (HasConditions(extraFilter))
            extraFilter = ResolveHostRefs(extraFilter, req.HostFilter, ref hostRefApplied);

        if (HasConditions(extraFilter))
        {
            var extra = AutomationFilter.TryBuildWhere(fact, extraFilter, allowed);
            if (extra == null) throw new ApiException(400, "筛选无法下推");
            exp = exp == null ? extra : exp & extra;
        }

        if (HasConditions(req.HostFilter))
        {
            if (hostPath.EqualIgnoreCase(typePath))
            {
                var hostExp = AutomationFilter.TryBuildWhere(fact, req.HostFilter, allowed);
                if (hostExp == null) throw new ApiException(400, "筛选无法下推");
                exp = exp == null ? hostExp : exp & hostExp;
                hostApplied = true;
            }
            else if (req.LinkFilter != null && req.LinkFilter.Count > 0)
            {
                var mapped = false;
                foreach (var link in req.LinkFilter)
                {
                    if (link == null) continue;
                    if (String.IsNullOrEmpty(link.HostField) || String.IsNullOrEmpty(link.SourceField)) continue;
                    var srcFi = fact.Table.FindByName(link.SourceField);
                    if (srcFi is null) throw new ApiException(400, $"未知源字段 {link.SourceField}");
                    Object raw = null;
                    if (req.HostValues != null)
                    {
                        if (!req.HostValues.TryGetValue(link.HostField, out raw))
                        {
                            foreach (var kv in req.HostValues)
                            {
                                if (String.Equals(kv.Key, link.HostField, StringComparison.OrdinalIgnoreCase)) { raw = kv.Value; break; }
                            }
                        }
                    }
                    if (raw == null) continue;
                    try
                    {
                        var eq = srcFi.Equal(raw);
                        exp = exp == null ? eq : exp & eq;
                        mapped = true;
                    }
                    catch
                    {
                        throw new ApiException(400, "跨实体字段类型不兼容");
                    }
                }
                // $host 解析成功也视为已应用宿主上下文（前端据此隐藏「未联动」角标）
                hostApplied = mapped || hostRefApplied;
            }
            else if (hostRefApplied)
            {
                hostApplied = true;
            }
        }
        else if (hostRefApplied)
        {
            // 宿主筛选为空但 extraFilter 有 $host 解析成功（理论不达，防御）
            hostApplied = true;
        }

        return exp;
    }

    /// <summary>解析部件查询条件中的 $host 宿主引用（OSC-260903e2a4）。值对象仅允许 {"$host": 宿主字段}，其余对象值 400；解析源=宿主筛选等值条件。</summary>
    /// <param name="filter">原始部件查询条件</param>
    /// <param name="hostFilter">宿主页当前筛选</param>
    /// <param name="applied">是否有 $host 成功解析</param>
    /// <returns>解析后的条件（克隆，不改原始 DTO，保证 Execute 时间窗判定沿用原始 ExtraFilter）</returns>
    static ViewFilterDto ResolveHostRefs(ViewFilterDto filter, ViewFilterDto hostFilter, ref Boolean applied)
    {
        var changed = false;
        var list = new List<ViewFilterConditionDto>();
        foreach (var c in filter.Conditions)
        {
            if (c == null) continue;
            var field = GetHostRefField(c.Value);
            if (field == "")
                throw new ApiException(400, "筛选值无效");
            if (field != null)
            {
                changed = true;
                var v = FindHostValue(hostFilter, field);
                if (v != null)
                {
                    list.Add(new ViewFilterConditionDto { Field = c.Field, Op = c.Op, Value = v });
                    applied = true;
                }
                continue;
            }
            list.Add(c);
        }
        if (!changed) return filter;
        return new ViewFilterDto { Logic = filter.Logic, Conditions = list };
    }

    /// <summary>识别 $host 宿主引用。null=字面量；""=对象但非法（非 $host 单键）；否则为宿主字段名。</summary>
    static String GetHostRefField(Object value)
    {
        if (value is JsonElement je)
        {
            if (je.ValueKind != JsonValueKind.Object) return null;
            String field = null;
            foreach (var p in je.EnumerateObject())
            {
                if (!p.Name.Equals("$host", StringComparison.Ordinal)) return "";
                if (p.Value.ValueKind != JsonValueKind.String) return "";
                field = p.Value.GetString();
            }
            if (field.IsNullOrWhiteSpace()) return "";
            return field.Trim();
        }
        if (value is JsonObject jo)
        {
            String field = null;
            foreach (var kv in jo)
            {
                if (!String.Equals(kv.Key, "$host", StringComparison.Ordinal)) return "";
                if (kv.Value is not JsonValue jv || !jv.TryGetValue<String>(out var s)) return "";
                field = s;
            }
            if (field.IsNullOrWhiteSpace()) return "";
            return field.Trim();
        }
        return null;
    }

    /// <summary>在宿主筛选中取某字段等值条件值；无则返回 null</summary>
    static Object FindHostValue(ViewFilterDto host, String field)
    {
        if (host?.Conditions == null) return null;
        foreach (var c in host.Conditions)
        {
            if (c == null || !String.Equals(c.Field, field, StringComparison.OrdinalIgnoreCase)) continue;
            var op = (c.Op + "").Trim().ToLowerInvariant();
            if (op.Length != 0 && op != "eq") continue;
            var v = c.Value;
            if (v is JsonElement jv && (jv.ValueKind == JsonValueKind.Null ||
                (jv.ValueKind == JsonValueKind.String && jv.GetString().IsNullOrEmpty()))) continue;
            if (v is String s && s.IsNullOrEmpty()) continue;
            if (v == null) continue;
            return v;
        }
        return null;
    }

    static Boolean HasConditions(ViewFilterDto filter) =>
        filter?.Conditions != null && filter.Conditions.Count > 0;

    /// <summary>取实体 search∪list 字段名白名单（DashboardJson 保存校验与查询端共用）。OSC-260903e2a4</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>白名单字段名集合</returns>
    internal static HashSet<String> GetAllowedNames(IEntityFactory fact)
    {
        var names = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in new FieldCollection(fact, ViewKinds.Search)) names.Add(f.Name);
        foreach (var f in new FieldCollection(fact, ViewKinds.List)) names.Add(f.Name);
        return names;
    }

    /// <summary>构造 viewFilter 字段白名单（GetPage search∪list 的 name）。OSC-260830a1b2：与 GetPage 一致的 FieldCollection 默认 ShowIn，禁止 AllFields</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>白名单判定委托</returns>
    static Func<String, Boolean> BuildAllowedField(IEntityFactory fact)
    {
        var names = GetAllowedNames(fact);
        return n => names.Contains(n);
    }

    static Object ScalarAggregate(IEntityFactory fact, Expression where, WidgetMeasure measure)
    {
        var fn = (measure?.Fn + "").Trim().ToLowerInvariant();
        if (fn.IsNullOrEmpty() || fn == "count")
            return fact.FindCount(where);

        var field = measure?.Field;
        var fi = ResolveNumeric(fact, field);
        var selects = fn switch
        {
            "sum" => fi.Sum(),
            "avg" => fi.Avg(),
            "min" => fi.Min(),
            "max" => fi.Max(),
            _ => throw new ApiException(400, "非法度量"),
        };
        var row = fact.FindAll(where, null, selects, 0, 1).FirstOrDefault();
        return row?[fi.Name];
    }

    static WidgetQueryResult GroupAggregate(IEntityFactory fact, Expression where, WidgetQueryRequest req, Int32 limit, Boolean hostApplied, String filterNarrowed = null)
    {
        var groupFi = fact.Table.FindByName(req.GroupBy)
            ?? fact.Fields.FirstOrDefault(f => f.Name.EqualIgnoreCase(req.GroupBy));
        if (groupFi is null) throw new ApiException(400, "未知分组字段");
        var top = Math.Min(limit, 20);

        // 多度量（OSC-260920）：m0..mN 与 measures 对齐；首度量同时写入 Value 兼容旧前端
        var measures = ResolveMeasures(fact, req);
        var (aggCols, aliases) = BuildAggregates(fact, measures);

        var sb = new SelectBuilder
        {
            Table = fact.Table.TableName,
            Column = $"{groupFi.ColumnName} as {groupFi.Name}, {aggCols}",
            Where = where + "",
            GroupBy = groupFi.ColumnName,
            OrderBy = BuildGroupOrderBy(req, groupFi),
        };
        var dt = fact.Session.Query(sb, 0, top);
        var items = new List<WidgetQueryItem>();
        IDictionary<Object, String> mapSource = null;
        try { mapSource = groupFi.Map?.Provider?.GetDataSource(); } catch { /* ignore */ }
        if (dt != null)
        {
            foreach (var row in dt)
            {
                var key = row[groupFi.Name] + "";
                var values = ReadRowValues(row, aliases);
                items.Add(new WidgetQueryItem
                {
                    Key = key,
                    Label = FormatGroupLabel(groupFi, key, mapSource),
                    Value = values.Count > 0 ? values[0] : null,
                    Values = values,
                });
            }
        }
        return new WidgetQueryResult { Items = items, HostFilterApplied = hostApplied, FilterNarrowed = filterNarrowed };
    }

    /// <summary>按度量别名（m0..mN）从数据行取值，缺列容错为 null（OSC-260920）</summary>
    static List<Object> ReadRowValues(DbRow row, List<String> aliases)
    {
        var values = new List<Object>(aliases.Count);
        foreach (var alias in aliases)
        {
            Object val = null;
            try { val = row[alias]; } catch { /* ignore */ }
            values.Add(val);
        }
        return values;
    }

    /// <summary>分组聚合排序（OSC-260920）：x=横轴列（缺省正序）/ y=首度量（缺省倒序）/ record=记录顺序（不排序）</summary>
    /// <param name="req">查询请求</param>
    /// <param name="groupFi">分组字段</param>
    /// <returns>ORDER BY 片段；null 表示不排序</returns>
    static String BuildGroupOrderBy(WidgetQueryRequest req, FieldItem groupFi)
    {
        var by = (req.SortBy + "").Trim().ToLowerInvariant();
        var order = (req.SortOrder + "").Trim().ToLowerInvariant();
        if (by == "record") return null;
        if (by == "x")
        {
            var xAsc = order != "desc";
            return $"{groupFi.ColumnName} {(xAsc ? "asc" : "desc")}";
        }
        var yAsc = order == "asc";
        return $"m0 {(yAsc ? "asc" : "desc")}";
    }

    /// <summary>分组键 → 友好显示名（枚举 / Boolean / Map 数据源）</summary>
    public static String FormatGroupLabel(FieldItem fi, String key, IDictionary<Object, String> mapSource = null)
    {
        if (key.IsNullOrEmpty() || key.EqualIgnoreCase("null")) return "(空)";
        var type = fi?.Type;
        if (type != null)
        {
            var ut = Nullable.GetUnderlyingType(type) ?? type;
            if (ut.IsEnum)
            {
                try
                {
                    Object ev;
                    if (Int64.TryParse(key, out var n))
                        ev = Enum.ToObject(ut, n);
                    else
                        ev = Enum.Parse(ut, key, true);
                    var en = (Enum)ev;
                    var desc = en.GetDescription();
                    if (!desc.IsNullOrEmpty()) return desc;
                    return en.ToString();
                }
                catch { /* fall through */ }
            }
            if (ut == typeof(Boolean))
            {
                if (key.EqualIgnoreCase("true", "1", "yes")) return "是";
                if (key.EqualIgnoreCase("false", "0", "no")) return "否";
            }
        }
        if (mapSource != null)
        {
            foreach (var kv in mapSource)
            {
                if (kv.Key == null) continue;
                if (String.Equals(kv.Key + "", key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value.IsNullOrEmpty() ? key : kv.Value;
            }
        }
        return key;
    }

    static FieldItem ResolveNumeric(IEntityFactory fact, String field)
    {
        if (field.IsNullOrEmpty()) throw new ApiException(400, "度量字段不能为空");
        var fi = FindField(fact, field);
        if (fi is null) throw new ApiException(400, "未知度量字段");
        if (fi.PrimaryKey || fi.IsIdentity) throw new ApiException(400, "非法度量字段");
        if (fi.Name.EqualIgnoreCase("Password", "Secret", "Salt")) throw new ApiException(400, "非法度量字段");
        if (!IsNumericType(fi.Type)) throw new ApiException(400, "度量字段必须为数值");
        return fi;
    }

    /// <summary>字段名/列名查找（忽略大小写）</summary>
    /// <param name="fact">实体工厂</param>
    /// <param name="name">字段名</param>
    /// <returns>字段项；未找到返回 null</returns>
    internal static FieldItem FindField(IEntityFactory fact, String name)
    {
        if (name.IsNullOrEmpty()) return null;
        return fact.Table.FindByName(name) ?? fact.Fields.FirstOrDefault(f => f.Name.EqualIgnoreCase(name));
    }

    /// <summary>数值类型判定（与前端数值字段候选一致，OSC-260920）</summary>
    /// <param name="type">字段类型（含可空）</param>
    /// <returns>是否可作为 sum/avg/min/max 度量</returns>
    internal static Boolean IsNumericType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(Int16) || t == typeof(Int32) || t == typeof(Int64)
            || t == typeof(Single) || t == typeof(Double) || t == typeof(Decimal)
            || t == typeof(Byte) || t == typeof(SByte)
            || t == typeof(UInt16) || t == typeof(UInt32) || t == typeof(UInt64);
    }

    /// <summary>度量函数白名单</summary>
    internal static readonly HashSet<String> MeasureFns = new(StringComparer.OrdinalIgnoreCase)
    {
        "count", "sum", "avg", "min", "max"
    };

    /// <summary>单部件度量上限（OSC-260920）</summary>
    public const Int32 MaxMeasures = 5;

    /// <summary>校验单个度量（函数白名单 + 字段存在/类型/敏感）。返回错误文案，null=通过。查询与保存校验共用（OSC-260920）</summary>
    /// <param name="fact">实体工厂</param>
    /// <param name="fn">度量函数</param>
    /// <param name="field">字段名；count 可空（=Count(*)）</param>
    /// <returns>错误文案；null 表示合法</returns>
    internal static String CheckMeasure(IEntityFactory fact, String fn, String field)
    {
        fn = (fn + "").Trim().ToLowerInvariant();
        if (!MeasureFns.Contains(fn)) return "非法度量";
        if (fn == "count")
        {
            // count 可无字段（Count(*)）；带字段时统计该字段非空值，任意标量字段均可
            if (field.IsNullOrEmpty()) return null;
            var cfi = FindField(fact, field);
            if (cfi is null) return "未知度量字段";
            if (cfi.Name.EqualIgnoreCase("Password", "Secret", "Salt")) return "非法度量字段";
            var ct = Nullable.GetUnderlyingType(cfi.Type) ?? cfi.Type;
            if (ct == typeof(Byte[])) return "非法度量字段";
            return null;
        }
        if (field.IsNullOrEmpty()) return "度量字段不能为空";
        var fi = FindField(fact, field);
        if (fi is null) return "未知度量字段";
        if (fi.PrimaryKey || fi.IsIdentity) return "非法度量字段";
        if (fi.Name.EqualIgnoreCase("Password", "Secret", "Salt")) return "非法度量字段";
        if (!IsNumericType(fi.Type)) return "度量字段必须为数值";
        return null;
    }

    /// <summary>解析有效度量：Measures 优先，其次 Measure，缺省 Count(*)；数量与字段均校验（OSC-260920）</summary>
    /// <param name="fact">实体工厂</param>
    /// <param name="req">查询请求</param>
    /// <returns>已归一化（小写 fn）的度量列表</returns>
    static List<WidgetMeasure> ResolveMeasures(IEntityFactory fact, WidgetQueryRequest req)
    {
        var list = new List<WidgetMeasure>();
        if (req.Measures != null && req.Measures.Count > 0)
            list.AddRange(req.Measures.Where(e => e != null));
        else if (req.Measure != null)
            list.Add(req.Measure);
        if (list.Count == 0) list.Add(new WidgetMeasure { Fn = "count" });
        if (list.Count > MaxMeasures) throw new ApiException(400, $"度量不能超过 {MaxMeasures} 个");

        foreach (var m in list)
        {
            var fn = (m.Fn + "").Trim().ToLowerInvariant();
            var err = CheckMeasure(fact, fn, m.Field);
            if (err != null) throw new ApiException(400, err);
            m.Fn = fn;
            if (m.Label != null && m.Label.Length > 40) m.Label = m.Label[..40];
        }
        return list;
    }

    /// <summary>构造聚合 SELECT 列（别名 m0..mN 与度量对齐，OSC-260920）</summary>
    /// <param name="fact">实体工厂</param>
    /// <param name="measures">度量列表</param>
    /// <returns>聚合列 SQL 与别名列表</returns>
    static (String Columns, List<String> Aliases) BuildAggregates(IEntityFactory fact, List<WidgetMeasure> measures)
    {
        var cols = new List<String>();
        var aliases = new List<String>();
        for (var i = 0; i < measures.Count; i++)
        {
            var m = measures[i];
            var alias = "m" + i;
            String col;
            if (m.Fn == "count")
                col = m.Field.IsNullOrEmpty() ? $"Count(*) as {alias}" : $"{FindField(fact, m.Field).Count(alias)}";
            else
            {
                var fi = ResolveNumeric(fact, m.Field);
                col = m.Fn switch
                {
                    "sum" => $"{fi.Sum(alias)}",
                    "avg" => $"{fi.Avg(alias)}",
                    "min" => $"{fi.Min(alias)}",
                    "max" => $"{fi.Max(alias)}",
                    _ => throw new ApiException(400, "非法度量"),
                };
            }
            cols.Add(col);
            aliases.Add(alias);
        }
        return (String.Join(", ", cols), aliases);
    }

    /// <summary>list 投影：主键 + 非敏感标量（排除 Password/Secret/Salt/二进制）</summary>
    static List<FieldItem> ListProjectionFields(IEntityFactory fact)
    {
        var list = new List<FieldItem>();
        foreach (var fi in fact.Fields)
        {
            if (fi == null) continue;
            if (fi.Name.EqualIgnoreCase("Password", "Secret", "Salt")) continue;
            var t = Nullable.GetUnderlyingType(fi.Type) ?? fi.Type;
            if (t == typeof(Byte[]) || t == typeof(Stream)) continue;
            if (fi.PrimaryKey || fi.IsIdentity || IsScalarListField(t))
                list.Add(fi);
        }
        return list;
    }

    static Boolean IsScalarListField(Type t) =>
        t == typeof(String) || t == typeof(Boolean) || t == typeof(DateTime) || t == typeof(Guid)
        || t == typeof(Int16) || t == typeof(Int32) || t == typeof(Int64)
        || t == typeof(Single) || t == typeof(Double) || t == typeof(Decimal)
        || t.IsEnum;

    /// <summary>按日历日分桶；方言不支持 → 400</summary>
    static WidgetQueryResult TimeBucketAggregate(IEntityFactory fact, Expression where, WidgetQueryRequest req, Boolean hostApplied, String filterNarrowed = null)
    {
        var timeFi = fact.Table.FindByName(req.TimeField)
            ?? fact.Fields.FirstOrDefault(f => f.Name.EqualIgnoreCase(req.TimeField));
        if (timeFi is null) throw new ApiException(400, "未知时间字段");
        var tt = Nullable.GetUnderlyingType(timeFi.Type) ?? timeFi.Type;
        if (tt != typeof(DateTime)) throw new ApiException(400, "时间字段必须为日期时间");

        var buckets = req.Buckets <= 0 ? 12 : req.Buckets;
        if (buckets > 24) buckets = 24;
        var start = DateTime.Today.AddDays(1 - buckets);
        var range = timeFi >= start;
        where = where == null ? range : where & range;

        var dbType = fact.Session.Dal.DbType;
        var dayExpr = DateBucketSql(dbType, timeFi.ColumnName);
        if (dayExpr.IsNullOrEmpty()) throw new ApiException(400, "不支持时间分桶");

        // 多度量（OSC-260920）：时间桶恒定按 Bucket 正序
        var measures = ResolveMeasures(fact, req);
        var (aggCols, aliases) = BuildAggregates(fact, measures);

        var sb = new SelectBuilder
        {
            Table = fact.Table.TableName,
            Column = $"{dayExpr} as Bucket, {aggCols}",
            Where = where + "",
            GroupBy = dayExpr,
            OrderBy = "Bucket asc",
        };
        var dt = fact.Session.Query(sb, 0, buckets);
        var items = new List<WidgetQueryItem>();
        if (dt != null)
        {
            foreach (var row in dt)
            {
                var key = row["Bucket"] + "";
                var values = ReadRowValues(row, aliases);
                items.Add(new WidgetQueryItem { Key = key, Label = key, Value = values.Count > 0 ? values[0] : null, Values = values });
            }
        }
        return new WidgetQueryResult { Items = items, HostFilterApplied = hostApplied, FilterNarrowed = filterNarrowed };
    }

    /// <summary>日历日截断 SQL；不支持返回 null</summary>
    public static String DateBucketSql(DatabaseType dbType, String columnName)
    {
        if (columnName.IsNullOrEmpty()) return null;
        return dbType switch
        {
            DatabaseType.SQLite => $"strftime('%Y-%m-%d', {columnName})",
            DatabaseType.MySql => $"DATE_FORMAT({columnName}, '%Y-%m-%d')",
            DatabaseType.SqlServer or DatabaseType.SqlCe => $"CONVERT(varchar(10), {columnName}, 23)",
            DatabaseType.PostgreSQL or DatabaseType.KingBase or DatabaseType.HighGo => $"to_char({columnName}, 'YYYY-MM-DD')",
            _ => null,
        };
    }
}
