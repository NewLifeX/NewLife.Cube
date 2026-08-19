using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.AI.Models;
using NewLife.AI.Tools;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Cube.AI;
using NewLife.Cube.Automation;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
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

        // 视图筛选下推（OSC-260819e483 P2）：复用 AutomationFilter/ViewFilterDto（logic=all/any 与 OSC-0015 同构）。
        // 任一条件无法下推（未知字段/不支持操作符）则整段返回 null，忽略服务端过滤——前端 matchesViewFilter 仍在，
        // 翻页不完整为已知限制（只保证本页复核），不 500。
        // 数据权限表达式（builder）始终保留：logic=any 只 OR 筛选条件，不得放大 CreateWhere 权限范围。
        var viewFilter = p["viewFilter"];
        if (!viewFilter.IsNullOrEmpty())
        {
            var filter = AutomationFilter.ParseViewFilter(viewFilter);
            var viewExp = AutomationFilter.TryBuildWhere(Factory, filter);
            if (viewExp != null)
            {
                // WhereBuilder.GetExpression() 对含常量/占位符无法解析的表达式（如多租户 fail-closed "1=0"）会抛异常，
                // 此时放弃 viewFilter 下推（State 保持原 WhereBuilder，FindAll 仍按既有路径消费），前端 matchesViewFilter 兜底，不 500
                try
                {
                    if (builder != null)
                    {
                        builder.Factory ??= Factory;
                        p.State = builder.GetExpression() & viewExp;
                    }
                    else
                    {
                        p.State = viewExp;
                    }
                }
                catch
                {
                    if (builder == null) p.State = null;
                }
            }
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
        if (set.EnableTenant && IsTenantSource)
        {
            var ctxTenant = TenantContext.Current;

            // 无租户上下文（未设置/匿名请求）：fail-closed，拒绝查询，防止无租户场景看到全量数据
            if (ctxTenant == null)
            {
                XTrace.WriteLine($"多租户模式下缺少租户上下文，禁止查询{typeof(TEntity).Name}");
                exp = "1=0";
            }
            else if (ctxTenant.TenantId == 0)
            {
                // 管理后台模式（TenantId=0），不限制租户数据，管理后台要能看到所有租户的数据
            }
            else
            {
                // 租户模式（TenantId>0）：校验租户存在且启用，无效则 fail-closed，防止伪造租户ID绕过数据隔离
                var tenant = ctxTenant.Tenant;
                tenant ??= Tenant.FindById(ctxTenant.TenantId);
                if (tenant == null || !tenant.Enable)
                {
                    XTrace.WriteLine($"多租户模式下租户[{ctxTenant.TenantId}]不存在或已禁用，禁止查询{typeof(TEntity).Name}");
                    exp = "1=0";
                }
                else
                {
                    // WhereBuilder 内部会从 HttpContext.Items 读取 TenantId
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
    protected virtual Boolean IsTenantSource => typeof(TEntity).GetInterfaces().Any(e => e == typeof(ITenantScope));

    /// <summary>获取选中键</summary>
    /// <returns></returns>
    protected virtual String[] SelectKeys => GetRequest("Keys")?.Split(",");

    /// <summary>获取缓存的分页对象。内含查询条件和排序条件</summary>
    /// <returns></returns>
    protected virtual Pager GetCachePager()
    {
        var request = WebHelper.Params;
        var queryData = request["_query"];
        if (!queryData.IsNullOrEmpty())
        {
            queryData = queryData.ToBase64().ToStr();
            var p = new Pager();
            p.Parse(queryData);
            return p;
        }
        else
        {
            // 计算目标数据量。不能破坏缓存对象，需要new一个新对象
            var p = Session[CacheKey] as Pager;
            return new Pager(p);
        }
    }

    /// <summary>多次导出数据</summary>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportData(Int32 max = 0)
    {
        var set = CubeSetting.Current;
        if (max <= 0) max = set.MaxExport;

        var p = GetCachePager();
        p.RetrieveTotalCount = true;
        p.PageIndex = 1;
        p.PageSize = 1;
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
        //var p = Session[CacheKey] as Pager;
        //p = new Pager(p)
        //{
        //    // 不要查记录数
        //    RetrieveTotalCount = false,
        //    PageIndex = 1,
        //    PageSize = pageSize
        //};
        var p = GetCachePager();
        p.RetrieveTotalCount = false;
        p.PageIndex = 1;
        p.PageSize = pageSize;

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
        //var p = Session[CacheKey] as Pager;
        //p = new Pager(p)
        //{
        //    // 不要查记录数
        //    RetrieveTotalCount = false,
        //    PageIndex = 1,
        //    PageSize = 0,
        //};
        var p = GetCachePager();
        p.RetrieveTotalCount = false;
        p.PageIndex = 1;
        p.PageSize = 0;

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

    #region AI 对话
    // AI 对话端点已统一收拢到全局 AiController（/Ai/AiChat）。
    // 本控制器实现 IEntityAiContext 能力接口，向全局端点提供数据查询（SearchData）、工具集（CreateCubeTools）与提示词（BuildChatSystemPrompt）等重载点。

    /// <summary>创建 AI 工具集。二次开发者可重载，返回自定义工具集以调整 AI 使用的数据逻辑</summary>
    /// <remarks>
    /// 默认工具集 <see cref="CubeTools{TEntity}"/> 提供数据上下文、表单 Schema、回填表单等能力。
    /// 重载时通常继承 <see cref="CubeTools{TEntity}"/> 并重写其 virtual 工具方法
    /// （GetDataContext / GetFormSchema / FillForm），或重写数据收集方法（GetListContext / GetRecordContext），
    /// 或返回全新的 IToolProvider 实现。数据查询委托默认走 SearchData（保留子类重载与数据权限）。
    /// </remarks>
    /// <param name="pager">当前查询条件（可为空）</param>
    /// <param name="entityId">当前记录编号</param>
    /// <returns>AI 工具集</returns>
    protected virtual CubeTools<TEntity> CreateCubeTools(Pager? pager, Int64 entityId)
        => new CubeTools<TEntity>(Factory, pager, entityId, p => SearchData(p).ToList());

    /// <summary>构建 AI 对话系统提示词，注入当前页面上下文</summary>
    /// <param name="req">对话请求</param>
    /// <param name="pager">当前查询条件</param>
    /// <returns></returns>
    protected virtual String BuildChatSystemPrompt(AiChatRequest req, Pager? pager)
    {
        var tb = Factory.Table.DataTable;
        var name = Factory.EntityType.GetDisplayName() ?? tb.DisplayName ?? Factory.EntityType.Name;
        // 系统名称取系统设置里的配置，空值时兜底为默认名称
        var sysName = SysConfig?.DisplayName;
        if (sysName.IsNullOrEmpty()) sysName = "魔方后台管理系统";

        var sb = Pool.StringBuilder.Get();
        sb.AppendLine($"你是{sysName}的 AI 助手，正在协助管理员操作当前页面。");
        sb.AppendLine();
        sb.AppendLine($"当前实体：{name}（表 {tb.TableName}）");
        if (!tb.Description.IsNullOrEmpty()) sb.AppendLine($"实体说明：{tb.Description}");
        var pageName = req.Page switch
        {
            "form" => req.Mode.EqualIgnoreCase("edit") ? "编辑表单" : "新增表单",
            "detail" => "详情页",
            _ => "列表页",
        };
        sb.AppendLine($"页面类型：{pageName}");
        if (req.Id > 0) sb.AppendLine($"当前记录编号：{req.Id}");
        if (pager != null && pager.Params.Count > 0)
        {
            sb.AppendLine("当前查询条件：");
            foreach (var kv in pager.Params.Where(e => !e.Key.EqualIgnoreCase("_query", "Sort", "Desc", "PageIndex", "PageSize")))
            {
                sb.AppendLine($"- {kv.Key}: {kv.Value}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("可用工具：get_data_context / get_form_schema / fill_form / get_page_context / get_system_info / run_js（详细说明见函数定义，按需调用）");
        sb.AppendLine();
        sb.AppendLine("规则：");
        sb.AppendLine("1. 使用简体中文回答，语言简洁专业");
        sb.AppendLine("2. 用户要求分析/洞察当前数据或单条记录时，先调用 get_data_context 获取数据，再给出分析结论与建议");
        sb.AppendLine("3. 用户要求新建/填写/补全表单时，先调用 get_form_schema 了解字段，再调用 fill_form 生成值（对 Value 为 null 的可填字段，若适合自动生成如编码 Code 类，应生成合理唯一值；对已有值的字段保持原值；不要编造邮箱/手机/生日等真实个人数据），最后提示用户检查后提交");
        sb.AppendLine("4. 用户询问当前页面结构/页面元素（表格列、分页、可见数据行等 DOM 层信息）时，调用 get_page_context 采集浏览器当前页面内容");
        sb.AppendLine("5. 用户询问系统状态/诊断时，调用 get_system_info");
        sb.AppendLine("6. 用户要求读取或操作当前页面元素（填写输入框、点击按钮、读取标题等）时，可调用 run_js 执行 JavaScript；脚本在用户浏览器当前页面执行，可用 document.querySelector 等定位元素；修改页面内容或提交表单等写操作前，先向用户说明将执行的操作");
        sb.AppendLine("7. 不要编造数据；信息不足时主动询问用户澄清");

        return sb.Return(true);
    }

    #region 能力接口
    // IEntityAiContext：向全局 AiController 暴露实体 AI 重载点（仿 IPageDataContext 能力接口模式），子类重载经 virtual 委托生效
    IEntityFactory IEntityAiContext.Factory => Factory;

    IEnumerable<Object> IEntityAiContext.SearchData(Pager p) => SearchData(p).Cast<Object>();

    Object IEntityAiContext.CreateCubeTools(Pager? pager, Int64 entityId) => CreateCubeTools(pager, entityId);

    String IEntityAiContext.BuildChatSystemPrompt(AiChatRequest req, Pager? pager) => BuildChatSystemPrompt(req, pager);
    #endregion
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

        // 多租户写路径归属校验：租户模式下，租户实体的新增/修改/删除必须归属当前租户
        if (post && CubeSetting.Current.EnableTenant && IsTenantSource)
        {
            var tenantId = TenantContext.CurrentId;
            if (tenantId > 0)
            {
                var ie = entity as IEntity;
                var entityTenantId = ie?["TenantId"].ToInt() ?? 0;
                switch (type)
                {
                    case DataObjectMethodType.Insert:
                        // 新增强制归属当前租户
                        if (ie != null) ie["TenantId"] = tenantId;
                        break;
                    case DataObjectMethodType.Update:
                    case DataObjectMethodType.Delete:
                        // 修改/删除校验归属，防止跨租户操作
                        if (entityTenantId != tenantId)
                            throw new NoPermissionException(PermissionFlags.None, $"无权操作其它租户的数据[{entityTenantId}]");
                        break;
                }
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

        // 租户模式：隐藏 ITenantScope 实体的 TenantId，避免租户用户改隔离键
        if (CubeSetting.Current.EnableTenant && TenantContext.CurrentId > 0 && IsTenantSource
            && (kind == ViewKinds.AddForm || kind == ViewKinds.EditForm || kind == ViewKinds.Detail))
        {
            fields.RemoveField("TenantId", "TenantName");
        }

        return fields;
    }

    /// <summary>展开字段</summary>
    protected virtual FieldCollection OnExpandFields(ExpandField field, TEntity entity, Object parameter) => field.Expand(entity, parameter);
    #endregion
}