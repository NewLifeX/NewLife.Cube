using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>实体控制器基类</summary>
public partial class EntityController<TEntity, TModel>
{
    #region 默认Action
    /// <summary>删除</summary>
    /// <param name="id">主键</param>
    /// <returns>删除后重定向到列表页</returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("删除{type}")]
    public virtual ActionResult Delete(String id)
    {
        var act = "删除";
        var entity = FindData(id);
        try
        {
            act = ProcessDelete(entity);

            if (Request.IsAjaxRequest()) return JsonRefresh($"{act}成功！");
        }
        catch (Exception ex)
        {
            DefaultSpan.Current?.SetError(ex);
            var err = ex.GetTrue().Message;
            WriteLog("Delete", false, err);

            if (Request.IsAjaxRequest())
                return JsonRefresh($"{act}失败！{err}");

            throw;
        }

        var url = Request.GetReferer();
        if (!url.IsNullOrEmpty()) return Redirect(url);

        return RedirectToAction("Index");
    }

    /// <summary>表单，添加/修改</summary>
    /// <returns>新增表单视图</returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [DisplayName("添加{type}")]
    public virtual ActionResult Add()
    {
        var entity = CreateNewEntity();

        // 填充QueryString参数
        var qs = Request.Query;
        foreach (var item in Factory.Fields)
        {
            var v = qs[item.Name];
            if (v.Count > 0) entity[item.Name] = v[0];
        }

        // 验证数据权限
        Valid(entity, DataObjectMethodType.Insert, false);

        // 记下添加前的来源页，待会添加成功以后跳转
        // 如果列表页有查询条件，优先使用
        String returnUrl;
        if (Session[CacheKey] is Pager p)
        {
            var sb = p.GetBaseUrl(true, true, true);
            returnUrl = sb.Length > 0 ? "Index?" + sb : Request.GetReferer();
        }
        else
            returnUrl = Request.GetReferer();

        ViewBag.ReturnUrl = returnUrl;

        // 用于显示的列
        ViewBag.Fields = OnGetFields(ViewKinds.AddForm, entity);

        return View("AddForm", entity);
    }

    /// <summary>保存</summary>
    /// <param name="model">提交的表单模型</param>
    /// <returns>保存成功后重定向到列表页；失败时回填表单并提示错误</returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost]
    public virtual async Task<ActionResult> Add(TModel model)
    {
        var returnUrl = GetRequest("returnUrl");
        // 实例化实体对象，然后拷贝
        if (model is not TEntity entity)
        {
            entity = CreateNewEntity();

            if (entity is IEntity<TModel> entity2)
                entity2.Copy(model);
            else if (model is IModel src)
                entity.CopyFrom(src, true, true);
            else
                entity.Copy(model);
        }

        // 检测避免乱用Add/id
        if (Factory.Unique.IsIdentity && entity[Factory.Unique.Name].ToInt() != 0)
            throw new Exception("When adding via POST, the route id part must be empty, otherwise the model binder misreads it!");

        try
        {
            if (!Valid(entity, DataObjectMethodType.Insert, true))
                throw new Exception("Validation failed");

            OnInsert(entity);

            var fs = await SaveFiles(entity);
            if (fs.Count > 0) OnUpdate(entity);

            if (LogOnChange) LogProvider.Provider.WriteLog("Insert", entity);
        }
        catch (Exception ex)
        {
            DefaultSpan.Current?.SetError(ex);

            var code = ex is ApiException ae ? ae.Code : 500;
            var err = ex.Message;
            ModelState.AddModelError((ex as ArgumentException)?.ParamName ?? "", ex.Message);

            WriteLog("Add", false, err);

            ViewBag.StatusMessage = SysConfig.Develop ? ("添加失败！" + err) : "添加失败！";

            // 添加失败，ID清零，否则会显示保存按钮
            entity[Factory.Unique.Name] = 0;

            if (IsJsonRequest) return Json(code, ViewBag.StatusMessage);

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Fields = OnGetFields(ViewKinds.AddForm, entity);

            return View("AddForm", entity);
        }

        ViewBag.StatusMessage = "Added successfully!";

        if (IsJsonRequest) return Json(0, ViewBag.StatusMessage);

        if (!returnUrl.IsNullOrEmpty()) return Redirect(returnUrl);

        // 新增完成跳到列表页，更新完成保持本页
        return RedirectToAction("Index");
    }

