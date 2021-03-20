using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Web;
using XCode;
using XCode.Membership;
using NewLife.Log;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using NewLife.Remoting;
using System.IO;
#if __CORE__
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
#else
using System.Web.Mvc;
#endif

namespace NewLife.Cube
{
    /// <summary>实体控制器基类</summary>
    /// <typeparam name="TEntity"></typeparam>
    //[EntityAuthorize]
    public class EntityController<TEntity> : ReadOnlyEntityController<TEntity> where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        private String CacheKey => $"CubeView_{typeof(TEntity).FullName}";
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public EntityController()
        {
            PageSetting.IsReadOnly = false;
        }
        #endregion

        #region 默认Action
        /// <summary>删除</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("删除{type}")]
        public virtual ActionResult Delete(String id)
        {
#if __CORE__
            var url = Request.Headers["Referer"].FirstOrDefault() + "";
#else
            var url = Request.UrlReferrer + "";
#endif

            var entity = FindData(id);
            Valid(entity, DataObjectMethodType.Delete, true);

            try
            {
                OnDelete(entity);
            }
            catch (Exception ex)
            {
                var err = ex.GetTrue().Message;
                WriteLog("Delete", false, err);

                if (Request.IsAjaxRequest())
                    return JsonRefresh("删除失败！" + err);

                throw;
            }

            if (Request.IsAjaxRequest())
                return JsonRefresh("删除成功！");
            else if (!url.IsNullOrEmpty())
                return Redirect(url);
            else
                return RedirectToAction("Index");
        }

        /// <summary>表单，添加/修改</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [DisplayName("添加{type}")]
        public virtual ActionResult Add()
        {
            var entity = Factory.Create(true) as TEntity;

#if __CORE__
            // 填充QueryString参数
            var qs = Request.Query;
            foreach (var item in Entity<TEntity>.Meta.Fields)
            {
                var v = qs[item.Name];
                if (v.Count > 0) entity[item.Name] = v[0];
            }
#else
            // 填充QueryString参数
            var qs = Request.QueryString;
            foreach (var item in Entity<TEntity>.Meta.Fields)
            {
                var v = qs[item.Name];
                if (!v.IsNullOrEmpty()) entity[item.Name] = v;
            }
#endif

            // 验证数据权限
            Valid(entity, DataObjectMethodType.Insert, false);

            // 记下添加前的来源页，待会添加成功以后跳转
            //Session["Cube_Add_Referrer"] = Request.UrlReferrer.ToString();
#if __CORE__
            var url = Request.Headers["Referer"].FirstOrDefault() + "";
#else
            var url = Request.UrlReferrer + "";
#endif
            Session["Cube_Add_Referrer"] = url;

            // 用于显示的列
            ViewBag.Fields = AddFormFields;

            return View("AddForm", entity);
        }

        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [HttpPost]
#if __CORE__
        //[ValidateAntiForgeryToken]
#else
        [ValidateInput(false)]
#endif
        public virtual ActionResult Add(TEntity entity)
        {
            // 检测避免乱用Add/id
            if (Factory.Unique.IsIdentity && entity[Factory.Unique.Name].ToInt() != 0) throw new Exception("我们约定添加数据时路由id部分默认没有数据，以免模型绑定器错误识别！");

            if (!Valid(entity, DataObjectMethodType.Insert, true))
            {
                ViewBag.StatusMessage = "验证失败！";
                ViewBag.Fields = AddFormFields;

                return View("AddForm", entity);
            }

            var rs = false;
            var err = "";
            try
            {
                //SaveFiles(entity);

                OnInsert(entity);

                var fs = SaveFiles(entity);
                if (fs.Count > 0) OnUpdate(entity);

                if (LogOnChange) LogProvider.Provider.WriteLog("Insert", entity);

                rs = true;
            }
            catch (ArgumentException aex)
            {
                err = aex.Message;
                ModelState.AddModelError(aex.ParamName, aex.Message);
            }
            catch (Exception ex)
            {
                err = ex.Message;
                ModelState.AddModelError("", ex.Message);
            }

            if (!rs)
            {
                WriteLog("Add", false, err);

                ViewBag.StatusMessage = "添加失败！" + err;
                // 添加失败，ID清零，否则会显示保存按钮
                entity[Entity<TEntity>.Meta.Unique.Name] = 0;

                if (IsJsonRequest) return Json(500, ViewBag.StatusMessage);

                ViewBag.Fields = AddFormFields;

                return View("AddForm", entity);
            }

            ViewBag.StatusMessage = "添加成功！";

            if (IsJsonRequest) return Json(0, ViewBag.StatusMessage);

            var url = Session["Cube_Add_Referrer"] as String;
            if (!url.IsNullOrEmpty())
                return Redirect(url);
            else
                // 新增完成跳到列表页，更新完成保持本页
                return RedirectToAction("Index");
        }

