using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.AI;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Jobs;
using NewLife.Reflection;
using NewLife.Serialization;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>数据库管理</summary>
[DisplayName("数据库")]
[EntityAuthorize(PermissionFlags.Detail)]
[AdminArea]
[Menu(26, true, Icon = "DataBoard")]
public class DbController : ControllerBaseX, IPageDataContext
{
    /// <summary>数据库列表</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]")]
    public ActionResult Index()
    {
        var list = BuildDatabaseList();
        return Json(0, null, list);
    }

    /// <summary>收集数据库连接列表（供页面展示与 AI 页面上下文共用）</summary>
    /// <returns>数据库列表</returns>
    private static List<DbItem> BuildDatabaseList()
    {
        var list = new List<DbItem>();
        var dir = NewLife.Setting.Current.BackupPath.GetBasePath().AsDirectory();

        // 读取配置文件
        foreach (var item in DAL.ConnStrs.ToArray())
        {
            var di = new DbItem
            {
                Name = item.Key,
                ConnStr = item.Value
            };

            var dal = DAL.Create(item.Key);
            di.Type = dal.DbType;

            var t = Task.Run(() =>
            {
                try
                {
                    return dal.Db.ServerVersion;
                }
                catch { return null; }
            });
            if (t.Wait(300)) di.Version = t.Result;

            if (dir.Exists) di.Backups = dir.GetFiles($"{dal.ConnName}_*", SearchOption.TopDirectoryOnly).Length;

            list.Add(di);
        }

        return list;
    }

    /// <summary>收集当前页面数据上下文（数据库列表），供 AI 分析当前页面。实现 <see cref="IPageDataContext"/>，get_page_context 优先调用服务端实现</summary>
    /// <returns>数据库列表 JSON。不含连接字符串，避免泄露敏感信息</returns>
    [HttpGet]
    public Task<String> GetPageDataContextAsync()
    {
        var list = BuildDatabaseList();
        var data = list.Select(e => new { name = e.Name, type = e.Type + "", version = e.Version, backups = e.Backups }).ToList();
        return Task.FromResult(new { page = "数据库信息", databases = data }.ToJson());
    }

    /// <summary>备份数据库</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost]
    public ActionResult Backup(String name)
    {
        var sw = Stopwatch.StartNew();

        var dal = DAL.Create(name);
        //var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null, false);
        //var bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, false);
        var bak = dal.Db.CreateMetaData().BackupDatabase(dal.ConnName);

        // 如果备份结果已经是zip，跳过后续压缩
        if (BackupHelper.IsCompressedBackup(bak))
        {
            sw.Stop();
            WriteLog("备份", true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}");
            return Index();
        }

        // SQLite备份文件多做一步WAL checkpoint（仅对.db文件有效）
        var bakFile = bak as String;
        if (!bakFile.IsNullOrEmpty())
            BackupHelper.CompactBackupFile(bakFile);

        // 压缩备份文件为zip
        var file = BackupHelper.GetBackupFile(bak);
        if (file != null)
        {
            var rs = BackupHelper.CompressBackupFile(file);
            if (!rs.IsNullOrEmpty())
            {
                sw.Stop();
                WriteLog("备份", true, $"备份数据库 {name} 到 {rs}，耗时 {sw.Elapsed}");
                return Index();
            }
        }

        sw.Stop();
        WriteLog("备份", true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}");

        return Index();
    }

    /// <summary>备份并压缩数据库</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost]
    public ActionResult BackupAndCompress(String name)
    {
        var sw = Stopwatch.StartNew();

        var dal = DAL.Create(name);
        //var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null, true);
        //var bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, true);
        var bak = $"{name}_{DateTime.Now:yyyyMMddHHmmss}.zip";
        bak = NewLife.Setting.Current.BackupPath.CombinePath(bak);
        //var tables = dal.Tables;
        var tables = EntityFactory.GetTables(name, false);
        dal.BackupAll(tables, bak);

        sw.Stop();
        WriteLog("备份", true, $"备份数据库 {name} 并压缩到 {bak}，耗时 {sw.Elapsed}");

        return Index();
    }

    /// <summary>下载数据库备份</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult Download(String name)
    {
        var dal = DAL.Create(name);
        var xml = DAL.Export(dal.Tables);

        WriteLog("下载", true, "下载数据库架构 " + name);

        return File(xml.GetBytes(), "application/xml", name + ".xml");
    }

