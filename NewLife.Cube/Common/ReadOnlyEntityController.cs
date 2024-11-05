using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
/// <typeparam name="TEntity"></typeparam>
public partial class ReadOnlyEntityController<TEntity> : ControllerBaseX where TEntity : Entity<TEntity>, new()
{
    #region 构造
    /// <summary>动作执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext filterContext)
    {
        // 多选框强制使用Form提交数据，未选中时不会提交数据，但也要强行覆盖Url参数
        if (Request.HasFormContentType)
        {
            if (filterContext.ActionArguments.TryGetValue("p", out var aa) && aa is Pager p)
            {
                foreach (var item in OnGetFields(ViewKinds.Search, null))
                {
                    if (item is SearchField sf && sf.Multiple)
                    {
                        p[sf.Name] = Request.Form.TryGetValue(sf.Name, out var vs) ? (String)vs : null;
                        //// 以下写法，Form没有数据时，也会返回空字符串，而不是null
                        //p[sf.Name] = Request.Form[sf.Name];
                    }
                }
            }
        }

        base.OnActionExecuting(filterContext);
    }
    #endregion

    #region 默认Action
    /// <summary>多行数据列表</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("{type}管理")]
    [HttpGet("/[area]/[controller]")]
    public virtual ApiListResponse<TEntity> Index()
    {
        var p = new Pager(WebHelper.Params)
        {
            // 需要总记录数来分页
            RetrieveTotalCount = true
        };

        var list = SearchData(p);

        //return Json(0, null, OnFilter(list.Cast<IModel>(), ViewKinds.List).ToList(), new { pager = p, stat = p.State });
        return new ApiListResponse<TEntity>
        {
            Data = list.ToList(),
            Page = p.ToModel(),
            Stat = (TEntity)p.State,
        };
    }

