using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public class EntityController<TEntity> : EntityController<TEntity, TEntity> where TEntity : Entity<TEntity>, new() { }

/// <summary>实体控制器基类</summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TModel">数据模型，用于接口数据传输</typeparam>
public class EntityController<TEntity, TModel> : ReadOnlyEntityController<TEntity> where TEntity : Entity<TEntity>, new()
{
    #region 属性
    private String CacheKey => $"CubeView_{typeof(TEntity).FullName}";
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public EntityController() => PageSetting.IsReadOnly = false;
    #endregion

    #region 默认Action
    /// <summary>删除数据</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("删除{type}")]
    [HttpDelete("/[area]/[controller]")]
    public virtual ApiResponse<TEntity> Delete([Required] String id)
    {
        var entity = FindData(id);
        var rs = false;
        var err = "";
        try
        {
            if (Valid(entity, DataObjectMethodType.Delete, true))
            {
                OnDelete(entity);

                rs = true;
            }
            else
                err = "验证失败";
        }
        catch (Exception ex)
        {
            err = ex.GetTrue().Message;
            WriteLog("Delete", false, err);
        }

        if (rs)
            return new ApiResponse<TEntity>(0, "删除成功！", entity);
        else
            return new ApiResponse<TEntity>(500, "删除失败！" + err, entity);
    }

