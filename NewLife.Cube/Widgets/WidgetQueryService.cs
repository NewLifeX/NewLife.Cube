using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewLife;
using NewLife.Cube.Automation;
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
public class WidgetQueryRequest
{
    /// <summary>aggregate | list</summary>
    public String Mode { get; set; } = "aggregate";

    /// <summary>源实体</summary>
    public String TypePath { get; set; }

    /// <summary>度量</summary>
    public WidgetMeasure Measure { get; set; }

    /// <summary>分组字段</summary>
    public String GroupBy { get; set; }

    /// <summary>时间字段</summary>
    public String TimeField { get; set; }

    /// <summary>时间桶数</summary>
    public Int32 Buckets { get; set; } = 12;

    /// <summary>条数上限</summary>
    public Int32 Limit { get; set; } = 30;

    /// <summary>部件自有筛选</summary>
    public ViewFilterDto ExtraFilter { get; set; }

    /// <summary>宿主 typePath</summary>
    public String HostTypePath { get; set; }

    /// <summary>宿主筛选</summary>
    public ViewFilterDto HostFilter { get; set; }

    /// <summary>跨实体字段映射</summary>
    public List<WidgetLinkFilter> LinkFilter { get; set; }

    /// <summary>宿主字段当前值</summary>
    public Dictionary<String, Object> HostValues { get; set; }

    /// <summary>拒绝 sql/script/join</summary>
    [JsonExtensionData]
    public Dictionary<String, JsonElement> Extra { get; set; }
}

/// <summary>度量</summary>
public class WidgetMeasure
{
    /// <summary>count/sum/avg/min/max</summary>
    public String Fn { get; set; } = "count";

    /// <summary>字段</summary>
    public String Field { get; set; }
}

/// <summary>跨实体映射</summary>
public class WidgetLinkFilter
{
    /// <summary>宿主字段</summary>
    public String HostField { get; set; }

    /// <summary>源字段</summary>
    public String SourceField { get; set; }
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
}

/// <summary>分组项</summary>
public class WidgetQueryItem
{
    /// <summary>键</summary>
    public String Key { get; set; }

    /// <summary>标签</summary>
    public String Label { get; set; }

    /// <summary>值</summary>
    public Object Value { get; set; }
}

/// <summary>带鉴权的只读聚合/列表查询</summary>
public static class WidgetQueryService
{
    /// <summary>执行查询。无权限抛 ApiException 403；非法参数 400。</summary>
    public static WidgetQueryResult Execute(IUser user, WidgetQueryRequest req)
    {
        if (req == null) throw new ApiException(400, "body 不能为空");
        RejectForbiddenKeys(req);
        var typePath = AutomationPaths.NormalizeTypePath(req.TypePath);
        if (typePath.IsNullOrEmpty()) throw new ApiException(400, "typePath 不能为空");
        if (user == null) throw new ApiException(401, "未授权");
        if (!AutomationAuth.HasPermission(user, typePath, PermissionFlags.Detail))
            throw new ApiException(403, "无权查看");

        var entityType = ResolveEntityType(typePath);
        if (entityType == null) throw new ApiException(400, "未知实体");
        var fact = EntityFactory.CreateFactory(entityType);
        if (fact == null) throw new ApiException(400, "未知实体");

        var ctrlType = FindControllerType(entityType);
        if (ctrlType == null) throw new ApiException(403, "无法解析实体控制器");

        var hostApplied = false;
        var where = BuildWhere(user, fact, ctrlType, entityType, req, typePath, ref hostApplied);
        var mode = (req.Mode + "").Trim().ToLowerInvariant();
        if (mode.IsNullOrEmpty()) mode = "aggregate";
        var limit = req.Limit <= 0 ? 30 : req.Limit;
        if (limit > 50) limit = 50;

        if (mode == "list")
        {
            var list = fact.FindAll(where, null, null, 0, limit);
            var project = ListProjectionFields(fact);
            var rows = new List<IDictionary<String, Object>>();
            foreach (var e in list)
            {
                var map = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
                foreach (var fi in project)
                    map[fi.Name] = e[fi.Name];
                rows.Add(map);
            }
            return new WidgetQueryResult { Rows = rows, HostFilterApplied = hostApplied };
        }

        if (!req.GroupBy.IsNullOrEmpty())
            return GroupAggregate(fact, where, req, limit, hostApplied);
        if (!req.TimeField.IsNullOrEmpty())
            return TimeBucketAggregate(fact, where, req, hostApplied);

        var value = ScalarAggregate(fact, where, req.Measure);
        return new WidgetQueryResult { Value = value, HostFilterApplied = hostApplied };
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

    static Type ResolveEntityType(String typePath)
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

        var att = ctrlType.GetCustomAttribute<DataPermissionAttribute>(true);
        if (att != null && (user.Roles == null || !user.Roles.Any(e => e.IsSystem) && !att.Valid(user.Roles)))
        {
            var builder = new WhereBuilder { Factory = fact, Expression = att.Expression };
            try { exp = builder.GetExpression(); }
            catch { throw new ApiException(400, "数据权限表达式无法解析"); }
        }

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

        if (HasConditions(req.ExtraFilter))
        {
            var extra = AutomationFilter.TryBuildWhere(fact, req.ExtraFilter);
            if (extra == null) throw new ApiException(400, "筛选无法下推");
            exp = exp == null ? extra : exp & extra;
        }

        var hostPath = AutomationPaths.NormalizeTypePath(req.HostTypePath);
        if (HasConditions(req.HostFilter))
        {
            if (hostPath.EqualIgnoreCase(typePath))
            {
                var hostExp = AutomationFilter.TryBuildWhere(fact, req.HostFilter);
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
                hostApplied = mapped;
            }
        }

        return exp;
    }

