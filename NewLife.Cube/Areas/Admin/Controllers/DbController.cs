using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;
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
    /// <returns>数据库连接列表 JSON</returns>
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

            // 连接串可能含密码：返回前按受保护密钥脱敏，页面无需完整连接串
            try
            {
                di.ConnStr = dal.ProtectedKey.Hide(item.Value);
            }
            catch { }

            // 驱动信息：程序集名与文件版本
            var driver = dal.Db.Factory;
            if (driver != null)
            {
                var ax = AssemblyX.Create(driver.GetType().Assembly);
                di.Driver = ax.Name;
                di.DriverVersion = ax.FileVersion;
            }

            var t = Task.Run(() =>
            {
                try
                {
                    return dal.Db.ServerVersion;
                }
                catch { return null; }
            });
            if (t.Wait(300)) di.Version = t.Result;

            // 实体数与表数：连接异常时保持 0，避免整页失败
            try
            {
                di.Tables = dal.Tables.Count;
                di.Entities = EntityFactory.LoadEntities(item.Key).Count();
            }
            catch { }

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
        var data = list.Select(e => new { name = e.Name, type = e.Type + "", version = e.Version, driver = e.Driver, driverVersion = e.DriverVersion, entities = e.Entities, tables = e.Tables, backups = e.Backups }).ToList();
        return Task.FromResult(new { page = L.T("Database information"), databases = data }.ToJson());
    }

    /// <summary>压缩数据库（回收空闲空间）。对SQLite执行VACUUM</summary>
    /// <param name="name">连接名</param>
    /// <returns>压缩结果 JSON</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost("/api/[area]/[controller]/compact")]
    public ActionResult Compact(String? name)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var dal = DAL.Create(name);
            var meta = dal.Db.CreateMetaData();
            meta.Invoke("CompactDatabase");
            sw.Stop();
            WriteLog(L.T("Compact"), true, L.T("Compressed database {0} in {1}", name, sw.Elapsed));
        }
        catch (Exception ex)
        {
            sw.Stop();
            WriteLog(L.T("Compact"), false, L.T("Failed to compact database {0}: {1}", name, ex.Message));
        }

        return Index();
    }

    /// <summary>数据表结构。数据库实际表与列快照</summary>
    /// <param name="name">连接名</param>
    /// <returns>表结构列表</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/tables")]
    public ActionResult ShowTables(String name)
    {
        ValidateConnName(name);

        var dal = DAL.Create(name);
        var list = new List<DbSchemaTable>();
        foreach (var item in dal.Tables)
        {
            var columns = new List<DbSchemaColumn>();
            foreach (var column in item.Columns)
            {
                columns.Add(new DbSchemaColumn
                {
                    Name = column.ColumnName,
                    ColumnName = column.ColumnName,
                    DisplayName = column.DisplayName,
                    Type = column.RawType ?? column.DataType?.FullName.TrimPrefix("System."),
                    Length = column.Length,
                    Precision = column.Precision,
                    Scale = column.Scale,
                    PrimaryKey = column.PrimaryKey,
                    Identity = column.Identity,
                    Nullable = column.Nullable,
                    Unique = item.Indexes.Any(e => e.Unique && e.Columns.Length == 1 && e.Columns[0].EqualIgnoreCase(column.Name, column.ColumnName)),
                    Description = column.Description?.TrimPrefix(column.DisplayName).TrimPrefix("。", "，"),
                });
            }

            list.Add(new DbSchemaTable
            {
                Name = item.DisplayName,
                TableName = item.TableName,
                DisplayName = item.DisplayName,
                Count = dal.SelectCount(item.TableName, CommandType.Text),
                Columns = columns,
            });
        }

        return Json(0, null, list.OrderBy(e => e.Name).ToList());
    }

    /// <summary>实体结构。实体类模型与列快照</summary>
    /// <param name="name">连接名</param>
    /// <returns>实体结构列表</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/entities")]
    public ActionResult ShowEntities(String name)
    {
        ValidateConnName(name);

        var dal = DAL.Create(name);
        var list = new List<DbSchemaTable>();
        foreach (var item in EntityFactory.LoadEntities(name))
        {
            var factory = item.AsFactory();
            if (factory == null) continue;

            var table = factory.Table.DataTable;
            var columns = new List<DbSchemaColumn>();
            foreach (var field in factory.Fields)
            {
                var column = field.Field;
                columns.Add(new DbSchemaColumn
                {
                    Name = field.Name,
                    ColumnName = field.ColumnName,
                    DisplayName = field.DisplayName,
                    Type = field.Type?.FullName.TrimPrefix("System."),
                    Length = field.Length,
                    Precision = column?.Precision ?? 0,
                    Scale = column?.Scale ?? 0,
                    PrimaryKey = field.PrimaryKey,
                    Identity = column?.Identity ?? false,
                    Nullable = column?.Nullable ?? false,
                    Unique = table.Indexes.Any(e => e.Unique && e.Columns.Length == 1 && e.Columns[0].EqualIgnoreCase(field.Name, field.ColumnName)),
                    Description = column?.Description?.TrimPrefix(field.DisplayName).TrimPrefix("。", "，"),
                });
            }

            var count = 0L;
            if (factory.Session.Dal.TableNames.Contains(table.TableName)) count = factory.Session.LongCount;

            list.Add(new DbSchemaTable
            {
                Name = item.Name,
                TableName = table.TableName,
                DisplayName = table.DisplayName,
                Count = count,
                Columns = columns,
            });
        }

        return Json(0, null, list.OrderBy(e => e.Name).ToList());
    }

    /// <summary>模型差异。对比数据库与实体类的表架构，返回数据库多出来的字段</summary>
    /// <param name="name">连接名</param>
    /// <returns>差异列表</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]/diff")]
    public ActionResult ModelDiff(String name)
    {
        ValidateConnName(name);

        var dal = DAL.Create(name);
        var dbTables = dal.Tables;

        // 构建实体表字典：DB表名 -> (实体类名, 实体DataTable)
        var entityTables = new Dictionary<String, (String EntityName, IDataTable DataTable)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entityType in EntityFactory.LoadEntities(name))
        {
            var factory = entityType.AsFactory();
            if (factory == null) continue;

            var dt = factory.Table.DataTable;
            if (dt != null && !dt.TableName.IsNullOrEmpty()) entityTables[dt.TableName] = (entityType.Name, dt);
        }

        var list = new List<DbDiffTable>();
        foreach (var dbTable in dbTables.OrderBy(t => t.Name))
        {
            List<IDataColumn> extraColumns;
            var hasEntityModel = entityTables.TryGetValue(dbTable.TableName, out var entityInfo);

            if (!hasEntityModel)
            {
                // 数据库有该表但无对应实体类，所有字段均视为多出字段
                extraColumns = dbTable.Columns.ToList();
            }
            else
            {
                // 找出数据库有但实体模型没有的字段（按数据库列名匹配）
                static String GetColName(IDataColumn c) => c.ColumnName.IsNullOrEmpty() ? c.Name : c.ColumnName;

                var entityColumnNames = new HashSet<String>(
                    entityInfo.DataTable.Columns.Select(c => GetColName(c)),
                    StringComparer.OrdinalIgnoreCase);

                extraColumns = dbTable.Columns
                    .Where(c => !entityColumnNames.Contains(GetColName(c)))
                    .ToList();
            }

            if (extraColumns.Count == 0) continue;

            list.Add(new DbDiffTable
            {
                Name = hasEntityModel ? entityInfo.EntityName : dbTable.Name,
                TableName = dbTable.TableName,
                DisplayName = dbTable.DisplayName,
                HasEntityModel = hasEntityModel,
                ExtraColumns = extraColumns.Select(e => e.Name).ToArray(),
                XmlFragment = BuildColumnsXml(extraColumns),
            });
        }

        WriteLog(L.T("Model diff"), true, L.T("Checked model diff of database {0}: {1} tables differ", name, list.Count));

        return Json(0, null, list);
    }

    /// <summary>校验连接名必须来自配置，避免任意连接字符串注入</summary>
    /// <param name="name">连接名</param>
    private static void ValidateConnName(String name)
    {
        if (!DAL.ConnStrs.ContainsKey(name)) throw new XException("Illegal operation!");
    }

    /// <summary>将数据列集合序列化为 XCode Model.xml 格式的 Column XML 片段</summary>
    /// <param name="columns">数据列集合</param>
    /// <returns>XML 字符串片段</returns>
    private static String BuildColumnsXml(IList<IDataColumn> columns)
    {
        var sb = new StringBuilder();
        foreach (var col in columns)
        {
            sb.Append("        <Column");
            sb.Append($" Name=\"{col.Name}\"");

            if (!col.ColumnName.IsNullOrEmpty() && !col.ColumnName.EqualIgnoreCase(col.Name))
                sb.Append($" ColumnName=\"{col.ColumnName}\"");

            if (col.DataType != null)
                sb.Append($" DataType=\"{col.DataType.Name}\"");

            if (col.Length > 0)
                sb.Append($" Length=\"{col.Length}\"");

            if (col.Precision > 0)
                sb.Append($" Precision=\"{col.Precision}\"");

            if (col.Scale > 0)
                sb.Append($" Scale=\"{col.Scale}\"");

            if (col.Identity)
                sb.Append(" Identity=\"True\"");

            if (col.PrimaryKey)
                sb.Append(" PrimaryKey=\"True\"");

            if (!col.Nullable)
                sb.Append(" Nullable=\"False\"");

            if (!col.Description.IsNullOrEmpty())
            {
                var desc = col.Description
                    .Replace("&", "&amp;")
                    .Replace("\"", "&quot;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");
                sb.Append($" Description=\"{desc}\"");
            }

            sb.AppendLine(" />");
        }
        return sb.ToString();
    }

    /// <summary>备份数据库</summary>
    /// <param name="name">连接名；空则处理全部连接</param>
    /// <returns>备份后返回数据库列表</returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost]
    public ActionResult Backup(String? name)
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
            WriteLog(L.T("Backup"), true, L.T("Backed up database {0} to {1}; elapsed {2}", name, bak, sw.Elapsed));
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
                WriteLog(L.T("Backup"), true, L.T("Backed up database {0} to {1}; elapsed {2}", name, rs, sw.Elapsed));
                return Index();
            }
        }

        sw.Stop();
        WriteLog(L.T("Backup"), true, L.T("Backed up database {0} to {1}; elapsed {2}", name, bak, sw.Elapsed));

        return Index();
    }

    /// <summary>备份并压缩数据库</summary>
    /// <param name="name">连接名；空则处理全部连接</param>
    /// <returns>备份并压缩后返回数据库列表</returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    [HttpPost]
    public ActionResult BackupAndCompress(String? name)
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
        WriteLog(L.T("Backup"), true, L.T("Backed up and compressed database {0} to {1}; elapsed {2}", name, bak, sw.Elapsed));

        return Index();
    }

    /// <summary>下载数据库备份</summary>
    /// <param name="name">连接名</param>
    /// <returns>备份文件下载响应</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public ActionResult Download(String name)
    {
        var dal = DAL.Create(name);
        var xml = DAL.Export(dal.Tables);

        WriteLog(L.T("Download"), true, L.T("Downloaded database schema {0}.", name));

        return File(xml.GetBytes(), "application/xml", name + ".xml");
    }
}