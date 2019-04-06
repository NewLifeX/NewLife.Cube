using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Serialization;
using NewLife.Web;
using NewLife.Xml;
using XCode;
using XCode.Configuration;
using XCode.Membership;
using NewLife.IO;
using System.IO;
using System.Xml.Serialization;
using System.IO.Compression;
#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Net.Http.Headers;
using NewLife.Cube.Extensions;
#else
using System.Web.Mvc;
#endif

namespace NewLife.Cube
{
    /// <summary>实体控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class ReadOnlyEntityController<TEntity> : ControllerBaseX where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        /// <summary>实体工厂</summary>
        public static IEntityOperate Factory => Entity<TEntity>.Meta.Factory;

        private String CacheKey => $"CubeView_{typeof(TEntity).FullName}";
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
            var title = Entity<TEntity>.Meta.Table.DataTable.DisplayName + "管理";
            ViewBag.Title = title;
        }

        /// <summary>动作执行前</summary>
        /// <param name="filterContext"></param>
#if __CORE__
        public override void OnActionExecuting(ActionExecutingContext filterContext)
#else
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
#endif
        {
            // Ajax请求不需要设置ViewBag
            if (!Request.IsAjaxRequest())
            {
                // 默认加上实体工厂
                ViewBag.Factory = Factory;

                // 默认加上分页给前台
#if __CORE__
                var ps = filterContext.ActionArguments.ToNullable();
                var p = ps["p"] as Pager ?? new Pager();
#else
                var ps = filterContext.ActionParameters.ToNullable();
                var p = ps["p"] as Pager ?? new Pager { Params = WebHelper.Params };
#endif
                ViewBag.Page = p;

                // 用于显示的列
                if (!ps.ContainsKey("entity")) ViewBag.Fields = GetFields(false);

                if (ViewBag.HeaderTitle == null) ViewBag.HeaderTitle = Entity<TEntity>.Meta.Table.Description + "管理";

                var txt = (String)ViewBag.HeaderContent;
                if (txt.IsNullOrEmpty()) txt = (ViewBag.Menu as IMenu)?.Remark;
                if (txt.IsNullOrEmpty()) txt = GetType().GetDescription();
                if (txt.IsNullOrEmpty()) txt = Entity<TEntity>.Meta.Table.Description;
                //if (txt.IsNullOrEmpty() && SysConfig.Current.Develop)
                //    txt = "这里是页头内容，来自于菜单备注，或者给控制器增加Description特性";
                ViewBag.HeaderContent = txt;

#if !__CORE__
                var actionName = filterContext.ActionDescriptor.ActionName;
                // 启用压缩
                if (Setting.Current.EnableCompress && actionName != nameof(ExportExcel)) SetCompress();
#endif
            }

            base.OnActionExecuting(filterContext);
        }

        /// <summary>执行后</summary>
        /// <param name="filterContext"></param>
#if __CORE__
        public override void OnActionExecuted(ActionExecutedContext filterContext)
#else
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
#endif
        {
            base.OnActionExecuted(filterContext);

            var title = ViewBag.Title + "";
            HttpContext.Items["Title"] = title;
        }

#if !__CORE__
        /// <summary>触发异常时</summary>
        /// <param name="filterContext"></param>
        protected override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                var ex = filterContext.Exception;
                // Json输出
                if (IsJsonRequest)
                {
                    filterContext.Result = JsonError(ex);
                    filterContext.ExceptionHandled = true;
                }
                //else if (ex is NoPermissionException nex)
                //{
                //    filterContext.Result = this.NoPermission(nex);
                //    filterContext.ExceptionHandled = true;
                //}
            }

            base.OnException(filterContext);
        }
