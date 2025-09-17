using System.ComponentModel;
using System.IO.Compression;
using System.Text;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.IO;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Web;
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

    #region 导入Excel/Csv/Json/Zip
    /// <summary>导入数据，保存落库</summary>
    /// <remarks>
    /// 此时得到的实体列表，都是全新创建，用于接收上传数据。
    /// 业务上，还需要考虑跟旧数据进行合并，已存在更新，不存在则新增。
    /// </remarks>
    /// <param name="list">导入实体列表</param>
    /// <param name="headers">表头</param>
    /// <param name="fields">导入字段</param>
    /// <returns></returns>
    protected virtual Int32 OnImport(IList<TEntity> list, IList<String> headers, IList<FieldItem> fields)
    {
        // 如果存在主键或者唯一索引，先查找是否存在，如果存在则更新，否则新增
        fields = fields.Where(e => e != null).ToList();
        var names = fields.Select(e => e.Name).ToList();
        var uk = Factory.Unique;
        if (uk != null && fields.Any(e => e.Name == uk.Name))
        {
            for (var i = 0; i < list.Count; i++)
            {
                var entity = list[i];
                var old = Factory.FindByKey(entity[uk.Name]) as TEntity;
                if (old != null) list[i] = CopyFrom(old, entity, fields);
            }
        }
        else
        {
            // 唯一索引
            var di = Factory.Table.DataTable.Indexes.FirstOrDefault(e => e.Unique && !e.Columns.Except(names).Any());
            if (di != null)
            {
                var fs = fields.Where(e => di.Columns.Contains(e.Name)).ToList();
                for (var i = 0; i < list.Count; i++)
                {
                    var entity = list[i];
                    var exp = new WhereExpression();
                    foreach (var fi in fs)
                    {
                        exp &= fi.Equal(entity[fi.Name]);
                    }
                    var old = Factory.Find(exp) as TEntity;
                    if (old != null) list[i] = CopyFrom(old, entity, fields);
                }
            }
        }

        return list.Save();
    }

    TEntity CopyFrom(TEntity entity, IModel source, IList<FieldItem> fields)
    {
        if (fields == null || fields.Count == 0) return entity;

        foreach (var fi in fields)
        {
            if (fi != null) entity.SetItem(fi.Name, source[fi.Name]);
        }

        return entity;
    }

    /// <summary>导入数据默认保存</summary>
    /// <param name="factory"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    protected virtual Int32 OnImport(IEntityFactory factory, IEnumerable<IEntity> list)
    {
        // 判断已有数据，如果没有直接插入，如果较少则合并，否则Upsert
        if (factory.Session.Count == 0) return list.Insert();

        if (factory.Session.Count < 10000 && factory.Unique != null)
            return factory.FindAll().Merge(list, [factory.Unique.Name], false).Count;

        return list.Upsert();
    }

    /// <summary>导入Excel</summary>
    /// <param name="name">文件名</param>
    /// <param name="stream">数据流。可能来自上传文件，也可能来自解压缩文件</param>
    /// <param name="factory">实体工厂。不一定是当前实体类</param>
    /// <param name="page">分页请求参数</param>
    /// <returns></returns>
    protected virtual Int32 ImportExcel(String name, Stream stream, IEntityFactory factory, Pager page)
    {
        using var reader = new ExcelReader(stream, Encoding.UTF8);

        var headers = new List<String>();
        var fields = new List<FieldItem>();
        var list = new List<TEntity>();
        var total = 0;
        var blank = 0;
        var result = 0;
        var batchSize = 10_000;

        // 流式读取Excel数据，一边解析一边处理，避免一次性加载占用大量内存
        foreach (var row in reader.ReadRows())
        {
            if (fields.Count == 0)
            {
                // 读取标题行，找到对应字段
                foreach (var item in row)
                {
                    headers.Add(item + "");

                    // 找到对应字段，可能为空
                    var field = factory.Fields.FirstOrDefault(e => (item + "").EqualIgnoreCase(e.Name, e.DisplayName));
                    fields.Add(field);
                }

                // 如果没有找到任何字段，说明该行不是标题行，需要检查下一行。该行可能是注释说明行
                if (!fields.Any(e => e != null))
                {
                    blank++;
                    headers.Clear();
                    fields.Clear();
                }
            }
            else
            {
                total++;
                // 如果该行所有列都为空，则直接跳过
                if (row.All(e => e == null || e.ToString().Trim().Length == 0))
                {
                    blank++;
                    continue;
                }

                // 实例化实体对象，读取一行，逐个字段赋值
                var entity = factory.Create() as TEntity;
                for (var i = 0; i < row.Length && i < fields.Count; i++)
                {
                    var field = fields[i];
                    if (field != null)
                        entity.SetItem(field.Name, row[i].ChangeType(field.Type));
                    else
                        entity[headers[i]] = row[i];
                }

                if ((entity as IEntity).HasDirty) list.Add(entity);

                // 如果足够一批，先保存一批
                if (list.Count >= batchSize)
                {
                    result += OnImport(list, headers, fields);
                    list.Clear();
                }
            }
        }

        // 保存数据
        if (list.Count > 0)
        {
            result += OnImport(list, headers, fields);
        }

        var msg = $"导入[{name}]，共[{total}]行，成功[{result}]行，{blank}行无效！";
        WriteLog("导入Excel", true, msg);

        return result;
    }

    /// <summary>导入Csv</summary>
    /// <param name="name">文件名</param>
    /// <param name="stream">数据流。可能来自上传文件，也可能来自解压缩文件</param>
    /// <param name="factory">实体工厂。不一定是当前实体类</param>
    /// <param name="page">分页请求参数</param>
    /// <returns></returns>
    protected virtual Int32 ImportCsv(String name, Stream stream, IEntityFactory factory, Pager page) => 0;

    /// <summary>导入Json</summary>
    /// <param name="name">文件名</param>
    /// <param name="stream">数据流。可能来自上传文件，也可能来自解压缩文件</param>
    /// <param name="factory">实体工厂。不一定是当前实体类</param>
    /// <param name="page">分页请求参数</param>
    /// <returns></returns>
    protected virtual Int32 ImportJson(String name, Stream stream, IEntityFactory factory, Pager page)
    {
        var type = Activator.CreateInstance(factory.EntityType);
        var json = new JsonParser(stream.ToStr());
        var list = json.Decode() as IList<Object>;

        // 解析json
        var rs = 0;
        foreach (var item in list)
        {
            var data = item as IDictionary<String, Object>;
            //if (data[factory.Fields[1].Name].ToString() == factory.Fields[1].DisplayName) //判断首行是否为标体列
            //    continue;

            // 检查主字段是否重复
            if (Entity<TEntity>.Find(factory.Master.Name, data[factory.Master.Name]) == null)
            {
                var entity = factory.Create();
                foreach (var field in factory.Fields)
                {
                    if (data.TryGetValue(field.Name, out var value))
                        entity.SetItem(field.Name, value);
                    else if (!field.IsNullable)
                        entity.SetItem(field.Name, null);
                }

                rs += factory.Session.Insert(entity);
            }
        }

        return rs;
    }

    /// <summary>从数据流导入Zip。内部文件</summary>
    /// <param name="name">文件名</param>
    /// <param name="stream">数据流。可能来自上传文件，也可能来自解压缩文件</param>
    /// <param name="factory">实体工厂。不一定是当前实体类</param>
    /// <param name="page">分页请求参数</param>
    /// <returns></returns>
    protected virtual Int32 ImportZip(String name, Stream stream, IEntityFactory factory, Pager page)
    {
        // 解压并读取数据集
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        var rs = 0;
        foreach (var entry in zip.Entries)
        {
            if (entry.Length <= 0) continue;

            var ext = Path.GetExtension(entry.Name).ToLower();
            if (ext.IsNullOrEmpty()) return 0;

            var entryName = entry.Name[..^ext.Length];
            var type = Type.GetType(entryName) ?? entryName.GetTypeEx();
            //if (type != null && !type.As<IEntity>()) return 0;

            // 从名称定位实体工厂
            var factory2 = type != null && type.As<IEntity>() ? type?.AsFactory() : null;
            if (factory2 == null)
            {
                type = Factory.EntityType;
                if (entryName.EqualIgnoreCase(type.Name, type.FullName)) factory2 = Factory;
            }
            factory2 ??= factory;

            // 仅解析当前控制器对应的数据集，其它数据交给 OnImportZip 重载处理
            using var entryStream = entry.Open();
            rs += ext switch
            {
                ".xls" or ".xlsx" => ImportExcel(entryName, entryStream, factory2, page),
                ".csv" => ImportCsv(entryName, entryStream, factory2, page),
                ".json" => ImportJson(entryName, entryStream, factory2, page),
                ".zip" => ImportZip(entryName, entryStream, factory2, page),
                ".db" => ImportDb(entryName, entryStream, factory2, page),
                _ => 0,
            };
        }

        return rs;
    }

    /// <summary>导入Zip时，处理附属数据集或自定义导入逻辑</summary>
    /// <remarks>根据名称反射实体工厂，然后批量插入数据库</remarks>
    /// <param name="name">文件名</param>
    /// <param name="stream">数据流。可能来自上传文件，也可能来自解压缩文件</param>
    /// <param name="factory">实体工厂。不一定是当前实体类</param>
    /// <param name="page">分页请求参数</param>
    protected virtual Int32 ImportDb(String name, Stream stream, IEntityFactory factory, Pager page)
    {
        if (factory != null)
        {
            var list = factory.Read(stream);
            if (list != null) return OnImport(factory, list);
        }

        return 0;
    }
    #endregion
}