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
            if (!DAL.ConnStrs.ContainsKey(name)) continue;

            var dal = DAL.Create(name);

            XTrace.WriteLine("在[{0}]上备份数据库", name);

            var sw = Stopwatch.StartNew();

            Object bak = null;
            try
            {
                bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, false);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("备份数据库失败：{0}", ex.Message);
                var job = (TimerX.Current?.State as MyJob)?.Job;
                job?.WriteLog(nameof(BackupDb), false, $"备份数据库 {name} 失败，{ex.Message}");
                continue;
            }

            // 如果备份结果已经是zip，跳过后续压缩
            if (BackupHelper.IsCompressedBackup(bak))
            {
                sw.Stop();
                var job = (TimerX.Current?.State as MyJob)?.Job;
                job?.WriteLog(nameof(BackupDb), true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}");
                continue;
            }

            // SQLite备份文件多做一步压缩回收空间（WAL checkpoint + VACUUM，仅对.db文件有效）
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
                    var job = (TimerX.Current?.State as MyJob)?.Job;
                    job?.WriteLog(nameof(BackupDb), true, $"备份数据库 {name} 到 {rs}，耗时 {sw.Elapsed}");
                    continue;
                }
            }

            sw.Stop();
            var job2 = (TimerX.Current?.State as MyJob)?.Job;
            job2?.WriteLog(nameof(BackupDb), true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}");
        }
    }
}
