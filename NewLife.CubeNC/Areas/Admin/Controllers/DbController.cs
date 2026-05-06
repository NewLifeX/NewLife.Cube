using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Reflection;
using XCode;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>数据库管理</summary>
[DisplayName("数据库")]
[EntityAuthorize(PermissionFlags.Detail)]
[AdminArea]
[Menu(26, true, Icon = "fa-database")]
public class DbController : ControllerBaseX
{
    /// <summary>数据库列表</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    public async Task<ActionResult> Index()
    {
        var list = new List<DbItem>();
        var dir = NewLife.Setting.Current.BackupPath.GetBasePath().AsDirectory();
        var timeout = TimeSpan.FromMilliseconds(300);

        foreach (var item in DAL.ConnStrs.ToArray())
        {
            var di = new DbItem { Name = item.Key, ConnStr = item.Value };

            try
            {
                var dal = DAL.Create(item.Key);
                di.Type = dal.DbType;

                var driver = dal.Db.Factory;
                if (driver != null)
                {
                    var ax = AssemblyX.Create(driver.GetType().Assembly);
                    di.Driver = ax.Name;
                    di.DriverVersion = ax.FileVersion;
                }

                using var cts = new CancellationTokenSource(timeout);
                try
                {
                    di.Version = await Task.Run(() => dal.Db.ServerVersion, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    di.Version = "获取数据库版本信息超时（300ms）";
                }
                catch (Exception ex)
                {
                    di.Version = $"获取版本失败: {ex.Message}";
                }

                di.Tables = dal.Tables.Count;
                di.Entities = EntityFactory.LoadEntities(item.Key).Count();

                if (dir.Exists)
                {
                    di.Backups = dir.GetFiles($"{dal.ConnName}_*", SearchOption.TopDirectoryOnly).Length;
                }
            }
            catch (Exception ex)
            {
                di.Version = $"初始化失败: {ex.Message}";
            }

            list.Add(di);
        }

        return View("Index", list);
    }

    /// <summary>备份数据库</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    public async Task<ActionResult> Backup(String name)
    {
        var sw = Stopwatch.StartNew();

        var dal = DAL.Create(name);
        //var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null, false);
        var bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, false);

        sw.Stop();
        WriteLog("备份", true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}");

        return await Index();
    }

    /// <summary>备份并压缩数据库</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    public async Task<ActionResult> BackupAndCompress(String name)
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

        return await Index();
    }

    /// <summary>下载数据库备份</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult Download(String name)
    {
        var dal = DAL.Create(name);
        var xml = DAL.Export(dal.Tables);

        WriteLog("下载", true, "下载数据库架构 " + name);

        return File(xml.GetBytes(), "application/xml", name + ".xml");
    }

    /// <summary>显示数据表</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult ShowTables(String name)
    {
        if (!name.EqualIgnoreCase(DAL.ConnStrs.Keys.ToArray())) throw new Exception("非法操作！");

        var dal = DAL.Create(name);

        var list = new List<DbTableModel>();
        foreach (var item in dal.Tables)
        {
            var dm = new DbTableModel
            {
                Name = item.Name,
                Table = item,
                Count = dal.SelectCount(item.TableName, CommandType.Text),
            };

            list.Add(dm);
        }

        var model = new DbTablesModel
        {
            Name = name,
            Tables = list.OrderBy(e => e.Name).ToList(),
        };

        return View("Tables", model);
    }

    /// <summary>显示实体类</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult ShowEntities(String name)
    {
        if (!name.EqualIgnoreCase(DAL.ConnStrs.Keys.ToArray())) throw new Exception("非法操作！");

        var types = EntityFactory.LoadEntities(name);

        var list = new List<DbEntityModel>();
        foreach (var item in types)
        {
            var factory = item.AsFactory();
            if (factory == null) continue;

            var dm = new DbEntityModel
            {
                Name = item.Name,
                Factory = factory,
                Table = factory.Table.DataTable,
            };

            // 如果数据表已存在，则获取行数
            if (factory.Session.Dal.TableNames.Contains(dm.Table.TableName))
                dm.Count = factory.Session.LongCount;

            list.Add(dm);
        }

        var model = new DbEntitiesModel
        {
            Name = name,
            Entities = list.OrderBy(e => e.Name).ToList(),
        };

        return View("Entities", model);
    }

    /// <summary>模型差异。对比数据库和实体类的表架构，显示数据库多出来的字段</summary>
    /// <param name="name">连接名</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult ModelDiff(String name)
    {
        if (!name.EqualIgnoreCase(DAL.ConnStrs.Keys.ToArray())) throw new Exception("非法操作！");

        var dal = DAL.Create(name);
        var dbTables = dal.Tables;

        // 构建实体表字典：DB表名 -> (实体类名, 实体DataTable)
        var entityTypes = EntityFactory.LoadEntities(name);
        var entityTables = new Dictionary<String, (String EntityName, IDataTable DataTable)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entityType in entityTypes)
        {
            var factory = entityType.AsFactory();
            if (factory == null) continue;

            var dt = factory.Table.DataTable;
            if (dt != null && !dt.TableName.IsNullOrEmpty())
                entityTables[dt.TableName] = (entityType.Name, dt);
        }

        var diffList = new List<DbTableDiffItem>();

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
                var entityColumnNames = new HashSet<String>(
                    entityInfo.DataTable.Columns.Select(c => c.ColumnName.IsNullOrEmpty() ? c.Name : c.ColumnName),
                    StringComparer.OrdinalIgnoreCase);

                extraColumns = dbTable.Columns
                    .Where(c => !entityColumnNames.Contains(c.ColumnName.IsNullOrEmpty() ? c.Name : c.ColumnName))
                    .ToList();
            }

            if (extraColumns.Count == 0) continue;

            diffList.Add(new DbTableDiffItem
            {
                Name = hasEntityModel ? entityInfo.EntityName : dbTable.Name,
                TableName = dbTable.TableName,
                DisplayName = dbTable.DisplayName,
                HasEntityModel = hasEntityModel,
                ExtraColumns = extraColumns,
                XmlFragment = BuildColumnsXml(extraColumns),
            });
        }

        WriteLog("模型差异", true, $"查看数据库 {name} 模型差异，共 {diffList.Count} 张表存在差异");

        var model = new DbDiffModel
        {
            Name = name,
            Tables = diffList,
        };

        return View("Diff", model);
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
}