    /// <summary>查看单行数据</summary>
    /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("查看{type}")]
    [HttpGet]
    public virtual ApiResponse<TEntity> Detail([Required] String id)
    {
        var entity = FindData(id);
        if (entity == null || (entity as IEntity).IsNullKey) throw new XException("要查看的数据[{0}]不存在！", id);

        // 验证数据权限
        Valid(entity, DataObjectMethodType.Select, false);

        //return Json(0, null, OnFilter(entity, ViewKinds.Detail));
        return new ApiResponse<TEntity> { Data = entity };
    }

    ///// <summary>清空全表数据</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("清空")]
    //[HttpPost]
    //public virtual ActionResult Clear()
    //{
    //    var url = Request.GetReferer();

    //    var p = Session[CacheKey] as Pager;
    //    p = new Pager(p);
    //    if (p != null && p.Params.Count > 0) return Json(500, "当前带有查询参数，为免误解，禁止全表清空！");

    //    try
    //    {
    //        var count = Entity<TEntity>.Meta.Session.Truncate();

    //        WriteLog("清空数据", true, $"共删除{count}行数据");

    //        if (Request.IsAjaxRequest())
    //            return JsonRefresh($"共删除{count}行数据");
    //        else if (!url.IsNullOrEmpty())
    //            return Redirect(url);
    //        else
    //            return RedirectToAction("Index");
    //    }
    //    catch (Exception ex)
    //    {
    //        WriteLog("清空数据", false, ex.GetMessage());

    //        throw;
    //    }
    //}
    #endregion

    #region 数据接口
    ///// <summary>Json接口</summary>
    ///// <param name="token">令牌</param>
    ///// <param name="p">分页</param>
    ///// <returns></returns>
    //[AllowAnonymous]
    //[DisplayName("Json接口")]
    //[HttpGet]
    //public virtual ActionResult Json(String token, Pager p)
    //{
    //    try
    //    {
    //        var issuer = ValidToken(token);

    //        // 需要总记录数来分页
    //        p.RetrieveTotalCount = true;

    //        var list = SearchData(p);

    //        // Json输出
    //        return Json(0, null, list, new { issuer, pager = p });
    //    }
    //    catch (Exception ex)
    //    {
    //        return Json(0, null, ex);
    //    }
    //}

    ///// <summary>验证令牌是否有效</summary>
    ///// <param name="token"></param>
    ///// <returns></returns>
    //protected virtual String ValidToken(String token)
    //{
    //    if (token.IsNullOrEmpty()) token = GetRequest("token");
    //    if (token.IsNullOrEmpty()) token = GetRequest("key");

    //    var app = App.FindBySecret(token);
    //    if (app != null)
    //    {
    //        if (!app.Enable) throw new XException("非法授权！");

    //        return app?.ToString();
    //    }
    //    else
    //    {
    //        var ut = UserToken.Valid(token, UserHost);
    //        var user = ut.User;

    //        // 定位菜单页面
    //        var menu = ManageProvider.Menu.FindByFullName(GetType().FullName);

    //        // 判断权限
    //        if (menu == null || !user.Has(menu, PermissionFlags.Detail)) throw new Exception($"该用户[{user}]无权访问[{menu}]");

    //        // 锁定页面
    //        if (!ut.Url.IsNullOrEmpty())
    //        {
    //            var url = ut.Url;
    //            if (url.Contains("?")) url = url.Substring(null, "?");
    //            if (!url.StartsWithIgnoreCase(menu.Url.TrimStart("~"))) throw new Exception($"该令牌[{user}]无权访问[{menu}]，仅限于[{url}]");
    //        }

    //        // 设置当前用户，用于数据权限控制
    //        HttpContext.Items["userId"] = user.ID;
    //        HttpContext.Items["CurrentUser"] = user;

    //        return user?.ToString();
    //    }
    //}

    ///// <summary>Xml接口</summary>
    ///// <param name="token">令牌</param>
    ///// <param name="p">分页</param>
    ///// <returns></returns>
    //[AllowAnonymous]
    //[DisplayName("Xml接口")]
    //[HttpGet]
    //public virtual ActionResult Xml(String token, Pager p)
    //{
    //    var xml = "";
    //    try
    //    {
    //        var issuer = ValidToken(token);

    //        // 需要总记录数来分页
    //        p.RetrieveTotalCount = true;

    //        var list = SearchData(p) as IList<TEntity>;

    //        var rs = new Root { Result = false, Data = list, Pager = p, Issuer = issuer };

    //        xml = rs.ToXml(null, false, false);
    //    }
    //    catch (Exception ex)
    //    {
    //        var rs = new { result = false, data = ex.GetTrue().Message };
    //        xml = rs.ToXml(null, false, false);
    //    }

    //    return Content(xml, "application/xml");
    //}

    //private class Root
    //{
    //    public Boolean Result { get; set; }
    //    public IList<TEntity> Data { get; set; }
    //    public Pager Pager { get; set; }
    //    public String Issuer { get; set; }
    //}

    ///// <summary>Csv接口</summary>
    ///// <param name="token">令牌</param>
    ///// <param name="p">分页</param>
    ///// <returns></returns>
    //[AllowAnonymous]
    //[DisplayName("Excel接口")]
    //[HttpGet]
    //public virtual async Task<ActionResult> Csv(String token, Pager p)
    //{
    //    var issuer = ValidToken(token);

    //    //// 需要总记录数来分页
    //    //p.RetrieveTotalCount = true;

    //    var list = SearchData(p);

    //    // 准备需要输出的列
    //    var fs = Factory.Fields.ToList();

    //    var rs = Response;
    //    var headers = rs.Headers;
    //    headers[HeaderNames.ContentEncoding] = "UTF8";
    //    //headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

    //    await OnExportCsv(fs, list, rs.Body);

    //    return new EmptyResult();
    //}

    ///// <summary>Csv接口</summary>
    ///// <param name="token">令牌</param>
    ///// <param name="p">分页</param>
    ///// <returns></returns>
    //[AllowAnonymous]
    //[DisplayName("Excel接口")]
    //[HttpGet]
    //public virtual async Task<ActionResult> Excel(String token, Pager p)
    //{
    //    var issuer = ValidToken(token);

    //    var list = SearchData(p);

    //    // 准备需要输出的列
    //    var fs = new List<FieldItem>();
    //    foreach (var fi in Factory.AllFields)
    //    {
    //        if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
    //        if (!fi.IsDataObjectField)
    //        {
    //            var pi = Factory.EntityType.GetProperty(fi.Name);
    //            if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
    //        }

    //        fs.Add(fi);
    //    }

    //    // 基本属性与扩展属性对调顺序
    //    for (var i = 0; i < fs.Count; i++)
    //    {
    //        var fi = fs[i];
    //        if (fi.OriField != null)
    //        {
    //            var k = fs.IndexOf(fi.OriField);
    //            if (k >= 0)
    //            {
    //                fs[i] = fs[k];
    //                fs[k] = fi;
    //            }
    //        }
    //    }

    //    var rs = Response;
    //    var headers = rs.Headers;
    //    headers[HeaderNames.ContentEncoding] = "UTF8";
    //    //headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

    //    await OnExportExcel(fs, list, rs.Body);

    //    return new EmptyResult();
    //}
    #endregion

    #region 导出Xml/Json/Excel/Csv
    ///// <summary>导出Xml</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("导出")]
    //[HttpGet]
    //public virtual ActionResult ExportXml()
    //{
    //    var obj = OnExportXml();
    //    var xml = "";
    //    if (obj is IEntity)
    //        xml = (obj as IEntity).ToXml();
    //    else if (obj is IList<TEntity>)
    //        xml = (obj as IList<TEntity>).ToXml();
    //    else if (obj is IEnumerable<TEntity> list)
    //        xml = list.ToList().ToXml();

    //    SetAttachment(null, ".xml", true);

    //    return Content(xml, "text/xml", Encoding.UTF8);
    //}

    ///// <summary>要导出Xml的对象</summary>
    ///// <returns></returns>
    //protected virtual Object OnExportXml() => ExportData();

    ///// <summary>设置附件响应方式</summary>
    ///// <param name="name"></param>
    ///// <param name="ext"></param>
    ///// <param name="includeTime">包含时间戳</param>
    //protected virtual void SetAttachment(String name, String ext, Boolean includeTime)
    //{
    //    if (name.IsNullOrEmpty()) name = GetType().GetDisplayName();
    //    if (name.IsNullOrEmpty()) name = Factory.EntityType.GetDisplayName();
    //    if (name.IsNullOrEmpty()) name = Factory.Table.DataTable.DisplayName;
    //    if (name.IsNullOrEmpty()) name = GetType().Name.TrimEnd("Controller");
    //    if (!ext.IsNullOrEmpty()) ext = ext.EnsureStart(".");

    //    if (includeTime) name += $"_{DateTime.Now:yyyyMMddHHmmss}";

    //    name += ext;
    //    name = HttpUtility.UrlEncode(name, Encoding.UTF8);

    //    Response.Headers.Add("Content-Disposition", "Attachment;filename=" + name);
    //}

    ///// <summary>导出Json</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("导出")]
    //[HttpGet]
    //public virtual ActionResult ExportJson()
    //{
    //    var json = OnExportJson().ToJson(true);

    //    SetAttachment(null, ".json", true);

    //    return Content(json, "application/json", Encoding.UTF8);
    //}

    ///// <summary>要导出Json的对象</summary>
    ///// <returns></returns>
    //protected virtual Object OnExportJson() => ExportData().ToList();

    ///// <summary>导出Excel</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("导出")]
    //[HttpGet]
    //public virtual async Task<ActionResult> ExportExcel()
    //{
    //    // 准备需要输出的列
    //    var fs = new List<FieldItem>();
    //    foreach (var fi in Factory.AllFields)
    //    {
    //        if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
    //        if (!fi.IsDataObjectField)
    //        {
    //            var pi = Factory.EntityType.GetProperty(fi.Name);
    //            if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
    //        }

    //        fs.Add(fi);
    //    }

    //    // 基本属性与扩展属性对调顺序
    //    for (var i = 0; i < fs.Count; i++)
    //    {
    //        var fi = fs[i];
    //        if (fi.OriField != null)
    //        {
    //            var k = fs.IndexOf(fi.OriField);
    //            if (k >= 0)
    //            {
    //                fs[i] = fs[k];
    //                fs[k] = fi;
    //            }
    //        }
    //    }

    //    // 要导出的数据超大时，启用流式输出
    //    if (Factory.Session.Count > 100_000)
    //    {
    //        var p = Session[CacheKey] as Pager;
    //        p = new Pager(p)
    //        {
    //            PageSize = 1,
    //            RetrieveTotalCount = true
    //        };
    //        SearchData(p);
    //    }

    //    SetAttachment(null, ".xls", true);

    //    var rs = Response;
    //    var headers = rs.Headers;
    //    headers[HeaderNames.ContentEncoding] = "UTF8";
    //    headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

    //    var data = ExportData();
    //    await OnExportExcel(fs, data, rs.Body);

    //    return new EmptyResult();
    //}

    ///// <summary>导出Excel模板</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("导出模板")]
    //[HttpGet]
    //public virtual async Task<ActionResult> ExportExcelTemplate()
    //{
    //    // 准备需要输出的列
    //    var fs = new List<FieldItem>();
    //    foreach (var fi in Factory.AllFields)
    //    {
    //        if (Type.GetTypeCode(fi.Type) == TypeCode.Object) continue;
    //        if (!fi.IsDataObjectField)
    //        {
    //            var pi = Factory.EntityType.GetProperty(fi.Name);
    //            if (pi != null && pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
    //        }

    //        //模板隐藏这几个字段
    //        if (fi.Name.EqualIgnoreCase("CreateUserID", "CreateUser", "CreateTime", "CreateIP",
    //                    "UpdateUserID", "UpdateUser", "UpdateTime", "UpdateIP", "Enable") || fi.Description.IsNullOrEmpty())
    //        {
    //            continue;
    //        }

    //        fs.Add(fi);
    //    }

    //    // 基本属性与扩展属性对调顺序
    //    for (var i = 0; i < fs.Count; i++)
    //    {
    //        var fi = fs[i];
    //        if (fi.OriField != null)
    //        {
    //            var k = fs.IndexOf(fi.OriField);
    //            if (k >= 0)
    //            {
    //                fs[i] = fs[k];
    //                fs[k] = fi;
    //            }
    //        }
    //    }

    //    // 要导出的数据超大时，启用流式输出
    //    if (Factory.Session.Count > 100_000)
    //    {
    //        var p = Session[CacheKey] as Pager;
    //        p = new Pager(p)
    //        {
    //            PageSize = 1,
    //            RetrieveTotalCount = true
    //        };
    //        SearchData(p);
    //    }

    //    SetAttachment(null, ".xls", true);

    //    var rs = Response;
    //    var headers = rs.Headers;
    //    headers[HeaderNames.ContentEncoding] = "UTF8";
    //    headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

    //    var data = ExportData(1);
    //    await OnExportExcel(fs, data, rs.Body);

    //    return new EmptyResult();
    //}

    ///// <summary>导出Excel，可重载修改要输出的列</summary>
    ///// <param name="fs">字段列表</param>
    ///// <param name="list">数据集</param>
    ///// <param name="output">输出流</param>
    //protected virtual async ValueTask OnExportExcel(List<FieldItem> fs, IEnumerable<TEntity> list, Stream output)
    //{
    //    await using var csv = new CsvFile(output, true);

    //    // 列头
    //    var headers = new List<String>();
    //    foreach (var fi in fs)
    //    {
    //        var name = fi.DisplayName;
    //        if (name.IsNullOrEmpty()) name = fi.Description;
    //        if (name.IsNullOrEmpty()) name = fi.Name;

    //        // 第一行以ID开头的csv文件，容易被识别为SYLK文件
    //        if (name == "ID" && fi == fs[0]) name = "Id";
    //        headers.Add(name);
    //    }
    //    await csv.WriteLineAsync(headers);

    //    // 内容
    //    foreach (var entity in list)
    //    {
    //        await csv.WriteLineAsync(fs.Select(e => entity[e.Name]));
    //    }
    //}

    ///// <summary>导出Csv</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("导出")]
    //[HttpGet]
    //public virtual async Task<ActionResult> ExportCsv()
    //{
    //    // 准备需要输出的列
    //    var fs = Factory.Fields.ToList();

    //    if (Factory.Session.Count > 100_000)
    //    {
    //        var p = Session[CacheKey] as Pager;
    //        p = new Pager(p)
    //        {
    //            PageSize = 1,
    //            RetrieveTotalCount = true
    //        };
    //        SearchData(p);
    //    }

    //    var name = GetType().Name.TrimEnd("Controller");
    //    SetAttachment(name, ".csv", true);

    //    var rs = Response;
    //    var headers = rs.Headers;
    //    headers[HeaderNames.ContentEncoding] = "UTF8";
    //    headers[HeaderNames.ContentType] = "application/vnd.ms-excel";

    //    //// 允许同步IO，便于CsvFile刷数据Flush
    //    //var ft = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
    //    //if (ft != null) ft.AllowSynchronousIO = true;

    //    var data = ExportData();
    //    await OnExportCsv(fs, data, rs.Body);

    //    return new EmptyResult();
    //}

    ///// <summary>导出Csv，可重载修改要输出的列</summary>
    ///// <param name="fs">字段列表</param>
    ///// <param name="list">数据集</param>
    ///// <param name="output">输出流</param>
    //protected virtual async ValueTask OnExportCsv(List<FieldItem> fs, IEnumerable<TEntity> list, Stream output)
    //{
    //    await using var csv = new CsvFile(output, true);

    //    // 列头
    //    var headers = fs.Select(e => e.Name).ToArray();
    //    if (headers[0] == "ID") headers[0] = "Id";
    //    await csv.WriteLineAsync(headers);

    //    // 内容
    //    foreach (var entity in list)
    //    {
    //        await csv.WriteLineAsync(fs.Select(e => entity[e.Name]));
    //    }
    //}
    #endregion

    #region 备份/还原/导出/分享
    ///// <summary>备份到服务器本地目录</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("备份")]
    //[HttpGet]
    //public virtual ActionResult Backup()
    //{
    //    try
    //    {
    //        var fact = Factory;
    //        if (fact.Session.Count > 10_000_000) throw new XException($"数据量[{fact.Session.Count:n0}>10_000_000]，禁止备份！");

    //        var dal = fact.Session.Dal;

    //        var name = GetType().Name.TrimEnd("Controller");
    //        var fileName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}.gz";
    //        var bak = NewLife.Setting.Current.BackupPath.CombinePath(fileName).GetBasePath();
    //        bak.EnsureDirectory(true);

    //        var rs = dal.Backup(fact.Table.DataTable, bak);

    //        WriteLog("备份", true, $"备份[{fileName}]（{rs:n0}行）成功！");

    //        return Json(0, $"备份[{fileName}]（{rs:n0}行）成功！");
    //    }
    //    catch (Exception ex)
    //    {
    //        XTrace.WriteException(ex);

    //        WriteLog("备份", false, ex.GetMessage());

    //        return Json(500, null, ex);
    //    }
    //}

    ///// <summary>备份导出</summary>
    ///// <remarks>备份并下载</remarks>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("导出")]
    //[HttpGet]
    //public virtual async Task<ActionResult> BackupAndExport()
    //{
    //    var fact = Factory;
    //    if (fact.Session.Count > 10_000_000) throw new XException($"数据量[{fact.Session.Count:n0}>10_000_000]，禁止备份！");

    //    var dal = fact.Session.Dal;

    //    var name = GetType().Name.TrimEnd("Controller");
    //    SetAttachment(name, ".gz", true);

    //    // 允许同步IO，便于刷数据Flush
    //    var ft = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
    //    if (ft != null) ft.AllowSynchronousIO = true;

    //    var ms = Response.Body;
    //    try
    //    {
    //        await using var gs = new GZipStream(ms, CompressionLevel.Optimal, true);
    //        var count = dal.Backup(fact.Table.DataTable, gs);

    //        WriteLog("备份导出", true, $"备份[{name}]（{count:n0}行）成功！");

    //        return new EmptyResult();
    //    }
    //    catch (Exception ex)
    //    {
    //        XTrace.WriteException(ex);

    //        WriteLog("备份导出", false, ex.GetMessage());

    //        return Json(500, null, ex);
    //    }
    //}

    ///// <summary>分享数据</summary>
    ///// <remarks>
    ///// 为当前url创建用户令牌
    ///// </remarks>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("分享{type}")]
    //[HttpGet]
    //public virtual ActionResult Share()
    //{
    //    // 当前用户所有令牌
    //    var userId = ManageProvider.User.ID;
    //    var list = UserToken.Search(null, userId, true, DateTime.Now, DateTime.MinValue, null);

    //    var p = Session[CacheKey] as Pager;
    //    p = new Pager(p)
    //    {
    //        RetrieveTotalCount = false,
    //    };

    //    // 构造url
    //    var cs = GetControllerAction();
    //    var url = cs[0].IsNullOrEmpty() ? $"/{cs[1]}" : $"/{cs[0]}/{cs[1]}";
    //    var sb = p.GetBaseUrl(true, true, true);
    //    if (sb.Length > 0) url += "?" + sb;

    //    // 如果该url已存在，则延长有效期
    //    var ut = list.FirstOrDefault(e => e.Url.EqualIgnoreCase(url));
    //    ut ??= new UserToken { UserID = userId, Url = url };

    //    if (ut.Token.IsNullOrEmpty()) ut.Token = Rand.NextString(8);
    //    ut.Enable = true;
    //    ut.Expire = DateTime.Now.AddSeconds(Setting.Current.ShareExpire);
    //    ut.Save();

    //    //var url2 = $"/Admin/UserToken?q={ut.Token}";

    //    //return Json(0, "分享成功！" + url, null, new { url = url2, time = 3 });

    //    return RedirectToAction("Index", "UserToken", new { area = "Admin", q = ut.Token });
    //}
    #endregion

    #region 模版Action
    ///// <summary>生成列表</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("生成列表")]
    //[HttpGet]
    //public ActionResult MakeList()
    //{
    //    if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

    //    // 找到项目根目录
    //    var root = GetProjectRoot();

    //    // 视图路径，Areas/区域/Views/控制器/_List_Data.cshtml
    //    var cs = GetControllerAction();
    //    var vpath = $"Areas/{cs[0]}/Views/{cs[1]}/_List_Data.cshtml";
    //    if (!root.IsNullOrEmpty()) vpath = root.EnsureEnd("/") + vpath;

    //    _ = ViewHelper.MakeListView(typeof(TEntity), vpath, ListFields);

    //    WriteLog("生成列表", true, vpath);

    //    return RedirectToAction("Index");
    //}

    ///// <summary>生成表单</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("生成表单")]
    //[HttpGet]
    //public ActionResult MakeForm()
    //{
    //    if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

    //    // 找到项目根目录
    //    var root = GetProjectRoot();

    //    // 视图路径，Areas/区域/Views/控制器/_Form_Body.cshtml
    //    var cs = GetControllerAction();
    //    var vpath = $"Areas/{cs[0]}/Views/{cs[1]}/_Form_Body.cshtml";
    //    if (!root.IsNullOrEmpty()) vpath = root.EnsureEnd("/") + vpath;

    //    _ = ViewHelper.MakeFormView(typeof(TEntity), vpath, EditFormFields);

    //    WriteLog("生成表单", true, vpath);

    //    return RedirectToAction("Index");
    //}

    ///// <summary>生成搜索</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Detail)]
    //[DisplayName("生成搜索")]
    //[HttpGet]
    //public ActionResult MakeSearch()
    //{
    //    if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

    //    // 找到项目根目录
    //    var root = GetProjectRoot();

    //    // 视图路径，Areas/区域/Views/控制器/_List_Search.cshtml
    //    var cs = GetControllerAction();
    //    var vpath = $"Areas/{cs[0]}/Views/{cs[1]}/_List_Search.cshtml";
    //    if (!root.IsNullOrEmpty()) vpath = root.EnsureEnd("/") + vpath;

    //    _ = ViewHelper.MakeSearchView(typeof(TEntity), vpath, ListFields);

    //    WriteLog("生成搜索", true, vpath);

    //    return RedirectToAction("Index");
    //}

    //private String GetProjectRoot()
    //{
    //    var asm = GetType().Assembly;
    //    var name = asm.GetName().Name;

    //    // core程序出现这种情况：bin/Debug/netcoreapp3.1
    //    // 因此添加"../../../" 
    //    var ps = new[] { "./", "../../", "../../" + name, "../../../", "../../../" + name };
    //    String err = null;
    //    foreach (var item in ps)
    //    {
    //        var dir = item.AsDirectory();
    //        err += dir + "；";
    //        if (!dir.Exists) continue;
    //        var fis = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
    //        if (fis != null && fis.Length > 0) return item;
    //    }

    //    // 找不到项目根目录，就用当前目录，因为某些项目文件名和输出名可能不一致
    //    return "./";

    //    //err = $"遍历以下路径均找不到项目路径，请检查项目路径：{err}";
    //    //throw new InvalidOperationException(err);
    //}
    #endregion

    #region 列表字段和表单字段
    /// <summary>获取字段信息。支持用户重载并根据上下文定制界面</summary>
    /// <param name="kind">字段类型：1-列表List、2-详情Detail、3-添加AddForm、4-编辑EditForm、5-搜索Search</param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public virtual List<DataField> GetFields(ViewKinds kind) => OnGetFields(kind, null);
    #endregion
}