        /// <summary>表单，添加/修改</summary>
        /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        [DisplayName("更新{type}")]
        public virtual ActionResult Edit(String id)
        {
            var entity = FindData(id);
            if (entity == null || (entity as IEntity).IsNullKey) throw new XException("要编辑的数据[{0}]不存在！", id);

            // 验证数据权限
            Valid(entity, DataObjectMethodType.Update, false);

            // Json输出
            if (IsJsonRequest) return Json(0, null, entity);

            ViewBag.Fields = EditFormFields;

            return View("EditForm", entity);
        }

        /// <summary>保存</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        [HttpPost]
#if __CORE__
        //[ValidateAntiForgeryToken]
#else
        [ValidateInput(false)]
#endif
        public virtual ActionResult Edit(TEntity entity)
        {
            if (!Valid(entity, DataObjectMethodType.Update, true))
            {
                ViewBag.StatusMessage = "验证失败！";
                ViewBag.Fields = EditFormFields;

                return View("EditForm", entity);
            }

            var rs = false;
            var err = "";
            try
            {
                SaveFiles(entity);

                OnUpdate(entity);

                rs = true;
            }
            catch (ArgumentException aex)
            {
                err = aex.Message;
                ModelState.AddModelError(aex.ParamName, aex.Message);
            }
            catch (Exception ex)
            {
                err = ex.Message;
                //ModelState.AddModelError("", ex.Message);
#if __CORE__
                ModelState.AddModelError("", ex.Message);
#else
                ModelState.AddModelError("", ex);
#endif
            }

            //ViewBag.RowsAffected = rs;
            if (!rs)
            {
                WriteLog("Edit", false, err);

                ViewBag.StatusMessage = "保存失败！" + err;
            }
            else
                ViewBag.StatusMessage = "保存成功！";

            if (IsJsonRequest) return Json(0, ViewBag.StatusMessage);

            ViewBag.Fields = EditFormFields;

            return View("EditForm", entity);
        }

        /// <summary>保存上传文件</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual IList<String> SaveFiles(TEntity entity)
        {
            var list = new List<String>();

#if __CORE__
            if (!Request.HasFormContentType) return list;
            var files = Request.Form.Files;
#else
            var files = Request.Files;
#endif
            var fields = Factory.Fields;
            foreach (var fi in fields)
            {
                var dc = fi.Field;
                if (dc.ItemType.EqualIgnoreCase("file", "image"))
                {
                    var f = files[dc.Name];
                    if (f != null)
                    {
                        // 保存文件
                        var ext = Path.GetExtension(f.FileName);
                        var fileName = entity[Factory.Unique] + ext;
                        fileName = $"{Factory.EntityType.Name}\\{DateTime.Today:yyyyMMdd}\\{fileName}";
                        fileName = Setting.Current.UploadPath.CombinePath(fileName).GetBasePath();
                        fileName.EnsureDirectory(true);

                        f.SaveAs(fileName);
                    }
                }
            }

            return list;
        }
        #endregion

        #region 高级Action
        /// <summary>导入Xml</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [DisplayName("导入")]
        [HttpPost]
        public virtual ActionResult ImportXml() => throw new NotImplementedException();

        /// <summary>导入Json</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [DisplayName("导入")]
        [HttpPost]
        public virtual ActionResult ImportJson() => throw new NotImplementedException();

        /// <summary>启用 或 禁用</summary>
        /// <param name="id"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Update)]
        public ActionResult SetEnable(Int32 id = 0, Boolean enable = true)
        {
            var fi = Factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase("Enable"));
            if (fi == null) throw new InvalidOperationException($"启用/禁用仅支持Enable字段。");

