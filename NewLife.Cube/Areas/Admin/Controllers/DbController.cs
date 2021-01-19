using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using XCode.Membership;
using System.Diagnostics;
using XCode;
#if __CORE__
using Microsoft.AspNetCore.Http;
using NewLife.Cube.Extensions;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>数据库管理</summary>
    [DisplayName("数据库")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class DbController : ControllerBaseX
    {
        /// <summary>菜单顺序。扫描是会反射读取</summary>
        protected static Int32 MenuOrder { get; set; }

        static DbController() => MenuOrder = 26;

        /// <summary>数据库列表</summary>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [HttpGet("[action]")]
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

            return View("Index", list);
        }

        /// <summary>备份数据库</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Insert)]
        [HttpGet("[action]")]
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
        [HttpGet("[action]")]
        public ActionResult BackupAndCompress(String name)
        {
            var sw = Stopwatch.StartNew();

            var dal = DAL.Create(name);
            //var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null, true);
            //var bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, true);
            var bak = $"{dal.ConnName}_{DateTime.Now:yyyyMMddHHmmss}.zip";
            bak = NewLife.Setting.Current.BackupPath.CombinePath(bak);
            var tables = dal.Tables;
            dal.BackupAll(tables, bak);

            sw.Stop();
            WriteLog("备份", true, $"备份数据库 {name} 并压缩到 {bak}，耗时 {sw.Elapsed}");

            return Index();
        }

        /// <summary>下载数据库备份</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [EntityAuthorize(PermissionFlags.Detail)]
        [HttpGet("[action]")]
        public ActionResult Download(String name)
        {
            var dal = DAL.Create(name);
            var xml = DAL.Export(dal.Tables);

            WriteLog("下载", true, "下载数据库架构 " + name);

            return File(xml.GetBytes(), "application/xml", name + ".xml");
        }
    }
}