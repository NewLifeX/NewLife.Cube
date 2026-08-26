using System;
using System.IO;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>文件管理器目录枚举 / 大小统计：损坏路径不得抛出</summary>
public class FileManagerIoTests
{
    [Fact]
    public void GetDirectorySize_MissingDirectory_ReturnsZero()
    {
        var di = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cube-fm-missing-" + Guid.NewGuid().ToString("N")));
        Assert.False(di.Exists);
        Assert.Equal(0L, FileManagerIo.GetDirectorySize(di, 3));
    }

    [Fact]
    public void GetChildren_MissingDirectory_ReturnsEmpty()
    {
        var di = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cube-fm-missing-" + Guid.NewGuid().ToString("N")));
        Assert.Empty(FileManagerIo.GetChildren(di));
        Assert.Empty(FileManagerIo.GetChildren(null));
    }

    [Fact]
    public void GetDirectorySize_NestedFiles_SumsBytes()
    {
        var root = Path.Combine(Path.GetTempPath(), "cube-fm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllBytes(Path.Combine(root, "a.bin"), new Byte[100]);
            var sub = Path.Combine(root, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllBytes(Path.Combine(sub, "b.bin"), new Byte[50]);

            Assert.Equal(150L, FileManagerIo.GetDirectorySize(new DirectoryInfo(root), 3));
            Assert.Equal(100L, FileManagerIo.GetDirectorySize(new DirectoryInfo(root), 1));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void GetDirectorySize_BrokenJunction_DoesNotThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), "cube-fm-junc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "ok.bin"), new Byte[10]);
        var missing = Path.Combine(Path.GetTempPath(), "cube-fm-gone-" + Guid.NewGuid().ToString("N"));
        var link = Path.Combine(root, "broken");
        try
        {
            try
            {
                Directory.CreateSymbolicLink(link, missing);
            }
            catch (Exception)
            {
                // 无开发者模式/管理员权限时跳过 junction 场景，缺失目录用例已覆盖容错
                return;
            }

            var size = FileManagerIo.GetDirectorySize(new DirectoryInfo(root), 3);
            Assert.True(size >= 10);
            Assert.NotNull(FileManagerIo.GetChildren(new DirectoryInfo(root)));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { /* 损坏 junction 删除可能失败 */ }
        }
    }

    [Fact]
    public void IsIoAccess_DirectoryNotFound()
    {
        Assert.True(FileManagerIo.IsIoAccess(new DirectoryNotFoundException("missing")));
        Assert.True(FileManagerIo.IsIoAccess(new UnauthorizedAccessException()));
        Assert.False(FileManagerIo.IsIoAccess(new ArgumentException()));
    }
}
