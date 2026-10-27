using System;
using System.IO;
using System.IO.Compression;
using NewLife.Cube.Jobs;
using Xunit;

namespace XUnitTest;

/// <summary>备份辅助类单测。覆盖备份结果判定、路径提取、压缩与 SQLite 收缩的边界</summary>
public class BackupHelperTests : IDisposable
{
    private readonly String _dir;

    public BackupHelperTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "cube-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }
        catch { }
    }

    private String CreateFile(String name, String? content = null)
    {
        var file = Path.Combine(_dir, name);
        File.WriteAllText(file, content ?? "backup-content");
        return file;
    }

    [Fact(DisplayName = "备份结果判定：仅存在且以 .zip 结尾的字符串算已压缩")]
    public void IsCompressedBackup_Variants()
    {
        var zip = CreateFile("db.zip");
        var upper = CreateFile("db2.ZIP");
        var bak = CreateFile("db.bak");

        Assert.False(BackupHelper.IsCompressedBackup(null));
        Assert.False(BackupHelper.IsCompressedBackup(123));
        Assert.False(BackupHelper.IsCompressedBackup(Path.Combine(_dir, "missing.zip")));
        Assert.False(BackupHelper.IsCompressedBackup(bak));
        Assert.True(BackupHelper.IsCompressedBackup(zip));
        // 扩展名判定忽略大小写；用真实存在的大写扩展名文件验证，兼容 Linux 大小写敏感路径
        Assert.True(BackupHelper.IsCompressedBackup(upper));
    }

    [Fact(DisplayName = "获取备份文件路径：空值与不存在的文件返回 null")]
    public void GetBackupFile_Variants()
    {
        var file = CreateFile("db.bak");

        Assert.Null(BackupHelper.GetBackupFile(null));
        Assert.Null(BackupHelper.GetBackupFile(""));
        Assert.Null(BackupHelper.GetBackupFile(Path.Combine(_dir, "missing.bak")));
        Assert.Null(BackupHelper.GetBackupFile(123));
        Assert.Equal(file, BackupHelper.GetBackupFile(file));
    }

    [Fact(DisplayName = "压缩备份：生成 zip 并删除原文件")]
    public void CompressBackupFile_CreatesZipAndRemovesSource()
    {
        var file = CreateFile("cube.db", "payload");

        var zip = BackupHelper.CompressBackupFile(file);

        Assert.NotNull(zip);
        Assert.Equal(Path.ChangeExtension(file, ".zip"), zip);
        Assert.True(File.Exists(zip));
        Assert.False(File.Exists(file));

        using var archive = ZipFile.OpenRead(zip!);
        Assert.Single(archive.Entries);
    }

    [Fact(DisplayName = "压缩备份：已是 zip 原样返回，文件不存在返回 null")]
    public void CompressBackupFile_EdgeCases()
    {
        var zip = CreateFile("cube.zip", "already-zip");

        Assert.Equal(zip, BackupHelper.CompressBackupFile(zip));
        Assert.Null(BackupHelper.CompressBackupFile(Path.Combine(_dir, "missing.db")));
        Assert.Null(BackupHelper.CompressBackupFile(""));
    }

    [Fact(DisplayName = "SQLite 收缩：非 db 与不存在的文件直接跳过且不抛异常")]
    public void CompactBackupFile_SkipsNonDatabase()
    {
        var txt = CreateFile("note.txt", "hello");

        BackupHelper.CompactBackupFile(txt);
        BackupHelper.CompactBackupFile(Path.Combine(_dir, "missing.db"));
        BackupHelper.CompactBackupFile("");
    }
}
