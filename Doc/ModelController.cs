using System.ComponentModel;
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
using NewLife.Data;
using NewLife.IO;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
using NewLife.Xml;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>模型控制器基类。不依赖于数据库实体</summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class ModelController<TEntity> : ControllerBaseX
{
    #region 属性
    /// <summary>系统配置</summary>
    public SysConfig SysConfig { get; set; }

    /// <summary>当前列表页的查询条件缓存Key</summary>
    private static String CacheKey => $"CubeView_{typeof(TEntity).FullName}";

    private static String _fullName = typeof(TEntity).FullName;
    /// <summary>显示名</summary>
    protected static String _displayName = null;
    /// <summary>描述</summary>
    protected static String _description = null;
    private static DataField[] _fields = null;
    #endregion

    #region 构造
    static ModelController()
    {
    }

    /// <summary>构造函数</summary>
    public ModelController()
    {
        PageSetting.IsReadOnly = true;
        PageSetting.EnableTableDoubleClick = Setting.Current.EnableTableDoubleClick;

        SysConfig = SysConfig.Current;

        // 初始化显示名和备注
        if (_description == null)
        {
            var des = GetType().GetDescription() ?? typeof(TEntity).GetDescription();
            _description = des + "";
        }
        if (_displayName == null)
        {
            var dis = GetType().GetDisplayName() ?? typeof(TEntity).GetDisplayName();
            if (dis.IsNullOrEmpty())
            {
                var des = GetType().GetDescription() ?? typeof(TEntity).GetDescription();
                if (!des.IsNullOrEmpty())
                {
                    var p = des.IndexOfAny(new[] { '。', '，' });
                    if (p > 0) des = des[..p];

                    dis = des;
                }
            }
            _displayName = dis + "";
        }
    }

    /// <summary>动作执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        //var title = GetType().GetDisplayName() ?? typeof(TEntity).GetDisplayName() ?? Entity<TEntity>.Meta.Table.DataTable.DisplayName;
        ViewBag.Title = _displayName;

        // Ajax请求不需要设置ViewBag
        if (!Request.IsAjaxRequest())
        {
            // 默认加上分页给前台
            var ps = filterContext.ActionArguments.ToNullable();
            var p = ps["p"] as Pager ?? new Pager();
            ViewBag.Page = p;

            // 用于显示的列
            if (!ps.ContainsKey("entity")) ViewBag.Fields = ListFields;

            var txt = (String)ViewBag.HeaderContent;
            if (txt.IsNullOrEmpty()) txt = Menu?.Remark;
            if (txt.IsNullOrEmpty()) txt = _description;
            //if (txt.IsNullOrEmpty()) txt = Entity<TEntity>.Meta.Table.Description;

            ViewBag.HeaderContent = txt;
        }

        base.OnActionExecuting(filterContext);
    }
    #endregion

    #region 数据获取
    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected abstract IEnumerable<TEntity> Search(Pager p);

    /// <summary>搜索数据，支持数据权限</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> SearchData(Pager p) => Search(p);

    /// <summary>查找单行数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected abstract TEntity Find(Object key);

    /// <summary>查找单行数据，并判断数据权限</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual TEntity FindData(Object key) => Find(key);

    /// <summary>获取选中键</summary>
    /// <returns></returns>
    protected virtual String[] SelectKeys => GetRequest("Keys").Split(",");

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

                XTrace.WriteLine("[{0}]导出数据[{1:n0}]，时间区间（{2},{3}），分片步进{4:n0}秒", _fullName, p.TotalCount, start, end, step);

                return ExportDataByDatetime((Int32)step, max);
            }
        }

        XTrace.WriteLine("[{0}]导出数据[{1:n0}]，共[{2:n0}]页", _fullName, p.TotalCount, p.PageCount);

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

        var rs = Response;
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

        var rs = Response;
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

    /// <summary>获取字段集合</summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    protected virtual IList<DataField> GetFields(String kind)
    {
        if (_fields != null) return _fields;

        var fields = new List<DataField>();

        foreach (var pi in typeof(TEntity).GetProperties(true))
        {
            var df = new DataField
            {
                Name = pi.Name,
                DisplayName = pi.GetDisplayName(),
                Description = pi.GetDescription(),
                Type = pi.PropertyType,
            };
            fields.Add(df);
        }

        return _fields = fields.ToArray();
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

        // Json输出
        //if (IsJsonRequest) return Json(0, null, EntitiesFilter(list), new { pager = p });
        if (IsJsonRequest) return Json(0, null, list, new { pager = p });

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
        //if (IsJsonRequest) return Json(0, null, EntityFilter(entity, ShowInForm.详情));
        if (IsJsonRequest) return Json(0, null, entity);

        // 用于显示的列
        ViewBag.Fields = DetailFields;

        return View("Detail", entity);
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
            return Json(0, null, list, new { issuer, pager = p });
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
        var fs = GetFields("Csv");

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
        var fs = GetFields("Excel");
        //var fs = new List<FieldItem>();
        //foreach (var fi in Factory.AllFields)
        //{
        //    if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
        //    if (!fi.IsDataObjectField)
        //    {
        //        var pi = Factory.EntityType.GetProperty(fi.Name);
        //        if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
        //    }

        //    fs.Add(fi);
        //}

        //// 基本属性与扩展属性对调顺序
        //for (var i = 0; i < fs.Count; i++)
        //{
        //    var fi = fs[i];
        //    if (fi.OriField != null)
        //    {
        //        var k = fs.IndexOf(fi.OriField);
        //        if (k >= 0)
        //        {
        //            fs[i] = fs[k];
        //            fs[k] = fi;
        //        }
        //    }
        //}

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
        if (name.IsNullOrEmpty()) name = _displayName;
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
        var fs = GetFields("Excel");
        //var fs = new List<FieldItem>();
        //foreach (var fi in Factory.AllFields)
        //{
        //    if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
        //    if (!fi.IsDataObjectField)
        //    {
        //        var pi = Factory.EntityType.GetProperty(fi.Name);
        //        if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
        //    }

        //    fs.Add(fi);
        //}

        //// 基本属性与扩展属性对调顺序
        //for (var i = 0; i < fs.Count; i++)
        //{
        //    var fi = fs[i];
        //    if (fi.OriField != null)
        //    {
        //        var k = fs.IndexOf(fi.OriField);
        //        if (k >= 0)
        //        {
        //            fs[i] = fs[k];
        //            fs[k] = fi;
        //        }
        //    }
        //}

        //// 要导出的数据超大时，启用流式输出
        //if (Factory.Session.Count > 100_000)
        //{
        //    var p = Session[CacheKey] as Pager;
        //    p = new Pager(p)
        //    {
        //        PageSize = 1,
        //        RetrieveTotalCount = true
        //    };
        //    SearchData(p);
        //}

        SetAttachment(null, ".xls", true);

        var rs = Response;
        var headers = rs.Headers;
        headers[HeaderNames.ContentEncoding] = "UTF8";
        headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

        var data = ExportData();
        await OnExportExcel(fs, data, rs.Body);

        return new EmptyResult();
    }

    /// <summary>导出Excel，可重载修改要输出的列</summary>
    /// <param name="fs">字段列表</param>
    /// <param name="list">数据集</param>
    /// <param name="output">输出流</param>
    protected virtual async ValueTask OnExportExcel(IEnumerable<DataField> fs, IEnumerable<IExtend> list, Stream output)
    {
        await using var csv = new CsvFile(output, true);

        // 列头
        var headers = new List<String>();
        var idx = 0;
        foreach (var fi in fs)
        {
            var name = fi.DisplayName;
            if (name.IsNullOrEmpty()) name = fi.Description;
            if (name.IsNullOrEmpty()) name = fi.Name;

            // 第一行以ID开头的csv文件，容易被识别为SYLK文件
            if (name == "ID" && idx++ == 0) name = "Id";
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
        var fs = GetFields("Csv");
        //var fs = Factory.Fields.ToList();

        //if (Factory.Session.Count > 100_000)
        //{
        //    var p = Session[CacheKey] as Pager;
        //    p = new Pager(p)
        //    {
        //        PageSize = 1,
        //        RetrieveTotalCount = true
        //    };
        //    SearchData(p);
        //}

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
    protected virtual async ValueTask OnExportCsv(IEnumerable<DataField> fs, IEnumerable<IExtend> list, Stream output)
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
        ut.Expire = DateTime.Now.AddSeconds(Setting.Current.ShareExpire);
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

        var rs = ViewHelper.MakeListView(typeof(TEntity), vpath, ListFields);

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

        var rs = ViewHelper.MakeFormView(typeof(TEntity), vpath, DetailFields);

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

        var rs = ViewHelper.MakeSearchView(typeof(TEntity), vpath, ListFields);

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
    /// <summary>列表字段过滤</summary>
    protected static FieldCollection ListFields { get; set; } = new FieldCollection(null, "List");

    /// <summary>表单字段过滤</summary>
    protected static FieldCollection DetailFields { get; set; } = new FieldCollection(null, "Detail");
    #endregion
}