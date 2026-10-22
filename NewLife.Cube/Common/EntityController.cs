using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
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
    /// <summary>删除数据。支持两种风格：传统查询参数 /api/{area}/{controller}?id=，以及 RESTful 路径 /api/{area}/{controller}/{id}</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("Delete {type}")]
    [HttpDelete("/api/[area]/[controller]")]
    public virtual ApiResponse<TEntity> Delete([Required] String id)
    {
        var act = L.T("Delete");
        var entity = FindData(id);
        try
        {
            act = ProcessDelete(entity);
            return new ApiResponse<TEntity> { Code = 0, Message = L.T("{0} succeeded!", act), Data = entity };
        }
        catch (Exception ex)
        {
            return BuildFailResponse(ex, act, entity);
        }
    }

    /// <summary>删除数据（RESTful 风格）。主键在路径中，转发到 Delete 以保留子类重载</summary>
    /// <param name="id">主键</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("Delete {type}")]
    [HttpDelete("/api/[area]/[controller]/{id}")]
    public virtual ApiResponse<TEntity> DeleteById(String id)
    {
        if (id.IsNullOrEmpty()) throw new ApiException(Models.CubeCode.ParamError.ToInt(), L.T("Missing primary key."));

        return Delete(id);
    }

    /// <summary>批量删除选中数据。前端 deleteSelect 调用，支持重复参数 id=1&amp;id=2、索引形式 id[0]=1&amp;id[1]=2、逗号分隔 id=1,2</summary>
    /// <param name="id">主键集合。为空时返回参数错误</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("Delete {type} in bulk")]
    [HttpDelete("/api/[area]/[controller]/DeleteSelect")]
    public virtual ApiResponse<String> DeleteSelect([FromQuery] String[] id)
    {
        var act = L.T("Delete");
        try
        {
            if (id == null || id.Length == 0)
                throw new ApiException(Models.CubeCode.ParamError.ToInt(), L.T("No data specified for deletion."));

            var n = 0;
            foreach (var item in id)
            {
                // 兼容逗号分隔形式 id=1,2,3（String[] 绑定为单个元素时拆分）
                var parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    var entity = FindData(part);
                    if (entity == null) continue;

                    ProcessDelete(entity);
                    n++;
                }
            }

            return new ApiResponse<String> { Code = 0, Message = L.T("{0} succeeded! Total {1} rows", act, n), Data = n.ToString() };
        }
        catch (Exception ex)
        {
            // DeleteSelect 返回 ApiResponse<String>，复用 BuildFailResponse 后做类型适配（保留错误码/消息/字段错误）
            var fail = BuildFailResponse(ex, act, null);
            return new ApiResponse<String> { Code = fail.Code, Message = fail.Message, Data = null, FieldErrors = fail.FieldErrors };
        }
    }

    /// <summary>按当前搜索条件删除全部数据。至少需携带一个业务搜索条件，防止误删全表</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("Delete {type} by condition")]
    [HttpDelete("/api/[area]/[controller]/DeleteAll")]
    public virtual ApiResponse<String> DeleteAll()
    {
        var act = L.T("Delete");
        try
        {
            var p = new Pager(WebHelper.Params);
            // 排除分页系统参数后若无业务搜索条件，拒绝删除
            var sys = new[] { "pageIndex", "pageSize", "sort", "desc" };
            if (!p.Params.Keys.Any(e => !e.EqualIgnoreCase(sys)))
                throw new ApiException(Models.CubeCode.ParamError.ToInt(), L.T("No delete condition specified; refusing to delete all data."));

            var list = SearchData(p).ToList();
            var n = 0;
            foreach (var entity in list)
            {
                ProcessDelete(entity);
                n++;
            }

            return new ApiResponse<String> { Code = 0, Message = L.T("{0} succeeded! Total {1} rows", act, n), Data = n.ToString() };
        }
        catch (Exception ex)
        {
            // DeleteAll 返回 ApiResponse<String>，复用 BuildFailResponse 后做类型适配（保留错误码/消息/字段错误）
            var fail = BuildFailResponse(ex, act, null);
            return new ApiResponse<String> { Code = fail.Code, Message = fail.Message, Data = null, FieldErrors = fail.FieldErrors };
        }
    }

    /// <summary>添加数据</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [DisplayName("Add {type}")]
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost("/api/[area]/[controller]")]
    public virtual async Task<ApiResponse<TEntity>> Insert(TModel model)
    {
        // 实例化实体对象，然后拷贝
        if (model is TEntity entity) return await ProcessInsert(entity);

        entity = Factory.Create(false) as TEntity;

        try
        {
            if (model is IModel src)
                entity.CopyFrom(src, true, true);
            else
                entity.Copy(model);
        }
        catch (Exception ex)
        {
            return BuildFailResponse(ex, L.T("Add"), entity);
        }

        return await ProcessInsert(entity);
    }

    /// <summary>添加数据</summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [NonAction]
    protected virtual async Task<ApiResponse<TEntity>> ProcessInsert(TEntity entity)
    {
        // 检测避免乱用Add/id
        if (Factory.Unique.IsIdentity && entity[Factory.Unique.Name].ToInt() != 0)
            throw new Exception(L.T("Adding data must not include a route id."));

        try
        {
            if (!Valid(entity, DataObjectMethodType.Insert, true))
                throw new Exception(L.T("Validation failed."));

            // 基于 Model.xml 元数据的字段级校验（必填、长度等），提前发现问题返回明确错误
            // 子类可通过 override EnableFieldValidation => false 关闭
            if (EnableFieldValidation)
            {
                var fieldErrors = ValidateEntityFields(entity, DataObjectMethodType.Insert);
                if (fieldErrors != null)
                {
                    var firstMsg = fieldErrors[0].Message;
                    WriteLog("Add", false, firstMsg);
                    return new ApiResponse<TEntity>
                    {
                        Code = Models.CubeCode.ParamError.ToInt(),
                        Message = SysConfig.Develop ? $"{L.T("Add failed!")} {firstMsg}" : L.T("Add failed!"),
                        Data = entity,
                        FieldErrors = fieldErrors
                    };
                }
            }

            OnInsert(entity);

            // 先插入再保存附件，主要是为了在附件表关联业务对象主键
            var fs = await SaveFiles(entity);
            // 将通过独立上传得到的临时附件绑定到已新建主记录
            await BindAttachments(entity);
            if (fs.Count > 0) OnUpdate(entity);

            if (LogOnChange) LogProvider.Provider.WriteLog("Insert", entity);

            return new ApiResponse<TEntity> { Code = 0, Message = L.T("Add succeeded!"), Data = entity };
        }
        catch (Exception ex)
        {
            // 添加失败，ID清零，否则会显示保存按钮
            entity[Factory.Unique.Name] = 0;

            return BuildFailResponse(ex, L.T("Add"), entity);
        }
    }

    /// <summary>更新数据。支持两种风格：请求体携带主键，或 RESTful 路径 /api/{area}/{controller}/{id}</summary>
    /// <remarks>
    /// 语义为"部分更新"：先按主键查出库中记录，再只覆盖本次请求实际提交的字段。
    /// 提交范围优先取请求 JSON 的顶层字段名，非 JSON 请求退回实体脏字段标记。
    /// 未提交的字段保持数据库原值，显式提交 null 则清空该字段。
    /// </remarks>
    /// <param name="model"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("Update {type}")]
    [HttpPut("/api/[area]/[controller]")]
    public virtual async Task<ApiResponse<TEntity>> Update(TModel model)
    {
        var uk = Factory.Unique;
        var key = model is IModel ext ? ext[uk.Name] : model.GetValue(uk.Name);
        if (key == null || key + "" == "") throw new ApiException(Models.CubeCode.ParamError.ToInt(), L.T("Missing primary key."));

        // 先查出来，再按提交范围覆盖，避免"未提交字段被默认值清空"
        var entity = FindData(key);
        if (entity == null || (entity as IEntity).IsNullKey)
            throw new XException("The requested data [{0}] does not exist.", key);

        try
        {
            // 优先用请求 JSON 的顶层字段名确定"提交范围"，能区分"未提交"与"显式 null"；
            // 非 JSON 请求（如程序内调用）退回实体脏字段标记
            var fields = await GetSubmittedFieldsAsync();
            if (fields != null)
                CopySubmitted(entity, model, fields, uk.Name);
            else if (model is TEntity submitted)
                MergeSubmitted(entity, submitted);
            else
                entity.Copy(model, false, uk.Name);
        }
        catch (Exception ex)
        {
            return BuildFailResponse(ex, L.T("Save"), entity);
        }

        return await ProcessUpdate(entity);
    }

    /// <summary>把提交实体中实际赋值过的字段覆盖到库中实体</summary>
    /// <param name="target">库中实体</param>
    /// <param name="source">请求绑定的实体</param>
    protected void MergeSubmitted(TEntity target, TEntity source)
    {
        var uk = Factory.Unique;
        var type = typeof(TEntity);
        var dirtys = ((IEntity)source).Dirtys;
        foreach (var fi in Factory.Fields)
        {
            if (fi.Name.EqualIgnoreCase(uk.Name)) continue;
            if (!dirtys[fi.Name]) continue;

            var pi = type.GetProperty(fi.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi == null || !pi.CanRead || !pi.CanWrite) continue;

            pi.SetValue(target, pi.GetValue(source));
        }
    }

    /// <summary>把模型中被请求 JSON 命中的字段覆盖到库中实体</summary>
    /// <param name="target">库中实体</param>
    /// <param name="model">请求绑定的模型</param>
    /// <param name="fields">请求 JSON 的顶层字段名</param>
    /// <param name="keyName">主键名，跳过不覆盖</param>
    protected void CopySubmitted(TEntity target, TModel model, ISet<String> fields, String keyName)
    {
        var targetType = typeof(TEntity);
        var modelType = model.GetType();
        foreach (var fi in Factory.Fields)
        {
            if (fi.Name.EqualIgnoreCase(keyName)) continue;
            if (!fields.Contains(fi.Name)) continue;

            var tpi = targetType.GetProperty(fi.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var spi = modelType.GetProperty(fi.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (tpi == null || !tpi.CanWrite || spi == null || !spi.CanRead) continue;

            tpi.SetValue(target, spi.GetValue(model));
        }
    }

    /// <summary>读取请求体中实际提交的顶层字段名。未启用缓冲或非 JSON 对象时返回 null</summary>
    /// <returns></returns>
    protected async Task<ISet<String>> GetSubmittedFieldsAsync()
    {
        var body = Request.Body;
        if (body == null || !body.CanSeek) return null;

        var position = body.Position;
        try
        {
            body.Position = 0;
            using var doc = await JsonDocument.ParseAsync(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var set = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in doc.RootElement.EnumerateObject()) set.Add(item.Name);

            return set;
        }
        catch
        {
            return null;
        }
        finally
        {
            body.Position = position;
        }
    }

    /// <summary>更新数据（RESTful 风格）。主键在路径中，写入模型后转发到 Update 以保留子类重载</summary>
    /// <param name="id">主键</param>
    /// <param name="model">提交的模型</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("Update {type}")]
    [HttpPut("/api/[area]/[controller]/{id}")]
    public virtual Task<ApiResponse<TEntity>> UpdateById(String id, TModel model)
    {
        if (id.IsNullOrEmpty()) throw new ApiException(Models.CubeCode.ParamError.ToInt(), L.T("Missing primary key."));

        // 路径与请求体都带主键时必须一致，避免"路径指向 A、内容改 B"
        var uk = Factory.Unique;
        var current = (model is IModel src ? src[uk.Name] : model.GetValue(uk.Name)) + "";
        if (!current.IsNullOrEmpty() && current != "0" && current != id)
            throw new ApiException(Models.CubeCode.ParamError.ToInt(), L.T("The primary key in the route does not match the one in the request body."));

        model.SetValue(uk.Name, id.ChangeType(uk.Type));
        return Update(model);
    }

    /// <summary>更新数据</summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    [NonAction]
    protected virtual async Task<ApiResponse<TEntity>> ProcessUpdate(TEntity entity)
    {
        try
        {
            if (!Valid(entity, DataObjectMethodType.Update, true))
                throw new Exception(L.T("Validation failed."));

            // 基于 Model.xml 元数据的字段级校验（必填、长度等），提前发现问题返回明确错误
            // 子类可通过 override EnableFieldValidation => false 关闭
            if (EnableFieldValidation)
            {
                var fieldErrors = ValidateEntityFields(entity, DataObjectMethodType.Update);
                if (fieldErrors != null)
                {
                    var firstMsg = fieldErrors[0].Message;
                    WriteLog("Edit", false, firstMsg);
                    return new ApiResponse<TEntity>
                    {
                        Code = Models.CubeCode.ParamError.ToInt(),
                        Message = SysConfig.Develop ? $"{L.T("Save failed!")} {firstMsg}" : L.T("Save failed!"),
                        Data = null,
                        FieldErrors = fieldErrors
                    };
                }
            }

            await SaveFiles(entity);
            // 将通过独立上传得到的新附件绑定到主记录
            await BindAttachments(entity);

            OnUpdate(entity);

            return new ApiResponse<TEntity> { Code = 0, Message = L.T("Save succeeded!"), Data = entity };
        }
        catch (Exception ex)
        {
            // 保留 ModelState 以兼容 MVC 视图场景
            ModelState.AddModelError((ex as ArgumentException)?.ParamName ?? "", ex.Message);

            return BuildFailResponse(ex, L.T("Save"), null);
        }
    }
    #endregion

    #region 辅助方法
    /// <summary>根据字段名查找实体元数据中的 DisplayName（中文显示名）</summary>
    /// <param name="fieldName">字段名</param>
    /// <returns>DisplayName，找不到时返回原字段名</returns>
    private static String GetFieldDisplayName(String fieldName)
    {
        if (fieldName.IsNullOrEmpty()) return fieldName;
        var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(fieldName));
        return fi?.DisplayName ?? fieldName;
    }

    /// <summary>基于实体字段元数据（Model.xml）校验必填、长度等约束，返回字段级错误列表</summary>
    /// <param name="entity">待校验的实体对象</param>
    /// <param name="type">操作类型（Insert使用AddFormFields，Update使用EditFormFields）</param>
    /// <returns>字段错误列表，无错误时返回null</returns>
    private static List<FieldError> ValidateEntityFields(TEntity entity, DataObjectMethodType type)
    {
        var fields = type == DataObjectMethodType.Insert ? AddFormFields : EditFormFields;
        var errors = new List<FieldError>();

        foreach (var df in fields)
        {
            // 跳过主键和只读字段
            if (df.PrimaryKey || df.ReadOnly) continue;

            var value = entity[df.Name];
            var displayName = df.DisplayName ?? df.Name;

            // 1. 必填校验：数据库列定义为 NOT NULL 的字段
            if (!df.Nullable)
            {
                if (value == null || (value is String s && s.IsNullOrEmpty()))
                {
                    errors.Add(new FieldError
                    {
                        Field = df.Name,
                        Message = L.T("{0} is required.", displayName)
                    });
                    continue; // 必填不通过则跳过后续长度检查
                }
            }

            // 2. 字符串长度校验
            if (df.Length > 0 && value is String str && str.Length > df.Length)
            {
                errors.Add(new FieldError
                {
                    Field = df.Name,
                    Message = L.T("{0} must not exceed {1} characters.", displayName, df.Length)
                });
            }
        }

        return errors.Count > 0 ? errors : null;
    }

    /// <summary>从异常链中提取字段级验证错误，自动查找 DisplayName 增强消息可读性</summary>
    /// <param name="ex">异常对象</param>
    /// <returns>字段错误列表，无字段信息时返回null</returns>
    private static List<FieldError> BuildFieldErrors(Exception ex)
    {
        if (ex == null) return null;

        var list = new List<FieldError>();
        var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        // 遍历异常链，收集所有带字段名的参数异常
        var current = ex;
        while (current != null)
        {
            if (current is ArgumentException ae && !ae.ParamName.IsNullOrEmpty())
            {
                // 去重：同一个字段只保留第一条错误
                if (seen.Add(ae.ParamName))
                {
                    // 查找 DisplayName，如果消息中未包含中文名则补充
                    var displayName = GetFieldDisplayName(ae.ParamName);
                    var msg = ae.Message;
                    if (displayName != ae.ParamName && !msg.Contains(displayName))
                        msg = $"{displayName}{msg}";

                    list.Add(new FieldError { Field = ae.ParamName, Message = msg });
                }
            }
            else if (current is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    var innerErrors = BuildFieldErrors(inner);
                    if (innerErrors != null) list.AddRange(innerErrors);
                }
            }
            current = current.InnerException;
        }
        return list.Count > 0 ? list : null;
    }

    /// <summary>构建失败响应，自动提取字段级错误</summary>
    /// <param name="ex">异常对象</param>
    /// <param name="action">操作名称（添加/保存/删除）</param>
    /// <param name="entity">实体对象</param>
    /// <returns></returns>
    private ApiResponse<TEntity> BuildFailResponse(Exception ex, String action, TEntity entity)
    {
        DefaultSpan.Current?.SetError(ex);

        var trueEx = ex.GetTrue();
        var detail = trueEx.Message;
        WriteLog(action, false, detail);

        var fieldErrors = BuildFieldErrors(ex);

        // ApiException 保留自身码与消息；其余异常统一 500，XException 视为已知业务异常、消息保留，未知异常仅开发模式暴露细节
        var isBiz = trueEx is ApiException or XException;
        var tip = fieldErrors != null && fieldErrors.Count > 0 ? fieldErrors[0].Message : detail;
        var err = isBiz || SysConfig.Develop ? $"{L.T("{0} failed!", action)} {tip}" : L.T("{0} failed!", action);

        var code = trueEx is ApiException ae ? ae.Code : ApiCode.InternalServerError.ToInt();
        return new ApiResponse<TEntity>
        {
            Code = code,
            Message = err,
            Data = entity,
            FieldErrors = fieldErrors
        };
    }
    #endregion

    #region 导入Excel/Csv/Json/Zip
    /// <summary>导入文件（Excel/Csv/Json/Zip），然后批量写入数据库。经类级路由 api/[area]/[controller]/[action] 暴露为 POST /api/{area}/{controller}/ImportFile</summary>
    /// <param name="file">上传的 Excel/Csv/Json/Zip 文件</param>
    /// <returns>统一 Json 响应，message 含导入行数</returns>
    /// <remarks>依据文件扩展名分发到 ImportExcel/ImportCsv/ImportJson/ImportZip；导入失败时返回 code!=0 的业务错误</remarks>
    [HttpPost]
    [EntityAuthorize(PermissionFlags.Insert)]
    [DisplayName("Import Excel/Csv/Json/Zip")]
    public virtual IActionResult ImportFile(IFormFile file)
    {
        if (file == null || file.Length <= 0) throw new ArgumentNullException(nameof(file), L.T("No file was uploaded."));

        WriteLog(nameof(ImportFile), true, L.T("Start importing file [{0}], size [{1:n0}] bytes, type [{2}].", file.FileName, file.Length, file.ContentType));

        var factory = Factory;
        var page = GetCachePager();
        using var stream = file.OpenReadStream();
        var name = file.FileName;
        var ext = Path.GetExtension(file.FileName).ToLower();
        var rs = ext switch
        {
            ".xls" or ".xlsx" => ImportExcel(name, stream, factory, page),
            ".csv" => ImportCsv(name, stream, factory, page),
            ".json" => ImportJson(name, stream, factory, page),
            ".zip" => ImportZip(name, stream, factory, page),
            _ => throw new NotSupportedException(L.T("Unsupported import file type [{0}].", ext)),
        };
        var msg = L.T("Imported [{0}], {1} rows.", name, rs);
        WriteLog(nameof(ImportFile), true, msg);

        return Json(0, msg);
    }
    #endregion
}