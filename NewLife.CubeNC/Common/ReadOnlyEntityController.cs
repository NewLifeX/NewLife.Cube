using System.ComponentModel;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.IO;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
using NewLife.Xml;
using XCode;
using XCode.Configuration;
using XCode.Membership;
using XCode.Model;

namespace NewLife.Cube;

/// <summary>只读实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public class ReadOnlyEntityController<TEntity> : ControllerBaseX where TEntity : Entity<TEntity>, new()
{
    #region 属性
    /// <summary>实体工厂</summary>
    public static IEntityFactory Factory => Entity<TEntity>.Meta.Factory;

    /// <summary>实体改变时写日志。默认false</summary>
    protected static Boolean LogOnChange { get; set; }

    /// <summary>系统配置</summary>
    public SysConfig SysConfig { get; set; }

    /// <summary>当前列表页的查询条件缓存Key</summary>
    private static String CacheKey => $"CubeView_{typeof(TEntity).FullName}";
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
        PageSetting.IsReadOnly = true;

        PageSetting.EnableTableDoubleClick = CubeSetting.Current.EnableTableDoubleClick;

        SysConfig = SysConfig.Current;
    }

    /// <summary>动作执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var title = GetType().GetDisplayName() ?? typeof(TEntity).GetDisplayName() ?? Entity<TEntity>.Meta.Table.DataTable.DisplayName;
        ViewBag.Title = title;

        // Ajax请求不需要设置ViewBag
        if (!Request.IsAjaxRequest())
        {
            // 默认加上实体工厂
            ViewBag.Factory = Factory;

            // 默认加上分页给前台
            var ps = filterContext.ActionArguments.ToNullable();
            var p = ps["p"] as Pager ?? new Pager();
            ViewBag.Page = p;

            //// 用于显示的列
            //if (!ps.ContainsKey("entity")) ViewBag.Fields = OnGetFields(ViewKinds.List, null);

            var txt = (String)ViewBag.HeaderContent;
            if (txt.IsNullOrEmpty()) txt = Menu?.Remark;
            if (txt.IsNullOrEmpty()) txt = GetType().GetDescription();
            if (txt.IsNullOrEmpty()) txt = Entity<TEntity>.Meta.Table.Description;
            //if (txt.IsNullOrEmpty() && SysConfig.Current.Develop)
            //    txt = "这里是页头内容，来自于菜单备注，或者给控制器增加Description特性";
            ViewBag.HeaderContent = txt;
        }

        base.OnActionExecuting(filterContext);
    }

    /// <summary>执行后</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuted(ActionExecutedContext filterContext)
    {
        base.OnActionExecuted(filterContext);

        var title = ViewBag.Title + "";
        HttpContext.Items["Title"] = title;
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

        //添加映射字段查询
        foreach (var item in Factory.Fields)
        {
            var val = p[item.Name];
            if (!val.IsNullOrWhiteSpace())
            {
                whereExpression &= item.Equal(val);
            }
        }

        return Entity<TEntity>.FindAll(whereExpression, p);
    }

    /// <summary>搜索数据，支持数据权限</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected IEnumerable<TEntity> SearchData(Pager p)
    {
        // 缓存数据，用于后续导出
        //SetSession(CacheKey, p);
        //Session[CacheKey] = p;

        // 数据权限
        var builder = CreateWhere();
        if (builder != null)
        {
            builder.Data2 ??= p.Items;
            p.State = builder;
        }

        return Search(p);
    }

    /// <summary>查找单行数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual TEntity Find(Object key)
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
        var ctxTenant = TenantContext.Current;
        if (ctxTenant != null && IsTenantSource)
        {
            var tenant = Tenant.FindById(ctxTenant.TenantId);
            if (tenant != null)
            {
                HttpContext.Items["TenantId"] = tenant.Id;

                if (!exp.IsNullOrEmpty())
                    exp = "TenantId={#TenantId} and " + exp;
                else
                    exp = "TenantId={#TenantId}";
            }
        }

        if (exp.IsNullOrEmpty()) return null;

        var builder = new WhereBuilder
        {
            Factory = Factory,
            Expression = exp,
            //Data = Session,
        };
        builder.SetData(Session);
        //builder.Data2 = new ItemsExtend { Items = HttpContext.Items };
        builder.SetData2(HttpContext.Items.ToDictionary(e => e.Key + "", e => e.Value));

        return builder;
    }

    /// <summary>是否租户实体类</summary>
    public Boolean IsTenantSource => typeof(TEntity).GetInterfaces().Any(e => e == typeof(ITenantSource));

    /// <summary>获取选中键</summary>
    /// <returns></returns>
    protected virtual String[] SelectKeys => GetRequest("Keys")?.Split(",");

    /// <summary>多次导出数据</summary>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportData(Int32 max = 10_000_000)
    {
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
    /// <param name="max">最大行数。默认10_000_000</param>
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
    /// <param name="max">最大行数。默认10_000_000</param>
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

    #region 默认Action
    /// <summary>数据列表首页</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("{type}管理")]
    //[AllowAnonymous]
    public virtual ActionResult Index(Pager p = null)
    {
        p ??= ViewBag.Page as Pager;

        // 缓存数据，用于后续导出
        //SetSession(CacheKey, p);
        Session[CacheKey] = p;

        return IndexView(p);
    }

    /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected virtual ActionResult IndexView(Pager p)
    {
        // 需要总记录数来分页
        p.RetrieveTotalCount = true;

        var list = SearchData(p);

        // 用于显示的列
        ViewBag.Fields = OnGetFields(ViewKinds.List, list);

        // Json输出
        if (IsJsonRequest) return Json(0, null, list, new { page = p });

        return View("List", list);
    }

    /// <summary>表单，查看</summary>
    /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("查看{type}")]
    public virtual ActionResult Detail(String id)
    {
        var entity = FindData(id);
        if (entity == null || (entity as IEntity).IsNullKey) throw new XException("要查看的数据[{0}]不存在！", id);

        // 验证数据权限
        Valid(entity, DataObjectMethodType.Select, false);

        // Json输出
        if (IsJsonRequest) return Json(0, null, entity);

        // 用于显示的列
        ViewBag.Fields = OnGetFields(ViewKinds.Detail, entity);

        return View("Detail", entity);
    }

    /// <summary>清空全表数据</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("清空")]
    public virtual ActionResult Clear()
    {
        var url = Request.GetReferer();

        var p = Session[CacheKey] as Pager;
        p = new Pager(p);
        if (p != null && p.Params.Count > 0) return Json(500, "当前带有查询参数，为免误解，禁止全表清空！");

        try
        {
            var count = Entity<TEntity>.Meta.Session.Truncate();

            WriteLog("清空数据", true, $"共删除{count}行数据");

            if (Request.IsAjaxRequest())
                return JsonRefresh($"共删除{count}行数据");
            else if (!url.IsNullOrEmpty())
                return Redirect(url);
            else
                return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            WriteLog("清空数据", false, ex.GetMessage());

            throw;
        }
    }
    #endregion

    #region 数据接口
    /// <summary>页面</summary>
    /// <param name="token">令牌</param>
    /// <param name="p">分页</param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("页面")]
    public virtual ActionResult Html(String token, Pager p)
    {
        try
        {
            var issuer = ValidToken(token);

            // 需要总记录数来分页
            p.RetrieveTotalCount = true;

            var list = SearchData(p);

            return View("List", list);
        }
        catch (Exception ex)
        {
            return Content(ex.Message);
        }
    }

    /// <summary>Json接口</summary>
    /// <param name="token">令牌</param>
    /// <param name="p">分页</param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("Json接口")]
    public virtual ActionResult Json(String token, Pager p)
    {
        try
        {
            var issuer = ValidToken(token);

            // 需要总记录数来分页
            p.RetrieveTotalCount = true;

            var list = SearchData(p);

            // Json输出
            return Json(0, null, list, new { issuer, page = p });
        }
        catch (Exception ex)
        {
            return Json(0, null, ex);
        }
    }

    /// <summary>验证令牌是否有效</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    protected virtual String ValidToken(String token)
    {
        if (token.IsNullOrEmpty()) token = GetRequest("token");
        if (token.IsNullOrEmpty()) token = GetRequest("key");

        var app = App.FindBySecret(token);
        if (app != null)
        {
            if (!app.Enable) throw new XException("非法授权！");

            return app?.ToString();
        }
        else
        {
            var ut = UserToken.Valid(token, UserHost);
            var user = ut.User;

            // 定位菜单页面
            var menu = ManageProvider.Menu.FindByFullName(GetType().FullName);

            // 判断权限
            if (menu == null || !user.Has(menu, PermissionFlags.Detail)) throw new Exception($"该用户[{user}]无权访问[{menu}]");

            // 锁定页面
            if (!ut.Url.IsNullOrEmpty())
            {
                var url = ut.Url;
                if (url.Contains("?")) url = url.Substring(null, "?");
                if (!url.StartsWithIgnoreCase(menu.Url.TrimStart("~"))) throw new Exception($"该令牌[{user}]无权访问[{menu}]，仅限于[{url}]");
            }

            // 设置当前用户，用于数据权限控制
            HttpContext.Items["userId"] = user.ID;
            HttpContext.Items["CurrentUser"] = user;

            return user?.ToString();
        }
    }

    /// <summary>Xml接口</summary>
    /// <param name="token">令牌</param>
    /// <param name="p">分页</param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("Xml接口")]
    public virtual ActionResult Xml(String token, Pager p)
    {
        var xml = "";
        try
        {
            var issuer = ValidToken(token);

            // 需要总记录数来分页
            p.RetrieveTotalCount = true;

            var list = SearchData(p) as IList<TEntity>;

            var rs = new Root { Result = false, Data = list, Pager = p, Issuer = issuer };

            xml = rs.ToXml(null, false, false);
        }
        catch (Exception ex)
        {
            var rs = new { result = false, data = ex.GetTrue().Message };
            xml = rs.ToXml(null, false, false);
        }

        return Content(xml, "application/xml");
    }

    private class Root
    {
        public Boolean Result { get; set; }
        public IList<TEntity> Data { get; set; }
        public Pager Pager { get; set; }
        public String Issuer { get; set; }
    }

    /// <summary>Csv接口</summary>
    /// <param name="token">令牌</param>
    /// <param name="p">分页</param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("Excel接口")]
    public virtual async Task<ActionResult> Csv(String token, Pager p)
    {
        var issuer = ValidToken(token);

        //// 需要总记录数来分页
        //p.RetrieveTotalCount = true;

        var list = SearchData(p);

        // 准备需要输出的列
        var fs = Factory.Fields.ToList();

        var rs = Response;
        var headers = rs.Headers;
        headers[HeaderNames.ContentEncoding] = "UTF8";
        //headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

        await OnExportCsv(fs, list, rs.Body);

        return new EmptyResult();
    }

    /// <summary>Csv接口</summary>
    /// <param name="token">令牌</param>
    /// <param name="p">分页</param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("Excel接口")]
    public virtual async Task<ActionResult> Excel(String token, Pager p)
    {
        var issuer = ValidToken(token);

        var list = SearchData(p);

        // 准备需要输出的列
        var fs = new List<FieldItem>();
        foreach (var fi in Factory.AllFields)
        {
            if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
            if (!fi.IsDataObjectField)
            {
                var pi = Factory.EntityType.GetProperty(fi.Name);
                if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
            }

            fs.Add(fi);
        }

        // 基本属性与扩展属性对调顺序
        for (var i = 0; i < fs.Count; i++)
        {
            var fi = fs[i];
            if (fi.OriField != null)
            {
                var k = fs.IndexOf(fi.OriField);
                if (k >= 0)
                {
                    fs[i] = fs[k];
                    fs[k] = fi;
                }
            }
        }

        var rs = Response;
        var headers = rs.Headers;
        headers[HeaderNames.ContentEncoding] = "UTF8";
        //headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

        await OnExportExcel(fs, list, rs.Body);

        return new EmptyResult();
    }
    #endregion

    #region 导出Xml/Json/Excel/Csv
    /// <summary>导出Xml</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出")]
    public virtual ActionResult ExportXml()
    {
        var obj = OnExportXml();
        var xml = "";
        if (obj is IEntity)
            xml = (obj as IEntity).ToXml();
        else if (obj is IList<TEntity>)
            xml = (obj as IList<TEntity>).ToXml();
        else if (obj is IEnumerable<TEntity> list)
            xml = list.ToList().ToXml();

        SetAttachment(null, ".xml", true);

        return Content(xml, "text/xml", Encoding.UTF8);
    }

    /// <summary>要导出Xml的对象</summary>
    /// <returns></returns>
    protected virtual Object OnExportXml() => ExportData();

    /// <summary>设置附件响应方式</summary>
    /// <param name="name"></param>
    /// <param name="ext"></param>
    /// <param name="includeTime">包含时间戳</param>
    protected virtual void SetAttachment(String name, String ext, Boolean includeTime)
    {
        if (name.IsNullOrEmpty()) name = GetType().GetDisplayName();
        if (name.IsNullOrEmpty()) name = Factory.EntityType.GetDisplayName();
        if (name.IsNullOrEmpty()) name = Factory.Table.DataTable.DisplayName;
        if (name.IsNullOrEmpty()) name = GetType().Name.TrimEnd("Controller");
        if (!ext.IsNullOrEmpty()) ext = ext.EnsureStart(".");

        if (includeTime) name += $"_{DateTime.Now:yyyyMMddHHmmss}";

        name += ext;
        name = HttpUtility.UrlEncode(name, Encoding.UTF8);

        Response.Headers.Add("Content-Disposition", "Attachment;filename=" + name);
    }

    /// <summary>导出Json</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出")]
    public virtual ActionResult ExportJson()
    {
        var json = OnExportJson().ToJson(true);

        SetAttachment(null, ".json", true);

        return Content(json, "application/json", Encoding.UTF8);
    }

    /// <summary>要导出Json的对象</summary>
    /// <returns></returns>
    protected virtual Object OnExportJson() => ExportData().ToList();

    /// <summary>导出Excel</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出")]
    public virtual async Task<ActionResult> ExportExcel()
    {
        // 准备需要输出的列
        var fs = new List<FieldItem>();
        foreach (var fi in Factory.AllFields)
        {
            if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
            if (!fi.IsDataObjectField)
            {
                var pi = Factory.EntityType.GetProperty(fi.Name);
                if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
            }

            fs.Add(fi);
        }

        // 基本属性与扩展属性对调顺序
        for (var i = 0; i < fs.Count; i++)
        {
            var fi = fs[i];
            if (fi.OriField != null)
            {
                var k = fs.IndexOf(fi.OriField);
                if (k >= 0)
                {
                    fs[i] = fs[k];
                    fs[k] = fi;
                }
            }
        }

        // 要导出的数据超大时，启用流式输出
        if (Factory.Session.Count > 100_000)
        {
            var p = Session[CacheKey] as Pager;
            p = new Pager(p)
            {
                PageSize = 1,
                RetrieveTotalCount = true
            };
            SearchData(p);
        }

        SetAttachment(null, ".xls", true);

        var rs = Response;
        var headers = rs.Headers;
        headers[HeaderNames.ContentEncoding] = "UTF8";
        headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

        var data = ExportData();
        await OnExportExcel(fs, data, rs.Body);

        return new EmptyResult();
    }

    /// <summary>导出Excel模板</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出模板")]
    public virtual async Task<ActionResult> ExportExcelTemplate()
    {
        // 准备需要输出的列
        var fs = new List<FieldItem>();
        foreach (var fi in Factory.AllFields)
        {
            if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
            if (!fi.IsDataObjectField)
            {
                var pi = Factory.EntityType.GetProperty(fi.Name);
                if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
            }

            //模板隐藏这几个字段
            if (fi.Name.EqualIgnoreCase("CreateUserID", "CreateUser", "CreateTime", "CreateIP",
                        "UpdateUserID", "UpdateUser", "UpdateTime", "UpdateIP", "Enable") || fi.Description.IsNullOrEmpty())
            {
                continue;
            }

            fs.Add(fi);
        }

        // 基本属性与扩展属性对调顺序
        for (var i = 0; i < fs.Count; i++)
        {
            var fi = fs[i];
            if (fi.OriField != null)
            {
                var k = fs.IndexOf(fi.OriField);
                if (k >= 0)
                {
                    fs[i] = fs[k];
                    fs[k] = fi;
                }
            }
        }

        // 要导出的数据超大时，启用流式输出
        if (Factory.Session.Count > 100_000)
        {
            var p = Session[CacheKey] as Pager;
            p = new Pager(p)
            {
                PageSize = 1,
                RetrieveTotalCount = true
            };
            SearchData(p);
        }

        SetAttachment(null, ".xls", true);

        var rs = Response;
        var headers = rs.Headers;
        headers[HeaderNames.ContentEncoding] = "UTF8";
        headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

        var data = ExportData(1);
        await OnExportExcel(fs, data, rs.Body);

        return new EmptyResult();
    }

    /// <summary>导出Excel，可重载修改要输出的列</summary>
    /// <param name="fs">字段列表</param>
    /// <param name="list">数据集</param>
    /// <param name="output">输出流</param>
    protected virtual async ValueTask OnExportExcel(List<FieldItem> fs, IEnumerable<TEntity> list, Stream output)
    {
        await using var csv = new CsvFile(output, true);

        // 列头
        var headers = new List<String>();
        foreach (var fi in fs)
        {
            var name = fi.DisplayName;
            if (name.IsNullOrEmpty()) name = fi.Description;
            if (name.IsNullOrEmpty()) name = fi.Name;

            // 第一行以ID开头的csv文件，容易被识别为SYLK文件
            if (name == "ID" && fi == fs[0]) name = "Id";
            headers.Add(name);
        }
        await csv.WriteLineAsync(headers);

        // 内容
        foreach (var entity in list)
        {
            await csv.WriteLineAsync(fs.Select(e => entity[e.Name]));
        }
    }

    /// <summary>导出Csv</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出")]
    public virtual async Task<ActionResult> ExportCsv()
    {
        // 准备需要输出的列
        var fs = Factory.Fields.ToList();

        if (Factory.Session.Count > 100_000)
        {
            var p = Session[CacheKey] as Pager;
            p = new Pager(p)
            {
                PageSize = 1,
                RetrieveTotalCount = true
            };
            SearchData(p);
        }

        var name = GetType().Name.TrimEnd("Controller");
        SetAttachment(name, ".csv", true);

        var rs = Response;
        var headers = rs.Headers;
        headers[HeaderNames.ContentEncoding] = "UTF8";
        headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

        //// 允许同步IO，便于CsvFile刷数据Flush
        //var ft = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
        //if (ft != null) ft.AllowSynchronousIO = true;

        var data = ExportData();
        await OnExportCsv(fs, data, rs.Body);

        return new EmptyResult();
    }

    /// <summary>导出Csv，可重载修改要输出的列</summary>
    /// <param name="fs">字段列表</param>
    /// <param name="list">数据集</param>
    /// <param name="output">输出流</param>
    protected virtual async ValueTask OnExportCsv(List<FieldItem> fs, IEnumerable<TEntity> list, Stream output)
    {
        await using var csv = new CsvFile(output, true);

        // 列头
        var headers = fs.Select(e => e.Name).ToArray();
        if (headers[0] == "ID") headers[0] = "Id";
        await csv.WriteLineAsync(headers);

        // 内容
        foreach (var entity in list)
        {
            await csv.WriteLineAsync(fs.Select(e => entity[e.Name]));
        }
    }
    #endregion

    #region 备份/还原/导出/分享
    /// <summary>备份到服务器本地目录</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("备份")]
    public virtual ActionResult Backup()
    {
        try
        {
            var fact = Factory;
            if (fact.Session.Count > 10_000_000) throw new XException($"数据量[{fact.Session.Count:n0}>10_000_000]，禁止备份！");

            var dal = fact.Session.Dal;

            var name = GetType().Name.TrimEnd("Controller");
            var fileName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}.gz";
            var bak = NewLife.Setting.Current.BackupPath.CombinePath(fileName).GetBasePath();
            bak.EnsureDirectory(true);

            var rs = dal.Backup(fact.Table.DataTable, bak);

            WriteLog("备份", true, $"备份[{fileName}]（{rs:n0}行）成功！");

            return Json(0, $"备份[{fileName}]（{rs:n0}行）成功！");
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);

            WriteLog("备份", false, ex.GetMessage());

            return Json(500, null, ex);
        }
    }

    /// <summary>备份导出</summary>
    /// <remarks>备份并下载</remarks>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出")]
    public virtual async Task<ActionResult> BackupAndExport()
    {
        var fact = Factory;
        if (fact.Session.Count > 10_000_000) throw new XException($"数据量[{fact.Session.Count:n0}>10_000_000]，禁止备份！");

        var dal = fact.Session.Dal;

        var name = GetType().Name.TrimEnd("Controller");
        SetAttachment(name, ".gz", true);

        // 允许同步IO，便于刷数据Flush
        var ft = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
        if (ft != null) ft.AllowSynchronousIO = true;

        var ms = Response.Body;
        try
        {
            await using var gs = new GZipStream(ms, CompressionLevel.Optimal, true);
            var count = dal.Backup(fact.Table.DataTable, gs);

            WriteLog("备份导出", true, $"备份[{name}]（{count:n0}行）成功！");

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);

            WriteLog("备份导出", false, ex.GetMessage());

            return Json(500, null, ex);
        }
    }

    /// <summary>从服务器本地目录还原</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [DisplayName("还原")]
    public virtual ActionResult Restore()
    {
        try
        {
            var fact = Factory;
            var dal = fact.Session.Dal;

            var name = GetType().Name.TrimEnd("Controller");
            var fileName = $"{name}_*.gz";

            var di = NewLife.Setting.Current.BackupPath.GetBasePath().AsDirectory();
            //var fi = di?.GetFiles(fileName)?.LastOrDefault();
            var fi = di?.GetFiles(fileName)?.OrderByDescending(e => e.Name).FirstOrDefault();
            if (fi == null || !fi.Exists) throw new XException($"找不到[{fileName}]的备份文件");

            var rs = dal.Restore(fi.FullName, fact.Table.DataTable);

            WriteLog("恢复", true, $"恢复[{fileName}]（{rs:n0}行）成功！");

            return Json(0, $"恢复[{fileName}]（{rs:n0}行）成功！");
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);

            WriteLog("恢复", false, ex.GetMessage());

            return Json(500, null, ex);
        }
    }

    /// <summary>分享数据</summary>
    /// <remarks>
    /// 为当前url创建用户令牌
    /// </remarks>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("分享{type}")]
    public virtual ActionResult Share()
    {
        // 当前用户所有令牌
        var userId = ManageProvider.User.ID;
        var list = UserToken.Search(null, userId, true, DateTime.Now, DateTime.MinValue, null);

        var p = Session[CacheKey] as Pager;
        p = new Pager(p)
        {
            RetrieveTotalCount = false,
        };

        // 构造url
        var cs = GetControllerAction();
        var url = cs[0].IsNullOrEmpty() ? $"/{cs[1]}" : $"/{cs[0]}/{cs[1]}";
        var sb = p.GetBaseUrl(true, true, true);
        if (sb.Length > 0) url += "?" + sb;

        // 如果该url已存在，则延长有效期
        var ut = list.FirstOrDefault(e => e.Url.EqualIgnoreCase(url));
        ut ??= new UserToken { UserID = userId, Url = url };

        if (ut.Token.IsNullOrEmpty()) ut.Token = Rand.NextString(8);
        ut.Enable = true;
        ut.Expire = DateTime.Now.AddSeconds(CubeSetting.Current.ShareExpire);
        ut.Save();

        //var url2 = $"/Admin/UserToken?q={ut.Token}";

        //return Json(0, "分享成功！" + url, null, new { url = url2, time = 3 });

        return RedirectToAction("Index", "UserToken", new { area = "Admin", q = ut.Token });
    }
    #endregion

    #region 模版Action
    /// <summary>生成列表</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("生成列表")]
    public ActionResult MakeList()
    {
        if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

        // 找到项目根目录
        var root = GetProjectRoot();

        // 视图路径，Areas/区域/Views/控制器/_List_Data.cshtml
        var cs = GetControllerAction();
        var vpath = $"Areas/{cs[0]}/Views/{cs[1]}/_List_Data.cshtml";
        if (!root.IsNullOrEmpty()) vpath = root.EnsureEnd("/") + vpath;

        _ = ViewHelper.MakeListView(typeof(TEntity), vpath, OnGetFields(ViewKinds.List, null));

        WriteLog("生成列表", true, vpath);

        return RedirectToAction("Index");
    }

    /// <summary>生成表单</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("生成表单")]
    public ActionResult MakeForm()
    {
        if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

        // 找到项目根目录
        var root = GetProjectRoot();

        // 视图路径，Areas/区域/Views/控制器/_Form_Body.cshtml
        var cs = GetControllerAction();
        var vpath = $"Areas/{cs[0]}/Views/{cs[1]}/_Form_Body.cshtml";
        if (!root.IsNullOrEmpty()) vpath = root.EnsureEnd("/") + vpath;

        _ = ViewHelper.MakeFormView(typeof(TEntity), vpath, EditFormFields);

        WriteLog("生成表单", true, vpath);

        return RedirectToAction("Index");
    }

    /// <summary>生成搜索</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("生成搜索")]
    public ActionResult MakeSearch()
    {
        if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

        // 找到项目根目录
        var root = GetProjectRoot();

        // 视图路径，Areas/区域/Views/控制器/_List_Search.cshtml
        var cs = GetControllerAction();
        var vpath = $"Areas/{cs[0]}/Views/{cs[1]}/_List_Search.cshtml";
        if (!root.IsNullOrEmpty()) vpath = root.EnsureEnd("/") + vpath;

        _ = ViewHelper.MakeSearchView(typeof(TEntity), vpath, OnGetFields(ViewKinds.List, null));

        WriteLog("生成搜索", true, vpath);

        return RedirectToAction("Index");
    }

    private String GetProjectRoot()
    {
        var asm = GetType().Assembly;
        var name = asm.GetName().Name;

        // core程序出现这种情况：bin/Debug/netcoreapp3.1
        // 因此添加"../../../" 
        var ps = new[] { "./", "../../", "../../" + name, "../../../", "../../../" + name };
        String err = null;
        foreach (var item in ps)
        {
            var dir = item.AsDirectory();
            err += dir + "；";
            if (!dir.Exists) continue;
            var fis = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
            if (fis != null && fis.Length > 0) return item;
        }

        // 找不到项目根目录，就用当前目录，因为某些项目文件名和输出名可能不一致
        return "./";

        //err = $"遍历以下路径均找不到项目路径，请检查项目路径：{err}";
        //throw new InvalidOperationException(err);
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
            if (type == DataObjectMethodType.Delete ||
                (type == DataObjectMethodType.Update && (entity as IEntity).HasDirty))
                LogProvider.Provider.WriteLog(type + "", entity);
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
        return fields.Clone();
    }

    /// <summary>获取字段信息。支持用户重载并根据上下文定制界面</summary>
    /// <param name="kind">字段类型：1-列表List、2-详情Detail、3-添加AddForm、4-编辑EditForm、5-搜索Search</param>
    /// <returns></returns>
    [AllowAnonymous]
    public virtual ActionResult GetFields(ViewKinds kind)
    {
        var fields = OnGetFields(kind, null);

        Object data = new { code = 0, data = fields };

        return new JsonResult(data);
    }

    ///// <summary>
    ///// 实体过滤器，根据模型列的表单显示类型，不显示的字段去掉
    ///// </summary>
    ///// <param name="model"></param>
    ///// <param name="kind"></param>
    ///// <returns></returns>
    //protected virtual IDictionary<String, Object> OnFilter(IModel model, ViewKinds kind)
    //{
    //    if (model == null) return null;

    //    var dic = new Dictionary<String, Object>();
    //    var fields = OnGetFields(kind, model);
    //    if (fields != null)
    //    {
    //        var names = Factory.FieldNames;
    //        foreach (var field in fields)
    //        {
    //            if (!field.Name.IsNullOrEmpty() && names.Contains(field.Name))
    //                dic[field.Name] = model[field.Name];
    //        }
    //    }

    //    return dic;
    //}

    ///// <summary>
    ///// 实体列表过滤器，根据模型列的列表页显示类型，不显示的字段去掉
    ///// </summary>
    ///// <param name="models"></param>
    ///// <param name="kind"></param>
    ///// <returns></returns>
    //protected virtual IEnumerable<IDictionary<String, Object>> OnFilter(IEnumerable<IModel> models, ViewKinds kind)
    //{
    //    if (models == null) yield break;

    //    foreach (var item in models)
    //    {
    //        yield return OnFilter(item, kind);
    //    }
    //}
    #endregion
}