    /// <summary>表单，添加/修改</summary>
    /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
    /// <returns>编辑表单视图</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("更新{type}")]
    public virtual ActionResult Edit(String id)
    {
        var entity = FindData(id);
        if (entity == null || (entity as IEntity).IsNullKey) throw new XException("Data [{0}] to edit does not exist!", id);

        // 验证数据权限
        Valid(entity, DataObjectMethodType.Update, false);

        // 如果列表页有查询条件，优先使用
        String returnUrl;
        if (Session[CacheKey] is Pager p)
        {
            var sb = p.GetBaseUrl(true, true, true);
            returnUrl = sb.Length > 0 ? "../Index?" + sb : Request.GetReferer();
        }
        else
            returnUrl = Request.GetReferer();

        ViewBag.ReturnUrl = returnUrl;

        // Json输出
        if (IsJsonRequest) return Json(0, null, entity);

        ViewBag.Fields = OnGetFields(ViewKinds.EditForm, entity);

        return View("EditForm", entity);
    }

    /// <summary>保存</summary>
    /// <param name="model">提交的表单模型</param>
    /// <returns>保存成功后重定向到列表页；失败时回填表单并提示错误</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost]
    public virtual async Task<ActionResult> Edit(TModel model)
    {
        var returnUrl = GetRequest("returnUrl");

        // 实例化实体对象，然后拷贝
        if (model is not TEntity entity)
        {
            var uk = Factory.Unique;
            var key = model is IModel ext ? ext[uk.Name] : model.GetValue(uk.Name);

            // 先查出来，再拷贝。这里没有考虑脏数据的问题，有可能拷贝后并没有脏数据
            entity = FindData(key);

            if (entity is IEntity<TModel> entity2)
                entity2.Copy(model);
            else if (model is IModel src)
                entity.CopyFrom(src, true, true);
            else
                entity.Copy(model, false, uk.Name);
        }

        var rs = false;
        var err = "";
        try
        {
            if (!Valid(entity, DataObjectMethodType.Update, true))
                throw new Exception("Validation failed");

            await SaveFiles(entity);

            OnUpdate(entity);

            rs = true;
        }
        catch (Exception ex)
        {
            DefaultSpan.Current?.SetError(ex);
            err = ex.Message;
            ModelState.AddModelError((ex as ArgumentException)?.ParamName ?? "", ex.Message);
        }

        Object id = null;
        if (Factory.Unique != null) id = entity[Factory.Unique.Name];

        if (!rs)
        {
            WriteLog("Edit", false, err);

            ViewBag.StatusMessage = SysConfig.Develop ? ("保存失败！" + err) : "保存失败！";

            if (IsJsonRequest) return Json(500, ViewBag.StatusMessage);
        }
        else
        {
            ViewBag.StatusMessage = "Saved successfully!";

            if (IsJsonRequest) return Json(0, ViewBag.StatusMessage);

            // 实体对象保存成功后直接重定向到列表页，减少用户操作提高操作体验
            if (!returnUrl.IsNullOrEmpty()) return Redirect(returnUrl);
        }

        // 重新查找对象数据，以确保取得最新值
        if (id != null) entity = FindData(id);

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Fields = OnGetFields(ViewKinds.EditForm, entity);

        return View("EditForm", entity);
    }

    /// <summary>批量启用</summary>
    /// <param name="keys">主键集合</param>
    /// <param name="reason">操作原因</param>
    /// <returns>批量启用结果 JSON，含成功与失败行数</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public virtual ActionResult EnableSelect(String keys, String reason)
    {
        var count = EnableOrDisableSelect(true, reason);
        return JsonRefresh($"共启用[{count}]个");
    }

    /// <summary>批量禁用</summary>
    /// <param name="keys">主键集合</param>
    /// <param name="reason">操作原因</param>
    /// <returns>批量禁用结果 JSON，含成功与失败行数</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public virtual ActionResult DisableSelect(String keys, String reason)
    {
        var count = EnableOrDisableSelect(false, reason);
        return JsonRefresh($"共禁用[{count}]个");
    }
    #endregion

