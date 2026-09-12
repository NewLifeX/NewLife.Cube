using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>实体控制器基类</summary>
public partial class EntityController<TEntity, TModel>
{
    #region 默认Action
    /// <summary>删除数据</summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("删除{type}")]
    [HttpDelete("/api/[area]/[controller]")]
    public virtual ApiResponse<TEntity> Delete([Required] String id)
    {
        var act = "删除";
        var entity = FindData(id);
        try
        {
            act = ProcessDelete(entity);
            return new ApiResponse<TEntity> { Code = 0, Message = $"{act}成功！", Data = entity };
        }
        catch (Exception ex)
        {
            return BuildFailResponse(ex, act, entity);
        }
    }

    /// <summary>批量启用。复用既有 EnableOrDisableSelect 逻辑（OnSetField + 日志 + 批量），供 SPA 启用徽标以 keys 传主键</summary>
    /// <param name="keys">主键集合，逗号分隔</param>
    /// <param name="reason">操作原因</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("启用{type}")]
    [HttpGet]
    public virtual ApiResponse<TEntity> EnableSelect(String keys, String reason = null)
    {
        var count = EnableOrDisableSelect(true, reason);
        if (count <= 0)
            return new ApiResponse<TEntity> { Code = 500, Message = "未找到可启用的记录（keys 无效或无 Enable 字段）" };
        return new ApiResponse<TEntity> { Code = 0, Message = $"共启用[{count}]个" };
    }

    /// <summary>批量禁用。复用既有 EnableOrDisableSelect 逻辑</summary>
    /// <param name="keys">主键集合，逗号分隔</param>
    /// <param name="reason">操作原因</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("禁用{type}")]
    [HttpGet]
    public virtual ApiResponse<TEntity> DisableSelect(String keys, String reason = null)
    {
        var count = EnableOrDisableSelect(false, reason);
        if (count <= 0)
            return new ApiResponse<TEntity> { Code = 500, Message = "未找到可禁用的记录（keys 无效或无 Enable 字段）" };
        return new ApiResponse<TEntity> { Code = 0, Message = $"共禁用[{count}]个" };
    }

