using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Extensions;
using NewLife.Data;
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
            return entity.ToOkApiResponse($"{act}成功！");
            //return new ApiResponse<TEntity>(0, $"{act}成功！", entity);
        }
        catch (Exception ex)
        {
            //var code = ex is ApiException ae ? ae.Code : 500;
            var err = ex.GetTrue().Message;
            WriteLog("Delete", false, err);


            if (ex is ApiException ae)//主动抛出的异常，用对应Code返回
                return entity.ToFailApiResponse(ae.Code, $"{act}失败！{err}");
            return entity.ToFailApiResponse($"{act}失败！{err}");// 兼容旧版 Yann

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

        if (model is IModel src)
            entity.CopyFrom(src, true);
        else
            entity.Copy(model);

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

            OnInsert(entity);

            // 先插入再保存附件，主要是为了在附件表关联业务对象主键
            var fs = await SaveFiles(entity);
            if (fs.Count > 0) OnUpdate(entity);

            if (LogOnChange) LogProvider.Provider.WriteLog("Insert", entity);

            return entity.ToOkApiResponse("添加成功！");
            //return new ApiResponse<TEntity>(0, "添加成功！", entity);
        }
        catch (Exception ex)
        {

            var msg = ex.Message;

            WriteLog("Add", false, msg);

            msg = SysConfig.Develop ? ("添加失败！" + msg) : "添加失败！";

            // 添加失败，ID清零，否则会显示保存按钮
            entity[Factory.Unique.Name] = 0;

            if (ex is ApiException ae)//主动抛出的异常，用对应Code返回
                return entity.ToFailApiResponse(ae.Code, msg);
            return entity.ToFailApiResponse(msg);// 兼容旧版 Yann
            //return new ApiResponse<TEntity>(code, msg, entity);
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

        if (model is IModel src)
            entity.CopyFrom(src, true);
        else
            entity.Copy(model, false, uk.Name);

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

            await SaveFiles(entity);

            OnUpdate(entity);

            return entity.ToOkApiResponse("保存成功！");
            //return new ApiResponse<TEntity>(0, "保存成功！", entity);
        }
        catch (Exception ex)
        {
            //var code = ex is ApiException ae ? ae.Code : 500;
            var err = ex.Message;
            ModelState.AddModelError((ex as ArgumentException)?.ParamName ?? "", ex.Message);

            WriteLog("Edit", false, err);

            err = SysConfig.Develop ? ("保存失败！" + err) : "保存失败！";
            if (ex is ApiException ae)//主动抛出的异常，用对应Code返回
                return default(TEntity).ToFailApiResponse(ae.Code, err);
            return default(TEntity).ToFailApiResponse(err);// 兼容旧版 Yann

            //return new ApiResponse<TEntity>(code, err, null);
        }
    }
    #endregion
}