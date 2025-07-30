using System.ComponentModel;
using System.Reflection;
using NewLife.Common;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XCode.Model;

namespace NewLife.Cube;

/// <summary>只读实体控制器基类</summary>
public partial class ReadOnlyEntityController<TEntity>
{
    #region 属性
    /// <summary>实体工厂</summary>
    public static IEntityFactory Factory => Entity<TEntity>.Meta.Factory;

    /// <summary>实体改变时写日志。默认false</summary>
    protected static Boolean LogOnChange { get; set; }

    /// <summary>系统配置</summary>
    public SysConfig SysConfig { get; set; }

    /// <summary>当前列表页的查询条件缓存Key</summary>
    protected static String CacheKey => $"CubeView_{typeof(TEntity).FullName}";
    #endregion

    #region 构造
    static ReadOnlyEntityController()
    {
        // 强行实例化一次，初始化实体对象
        var entity = new TEntity();
    }

    /// <summary>构造函数</summary>
    public ReadOnlyEntityController()
    {
        var set = PageSetting;
        set.IsReadOnly = true;

#if MVC
        set.EnableTableDoubleClick = CubeSetting.Current.EnableTableDoubleClick;
#endif

        if (set.OrderByKey)
        {
            // 大于100万条数据时，默认不启用数字型主键降序，避免数据库选择主键索引导致复杂查询变慢
            if (Entity<TEntity>.Meta.ShardPolicy == null && Entity<TEntity>.Meta.Count > 1_000_000)
                set.OrderByKey = false;
        }

        SysConfig = SysConfig.Current;
    }
    #endregion

    #region 数据获取
    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> Search(Pager p)
    {
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        var key = p["Q"];

        var whereExpression = Entity<TEntity>.SearchWhereByKeys(key);
        if (start > DateTime.MinValue || end > DateTime.MinValue)
        {
            var masterTime = Factory.MasterTime;
            if (masterTime != null)
                whereExpression &= masterTime.Between(start, end);
        }

        //// 根据模型列设置，拼接作为搜索字段的字段
        //var modelTable = ModelTable;
        //var modelCols = modelTable?.GetColumns()?.Where(w => w.ShowInSearch)?.ToList() ?? new List<ModelColumn>();

        //foreach (var col in modelCols)
        //{
        //    var val = p[col.Name];
        //    if (val.IsNullOrWhiteSpace()) continue;
        //    whereExpression &= col.Field == val;
        //}

        // 添加映射字段查询
        foreach (var field in Factory.Fields)
        {
            var val = p[field.Name];
            if (!val.IsNullOrWhiteSpace())
            {
                whereExpression &= field.Equal(val.ChangeType(field.Type));
            }
        }

        return Entity<TEntity>.FindAll(whereExpression, p);
    }

    /// <summary>搜索数据，支持数据权限</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> SearchData(Pager p)
    {
        // 数据权限
        var builder = CreateWhere();
        if (builder != null)
        {
            builder.Data2 ??= p.Items;
            p.State = builder;
        }

        // 数字型主键，默认降序
        if (PageSetting.OrderByKey && p.Sort.IsNullOrEmpty() && p.OrderBy.IsNullOrEmpty())
        {
            var uk = Factory.Unique;
            if (uk != null && uk.Type.IsInt())
            {
                p.OrderBy = uk.Desc();
            }
        }

        return Search(p);
    }

    /// <summary>查找单行数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual TEntity Find(Object key)
    {
        // 分表需要特殊处理
        var fact = Factory;
        var shardField = fact.ShardPolicy?.Field;
        if (shardField != null)
        {
            var dt = GetRequest(shardField.Name).ToDateTime();
            if (dt.Year > 2000)
            {
                var entity = new TEntity();
                entity[fact.Unique.Name] = key;
                entity[shardField.Name] = dt;
                return FindByKey(entity);
            }
        }

        return FindByKey(key);
    }