    /// <summary>批量删除选中数据。前端 deleteSelect 调用，支持重复参数 id=1&amp;id=2、索引形式 id[0]=1&amp;id[1]=2、逗号分隔 id=1,2</summary>
    /// <param name="id">主键集合。为空时返回参数错误</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Delete)]
    [DisplayName("批量删除{type}")]
    [HttpDelete("/api/[area]/[controller]/DeleteSelect")]
    public virtual ApiResponse<String> DeleteSelect([FromQuery] String[] id)
    {
        var act = "删除";
        try
        {
            if (id == null || id.Length == 0)
                throw new ApiException(Models.CubeCode.ParamError.ToInt(), "未指定要删除的数据！");

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

            return new ApiResponse<String> { Code = 0, Message = $"{act}成功！共{n}条", Data = n.ToString() };
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
    [DisplayName("按条件删除{type}")]
    [HttpDelete("/api/[area]/[controller]/DeleteAll")]
    public virtual ApiResponse<String> DeleteAll()
    {
        var act = "删除";
        try
        {
            var p = new Pager(WebHelper.Params);
            // 排除分页系统参数后若无业务搜索条件，拒绝删除
            var sys = new[] { "pageIndex", "pageSize", "sort", "desc" };
            if (!p.Params.Keys.Any(e => !e.EqualIgnoreCase(sys)))
                throw new ApiException(Models.CubeCode.ParamError.ToInt(), "未指定删除条件，拒绝删除全部数据！");

            var list = SearchData(p).ToList();
            var n = 0;
            foreach (var entity in list)
            {
                ProcessDelete(entity);
                n++;
            }

            return new ApiResponse<String> { Code = 0, Message = $"{act}成功！共{n}条", Data = n.ToString() };
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
    [DisplayName("添加{type}")]
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
            return BuildFailResponse(ex, "添加", entity);
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
            throw new Exception("我们约定添加数据时路由id部分默认没有数据，以免模型绑定器错误识别！");

        try
        {
            if (!Valid(entity, DataObjectMethodType.Insert, true))
                throw new Exception("验证失败");

            // 基于 Model.xml 元数据的字段级校验（必填、长度等），提前发现问题返回明确错误
            // 子类可通过 override EnableFieldValidation => false 关闭；写请求携带 X-Cube-Field-Validation 头也可选择加入（OSC-260819e483 P1）
            if (EnableFieldValidationRequested)
            {
                var fieldErrors = ValidateEntityFields(entity, DataObjectMethodType.Insert);
                if (fieldErrors != null)
                {
                    var firstMsg = fieldErrors[0].Message;
                    WriteLog("Add", false, firstMsg);
                    return new ApiResponse<TEntity>
                    {
                        Code = Models.CubeCode.ParamError.ToInt(),
                        Message = SysConfig.Develop ? ($"添加失败！{firstMsg}") : "添加失败！",
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

            return new ApiResponse<TEntity> { Code = 0, Message = "添加成功！", Data = entity };
        }
        catch (Exception ex)
        {
            // 添加失败，ID清零，否则会显示保存按钮
            entity[Factory.Unique.Name] = 0;

            return BuildFailResponse(ex, "添加", entity);
        }
    }

    /// <summary>更新数据</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("更新{type}")]
    [HttpPut("/api/[area]/[controller]")]
    public virtual async Task<ApiResponse<TEntity>> Update(TModel model)
    {
        // 实例化实体对象，然后拷贝
        if (model is TEntity entity) return await ProcessUpdate(entity);

        var uk = Factory.Unique;
        var key = model is IModel ext ? ext[uk.Name] : model.GetValue(uk.Name);

        // 先查出来，再拷贝。这里没有考虑脏数据的问题，有可能拷贝后并没有脏数据
        entity = FindData(key);

        try
        {
            if (model is IModel src)
                entity.CopyFrom(src, true, true);
            else
                entity.Copy(model, false, uk.Name);
        }
        catch (Exception ex)
        {
            return BuildFailResponse(ex, "保存", entity);
        }

        return await ProcessUpdate(entity);
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
                throw new Exception("验证失败");

            // 基于 Model.xml 元数据的字段级校验（必填、长度等），提前发现问题返回明确错误
            // 子类可通过 override EnableFieldValidation => false 关闭；写请求携带 X-Cube-Field-Validation 头也可选择加入（OSC-260819e483 P1）
            if (EnableFieldValidationRequested)
            {
                var fieldErrors = ValidateEntityFields(entity, DataObjectMethodType.Update);
                if (fieldErrors != null)
                {
                    var firstMsg = fieldErrors[0].Message;
                    WriteLog("Edit", false, firstMsg);
                    return new ApiResponse<TEntity>
                    {
                        Code = Models.CubeCode.ParamError.ToInt(),
                        Message = SysConfig.Develop ? ($"保存失败！{firstMsg}") : "保存失败！",
                        Data = null,
                        FieldErrors = fieldErrors
                    };
                }
            }

            await SaveFiles(entity);
            // 将通过独立上传得到的新附件绑定到主记录
            await BindAttachments(entity);

            OnUpdate(entity);

            return new ApiResponse<TEntity> { Code = 0, Message = "保存成功！", Data = entity };
        }
        catch (Exception ex)
        {
            // 保留 ModelState 以兼容 MVC 视图场景
            ModelState.AddModelError((ex as ArgumentException)?.ParamName ?? "", ex.Message);

            return BuildFailResponse(ex, "保存", null);
        }
    }
    #endregion

    #region 辅助方法
    /// <summary>是否启用字段级校验：EnableFieldValidation 为 true，或写请求携带校验头 X-Cube-Field-Validation（1/true/yes，忽略大小写）时启用（OSC-260819e483 P1）。读请求/GetPage/GetList 不加该头，外部客户端不带头则与今日一致。Request 为 null（单元测试直调控制器）时视为无头</summary>
    protected Boolean EnableFieldValidationRequested
    {
        get
        {
            if (EnableFieldValidation) return true;
            var req = Request;
            return req != null &&
                req.Headers.TryGetValue("X-Cube-Field-Validation", out var vs) &&
                vs.ToString().EqualIgnoreCase("1", "true", "yes");
        }
    }

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
    /// <param name="onlyFields">仅校验指定字段（PATCH/批量局部更新）；null 时校验全部表单字段（Insert/Update 整表单）</param>
    /// <returns>字段错误列表，无错误时返回null</returns>
    private static List<FieldError> ValidateEntityFields(TEntity entity, DataObjectMethodType type, IEnumerable<String> onlyFields = null)
    {
        var fields = type == DataObjectMethodType.Insert ? AddFormFields : EditFormFields;
        var errors = new List<FieldError>();

        foreach (var df in fields)
        {
            // 跳过主键和只读字段
            if (df.PrimaryKey || df.ReadOnly) continue;

            // 局部更新（PATCH/批量改字段）只校验本次提交字段：其它字段保持数据库原值，
            // 若因整实体必填校验（如某必填 String 原值为空串）而误伤，则改一个字段也会失败（OSC-260819e483 修复）
            if (onlyFields != null && !onlyFields.Any(f => f.EqualIgnoreCase(df.Name))) continue;

            // 租户字段：关闭多租户或 TenantId=0（全局角色）时查找列 TenantName 为空，不能报「租户不可以为空」
            if (df.IsTenantScopeField()) continue;

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
                        Message = $"{displayName}不可以为空！"
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
                    Message = $"{displayName}长度不能超过{df.Length}个字符！"
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

        var err = ex.GetTrue().Message;
        WriteLog(action, false, err);

        var fieldErrors = BuildFieldErrors(ex);

        // 如果有字段级错误，使用第一条作为主消息提示
        if (fieldErrors != null && fieldErrors.Count > 0)
            err = SysConfig.Develop ? ($"{action}失败！{fieldErrors[0].Message}") : $"{action}失败！";
        else
            err = SysConfig.Develop ? ($"{action}失败！{err}") : $"{action}失败！";

        var code = ex is ApiException ae ? ae.Code : 500;
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
    [DisplayName("导入Excel/Csv/Json/Zip")]
    public virtual IActionResult ImportFile(IFormFile file)
    {
        if (file == null || file.Length <= 0) throw new ArgumentNullException(nameof(file), "未上传文件");

        WriteLog(nameof(ImportFile), true, $"开始导入文件[{file.FileName}]，大小[{file.Length:n0}]字节，类型[{file.ContentType}]");

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
            _ => throw new NotSupportedException($"不支持的导入文件类型[{ext}]"),
        };
        var msg = $"导入[{name}] 共{rs}行";
        WriteLog(nameof(ImportFile), true, msg);

        return Json(0, msg);
    }
    #endregion
}