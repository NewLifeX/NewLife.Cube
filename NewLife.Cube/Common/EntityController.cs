using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
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
    [HttpDelete("/[area]/[controller]")]
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

    /// <summary>添加数据</summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [DisplayName("添加{type}")]
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost("/[area]/[controller]")]
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
    [HttpPut("/[area]/[controller]")]
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
}