    /// <summary>添加数据</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [DisplayName("添加{type}")]
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost("/[area]/[controller]")]
    public virtual async Task<ApiResponse<TEntity>> Insert(TModel model)
    {
        // 实例化实体对象，然后拷贝
        if (model is not TEntity entity)
        {
            entity = Factory.Create(false) as TEntity;

            if (model is IModel src)
                entity.CopyFrom(src, true);
            else
                entity.Copy(model);
        }

        return await Insert(entity);
    }

    /// <summary>添加数据</summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [NonAction]
    public virtual async Task<ApiResponse<TEntity>> Insert(TEntity entity)
    {
        // 检测避免乱用Add/id
        if (Factory.Unique.IsIdentity && entity[Factory.Unique.Name].ToInt() != 0)
            throw new Exception("我们约定添加数据时路由id部分默认没有数据，以免模型绑定器错误识别！");

        var rs = false;
        var err = "";
        try
        {
            if (Valid(entity, DataObjectMethodType.Insert, true))
            {
                //SaveFiles(entity);

                OnInsert(entity);

                // 先插入再保存附件，主要是为了在附件表关联业务对象主键
                var fs = await SaveFiles(entity);
                if (fs.Count > 0) OnUpdate(entity);

                if (LogOnChange) LogProvider.Provider.WriteLog("Insert", entity);

                rs = true;
            }
            else
                err = "验证失败";
        }
        catch (Exception ex)
        {
            err = ex.Message;
            ModelState.AddModelError((ex as ArgumentException)?.ParamName ?? "", ex.Message);
        }

        var msg = "";
        if (!rs)
        {
            WriteLog("Add", false, err);

            msg = SysConfig.Develop ? ("添加失败！" + err) : "添加失败！";

            // 添加失败，ID清零，否则会显示保存按钮
            entity[Entity<TEntity>.Meta.Unique.Name] = 0;

            return new ApiResponse<TEntity>(500, msg, null);
        }

        msg = "添加成功！";

        return new ApiResponse<TEntity>(0, msg, entity);
    }

    /// <summary>更新数据</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("更新{type}")]
    [HttpPut("/[area]/[controller]")]
    public virtual async Task<ApiResponse<TEntity>> Update(TModel model)
    {
        // 实例化实体对象，然后拷贝
        if (model is not TEntity entity)
        {
            var uk = Factory.Unique;
            var key = model is IModel ext ? ext[uk.Name] : model.GetValue(uk.Name);

            // 先查出来，再拷贝。这里没有考虑脏数据的问题，有可能拷贝后并没有脏数据
            entity = FindData(key);

            if (model is IModel src)
                entity.CopyFrom(src, true);
            else
                entity.Copy(model, false, uk.Name);
        }

        return await Update(entity);
    }

    /// <summary>更新数据</summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [NonAction]
    public virtual async Task<ApiResponse<TEntity>> Update(TEntity entity)
    {
        var rs = false;
        var err = "";
        try
        {
            if (Valid(entity, DataObjectMethodType.Update, true))
            {
                await SaveFiles(entity);

                OnUpdate(entity);

                rs = true;
            }
            else
                err = "验证失败";
        }
        catch (Exception ex)
        {
            err = ex.Message;
            ModelState.AddModelError((ex as ArgumentException)?.ParamName ?? "", ex.Message);
        }

        Object id = null;
        if (Factory.Unique != null) id = entity[Factory.Unique.Name];

        var msg = "";
        if (!rs)
        {
            WriteLog("Edit", false, err);

            msg = SysConfig.Develop ? ("保存失败！" + err) : "保存失败！";

            return new ApiResponse<TEntity>(500, msg, null);
        }
        else
        {
            msg = "保存成功！";

            return new ApiResponse<TEntity>(0, msg, entity);
        }
    }

    /// <summary>保存所有上传文件</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="uploadPath">上传目录。为空时默认UploadPath配置</param>
    /// <returns></returns>
    protected virtual async Task<IList<String>> SaveFiles(TEntity entity, String uploadPath = null)
    {
        var rs = new List<String>();

        if (!Request.HasFormContentType) return rs;

        var files = Request.Form.Files;
        var fields = Factory.Fields;
        foreach (var fi in fields)
        {
            var dc = fi.Field;
            if (dc.IsAttachment())
            {
                // 允许一次性上传多个文件到服务端
                var list = new List<String>();
                foreach (var file in files)
                {
                    if (file.Name.EqualIgnoreCase(fi.Name, fi.Name + "_attachment"))
                    {
                        var att = await SaveFile(entity, file, uploadPath, null);
                        if (att != null)
                        {
                            var url = ViewHelper.GetAttachmentUrl(att);
                            list.Add(url);
                            rs.Add(url);
                        }
                    }
                }

                if (list.Count > 0) entity.SetItem(fi.Name, list.Join(";"));
            }
        }

        return rs;
    }

    /// <summary>保存单个文件</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="file">文件</param>
    /// <param name="uploadPath">上传目录，默认使用UploadPath配置</param>
    /// <param name="fileName">文件名，如若指定则忽略前面的目录</param>
    /// <returns></returns>
    protected virtual async Task<Attachment> SaveFile(TEntity entity, IFormFile file, String uploadPath, String fileName)
    {
        if (fileName.IsNullOrEmpty()) fileName = file.FileName;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(SaveFile), fileName ?? file.FileName);

        var id = Factory.Unique != null ? entity[Factory.Unique] : null;
        var att = new Attachment
        {
            Category = typeof(TEntity).Name,
            Key = id + "",
            Title = entity + "",
            //FileName = fileName ?? file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            Enable = true,
            UploadTime = DateTime.Now,
        };

        if (id != null)
        {
            var ss = GetControllerAction();
            att.Url = $"/{ss[0]}/{ss[1]}/Detail/{id}";
        }

        var rs = false;
        var msg = "";
        try
        {
            rs = await att.SaveFile(file.OpenReadStream(), uploadPath, fileName);
        }
        catch (Exception ex)
        {
            rs = false;
            msg = ex.Message;
            span?.SetError(ex, att);

            throw;
        }
        finally
        {
            // 写日志
            var type = entity.GetType();
            var log = LogProvider.Provider.CreateLog(type, "上传", rs, $"上传 {file.FileName} ，目录 {uploadPath} ，保存为 {att.FilePath} " + msg, 0, null, UserHost);
            log.LinkID = id.ToLong();
            log.SaveAsync();
        }

        return att;
    }

    ///// <summary>批量启用</summary>
    ///// <param name="keys">主键集合</param>
    ///// <param name="reason">操作原因</param>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Update)]
    //[HttpPost]
    //public virtual ActionResult EnableSelect(String keys, String reason) => EnableOrDisableSelect(true, reason);

    ///// <summary>批量禁用</summary>
    ///// <param name="keys">主键集合</param>
    ///// <param name="reason">操作原因</param>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Update)]
    //[HttpPost]
    //public virtual ActionResult DisableSelect(String keys, String reason) => EnableOrDisableSelect(false, reason);

    /// <summary>
    /// 批量启用或禁用
    /// </summary>
    /// <param name="isEnable">启用/禁用</param>
    /// <param name="reason">操作原因</param>
    /// <returns></returns>
    protected virtual ActionResult EnableOrDisableSelect(Boolean isEnable, String reason)
    {
        var count = 0;
        var ids = GetRequest("keys").SplitAsInt();
        var fields = Factory.AllFields;
        if (ids.Length > 0 && fields.Any(f => f.Name.EqualIgnoreCase("enable")))
        {
            var log = LogProvider.Provider;
            foreach (var id in ids)
            {
                var entity = Factory.Find("ID", id);
                if (entity != null && entity["Enable"].ToBoolean() != isEnable)
                {
                    entity.SetItem("Enable", isEnable);

                    log.WriteLog("Update", entity);
                    log.WriteLog(entity.GetType(), isEnable ? "Enable" : "Disable", true, reason);

                    entity.Update();

                    Interlocked.Increment(ref count);
                }
            }
        }

        return Json(0, $"共{(isEnable ? "启用" : "禁用")}[{count}]个");
    }
    #endregion

    #region 高级Action
    ///// <summary>导入Excel</summary>
    ///// 当前采用前端解析的excel，表头第一行数据无效，从第二行开始处理
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Insert)]
    //[DisplayName("导入Excel")]
    //[HttpPost]
    //public virtual ActionResult ImportExcel(String data)
    //{
    //    if (String.IsNullOrWhiteSpace(data)) return Json(500, null, $"“{nameof(data)}”不能为 null 或空白。");
    //    try
    //    {
    //        var fact = Factory;
    //        var dal = fact.Session.Dal;
    //        var type = Activator.CreateInstance(fact.EntityType);
    //        var json = new JsonParser(data);
    //        var dataList = json.Decode() as IList<Object>;


    //        //解析json
    //        //var dataList = JArray.Parse(data);
    //        var errorString = String.Empty;
    //        Int32 okSum = 0, fiSum = 0;

    //        //using var tran = Entity<TEntity>.Meta.CreateTrans();
    //        foreach (var itemD in dataList)
    //        {
    //            var item = itemD.ToDictionary();
    //            if (item[fact.Fields[1].Name].ToString() == fact.Fields[1].DisplayName) //判断首行是否为标体列
    //                continue;

    //            //检查主字段是否重复
    //            if (Entity<TEntity>.Find(fact.Master.Name, item[fact.Master.Name].ToString()) == null)
    //            {
    //                //var entity = item.ToJson().ToJsonEntity(fact.EntityType);
    //                var entity = fact.Create();

    //                foreach (var fieldsItem in fact.Fields)
    //                {
    //                    if (!item.ContainsKey(fieldsItem.Name))
    //                    {
    //                        if (!fieldsItem.IsNullable)
    //                            fieldsItem.FromExcelToEntity(item, entity);
    //                    }
    //                    else
    //                        fieldsItem.FromExcelToEntity(item, entity);
    //                }

    //                if (fact.FieldNames.Contains("CreateTime"))
    //                    entity["CreateTime"] = DateTime.Now;

    //                if (fact.FieldNames.Contains("CreateIP"))
    //                    entity["CreateIP"] = "--";

    //                okSum += fact.Session.Insert(entity);
    //            }
    //            else
    //            {
    //                errorString += $"<br>{item[fact.Master.Name]}重复";
    //                fiSum++;
    //            }
    //        }

    //        //tran.Commit();

    //        WriteLog("导入Excel", true, $"导入Excel[{data}]（{dataList.Count()}行）成功！");

    //        return Json(0, $"导入成功:({okSum}行)，失败({fiSum}行)！{errorString}");
    //    }
    //    catch (Exception ex)
    //    {
    //        XTrace.WriteException(ex);

    //        WriteLog("导入Excel", false, ex.GetMessage());

    //        return Json(500, ex.GetMessage(), ex);
    //    }
    //}
    #endregion

    #region 批量删除
    ///// <summary>删除选中</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Delete)]
    //[DisplayName("删除选中")]
    //[HttpPost]
    //public virtual ActionResult DeleteSelect()
    //{
    //    var count = 0;
    //    var keys = SelectKeys;
    //    if (keys != null && keys.Length > 0)
    //    {
    //        using var tran = Entity<TEntity>.Meta.CreateTrans();
    //        var list = new List<IEntity>();
    //        foreach (var item in keys)
    //        {
    //            var entity = Entity<TEntity>.FindByKey(item);
    //            if (entity != null)
    //            {
    //                // 验证数据权限
    //                if (Valid(entity, DataObjectMethodType.Delete, true)) list.Add(entity);

    //                count++;
    //            }
    //        }
    //        list.Delete();
    //        tran.Commit();
    //    }
    //    return JsonRefresh($"共删除{count}行数据");
    //}

    ///// <summary>删除全部</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Delete)]
    //[DisplayName("删除全部")]
    //[HttpPost]
    //public virtual ActionResult DeleteAll()
    //{
    //    var url = Request.GetReferer();

    //    var count = 0;
    //    var p = Session[CacheKey] as Pager;
    //    p = new Pager(p);
    //    if (p != null)
    //    {
    //        // 循环多次删除
    //        for (var i = 0; i < 10; i++)
    //        {
    //            p.PageIndex = i + 1;
    //            p.PageSize = 100_000;
    //            // 不要查记录数
    //            p.RetrieveTotalCount = false;

    //            var list = SearchData(p).ToList();
    //            if (list.Count == 0) break;

    //            count += list.Count;
    //            //list.Delete();
    //            using var tran = Entity<TEntity>.Meta.CreateTrans();
    //            var list2 = new List<IEntity>();
    //            foreach (var entity in list)
    //            {
    //                // 验证数据权限
    //                if (Valid(entity, DataObjectMethodType.Delete, true)) list2.Add(entity);
    //            }
    //            list2.Delete();
    //            tran.Commit();
    //        }
    //    }

    //    if (Request.IsAjaxRequest())
    //        return JsonRefresh($"共删除{count}行数据");
    //    else if (!url.IsNullOrEmpty())
    //        return Redirect(url);
    //    else
    //        return RedirectToAction("Index");
    //}
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
    ///// <summary>同步数据</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Insert)]
    //[DisplayName("同步{type}")]
    //[HttpPost]
    //public async Task<ActionResult> Sync()
    //{
    //    //if (id.IsNullOrEmpty()) return RedirectToAction(nameof(Index));

    //    // 读取系统配置
    //    var ps = Parameter.FindAllByUserID(ManageProvider.User.ID); // UserID=0 && Category=Sync
    //    ps = ps.Where(e => e.Category == "Sync").ToList();
    //    var server = ps.FirstOrDefault(e => e.Name == "Server")?.Value;
    //    var token = ps.FirstOrDefault(e => e.Name == "Token")?.Value;
    //    var models = ps.FirstOrDefault(e => e.Name == "Models")?.Value;

    //    if (server.IsNullOrEmpty()) throw new ArgumentNullException("未配置 Sync:Server");
    //    if (token.IsNullOrEmpty()) throw new ArgumentNullException("未配置 Sync:Token");
    //    if (models.IsNullOrEmpty()) throw new ArgumentNullException("未配置 Sync:Models");

    //    var mds = models.Split(",");

    //    //// 创建实体工厂
    //    //var etype = mds.FirstOrDefault(e => e.Replace(".", "_") == id);
    //    //var fact = etype.GetTypeEx()?.AsFactory();
    //    //if (fact == null) throw new ArgumentNullException(nameof(id), "未找到模型 " + id);

    //    // 找到控制器，以识别动作地址
    //    var cs = GetControllerAction();
    //    var ctrl = cs[0].IsNullOrEmpty() ? cs[1] : $"{cs[0]}/{cs[1]}";
    //    if (!mds.Contains(ctrl)) throw new InvalidOperationException($"[{ctrl}]未配置为允许同步 Sync:Models");

    //    // 创建客户端，准备发起请求
    //    var url = server.EnsureEnd("/") + $"{ctrl}/Json/{token}?PageSize=100000";

    //    var http = new HttpClient
    //    {
    //        BaseAddress = new Uri(url)
    //    };

    //    var sw = Stopwatch.StartNew();

    //    var list = await http.InvokeAsync<TEntity[]>(HttpMethod.Get, null);

    //    sw.Stop();

    //    var fact = Factory;
    //    XTrace.WriteLine("[{0}]共同步数据[{1:n0}]行，耗时{2:n0}ms，数据源：{3}", fact.EntityType.FullName, list.Length, sw.ElapsedMilliseconds, url);

    //    var arrType = fact.EntityType.MakeArrayType();
    //    if (list.Length > 0)
    //    {
    //        XTrace.WriteLine("[{0}]准备覆盖写入[{1}]行数据", fact.EntityType.FullName, list.Length);
    //        using var tran = fact.Session.CreateTrans();

    //        // 清空
    //        try
    //        {
    //            fact.Session.Truncate();
    //        }
    //        catch (Exception ex) { XTrace.WriteException(ex); }

    //        // 插入
    //        //ms.All(e => { e.AllChilds = new List<Menu>(); return true; });
    //        fact.AllowInsertIdentity = true;
    //        //ms.Insert();
    //        //var empty = typeof(List<>).MakeGenericType(fact.EntityType).CreateInstance();
    //        foreach (IEntity entity in list)
    //        {
    //            if (entity is IEntityTree tree) tree.AllChilds.Clear();

    //            entity.Insert();
    //        }
    //        fact.AllowInsertIdentity = false;

    //        tran.Commit();
    //    }

    //    return Index();
    //}

    ///// <summary>从服务器本地目录还原</summary>
    ///// <returns></returns>
    //[EntityAuthorize(PermissionFlags.Insert)]
    //[DisplayName("还原")]
    //[HttpPost]
    //public virtual ActionResult Restore()
    //{
    //    try
    //    {
    //        var fact = Factory;
    //        var dal = fact.Session.Dal;

    //        var name = GetType().Name.TrimEnd("Controller");
    //        var fileName = $"{name}_*.gz";

    //        var di = NewLife.Setting.Current.BackupPath.GetBasePath().AsDirectory();
    //        //var fi = di?.GetFiles(fileName)?.LastOrDefault();
    //        var fi = di?.GetFiles(fileName)?.OrderByDescending(e => e.Name).FirstOrDefault();
    //        if (fi == null || !fi.Exists) throw new XException($"找不到[{fileName}]的备份文件");

    //        var rs = dal.Restore(fi.FullName, fact.Table.DataTable);

    //        WriteLog("恢复", true, $"恢复[{fileName}]（{rs:n0}行）成功！");

    //        return Json(0, $"恢复[{fileName}]（{rs:n0}行）成功！");
    //    }
    //    catch (Exception ex)
    //    {
    //        XTrace.WriteException(ex);

    //        WriteLog("恢复", false, ex.GetMessage());

    //        return Json(500, null, ex);
    //    }
    //}
    #endregion
}