    static Boolean HasConditions(ViewFilterDto filter) =>
        filter?.Conditions != null && filter.Conditions.Count > 0;

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

    static WidgetQueryResult GroupAggregate(IEntityFactory fact, Expression where, WidgetQueryRequest req, Int32 limit, Boolean hostApplied)
    {
        var groupFi = fact.Table.FindByName(req.GroupBy)
            ?? fact.Fields.FirstOrDefault(f => f.Name.EqualIgnoreCase(req.GroupBy));
        if (groupFi is null) throw new ApiException(400, "未知分组字段");
        var top = Math.Min(limit, 20);
        var fn = (req.Measure?.Fn + "").Trim().ToLowerInvariant();
        if (fn.IsNullOrEmpty()) fn = "count";
        String agg;
        if (fn == "count")
            agg = "Count(*) as Value";
        else
        {
            var fi = ResolveNumeric(fact, req.Measure?.Field);
            agg = fn switch
            {
                "sum" => $"{fi.Sum()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                "avg" => $"{fi.Avg()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                "min" => $"{fi.Min()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                "max" => $"{fi.Max()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                _ => throw new ApiException(400, "非法度量"),
            };
        }

        var sb = new SelectBuilder
        {
            Table = fact.Table.TableName,
            Column = $"{groupFi.ColumnName} as {groupFi.Name}, {agg}",
            Where = where + "",
            GroupBy = groupFi.ColumnName,
            OrderBy = "Value desc",
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
                Object val = null;
                try { val = row["Value"]; } catch { /* ignore */ }
                items.Add(new WidgetQueryItem
                {
                    Key = key,
                    Label = FormatGroupLabel(groupFi, key, mapSource),
                    Value = val,
                });
            }
        }
        return new WidgetQueryResult { Items = items, HostFilterApplied = hostApplied };
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
        var fi = fact.Table.FindByName(field) ?? fact.Fields.FirstOrDefault(f => f.Name.EqualIgnoreCase(field));
        if (fi is null) throw new ApiException(400, "未知度量字段");
        if (fi.PrimaryKey || fi.IsIdentity) throw new ApiException(400, "非法度量字段");
        if (fi.Name.EqualIgnoreCase("Password", "Secret", "Salt")) throw new ApiException(400, "非法度量字段");
        var t = fi.Type;
        if (t != typeof(Int32) && t != typeof(Int64) && t != typeof(Single) && t != typeof(Double) && t != typeof(Decimal)
            && t != typeof(Int32?) && t != typeof(Int64?) && t != typeof(Decimal?))
            throw new ApiException(400, "度量字段必须为数值");
        return fi;
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
    static WidgetQueryResult TimeBucketAggregate(IEntityFactory fact, Expression where, WidgetQueryRequest req, Boolean hostApplied)
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

        var fn = (req.Measure?.Fn + "").Trim().ToLowerInvariant();
        if (fn.IsNullOrEmpty()) fn = "count";
        String agg;
        if (fn == "count")
            agg = "Count(*) as Value";
        else
        {
            var fi = ResolveNumeric(fact, req.Measure?.Field);
            agg = fn switch
            {
                "sum" => $"{fi.Sum()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                "avg" => $"{fi.Avg()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                "min" => $"{fi.Min()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                "max" => $"{fi.Max()}".Replace($" as {fi.Name}", " as Value", StringComparison.OrdinalIgnoreCase),
                _ => throw new ApiException(400, "非法度量"),
            };
        }

        var sb = new SelectBuilder
        {
            Table = fact.Table.TableName,
            Column = $"{dayExpr} as Bucket, {agg}",
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
                Object val = null;
                try { val = row["Value"]; } catch { /* ignore */ }
                items.Add(new WidgetQueryItem { Key = key, Label = key, Value = val });
            }
        }
        return new WidgetQueryResult { Items = items, HostFilterApplied = hostApplied };
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
