using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Serialization;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public class EntityController<TEntity> : EntityController<TEntity, TEntity> where TEntity : Entity<TEntity>, new() { }

/// <summary>实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TModel"></typeparam>
public partial class EntityController<TEntity, TModel> : ReadOnlyEntityController<TEntity> where TEntity : Entity<TEntity>, new()
{
    #region 构造
    /// <summary>实例化</summary>
    public EntityController() => PageSetting.IsReadOnly = false;
    #endregion

    #region 默认Action
    private String ProcessDelete(TEntity entity)
    {
        // 假删除与恢复。首次删除标记假删除，假删除后再删除则是真正删除
        var act = "删除";
        var fi = GetDeleteField();
        if (fi != null)
        {
            var restore = GetRequest("restore").ToBoolean();
            // 首次假删除
            if (!entity[fi.Name].ToBoolean())
            {
                entity.SetItem(fi.Name, true);

                if (!Valid(entity, DataObjectMethodType.Update, true))
                    throw new Exception("验证失败");

                OnUpdate(entity);
            }
            // 假删除时第二次提交，则执行恢复，或者没有启用二次删除
            else if (restore || !PageSetting.DoubleDelete)
            {
                entity.SetItem(fi.Name, !restore);
                if (restore) act = "恢复";

                if (!Valid(entity, DataObjectMethodType.Update, true))
                    throw new Exception("验证失败");

                OnUpdate(entity);
            }
            // 假删除时第二次删除，则执行真正删除
            else
            {
                if (!Valid(entity, DataObjectMethodType.Delete, true))
                    throw new Exception("验证失败");

                OnDelete(entity);
            }
        }
        else
        {
            if (!Valid(entity, DataObjectMethodType.Delete, true))
                throw new Exception("验证失败");

            OnDelete(entity);
        }

        return act;
    }

    private static FieldItem GetDeleteField() => Factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase("Deleted", "IsDelete", "IsDeleted") && e.Type == typeof(Boolean));

    /// <summary>保存所有上传文件，并保存附件访问路径到实体对象的对应属性</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="uploadPath">上传目录。为空时默认UploadPath配置</param>
    /// <returns></returns>
    protected virtual async Task<IList<String>> SaveFiles(TEntity entity, String uploadPath = null)
    {
        var rs = new List<String>();
        var list = new List<String>();

        if (!Request.HasFormContentType) return list;

        var files = Request.Form.Files;
        var fields = Factory.Fields;
        foreach (var fi in fields)
        {
            var dc = fi.Field;
            if (dc.IsAttachment())
            {
                // 允许一次性上传多个文件到服务端
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

                if (list.Count > 0)
                {
                    entity.SetItem(fi.Name, list.Join(";"));
                    list.Clear();
                }
            }
        }

        return rs;
    }

    /// <summary>保存单个文件。新建附件</summary>
    /// <param name="entity">实体对象。读取主键与标题，不修改实体对象</param>
    /// <param name="file">文件</param>
    /// <param name="uploadPath">上传目录，默认使用UploadPath配置</param>
    /// <param name="fileName">文件名，如若指定则忽略前面的目录</param>
    /// <returns></returns>
    protected virtual async Task<Attachment> SaveFile(TEntity entity, IFormFile file, String uploadPath, String fileName)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (fileName.IsNullOrEmpty()) fileName = file.FileName;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(SaveFile), new { name = file.Name, fileName, uploadPath });

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
            att.Url = $"/{ss[0]}/{ss[1]}?id={id}";
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

    /// <summary>
    /// 批量启用或禁用
    /// </summary>
    /// <param name="isEnable">启用/禁用</param>
    /// <param name="reason">操作原因</param>
    /// <returns></returns>
    protected virtual Int32 EnableOrDisableSelect(Boolean isEnable, String reason)
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
                if (OnSetField(entity as TEntity, "Enable", isEnable))
                {
                    log.WriteLog("Update", entity);
                    log.WriteLog(entity.GetType(), isEnable ? "Enable" : "Disable", true, reason);

                    count += entity.Update();
                }
            }
        }

        return count;
    }

    /// <summary>设置指定布尔型字段的值</summary>
    /// <remarks>控制器可重载修改行为，例如设置启用禁用时，同步标记假删除</remarks>
    /// <param name="entity">实体对象</param>
    /// <param name="fieldName">字段名</param>
    /// <param name="value">数值</param>
    /// <returns></returns>
    protected virtual Boolean OnSetField(TEntity entity, String fieldName, Boolean value)
    {
        if (entity == null || entity[fieldName].ToBoolean() == value) return false;

        if (!Valid(entity, DataObjectMethodType.Update, true)) return false;

        entity.SetItem(fieldName, value);

        return true;
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
    protected virtual Int32 OnUpdate(TEntity entity)
    {
        if (Request.HasFormContentType)
        {
            // 遍历表单字段，部分字段可能有扩展
            foreach (var item in EditFormFields)
            {
                var field = (item as FormField)?.Expand;
                if (field?.Decode != null && !(entity as IEntity).Dirtys[item.Name])
                {
                    // 获取参数对象，展开参数，从表单字段接收参数
                    var p = field.Decode(entity);
                    if (p != null && p is not String)
                    {
                        // 保存参数对象
                        if (field.ReadForm(p, Request.Form))
                            entity.SetItem(item.Name, field.Encode?.Invoke(p) ?? p.ToJson(true));
                    }
                }
            }
        }

        return entity.Update();
    }

    /// <summary>删除实体对象</summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected virtual Int32 OnDelete(TEntity entity) => entity.Delete();
    #endregion
}