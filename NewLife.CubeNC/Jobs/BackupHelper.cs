using System.IO.Compression;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Jobs;

/// <summary>备份辅助类</summary>
public static class BackupHelper
{
    /// <summary>判断备份结果是否为已压缩的zip文件</summary>
    /// <param name="bak">备份方法返回的结果</param>
    /// <returns></returns>
    public static Boolean IsCompressedBackup(Object bak)
    {
        if (bak == null) return false;
        if (bak is not String file) return false;

        return file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && File.Exists(file);
    }

    /// <summary>获取备份文件路径。备份结果不是zip且文件存在时返回路径</summary>
    /// <param name="bak">备份方法返回的结果</param>
    /// <returns>文件路径，null表示无需处理</returns>
    public static String GetBackupFile(Object bak)
    {
        if (bak == null) return null;
        if (bak is not String file) return null;
        if (file.IsNullOrEmpty()) return null;
        if (!File.Exists(file)) return null;

        return file;
    }

    /// <summary>对SQLite备份文件执行WAL checkpoint，合并WAL到主库并截断</summary>
    /// <param name="bakFile">备份的数据库文件路径</param>
    public static void WalCheckpointBackup(String bakFile)
    {
        if (bakFile.IsNullOrEmpty() || !File.Exists(bakFile)) return;
        if (!bakFile.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) return;

        var tempName = "__wal_" + Guid.NewGuid().ToString("N");
        try
        {
            // 临时连接到备份文件，执行WAL checkpoint压缩WAL文件
            DAL.AddConnStr(tempName, "Data Source=" + bakFile, null, "SQLite");
            var bakDal = DAL.Create(tempName);
            bakDal.Execute("PRAGMA wal_checkpoint(TRUNCATE)");
        }
        catch (Exception ex)
        {
            XTrace.WriteLine("备份文件WAL checkpoint 失败：{0}", ex.Message);
        }
        finally
        {
            // 用完清理临时连接名
            DAL.ConnStrs?.TryRemove(tempName, out _);
        }
    }

    /// <summary>压缩备份文件为Zip。压缩成功后删除原始文件</summary>
    /// <param name="bakFile">备份文件路径</param>
    /// <returns>压缩后的zip文件路径，压缩失败返回null</returns>
    public static String CompressBackupFile(String bakFile)
    {
        if (bakFile.IsNullOrEmpty() || !File.Exists(bakFile)) return null;
        if (bakFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return bakFile;

        try
        {
            var zipFile = Path.ChangeExtension(bakFile, ".zip");
            var fi = new FileInfo(bakFile);
            XTrace.WriteLine("开始压缩备份文件 {0}，大小 {1:n0} 字节", bakFile, fi.Length);

            // 如果已存在同名zip，先删除
            if (File.Exists(zipFile)) File.Delete(zipFile);

            // 创建zip包，把备份文件添加进去
            using var archive = ZipFile.Open(zipFile, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(bakFile, fi.Name, CompressionLevel.Optimal);

            var fi2 = new FileInfo(zipFile);
            XTrace.WriteLine("备份压缩完成：{0}，大小 {1:n0} 字节，压缩率 {2:P1}",
                zipFile, fi2.Length, (Double)fi2.Length / fi.Length);

            // 压缩成功后删除原始文件
            File.Delete(bakFile);

            return zipFile;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            return null;
        }
    }
}