#endif
        #endregion

        #region 数据获取
        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected virtual IEnumerable<TEntity> Search(Pager p)
        {
            // 缓存数据，用于后续导出
            SetSession(CacheKey, p);

            return Entity<TEntity>.Search(p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p["Q"], p);
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
                        if (val != null) exp &= item.Equal(val);
                    }

                    return Entity<TEntity>.Find(exp);
                }
            }

            return Entity<TEntity>.FindByKeyForEdit(key);
        }

        /// <summary>获取选中键</summary>
        /// <returns></returns>
        protected virtual String[] SelectKeys => GetRequest("Keys").Split(",");

        /// <summary>导出当前页以后的数据</summary>
        /// <returns></returns>
        protected virtual IEnumerable<TEntity> ExportData(Int32 max = 100_000)
        {
            //// 跳过头部一些页数，导出当前页以及以后的数据
            var p = new Pager(GetSession<Pager>(CacheKey))
            {
                //p.StartRow = (p.PageIndex - 1) * p.PageSize;
                //p.PageSize = 1_000_000;
                // 不要查记录数
                //p.TotalCount = -1;
                RetrieveTotalCount = false
            };

            var size = 10_000;
            if (size > max) size = max;

            p.PageIndex = 1;
            p.PageSize = size;

            while (max > 0)
            {
                if (p.PageSize > max) p.PageSize = max;

                var list = Search(p);

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
        }
        #endregion

        #region 默认Action
        /// <summary>数据列表首页</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [DisplayName("{type}管理")]
        public virtual ActionResult Index(Pager p = null)
        {
            if (p == null) p = ViewBag.Page as Pager;

            // 缓存数据，用于后续导出
            SetSession(CacheKey, p);

            return IndexView(p);
        }

        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected virtual ActionResult IndexView(Pager p)
        {
            // 需要总记录数来分页
            p.RetrieveTotalCount = true;

            var list = Search(p);

            // Json输出
            if (IsJsonRequest) return JsonOK(list, new { pager = p });

            return View("List", list);
        }

        /// <summary>表单，查看</summary>
        /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [DisplayName("查看{type}")]
        public virtual ActionResult Detail(String id)
        {
            var entity = Find(id);
            if (entity == null || (entity as IEntity).IsNullKey) throw new XException("要查看的数据[{0}]不存在！", id);

            // 验证数据权限
            Valid(entity, DataObjectMethodType.Select, false);

            // Json输出
            if (IsJsonRequest) return JsonOK(entity, new { id });

            return FormView(entity);
        }

        /// <summary>表单页视图。子控制器可以重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual ActionResult FormView(TEntity entity)
        {
            // 用于显示的列
            ViewBag.Fields = GetFields(true);

            // 呈现表单前，保存实体对象。提交时优先使用该对象而不是去数据库查找，避免脏写
            EntityModelBinder.SetEntity(entity);

            return View("Form", entity);
        }
        #endregion

        #region 高级Action
        /// <summary>Json接口</summary>
        /// <param name="id">令牌</param>
        /// <param name="p">分页</param>
        /// <returns></returns>
        [AllowAnonymous]
        [DisplayName("Json接口")]
        public virtual ActionResult Json(String id, Pager p)
        {
            if (id.IsNullOrEmpty()) id = GetRequest("token");
            if (id.IsNullOrEmpty()) id = GetRequest("key");

            try
            {
                //var user = UserToken.Valid(id);
                var app = App.Valid(id);

                // 需要总记录数来分页
                p.RetrieveTotalCount = true;

                var list = Search(p);

                // Json输出
                return JsonOK(list, new { pager = p });
            }
            catch (Exception ex)
            {
                return JsonError(ex.GetTrue());
            }
        }

        /// <summary>Xml接口</summary>
        /// <param name="id">令牌</param>
        /// <param name="p">分页</param>
        /// <returns></returns>
        [AllowAnonymous]
        [DisplayName("Xml接口")]
        public virtual ActionResult Xml(String id, Pager p)
        {
            if (id.IsNullOrEmpty()) id = GetRequest("token");
            if (id.IsNullOrEmpty()) id = GetRequest("key");

            var xml = "";
            try
            {
                //var user = UserToken.Valid(id);
                var app = App.Valid(id);

                // 需要总记录数来分页
                p.RetrieveTotalCount = true;

                var list = Search(p) as IList<TEntity>;

                var rs = new Root { Result = false, Data = list, Pager = p };
                xml = rs.ToXml(null, false, true);
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
        }

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
#if !__CORE__
            if (xml.Length > 4 * 1024) SetCompress();
#endif

            SetAttachment(null, ".xml");

            return Content(xml, "text/xml", Encoding.UTF8);
        }

        /// <summary>要导出Xml的对象</summary>
        /// <returns></returns>
        protected virtual Object OnExportXml() => ExportData();

        /// <summary>设置附件响应方式</summary>
        /// <param name="name"></param>
        /// <param name="ext"></param>
        protected void SetAttachment(String name, String ext)
        {
            if (name.IsNullOrEmpty()) name = GetType().GetDisplayName();
            if (name.IsNullOrEmpty()) name = Factory.EntityType.GetDisplayName();
            if (name.IsNullOrEmpty()) name = Factory.Table.DataTable.DisplayName;
            if (name.IsNullOrEmpty()) name = GetType().Name.TrimEnd("Controller");
            if (!ext.IsNullOrEmpty()) ext = ext.EnsureStart(".");
            name += ext;
            name = HttpUtility.UrlEncode(name, Encoding.UTF8);

#if __CORE__
            Response.Headers.Add("Content-Disposition", "Attachment;filename=" + name);
#else
            Response.AddHeader("Content-Disposition", "Attachment;filename=" + name);
#endif
        }

        /// <summary>导出Json</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [DisplayName("导出")]
        public virtual ActionResult ExportJson()
        {
            var json = OnExportJson().ToJson(true);
#if !__CORE__
            if (json.Length > 4 * 1024) SetCompress();
#endif

            SetAttachment(null, ".json");

            return Content(json, "application/json", Encoding.UTF8);
        }

        /// <summary>要导出Json的对象</summary>
        /// <returns></returns>
        protected virtual Object OnExportJson() => ExportData();

        /// <summary>导出Excel</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [DisplayName("导出")]
        public virtual ActionResult ExportExcel()
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

            // 要导出的数据超大时，启用流式输出
            var buffer = true;
            if (Factory.Count > 100_000)
            {
                var p = new Pager(GetSession<Pager>(CacheKey))
                {
                    PageSize = 1,
                    RetrieveTotalCount = true
                };
                Search(p);

                // 超过一万行
                if (p.TotalCount > 10_000) buffer = false;
            }

            var name = GetType().GetDisplayName()
                ?? Factory.EntityType.GetDisplayName()
                ?? Factory.Table.DataTable.DisplayName
                ?? Factory.EntityType.Name;

            var fileName = "{0}_{1:yyyyMMddHHmmss}.csv".F(name, DateTime.Now);


#if __CORE__
            var rs = Response;
            var headers = rs.Headers;
            headers[HeaderNames.ContentEncoding] = "UTF8";
            headers[HeaderNames.ContentDisposition] = 
                "attachment;filename=" + HttpUtility.UrlEncode(fileName, Encoding.UTF8);
            headers[HeaderNames.ContentType] = "application/vnd.ms-excel";
            
            var data = ExportData(1_000_000);
            OnExportExcel(fs, data, rs.Body);
#else
            var rs = Response;
            rs.Charset = "UTF-8";
            rs.ContentEncoding = Encoding.UTF8;
            rs.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, Encoding.UTF8));
            rs.ContentType = "application/vnd.ms-excel";

            if (buffer) SetCompress();
            rs.Buffer = buffer;

            var data = ExportData(1_000_000);
            OnExportExcel(fs, data, rs.OutputStream);

            rs.Flush();
