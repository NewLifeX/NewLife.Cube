using NewLife;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Services;

/// <summary>本地磁盘附件存储。默认存储方式，附件存放于CubeSetting.UploadPath目录</summary>
public class LocalAttachmentStorage : IAttachmentStorage
{
    /// <summary>存储类型</summary>
    public String Name => "Local";

    /// <summary>是否本地磁盘存储</summary>
    public Boolean Local => true;

    /// <summary>上传目录。为空时使用CubeSetting.UploadPath</summary>
    public String UploadPath { get; set; }

    /// <summary>获取本地完整路径</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    protected virtual String GetFullPath(String filePath)
    {
        if (filePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(filePath));

        var root = UploadPath;
        if (root.IsNullOrEmpty()) root = CubeSetting.Current.UploadPath;

        return root.CombinePath(filePath).GetBasePath();
    }

    /// <summary>写入文件</summary>
    /// <param name="stream">数据流</param>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public async Task WriteAsync(Stream stream, String filePath)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var fullFile = GetFullPath(filePath);
        fullFile.EnsureDirectory(true);

        // 先写临时文件，再原子性重命名，避免分布式存储同步期间并发写入同一路径引发文件占用冲突
        var tmpFile = fullFile + ".tmp";
        try
        {
            {
                using var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fs);
            }
            File.Move(tmpFile, fullFile, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmpFile))
                try { File.Delete(tmpFile); } catch { }
            throw;
        }
    }

    /// <summary>读取文件流</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns>文件流，不存在时返回null</returns>
    public Task<Stream> ReadAsync(String filePath)
    {
        var fullFile = GetFullPath(filePath);
        if (!File.Exists(fullFile)) return Task.FromResult<Stream>(null);

        return Task.FromResult<Stream>(File.OpenRead(fullFile));
    }

    /// <summary>文件是否存在</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public Task<Boolean> ExistsAsync(String filePath) => Task.FromResult(File.Exists(GetFullPath(filePath)));

    /// <summary>获取文件直接访问Url。本地存储返回null</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public String GetUrl(String filePath) => null;

    /// <summary>删除文件</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public Task DeleteAsync(String filePath)
    {
        var fullFile = GetFullPath(filePath);
        if (File.Exists(fullFile))
            try { File.Delete(fullFile); } catch { }

        return Task.CompletedTask;
    }
}
