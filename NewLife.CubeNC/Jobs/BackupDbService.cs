using System.ComponentModel;
using System.Diagnostics;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Jobs;

/// <summary>备份数据库服务</summary>
public class BackupDbService
{
    /// <summary>初始化默认作业</summary>
    public static void Init()
    {
        // 传统简单的定时作业
        CronJob.Add(null, BackupDb, "5 0 0 * * ? *", false);
    }

    /// <summary>备份数据库</summary>
    /// <param name="connNames"></param>
    [DisplayName("备份数据库")]
    [Description("参数是连接名connName，多个逗号隔开。仅支持SQLite")]
    public static void BackupDb(String connNames)
    {
        var ns = connNames.Split(",", ";");
        foreach (var name in ns)
        {
            if (DAL.ConnStrs.ContainsKey(name))
            {
                // 仅支持备份SQLite
                var dal = DAL.Create(name);
                if (dal.DbType == DatabaseType.SQLite)
                {
                    XTrace.WriteLine("在[{0}]上备份数据库", name);

                    var sw = Stopwatch.StartNew();

                    //var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null, false);
                    var bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, false);

                    sw.Stop();

                    var job = (TimerX.Current?.State as MyJob)?.Job;
                    job.WriteLog(nameof(BackupDb), true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}");
                }
            }
        }
    }
}