    #region 高级Action
    /// <summary>高级开发接口</summary>
    /// <param name="act">动作：Backup/BackupAndExport/Restore/Clear 等，其它值交给基类处理</param>
    /// <returns>高级动作执行结果</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override async Task<ActionResult> Develop(String act)
    {
        if (!NewLife.Common.SysConfig.Current.Develop) throw new InvalidOperationException("Only available in development mode!");

        var user = ManageProvider.User;
        if (user == null || !user.Roles.Any(e => e.IsSystem)) throw new InvalidOperationException("Only available to system administrators!");

        return act switch
        {
            "Sync" => Backup(),
            _ => await base.Develop(act),
        };
    }

    /// <summary>导入Excel</summary>
    /// <returns>导入结果 JSON，含成功与失败行数</returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [DisplayName("导入Excel")]
    [Obsolete("=>ImportFile")]
    public virtual ActionResult ImportExcel(IFormFile file)
    {
        try
        {
            var fact = Factory;
            var rs = ImportExcel(file.FileName, file.OpenReadStream(), fact, GetCachePager());

            var msg = $"Imported [{file.FileName}], {rs} rows succeeded!";
            return JsonRefresh(msg, 2);
        }
        catch (Exception ex)
        {
            DefaultSpan.Current?.SetError(ex);
            XTrace.WriteException(ex);

            WriteLog(L.TLog("Import Excel"), false, ex.GetMessage());

            return Json(500, ex.GetMessage(), ex);
        }
    }

    /// <summary>修改bool值</summary>
    /// <param name="id">主键</param>
    /// <param name="valName">待修改的布尔字段名</param>
    /// <param name="val">目标布尔值</param>
    /// <returns>更新结果 JSON，含受影响行数</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult SetBool(Int32 id = 0, String valName = "", Boolean val = true)
    {
        var fi = Factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase(valName));
        if (fi == null) throw new InvalidOperationException($"Field [{valName}] not found.");