    private TEntity FindByKey(Object key)
    {
        var fact = Factory;
        if (fact.Unique == null)
        {
            var pks = fact.Table.PrimaryKeys;
            if (pks.Length > 0)
            {
                var exp = new WhereExpression();
                foreach (var item in pks)
                {
                    // 如果前端没有传值，则不要参与构造查询
                    var val = GetRequest(item.Name);

                    // 2021.04.18 添加
                    // 表结构没有唯一键，只有联合主键，并且id是其中一个主键，
                    // 而id作为路由参数，上面从Request中获取到空值，
                    // 最终导致联合主键的表查询单条数据，只用到名称为非id的主键
                    if (val == null && item.Name.EqualIgnoreCase("id")) val = key.ToString();

                    if (val != null) exp &= item.Equal(val);
                }

                return Entity<TEntity>.Find(exp);
            }
        }

        return Entity<TEntity>.FindByKeyForEdit(key);
    }

    /// <summary>查找单行数据，并判断数据权限</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected TEntity FindData(Object key)
    {
        // 先查出来，再判断数据权限
        var entity = Find(key);
        if (entity != null)
        {
            // 数据权限
            var builder = CreateWhere();
            if (builder != null && !builder.Eval(entity)) throw new InvalidOperationException($"非法访问数据[{key}]");
        }

        return entity;
    }

    /// <summary>创建查询条件构造器，主要用于数据权限</summary>
    /// <returns></returns>
    protected virtual WhereBuilder CreateWhere()
    {
        var exp = "";
        var att = GetType().GetCustomAttribute<DataPermissionAttribute>();
        if (att != null)
        {
            // 已登录用户判断系统角色，未登录时不判断
            var user = HttpContext.Items["CurrentUser"] as IUser;
            user ??= ManageProvider.User;
            if (user == null || !user.Roles.Any(e => e.IsSystem) && !att.Valid(user.Roles))
                exp = att.Expression;
        }

        // 多租户
        var set = CubeSetting.Current;
        if (set.EnableTenant)
        {
            var ctxTenant = TenantContext.Current;
            if (ctxTenant != null && IsTenantSource)
            {
                var tenant = Tenant.FindById(ctxTenant.TenantId);
                if (tenant != null)
                {
                    HttpContext.Items["TenantId"] = tenant.Id;

                    if (typeof(TEntity) == typeof(Tenant))
                    {
                        if (!exp.IsNullOrEmpty())
                            exp = "Id={#TenantId} and " + exp;
                        else
                            exp = "Id={#TenantId}";
                    }
                    else
                    {
                        if (!exp.IsNullOrEmpty())
                            exp = "TenantId={#TenantId} and " + exp;
                        else
                            exp = "TenantId={#TenantId}";
                    }
                }
            }
        }

        if (exp.IsNullOrEmpty()) return null;

        var builder = new WhereBuilder
        {
            Factory = Factory,
            Expression = exp,
        };
        builder.SetData(Session);
        builder.SetData2(HttpContext.Items.ToDictionary(e => e.Key + "", e => e.Value));

        return builder;
    }

    /// <summary>是否租户实体类</summary>
    protected virtual Boolean IsTenantSource => typeof(TEntity).GetInterfaces().Any(e => e == typeof(ITenantSource));

    /// <summary>获取选中键</summary>
    /// <returns></returns>
    protected virtual String[] SelectKeys => GetRequest("Keys")?.Split(",");