    /// <summary>显示数据表</summary>
    /// <param name="name">连接名</param>
    /// <returns>数据表及行数</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/ShowTables")]
    public ActionResult ShowTables(String name)
    {
        if (!TryGetDatabase(name, out var dal)) return Json(1, "非法操作！");

        // 实体模型描述：数据库注释为空时回落，如 AccessRule → 访问规则
        var descriptions = EntityFactory.LoadEntities(name)
            .Select(item => item.AsFactory())
            .Where(factory => factory != null)
            .Select(factory => factory!.Table.DataTable)
            .Where(table => !table.TableName.IsNullOrEmpty() && !table.Description.IsNullOrEmpty())
            .GroupBy(table => table.TableName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Description!, StringComparer.OrdinalIgnoreCase);

        var tables = dal.Tables
            .Select(item =>
            {
                var description = item.Description;
                if (description.IsNullOrEmpty()) descriptions.TryGetValue(item.TableName, out description);
                return new
                {
                    name = item.Name,
                    tableName = item.TableName,
                    description,
                    count = dal.SelectCount(item.TableName, CommandType.Text),
                };
            })
            .OrderBy(item => item.name)
            .ToList();

        return Json(0, null, new { name, tables });
    }

    /// <summary>显示实体类</summary>
    /// <param name="name">连接名</param>
    /// <returns>实体类及行数</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/ShowEntities")]
    public ActionResult ShowEntities(String name)
    {
        if (!TryGetDatabase(name, out _)) return Json(1, "非法操作！");

        var entities = EntityFactory.LoadEntities(name)
            .Select(item =>
            {
                var factory = item.AsFactory();
                if (factory == null) return null;

                var table = factory.Table.DataTable;
                var exists = factory.Session.Dal.TableNames.Contains(table.TableName);
                return new
                {
                    name = item.Name,
                    tableName = table.TableName,
                    description = table.Description,
                    count = exists ? factory.Session.LongCount : (Int64?)null,
                };
            })
            .Where(item => item != null)
            .OrderBy(item => item!.name)
            .ToList();

        return Json(0, null, new { name, entities });
    }

    /// <summary>显示实体字段（数据字典）。各列对齐 CubeNC Db/Entities.cshtml 的字段架构定义</summary>
    /// <param name="name">连接名</param>
    /// <param name="type">实体类名</param>
    /// <returns>实体字段清单</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/ShowEntityFields")]
    public ActionResult ShowEntityFields(String name, String type)
    {
        if (!TryGetDatabase(name, out _)) return Json(1, "非法操作！");

        var entityType = EntityFactory.LoadEntities(name).FirstOrDefault(item => item.Name.EqualIgnoreCase(type));
        var factory = entityType?.AsFactory();
        if (factory == null) return Json(1, "实体不存在！");

        var table = factory.Table.DataTable;
        var fields = factory.Fields.Select(field =>
        {
            var column = field.Field;
            return new
            {
                name = field.Name,
                displayName = field.DisplayName,
                type = field.Type?.FullName.TrimPrefix("System."),
                length = field.Length,
                precision = column.Precision,
                scale = column.Scale,
                key = column.Identity ? "AI"
                    : field.PrimaryKey ? "PK"
                    : table.Indexes.Any(index => index.Unique && index.Columns.Length == 1 && index.Columns[0].EqualIgnoreCase(field.Name, field.ColumnName)) ? "UQ"
                    : null,
                nullable = column.Nullable,
                description = column.Description?.TrimPrefix(column.DisplayName).TrimPrefix("。", "，"),
            };
        }).ToList();

        return Json(0, null, new { name, type = entityType!.Name, tableName = table.TableName, fields });
    }