        var rs = 0;
        if (id > 0)
        {
            var entity = FindData(id);
            if (entity == null) throw new ArgumentNullException(nameof(id), "Record not found: " + id);

            //entity.Enable = enable;
            entity.SetItem(fi.Name, val);
            if (Valid(entity, DataObjectMethodType.Update, true))
                rs += OnUpdate(entity);
        }
        else
        {
            var ids = GetRequest("keys").SplitAsInt();

            foreach (var item in ids)
            {
                var entity = FindData(item);
                if (entity != null)
                {
                    //entity.Enable = enable;
                    entity.SetItem(fi.Name, val);
                    if (Valid(entity, DataObjectMethodType.Update, true))
                        rs += OnUpdate(entity);
                }
            }
        }
        return JsonRefresh($"Succeeded! [{rs}] rows updated!");
    }

    /// <summary>启用 或 禁用</summary>
    /// <param name="id">主键</param>
    /// <param name="enable">是否启用</param>
    /// <returns>更新结果 JSON</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    public virtual ActionResult SetEnable(Int64 id = 0, Boolean enable = true)
    {
        var fi = Factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase("Enable"));
        if (fi == null) throw new InvalidOperationException($"Enable/disable only supports the Enable field.");

        var rs = 0;
        if (id > 0)
        {
            var entity = FindData(id) ?? throw new ArgumentNullException(nameof(id), "Record not found: " + id);

            if (OnSetField(entity, fi.Name, enable))
                rs += OnUpdate(entity);
        }
        else
        {
            var ids = GetRequest("keys").SplitAsInt();

            foreach (var item in ids)
            {
                var entity = FindData(item);
                if (entity != null)
                {
                    if (OnSetField(entity, fi.Name, enable))
                        rs += OnUpdate(entity);
                }
            }
        }

        return JsonRefresh($"Succeeded! [{rs}] rows updated!");
    }
    #endregion

    #region 批量删除
    /// <summary>删除选中</summary>
    /// <returns>删除结果 JSON，含成功与失败行数</returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("删除选中")]
    public virtual ActionResult DeleteSelect()
    {
        var total = 0;
        var success = 0;
        var keys = SelectKeys;
        if (keys != null && keys.Length > 0)
        {
            // 假删除与恢复。首次删除标记假删除，假删除后再删除则是真正删除
            var fi = GetDeleteField();

            using var tran = Entity<TEntity>.Meta.CreateTrans();
            var updates = new List<IEntity>();
            var deletes = new List<IEntity>();
            foreach (var item in keys)
            {
                var entity = Entity<TEntity>.FindByKey(item);
                if (entity != null)
                {
                    // 验证数据权限
                    if (fi != null && (!PageSetting.DoubleDelete || !entity[fi.Name].ToBoolean()))
                    {
                        entity.SetItem(fi.Name, true);
                        if (Valid(entity, DataObjectMethodType.Update, true)) updates.Add(entity);
                    }
                    else
                    {
                        if (Valid(entity, DataObjectMethodType.Delete, true)) deletes.Add(entity);
                    }
                }
            }

            // 统计待处理行数 = 软删 + 硬删，避免纯硬删场景 total 为 0 造成文案误导
            total = updates.Count + deletes.Count;
            success += updates.Update();
            success += deletes.Delete();

            tran.Commit();
        }

        return JsonRefresh($"共删除{total}行数据，成功{success}行");
    }

    /// <summary>删除全部</summary>
    /// <returns>删除全部结果 JSON，含成功与失败行数</returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("删除全部")]
    public virtual ActionResult DeleteAll()
    {
        var url = Request.GetReferer();

        // 假删除与恢复。首次删除标记假删除，假删除后再删除则是真正删除
        var fi = GetDeleteField();

        var total = 0;
        var success = 0;
        //var p = Session[CacheKey] as Pager;
        //p = new Pager(p);
        var p = GetCachePager();
        if (p != null)
        {
            // 循环多次删除
            for (var i = 0; i < 10; i++)
            {
                p.PageIndex = i + 1;
                p.PageSize = 100_000;
                // 不要查记录数
                p.RetrieveTotalCount = false;

                var data = SearchData(p).ToList();
                if (data.Count == 0) break;

                total += data.Count;

                using var tran = Entity<TEntity>.Meta.CreateTrans();
                var updates = new List<IEntity>();
                var deletes = new List<IEntity>();
                foreach (var entity in data)
                {
                    // 验证数据权限
                    if (fi != null && (!PageSetting.DoubleDelete || !entity[fi.Name].ToBoolean()))
                    {
                        entity.SetItem(fi.Name, true);
                        if (Valid(entity, DataObjectMethodType.Update, true)) updates.Add(entity);
                    }
                    else
                    {
                        if (Valid(entity, DataObjectMethodType.Delete, true)) deletes.Add(entity);
                    }
                }

                success += updates.Update();
                success += deletes.Delete();
                tran.Commit();
            }
        }

        if (Request.IsAjaxRequest())
            return JsonRefresh($"共删除{total}行数据，成功{success}行");
        else if (!url.IsNullOrEmpty())
            return Redirect(url);
        else
            return RedirectToAction("Index");
    }
    #endregion

    #region 导入Excel/Csv/Json/Zip
    /// <summary>导入文件（Excel/Csv/Json/Zip），然后批量写入数据库</summary>
    /// <param name="file">上传的zip文件</param>
    /// <returns>导入结果 JSON，含成功与失败行数</returns>
    [HttpPost]
    [EntityAuthorize(PermissionFlags.Insert)]
    [DisplayName("导入Excel/Csv/Json/Zip")]
    public virtual IActionResult ImportFile(IFormFile file)
    {
        if (file == null || file.Length <= 0) return RedirectToAction("Index");

        WriteLog(nameof(ImportFile), true, L.TLog("Start importing file [{0}], size [{1:n0}] bytes, type [{2}]", file.FileName, file.Length, file.ContentType));

        var factory = Factory;
        var page = GetCachePager();
        var stream = file.OpenReadStream();
        var name = file.FileName;
        var ext = Path.GetExtension(file.FileName).ToLower();
        var rs = ext switch
        {
            ".xls" or ".xlsx" => ImportExcel(name, stream, factory, page),
            ".csv" => ImportCsv(name, stream, factory, page),
            ".json" => ImportJson(name, stream, factory, page),
            ".zip" => ImportZip(name, stream, factory, page),
            _ => throw new NotSupportedException($"Unsupported import file type [{ext}]"),
        };
        var msg = $"Imported [{name}], total {rs} rows";
        //WriteLog("导入" + ext.TrimStart('.'), true, msg);

        return JsonRefresh(msg, 3);
    }
    #endregion

    #region 同步/还原
    // 复用 HttpClient，避免每次同步创建连接导致端口耗尽
    private static readonly HttpClient _httpClient = new();

    /// <summary>同步数据</summary>
    /// <returns>同步结果；具体行为由派生控制器决定</returns>
    [NonAction]
    public async Task<ActionResult> Sync()
    {
        //if (id.IsNullOrEmpty()) return RedirectToAction(nameof(Index));

        // 读取系统配置
        var ps = Parameter.FindAllByUserID(ManageProvider.User.ID); // UserID=0 && Category=Sync
        ps = ps.Where(e => e.Category == "Sync").ToList();
        var server = ps.FirstOrDefault(e => e.Name == "Server")?.Value;
        var token = ps.FirstOrDefault(e => e.Name == "Token")?.Value;
        var models = ps.FirstOrDefault(e => e.Name == "Models")?.Value;

        if (server.IsNullOrEmpty()) throw new ArgumentNullException("Sync:Server is not configured");
        if (token.IsNullOrEmpty()) throw new ArgumentNullException("Sync:Token is not configured");
        if (models.IsNullOrEmpty()) throw new ArgumentNullException("Sync:Models is not configured");

        var mds = models.Split(",");

        //// 创建实体工厂
        //var etype = mds.FirstOrDefault(e => e.Replace(".", "_") == id);
        //var fact = etype.GetTypeEx()?.AsFactory();
        //if (fact == null) throw new ArgumentNullException(nameof(id), "未找到模型 " + id);

        // 找到控制器，以识别动作地址
        var cs = GetControllerAction();
        var ctrl = cs[0].IsNullOrEmpty() ? cs[1] : $"{cs[0]}/{cs[1]}";
        if (!mds.Contains(ctrl)) throw new InvalidOperationException($"[{ctrl}] is not allowed to sync, check Sync:Models");

        // 服务端 pageSize 有上限（Pager.MaxPageSize），单次请求拉不走整表，改为按页循环拉取
        var fact = Factory;
        var pageSize = Pager.MaxPageSize;
        var list = new List<TEntity>();

        var sw = Stopwatch.StartNew();
        var url = "";
        for (var pageIndex = 1; ; pageIndex++)
        {
            // 防御：正常实体不可能跑到这么多页，避免服务端忽略 PageIndex 时死循环
            if (pageIndex > 10_000) throw new InvalidOperationException($"Too many pages while syncing [{ctrl}], aborted at page {pageIndex}");

            url = server.EnsureEnd("/") + $"{ctrl}/Json/{token}?PageSize={pageSize}&PageIndex={pageIndex}";
            var page = await _httpClient.InvokeAsync<TEntity[]>(HttpMethod.Get, url);
            if (page == null || page.Length == 0) break;

            list.AddRange(page);
            if (page.Length < pageSize) break;
        }

        sw.Stop();

        XTrace.WriteLine(L.TLog("[{0}] synchronized [{1:n0}] rows in {2:n0}ms, source: {3}", fact.EntityType.FullName, list.Count, sw.ElapsedMilliseconds, url));

        var arrType = fact.EntityType.MakeArrayType();
        if (list.Count > 0)
        {
            XTrace.WriteLine(L.TLog("[{0}] preparing to overwrite [{1}] rows", fact.EntityType.FullName, list.Count));
            using var tran = fact.Session.CreateTrans();

            // 清空
            try
            {
                fact.Session.Truncate();
            }
            catch (Exception ex)
            {
                DefaultSpan.Current?.SetError(ex);
                XTrace.WriteException(ex);
            }

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
    #endregion
}