#endif

            //var data = ExportData(1_000_000);
            //var ms = new MemoryStream();
            //OnExportExcel(fs, data, ms);
            //ms.Position = 0;

            //return File(ms, "application/vnd.ms-excel", fileName);

            return new EmptyResult();
        }

        /// <summary>导出Excel，可重载修改要输出的列</summary>
        /// <param name="fs">字段列表</param>
        /// <param name="list">数据集</param>
        /// <param name="output">输出流</param>
        protected virtual void OnExportExcel(List<FieldItem> fs, IEnumerable<TEntity> list, Stream output)
        {
            using (var csv = new CsvFile(output))
            {
                // 列头
                var headers = new List<String>();
                foreach (var fi in fs)
                {
                    var name = fi.DisplayName;
                    if (name.IsNullOrEmpty()) name = fi.Description;
                    if (name.IsNullOrEmpty()) name = fi.Name;

                    headers.Add(name);
                }
                csv.WriteLine(headers);

                // 内容
                foreach (var item in list)
                {
                    var ds = new List<Object>();
                    foreach (var fi in fs)
                    {
                        ds.Add(item[fi.Name]);
                    }
                    csv.WriteLine(ds);
                }
            }
        }
        #endregion

        #region 模版Action
        /// <summary>生成列表</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("生成列表")]
        public ActionResult MakeList()
        {
            if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

            // 视图路径，Areas/区域/Views/控制器/_List_Data.cshtml
            var vpath = "Areas/{0}/Views/{1}/_List_Data.cshtml".F(RouteData.DataTokens["area"], GetType().Name.TrimEnd("Controller"));

            var rs = ViewHelper.MakeListView(typeof(TEntity), vpath, ListFields);

#if !__CORE__
            //Js.Alert("生成列表模版 {0} 成功！".F(vpath));
#endif

            return RedirectToAction("Index");
        }

        /// <summary>生成表单</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("生成表单")]
        public ActionResult MakeForm()
        {
            if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

            // 视图路径，Areas/区域/Views/控制器/_Form_Body.cshtml
            var vpath = "Areas/{0}/Views/{1}/_Form_Body.cshtml".F(RouteData.DataTokens["area"], GetType().Name.TrimEnd("Controller"));

            var rs = ViewHelper.MakeFormView(typeof(TEntity), vpath, FormFields);

#if !__CORE__
            //Js.Alert("生成表单模版 {0} 成功！".F(vpath));
#endif

            return RedirectToAction("Index");
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
        private static FieldCollection _ListFields;
        /// <summary>列表字段过滤</summary>
        protected static FieldCollection ListFields { get => _ListFields ?? (_ListFields = new FieldCollection(Factory).SetRelation(false)); set => _ListFields = value; }

        private static FieldCollection _FormFields;
        /// <summary>表单字段过滤</summary>
        protected static FieldCollection FormFields { get => _FormFields ?? (_FormFields = new FieldCollection(Factory).SetRelation(true)); set => _FormFields = value; }

        /// <summary>获取要显示的字段列表</summary>
        /// <param name="isForm">是否是表单</param>
        /// <returns></returns>
        protected virtual IList<FieldItem> GetFields(Boolean isForm) => (isForm ? FormFields : ListFields) ?? Entity<TEntity>.Meta.Fields.ToList();
        #endregion

        #region 权限菜单
        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; }

        /// <summary>自动从实体类拿到显示名</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            // 设置显示名
            if (menu.DisplayName.IsNullOrEmpty())
            {
                menu.DisplayName = Entity<TEntity>.Meta.Table.DataTable.DisplayName;
                menu.Visible = true;
                //menu.Save();
            }

            var dic = base.ScanActionMenu(menu);

            // 只写实体类过滤掉添删改权限
            if (Factory.Table.DataTable.InsertOnly)
            {
                var arr = new[] { PermissionFlags.Insert, PermissionFlags.Update, PermissionFlags.Delete }.Select(e => (Int32)e).ToArray();
                dic = dic.Where(e => !arr.Contains(e.Value)).ToDictionary(e => e.Key, e => e.Value);
            }

            return dic;
        }
        #endregion

        #region 辅助
        /// <summary>是否Json请求</summary>
        protected virtual Boolean IsJsonRequest
        {
            get
            {
                if (Request.ContentType.EqualIgnoreCase("application/json")) return true;

#if __CORE__
                if (Request.Headers["Accept"].Any(e => e.Split(',').Any(a => a.Trim() == "application/json"))) return true;
#else
                if (Request.AcceptTypes.Any(e => e == "application/json")) return true;
#endif

                if (GetRequest("output").EqualIgnoreCase("json")) return true;
                if ((RouteData.Values["output"] + "").EqualIgnoreCase("json")) return true;

                return false;
            }
        }
#if !__CORE__
        /// <summary>启用压缩</summary>
        protected virtual void SetCompress()
        {
            var ctx = HttpContext;
            if (ctx.Items["Compress"].ToBoolean()) return;
            ctx.Items["Compress"] = true;

            var accept = Request.Headers["Accept-Encoding"];
            if (!String.IsNullOrEmpty(accept))
            {
                var ps = accept.ToLower().Split(",", ";").Select(e => (e + "").Trim()).Where(e => !e.IsNullOrEmpty()).ToArray();
                var rs = Response;
                if (ps.Contains("deflate"))
                {
                    rs.AppendHeader("Content-encoding", "deflate");
                    rs.Filter = new DeflateStream(rs.Filter, CompressionMode.Compress, true);
                }
                else if (ps.Contains("gzip"))
                {
                    rs.AppendHeader("Content-encoding", "gzip");
                    rs.Filter = new GZipStream(rs.Filter, CompressionMode.Compress, true);
                }
            }
        }
#endif
        #endregion
    }
}