            var rs = 0;
            if (id > 0)
            {
                var entity = FindData(id);
                if (entity == null) throw new ArgumentNullException(nameof(id), "找不到任务 " + id);

                //entity.Enable = enable;
                entity.SetItem(fi.Name, enable);
                rs += OnUpdate(entity);
            }
            else
            {
                var ids = GetRequest("keys").SplitAsInt();

                foreach (var item in ids)
                {
                    var entity = FindData(item);
                    if (entity != null /*&& entity.Enable != enable*/)
                    {
                        //entity.Enable = enable;
                        entity.SetItem(fi.Name, enable);
                        rs += OnUpdate(entity);
                    }
                }
            }
            return JsonRefresh($"操作成功！共更新[{rs}]行！");
        }
        #endregion

        #region 批量删除
        /// <summary>删除选中</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("删除选中")]
        public virtual ActionResult DeleteSelect()
        {
            var count = 0;
            var keys = SelectKeys;
            if (keys != null && keys.Length > 0)
            {
                using var tran = Entity<TEntity>.Meta.CreateTrans();
                var list = new List<IEntity>();
                foreach (var item in keys)
                {
                    var entity = Entity<TEntity>.FindByKey(item);
                    if (entity != null)
                    {
                        // 验证数据权限
                        if (Valid(entity, DataObjectMethodType.Delete, true)) list.Add(entity);

                        count++;
                    }
                }
                list.Delete();
                tran.Commit();
            }
            return JsonRefresh($"共删除{count}行数据");
        }

        /// <summary>删除全部</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Delete)]
        [DisplayName("删除全部")]
        public virtual ActionResult DeleteAll()
        {
#if __CORE__
            var url = Request.Headers["Referer"].FirstOrDefault() + "";
#else
            var url = Request.UrlReferrer + "";
#endif

            var count = 0;
            var p = Session[CacheKey] as Pager;
            p = new Pager(p);
            if (p != null)
            {
                // 循环多次删除
                for (var i = 0; i < 10; i++)
                {
                    p.PageIndex = i + 1;
                    p.PageSize = 100_000;
                    // 不要查记录数
                    p.RetrieveTotalCount = false;

                    var list = SearchData(p).ToList();
                    if (list.Count == 0) break;

                    count += list.Count;
                    //list.Delete();
                    using var tran = Entity<TEntity>.Meta.CreateTrans();
                    var list2 = new List<IEntity>();
                    foreach (var entity in list)
                    {
                        // 验证数据权限
                        if (Valid(entity, DataObjectMethodType.Delete, true)) list2.Add(entity);
                    }
                    list2.Delete();
                    tran.Commit();
                }
            }

            if (Request.IsAjaxRequest())
                return JsonRefresh($"共删除{count}行数据");
            else if (!url.IsNullOrEmpty())
                return Redirect(url);
            else
                return RedirectToAction("Index");
        }
        #endregion

        #region 实体操作重载
        /// <summary>添加实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Int32 OnInsert(TEntity entity) => entity.Insert();

        /// <summary>更新实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Int32 OnUpdate(TEntity entity) => entity.Update();

        /// <summary>删除实体对象</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected virtual Int32 OnDelete(TEntity entity) => entity.Delete();
        #endregion

        #region 同步/还原
        /// <summary>同步数据</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [DisplayName("同步{type}")]
        public async Task<ActionResult> Sync()
        {
            //if (id.IsNullOrEmpty()) return RedirectToAction(nameof(Index));

            // 读取系统配置
            var ps = Parameter.FindAllByUserID(ManageProvider.User.ID); // UserID=0 && Category=Sync
            ps = ps.Where(e => e.Category == "Sync").ToList();
            var server = ps.FirstOrDefault(e => e.Name == "Server")?.Value;
            var token = ps.FirstOrDefault(e => e.Name == "Token")?.Value;
            var models = ps.FirstOrDefault(e => e.Name == "Models")?.Value;

            if (server.IsNullOrEmpty()) throw new ArgumentNullException("未配置 Sync:Server");
            if (token.IsNullOrEmpty()) throw new ArgumentNullException("未配置 Sync:Token");
            if (models.IsNullOrEmpty()) throw new ArgumentNullException("未配置 Sync:Models");

            var mds = models.Split(",");

            //// 创建实体工厂
            //var etype = mds.FirstOrDefault(e => e.Replace(".", "_") == id);
            //var fact = etype.GetTypeEx()?.AsFactory();
            //if (fact == null) throw new ArgumentNullException(nameof(id), "未找到模型 " + id);

            // 找到控制器，以识别动作地址
            var cs = GetControllerAction();
            var ctrl = cs[0].IsNullOrEmpty() ? cs[1] : $"{cs[0]}/{cs[1]}";
            if (!mds.Contains(ctrl)) throw new InvalidOperationException($"[{ctrl}]未配置为允许同步 Sync:Models");

            // 创建客户端，准备发起请求
            var url = server.EnsureEnd("/") + $"{ctrl}/Json/{token}?PageSize=100000";

            var http = new HttpClient
            {
                BaseAddress = new Uri(url)
            };

            var sw = Stopwatch.StartNew();

            var list = await http.InvokeAsync<TEntity[]>(HttpMethod.Get, null);

            sw.Stop();

            var fact = Factory;
            XTrace.WriteLine("[{0}]共同步数据[{1:n0}]行，耗时{2:n0}ms，数据源：{3}", fact.EntityType.FullName, list.Length, sw.ElapsedMilliseconds, url);

            var arrType = fact.EntityType.MakeArrayType();
            if (list.Length > 0)
            {
                XTrace.WriteLine("[{0}]准备覆盖写入[{1}]行数据", fact.EntityType.FullName, list.Length);
                using var tran = fact.Session.CreateTrans();

                // 清空
                try
                {
                    fact.Session.Truncate();
                }
                catch (Exception ex) { XTrace.WriteException(ex); }

                // 插入
                //ms.All(e => { e.AllChilds = new List<Menu>(); return true; });
                fact.AllowInsertIdentity = true;
                //ms.Insert();
                //var empty = typeof(List<>).MakeGenericType(fact.EntityType).CreateInstance();
                foreach (IEntity entity in list)
                {
                    if (entity is IEntityTree tree) tree.AllChilds.Clear();

                    entity.Insert();
                }
                fact.AllowInsertIdentity = false;

                tran.Commit();
            }

            return Index();
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
        #endregion
    }
}