    /// <summary>多次导出数据</summary>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportData(Int32 max = 0)
    {
        var set = CubeSetting.Current;
        if (max <= 0) max = set.MaxExport;

        // 计算目标数据量
        var p = Session[CacheKey] as Pager;
        p = new Pager(p)
        {
            RetrieveTotalCount = true,
            PageIndex = 1,
            PageSize = 1,
        };
        SearchData(p);
        p.PageSize = 20_000;

        //!!! 数据量很大，且有时间条件时，采用时间分片导出。否则统一分页导出
        //if (Factory.Count > 100_000)
        if (p.TotalCount > 100_000)
        {
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            if (start.Year > 2000 /*&& end.Year > 2000*/)
            {
                if (end.Year < 2000) end = DateTime.Now;

                // 计算步进，80%数据集中在20%时间上，凑够每页10000
                //var speed = (p.TotalCount * 0.8) / (24 * 3600 * 0.2);
                var speed = (Double)p.TotalCount / (24 * 3600);
                var step = p.PageSize / speed;

                XTrace.WriteLine("[{0}]导出数据[{1:n0}]，时间区间（{2},{3}），分片步进{4:n0}秒", Factory.EntityType.FullName, p.TotalCount, start, end, step);

                return ExportDataByDatetime((Int32)step, max);
            }
        }

        XTrace.WriteLine("[{0}]导出数据[{1:n0}]，共[{2:n0}]页", Factory.EntityType.FullName, p.TotalCount, p.PageCount);

        return ExportDataByPage(p.PageSize, max);
    }

    /// <summary>分页导出数据</summary>
    /// <param name="pageSize">页大小。默认10_000</param>
    /// <param name="max">最大行数</param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportDataByPage(Int32 pageSize, Int32 max)
    {
        // 跳过头部一些页数，导出当前页以及以后的数据
        var p = Session[CacheKey] as Pager;
        p = new Pager(p)
        {
            // 不要查记录数
            RetrieveTotalCount = false,
            PageIndex = 1,
            PageSize = pageSize
        };

        while (max > 0)
        {
            if (HttpContext.RequestAborted.IsCancellationRequested) yield break;
            if (p.PageSize > max) p.PageSize = max;

            var list = SearchData(p);

            var count = list.Count();
            if (count == 0) break;
            max -= count;

            foreach (var item in list)
            {
                yield return item;
            }

            if (count < p.PageSize) break;

            p.PageIndex++;
        }

        // 回收内存
        GC.Collect();
    }

    /// <summary>时间分片导出数据</summary>
    /// <param name="step">分片不仅。默认60</param>
    /// <param name="max">最大行数</param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportDataByDatetime(Int32 step, Int32 max)
    {
        // 跳过头部一些页数，导出当前页以及以后的数据
        var p = Session[CacheKey] as Pager;
        p = new Pager(p)
        {
            // 不要查记录数
            RetrieveTotalCount = false,
            PageIndex = 1,
            PageSize = 0,
        };

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        if (end.Year < 2000) end = DateTime.Now;

        //!!! 前后同一天必须查跨天
        if (start == start.Date && end == end.Date) end = end.AddDays(1);

        var dt = start;
        while (max > 0 && dt < end)
        {
            if (HttpContext.RequestAborted.IsCancellationRequested) yield break;

            var dt2 = dt.AddSeconds(step);
            if (dt2 > end) dt2 = end;

            p["dtStart"] = dt.ToFullString();
            p["dtEnd"] = dt2.ToFullString();

            var list = SearchData(p);

            var count = list.Count();
            //if (count == 0) break;

            foreach (var item in list)
            {
                yield return item;
            }

            dt = dt2;
            max -= count;
        }

        // 回收内存
        GC.Collect();
    }
    #endregion

    #region 实体操作重载
    /// <summary>验证实体对象</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="type">操作类型</param>
    /// <param name="post">是否提交数据阶段</param>
    /// <returns></returns>
    protected virtual Boolean Valid(TEntity entity, DataObjectMethodType type, Boolean post)
    {
        if (!ValidPermission(entity, type, post))
        {
            switch (type)
            {
                case DataObjectMethodType.Select: throw new NoPermissionException(PermissionFlags.Detail, "无权查看数据");
                case DataObjectMethodType.Update: throw new NoPermissionException(PermissionFlags.Update, "无权更新数据");
                case DataObjectMethodType.Insert: throw new NoPermissionException(PermissionFlags.Insert, "无权新增数据");
                case DataObjectMethodType.Delete: throw new NoPermissionException(PermissionFlags.Delete, "无权删除数据");
            }
        }

        if (post && LogOnChange)
        {
            // 必须提前写修改日志，否则修改后脏数据失效，保存的日志为空
            switch (type)
            {
                case DataObjectMethodType.Insert:
                case DataObjectMethodType.Delete:
                case DataObjectMethodType.Update when (entity as IEntity).HasDirty:
                    LogProvider.Provider.WriteLog(type + "", entity);
                    break;
            }
        }

        return true;
    }

