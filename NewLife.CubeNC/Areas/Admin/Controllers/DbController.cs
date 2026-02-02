using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
    public ActionResult Index()
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

            di.Tables = dal.Tables.Count;
            di.Entities = EntityFactory.LoadEntities(item.Key).Count();

            if (dir.Exists) di.Backups = dir.GetFiles($"{dal.ConnName}_*", SearchOption.TopDirectoryOnly).Length;

            list.Add(di);
        }

        return View("Index", list);
    }

    /// <summary>备份数据库</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Insert)]
    public ActionResult Backup(String name)
    {
        var sw = Stopwatch.StartNew();

        var dal = DAL.Create(name);
        //var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null, false);
        var bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, false);

        sw.Stop();
        WriteLog("备份", true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}");

        return Index();
    }

    /// <summary>备份并压缩数据库</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Insert)]
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
}