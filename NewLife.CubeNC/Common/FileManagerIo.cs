using System.Security;

namespace NewLife.Cube;

/// <summary>文件管理器 IO 辅助：目录枚举与大小统计对损坏 junction、无权限路径容错。</summary>
public static class FileManagerIo
{
    /// <summary>枚举子项；目录不可读或路径不存在时返回空数组，不抛出。</summary>
    /// <param name="di">目录</param>
    /// <returns>子文件与子目录；失败时为空</returns>
    public static FileSystemInfo[] GetChildren(DirectoryInfo di)
    {
        if (di == null) return Array.Empty<FileSystemInfo>();

        try
        {
            return di.GetFileSystemInfos();
        }
        catch (Exception ex) when (IsIoAccess(ex))
        {
            return Array.Empty<FileSystemInfo>();
        }
    }

    /// <summary>递归统计目录大小。损坏链接、无权限、路径不存在时返回已累计部分（最差为 0），不抛出。</summary>
    /// <param name="di">目录</param>
    /// <param name="level">剩余递归层数；≤1 时不进入子目录</param>
    /// <returns>字节数</returns>
    public static Int64 GetDirectorySize(DirectoryInfo di, Int32 level)
    {
        if (di == null) return 0;

        var size = 0L;
        try
        {
            foreach (var item in di.GetFiles())
            {
                try
                {
                    size += item.Length;
                }
                catch (Exception ex) when (IsIoAccess(ex))
                {
                    // 损坏符号链接 / 竞态删除：跳过该文件
                }
            }

            if (level > 1)
            {
                foreach (var item in di.GetDirectories())
                {
                    size += GetDirectorySize(item, level - 1);
                }
            }
        }
        catch (Exception ex) when (IsIoAccess(ex))
        {
            return size;
        }

        return size;
    }

    /// <summary>是否为目录遍历中可忽略的 IO / 权限异常</summary>
    public static Boolean IsIoAccess(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or SecurityException;
}