    /// <summary>验证实体对象</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="type">操作类型</param>
    /// <param name="post">是否提交数据阶段</param>
    /// <returns></returns>
    protected virtual Boolean ValidPermission(TEntity entity, DataObjectMethodType type, Boolean post) => true;
    #endregion

    #region 列表字段和表单字段
    private static FieldCollection _ListFields;
    /// <summary>列表字段过滤</summary>
    protected static FieldCollection ListFields => _ListFields ??= new FieldCollection(Factory, ViewKinds.List);

    //private static FieldCollection _FormFields;
    ///// <summary>表单字段过滤</summary>
    //[Obsolete]
    //protected static FieldCollection FormFields => _FormFields ??= new FieldCollection(Factory, "Form");

    private static FieldCollection _AddFormFields;
    /// <summary>表单字段过滤</summary>
    protected static FieldCollection AddFormFields => _AddFormFields ??= new FieldCollection(Factory, ViewKinds.AddForm);

    private static FieldCollection _EditFormFields;
    /// <summary>表单字段过滤</summary>
    protected static FieldCollection EditFormFields => _EditFormFields ??= new FieldCollection(Factory, ViewKinds.EditForm);

    private static FieldCollection _DetailFields;
    /// <summary>表单字段过滤</summary>
    protected static FieldCollection DetailFields => _DetailFields ??= new FieldCollection(Factory, ViewKinds.Detail);

    private static FieldCollection _SearchFields;
    /// <summary>搜索字段过滤</summary>
    protected static FieldCollection SearchFields => _SearchFields ??= new FieldCollection(Factory, ViewKinds.Search);

    /// <summary>获取字段信息。支持用户重载并根据上下文定制界面</summary>
    /// <param name="kind">字段类型：1-列表List、2-详情Detail、3-添加AddForm、4-编辑EditForm、5-搜索Search</param>
    /// <param name="model">获取字段列表时的相关模型，可能是实体对象或实体列表，可依次来定制要显示的字段</param>
    /// <returns></returns>
    protected virtual FieldCollection OnGetFields(ViewKinds kind, Object model)
    {
        var fields = kind switch
        {
            ViewKinds.List => ListFields,
            ViewKinds.Detail => DetailFields,
            ViewKinds.AddForm => AddFormFields,
            ViewKinds.EditForm => EditFormFields,
            ViewKinds.Search => SearchFields,
            _ => ListFields,
        };
        fields = fields.Clone();

        // 表单嵌入配置字段
        if ((kind == ViewKinds.EditForm || kind == ViewKinds.Detail) && model is TEntity entity)
        {
            // 获取参数对象，展开参数，作为表单字段
            foreach (var item in fields.ToArray())
            {
                var field = (item as FormField)?.Expand;
                var p = field?.Decode?.Invoke(entity);
                if (p != null && p is not String)
                {
                    if (field.Name.IsNullOrEmpty()) field.Name = item.Name;
                    if (field.Category.IsNullOrEmpty()) field.Category = item.Category;
                    if (field.Prefix.IsNullOrEmpty()) field.Prefix = item.Name + "_";

                    var fs = OnExpandFields(field, entity, p);
                    if (fs != null && fs.Count > 0)
                    {
                        fields.AddRange(fs);

                        if (!field.Retain) fields.Remove(item);
                    }
                }
            }
        }

        return fields;
    }

    /// <summary>展开字段</summary>
    protected virtual FieldCollection OnExpandFields(ExpandField field, TEntity entity, Object parameter) => field.Expand(entity, parameter);
    #endregion
}