    /// <summary>显示数据表字段（无实体模型的表字段字典）。数据源为数据库架构，列对齐 CubeNC Db/Entities.cshtml 定义</summary>
    /// <param name="name">连接名</param>
    /// <param name="table">表名</param>
    /// <returns>数据表字段清单</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/ShowTableFields")]
    public ActionResult ShowTableFields(String name, String table)
    {
        if (!TryGetDatabase(name, out var dal)) return Json(1, "非法操作！");

        var dbTable = dal.Tables.FirstOrDefault(item => item.TableName.EqualIgnoreCase(table));
        if (dbTable == null) return Json(1, "数据表不存在！");

        var fields = dbTable.Columns.Select(column => new
        {
            name = column.ColumnName.IsNullOrEmpty() ? column.Name : column.ColumnName,
            displayName = column.DisplayName,
            type = column.DataType?.Name,
            length = column.Length,
            precision = column.Precision,
            scale = column.Scale,
            key = column.Identity ? "AI"
                : column.PrimaryKey ? "PK"
                : dbTable.Indexes.Any(index => index.Unique && index.Columns.Length == 1 && index.Columns[0].EqualIgnoreCase(column.Name, column.ColumnName)) ? "UQ"
                : null,
            nullable = column.Nullable,
            description = column.Description?.TrimPrefix(column.DisplayName).TrimPrefix("。", "，"),
        }).ToList();

        return Json(0, null, new { name, table = dbTable.TableName, fields });
    }

    /// <summary>模型差异。返回数据库中存在而实体模型没有的字段</summary>
    /// <param name="name">连接名</param>
    /// <returns>模型差异</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/ModelDiff")]
    public ActionResult ModelDiff(String name)
    {
        if (!TryGetDatabase(name, out var dal)) return Json(1, "非法操作！");

        var entityTables = EntityFactory.LoadEntities(name)
            .Select(item => (item, factory: item.AsFactory()))
            .Where(item => item.factory != null)
            .Select(item => (item.item.Name, table: item.factory!.Table.DataTable))
            .Where(item => !item.table.TableName.IsNullOrEmpty())
            .ToDictionary(item => item.table.TableName, item => (item.Name, item.table), StringComparer.OrdinalIgnoreCase);

        var tables = new List<Object>();
        foreach (var dbTable in dal.Tables.OrderBy(item => item.Name))
        {
            var hasEntityModel = entityTables.TryGetValue(dbTable.TableName, out var entity);
            var entityColumns = hasEntityModel
                ? new HashSet<String>(
                    entity.table.Columns.Select(item => GetColumnName(item)),
                    StringComparer.OrdinalIgnoreCase)
                : null;
            var columns = dbTable.Columns
                .Where(item => entityColumns == null || !entityColumns.Contains(GetColumnName(item)))
                .Select(item => new
                {
                    name = item.Name,
                    columnName = item.ColumnName,
                    dataType = item.DataType?.Name,
                })
                .ToList();

            if (columns.Count == 0) continue;

            tables.Add(new
            {
                name = hasEntityModel ? entity.Name : dbTable.Name,
                tableName = dbTable.TableName,
                displayName = dbTable.DisplayName,
                hasEntityModel,
                columns,
            });
        }

        WriteLog("模型差异", true, $"查看数据库 {name} 模型差异，共 {tables.Count} 张表存在差异");
        return Json(0, null, new { name, tables });
    }

    /// <summary>压缩数据库（回收空闲空间；SQLite 执行 VACUUM）</summary>
    /// <param name="name">连接名</param>
    /// <returns>压缩结果</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost("/api/[area]/[controller]/Compact")]
    public ActionResult Compact(String name)
    {
        if (!TryGetDatabase(name, out var dal)) return Json(1, "非法操作！");

        var sw = Stopwatch.StartNew();
        try
        {
            dal.Db.CreateMetaData().Invoke("CompactDatabase");
            sw.Stop();
            WriteLog("压缩", true, $"压缩数据库 {name} 完成，耗时 {sw.Elapsed}");
            return Json(0, "压缩完成");
        }
        catch (Exception ex)
        {
            sw.Stop();
            WriteLog("压缩", false, $"压缩数据库 {name} 失败：{ex.Message}");
            return Json(1, ex.Message);
        }
    }

    private static Boolean TryGetDatabase(String name, out DAL dal)
    {
        dal = null!;
        if (name.IsNullOrEmpty() || DAL.ConnStrs == null || !DAL.ConnStrs.ContainsKey(name)) return false;

        dal = DAL.Create(name);
        return true;
    }

    private static String GetColumnName(IDataColumn column) => column.ColumnName.IsNullOrEmpty() ? column.Name : column.ColumnName;
}