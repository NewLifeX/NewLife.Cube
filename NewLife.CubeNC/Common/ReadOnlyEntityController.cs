using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Common;
using NewLife.Cube.Charts;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Results;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
using NewLife.Xml;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>只读实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public partial class ReadOnlyEntityController<TEntity> : ControllerBaseX where TEntity : Entity<TEntity>, new()
{
    #region 构造
    /// <summary>动作执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var ps = filterContext.ActionArguments.ToNullable();
        var p = ps["p"] as Pager ?? new Pager(WebHelper.Params);

        // 多选框强制使用Form提交数据，未选中时不会提交数据，但也要强行覆盖Url参数
        if (Request.HasFormContentType)
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

        var title = GetType().GetDisplayName() ?? typeof(TEntity).GetDisplayName() ?? Factory.Table.DataTable.DisplayName;
        ViewBag.Title = title;

        // Ajax请求不需要设置ViewBag
        if (!Request.IsAjaxRequest())
        {
            // 默认加上实体工厂
            ViewBag.Factory = Factory;

            // 默认加上分页给前台
            ViewBag.Page = p;

            // 用于显示的列
            // 注释掉后会导致分享的页面报错。2023-09-15 取消注释
            if (!ps.ContainsKey("entity")) ViewBag.Fields = OnGetFields(ViewKinds.List, null);

            var txt = (String)ViewBag.HeaderContent;
            if (txt.IsNullOrEmpty()) txt = Menu?.Remark;
            if (txt.IsNullOrEmpty()) txt = GetType().GetDescription();
            if (txt.IsNullOrEmpty()) txt = Factory.Table.Description;
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
        ViewBag.SearchFields = OnGetFields(ViewKinds.Search, list);

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
    [NonAction]
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
            return new ExceptionResult { Exception = ex };
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

    /// <summary>尝试验证令牌</summary>
    /// <param name="token"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    protected Boolean TryValidToken(String token, out ActionResult result)
    {
        result = null;
        try
        {
            ValidToken(token);

            return true;
        }
        catch (Exception ex)
        {
            result = new ExceptionResult { Exception = ex };

            return false;
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
    public virtual IActionResult Csv(String token, Pager p)
    {
        try
        {
            var issuer = ValidToken(token);

            var list = SearchData(p);

            return new CsvResult { Fields = GetFields(Factory.Fields, list), Data = list, ContentType = null };
        }
        catch (Exception ex)
        {
            return new ExceptionResult { Exception = ex };
        }
    }

    /// <summary>Csv接口</summary>
    /// <param name="token">令牌</param>
    /// <param name="p">分页</param>
    /// <returns></returns>
    [AllowAnonymous]
    [DisplayName("Excel接口")]
    public virtual IActionResult Excel(String token, Pager p)
    {
        try
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

            return new ExcelResult { Fields = GetFields(fs, list), Data = list };
        }
        catch (Exception ex)
        {
            return new ExceptionResult { Exception = ex };
        }
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
        name = GetAttachment(name, ext, includeTime);
        name = HttpUtility.UrlEncode(name, Encoding.UTF8);

        Response.Headers.Add("Content-Disposition", "Attachment;filename=" + name);
    }

    /// <summary>获取附件响应方式</summary>
    /// <param name="name"></param>
    /// <param name="ext"></param>
    /// <param name="includeTime">包含时间戳</param>
    protected virtual String GetAttachment(String name, String ext, Boolean includeTime)
    {
        if (name.IsNullOrEmpty()) name = GetType().GetDisplayName();
        if (name.IsNullOrEmpty()) name = Factory.EntityType.GetDisplayName();
        if (name.IsNullOrEmpty()) name = Factory.Table.DataTable.DisplayName;
        if (name.IsNullOrEmpty()) name = GetType().Name.TrimEnd("Controller");
        if (!ext.IsNullOrEmpty()) ext = ext.EnsureStart(".");

        if (includeTime) name += $"_{DateTime.Now:yyyyMMddHHmmss}";

        name += ext;

        return name;
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
    public virtual IActionResult ExportExcel()
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

        var name = GetAttachment(null, ".xls", true);

        var list = ExportData();

        return new ExcelResult { Fields = GetFields(fs, list), Data = list, AttachmentName = name };
    }

    /// <summary>导出Excel模板</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出模板")]
    public virtual IActionResult ExportExcelTemplate()
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

        var name = GetAttachment(null, ".xls", true);

        var list = ExportData(1);

        return new ExcelResult { Fields = GetFields(fs, list), Data = list, AttachmentName = name };
    }

    /// <summary>导出Csv</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("导出")]
    public virtual IActionResult ExportCsv()
    {
        var name = GetType().Name.TrimEnd("Controller");
        name = GetAttachment(name, ".csv", true);

        var list = ExportData();

        return new CsvResult { Fields = GetFields(Factory.Fields, list), Data = list, AttachmentName = name };
    }

    /// <summary>准备需要输出的列，包括IExtend属性</summary>
    /// <param name="fs"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    private IList<DataField> GetFields(IList<FieldItem> fs, IEnumerable<IModel> list)
    {
        var fields = fs.Select(e => new DataField(e)).ToList();
        if (list.FirstOrDefault() is IExtend ext)
        {
            foreach (var item in ext.Items)
            {
                if (!fields.Any(e => e.Name.EqualIgnoreCase(item.Key)))
                {
                    fields.Add(new DataField { Name = item.Key, Type = item.Value?.GetType(), });
                }
            }
        }

        return fields;
    }
    #endregion

    #region 高级Action
    /// <summary>高级开发接口</summary>
    /// <param name="act"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("高级开发")]
    public virtual async Task<ActionResult> Develop(String act)
    {
        if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

        var user = ManageProvider.User;
        if (user == null || !user.Roles.Any(e => e.IsSystem)) throw new InvalidOperationException("仅支持系统管理员使用！");

        return act switch
        {
            "Backup" => Backup(),
            "BackupAndExport" => await BackupAndExport(),
            "Restore" => Restore(),
            "Share" => Share(),
            "Clear" => Clear(),
            "MakeList" => MakeList(),
            "MakeForm" => MakeForm(),
            "MakeSearch" => MakeSearch(),
            "MakeBatch" => MakeBatch(),
            _ => throw new NotSupportedException($"未支持[{act}]"),
        };
    }
    #endregion

    #region 备份/还原/导出/分享
    /// <summary>备份到服务器本地目录</summary>
    /// <returns></returns>
    [NonAction]
    public virtual async Task<ActionResult> Backup()
    {
        try
        {
            var set = CubeSetting.Current;

            var fact = Factory;
            if (fact.Session.Count > set.MaxBackup)
                throw new XException($"数据量[{fact.Session.Count:n0}>{set.MaxBackup:n0}]，禁止备份！");

            var dal = fact.Session.Dal;

            var name = GetType().Name.TrimEnd("Controller");
            var fileName = $"{name}_{DateTime.Now:yyyyMMddHHmmss}.gz";
            var bak = NewLife.Setting.Current.BackupPath.CombinePath(fileName).GetBasePath();
            bak.EnsureDirectory(true);

            WriteLog("备份", true, $"开始备份[{name}]到[{fileName}]");

            var sw = Stopwatch.StartNew();
            await using var fs = new FileStream(bak, FileMode.OpenOrCreate);
            await using var gs = new GZipStream(fs, CompressionLevel.Optimal, true);
            var rs = dal.Backup(fact.Table.DataTable, gs, HttpContext.RequestAborted);
            sw.Stop();

            WriteLog("备份", true, $"备份[{name}]到[{fileName}]（{rs:n0}行）成功！耗时：{sw.Elapsed}");

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
    [NonAction]
    public virtual async Task<ActionResult> BackupAndExport()
    {
        var set = CubeSetting.Current;

        var fact = Factory;
        if (fact.Session.Count > set.MaxBackup)
            throw new XException($"数据量[{fact.Session.Count:n0}>{set.MaxBackup:n0}]，禁止备份！");

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
            var count = dal.Backup(fact.Table.DataTable, gs, HttpContext.RequestAborted);

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
    [NonAction]
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

            using var fs = fi.OpenRead();
            var rs = dal.Restore(fs, fact.Table.DataTable, HttpContext.RequestAborted);

            WriteLog("恢复", true, $"恢复[{fileName}]（{rs:n0}行）成功！");

            return JsonRefresh($"恢复[{fileName}]（{rs:n0}行）成功！", 2);
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
    [NonAction]
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
    [NonAction]
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
    [NonAction]
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
    [NonAction]
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

    /// <summary>生成批处理</summary>
    /// <returns></returns>
    [NonAction]
    public ActionResult MakeBatch()
    {
        if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

        // 找到项目根目录
        var root = GetProjectRoot();

        // 视图路径，Areas/区域/Views/控制器/_List_Toolbar_Batch.cshtml
        var cs = GetControllerAction();
        var vpath = $"Areas/{cs[0]}/Views/{cs[1]}/_List_Toolbar_Batch.cshtml";
        if (!root.IsNullOrEmpty()) vpath = root.EnsureEnd("/") + vpath;

        _ = ViewHelper.MakeBatchView(typeof(TEntity), vpath, OnGetFields(ViewKinds.List, null));

        WriteLog("生成批处理", true, vpath);

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

    #region 列表字段和表单字段
    /// <summary>获取字段信息。支持用户重载并根据上下文定制界面</summary>
    /// <param name="kind">字段类型：1-列表List、2-详情Detail、3-添加AddForm、4-编辑EditForm、5-搜索Search</param>
    /// <returns></returns>
    [AllowAnonymous]
    public virtual ActionResult GetFields(ViewKinds kind)
    {
        var fields = OnGetFields(kind, null);

        Object data = new { code = 0, message = "", data = fields };

        return new JsonResult(data);
    }
    #endregion

    #region 图表
    /// <summary>快捷添加图表</summary>
    /// <param name="data">数据集</param>
    /// <param name="xAxis">X轴。多X轴可独立设置</param>
    /// <param name="yAxis">Y轴。多Y轴可独立设置</param>
    /// <param name="yFields">数据字段。为每个数据字段绘制系列</param>
    /// <param name="seriesType">系列类型</param>
    /// <param name="position">位置。默认top，可选bottom</param>
    /// <returns></returns>
    [NonAction]
    public virtual ECharts AddChart(IList<TEntity> data, DataField xAxis, String yAxis = null, DataField[] yFields = null, SeriesTypes seriesType = SeriesTypes.Line, String position = "top")
    {
        var chart = new ECharts
        {
            Height = 400,
        };

        // X轴
        if (xAxis != null)
        {
            chart.SetX(data, xAxis);

            if (xAxis.Type == typeof(DateTime)) chart.AddDataZoom();
        }

        // Y轴
        if (!yAxis.IsNullOrEmpty()) chart.SetY(yAxis);

        // 数据图例
        if (yFields != null && yFields.Length > 0)
        {
            // 饼图需要分类，来自X轴
            if (seriesType == SeriesTypes.Pie)
            {
                foreach (var field in yFields)
                {
                    field.DisplayName = xAxis.DisplayName;
                    if (field.Category.IsNullOrEmpty()) field.Category = xAxis.Name;
                }
            }

            var ss = chart.Add(data, yFields, seriesType);

            // 第一条曲线，标记最大最小点和平均线
            if (ss != null && ss.Count > 0 && seriesType == SeriesTypes.Line)
            {
                if (ss[0] is SeriesLine line)
                {
                    line.SetMarkLine(true);
                    line.SetMarkPoint(true, true);
                }
            }

            // 如果没有Y轴，自动补上
            if (yFields.Length > 0 && (chart.YAxis == null || chart.YAxis.Count == 0))
                chart.SetY(yFields[0].Name, "value");
        }

        // Y轴最小值自动使用数据最小值
        var axis = chart.YAxis.FirstOrDefault();
        if (axis != null)
        {
            if (axis.Type == "value") axis.Min = "dataMin";
        }

        if (seriesType == SeriesTypes.Pie)
            chart.SetTooltip("item", null, null);
        else
            chart.SetTooltip();

        if (position.IsNullOrEmpty() || position == "top")
        {
            var charts = ViewBag.Charts as IList<ECharts> ?? [];
            charts.Add(chart);
            ViewBag.Charts = charts;
        }
        else
        {
            var charts = ViewBag.Charts2 as IList<ECharts> ?? [];
            charts.Add(chart);
            ViewBag.Charts2 = charts;
        }

        return chart;
    }
    #endregion
}