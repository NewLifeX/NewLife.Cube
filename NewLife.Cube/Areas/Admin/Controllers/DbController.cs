using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.AI;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Jobs;
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
}