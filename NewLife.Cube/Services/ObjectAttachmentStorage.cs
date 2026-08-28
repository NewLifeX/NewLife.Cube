using NewLife;
using NewLife.Data;
using NewLife.IO;

namespace NewLife.Cube.Services;

/// <summary>对象存储附件存储。包装 <see cref="IObjectStorage"/>，对接OSS/COS/七牛/EasyIO等对象存储</summary>
public class ObjectAttachmentStorage : IAttachmentStorage
{
    /// <summary>对象存储实现</summary>
    public IObjectStorage Storage { get; set; }

    /// <summary>存储类型。Oss/Cos/Qiniu/EasyIO</summary>
    public String Name { get; set; }

    /// <summary>是否本地磁盘存储</summary>
    public Boolean Local => false;

    /// <summary>写入文件</summary>
    /// <param name="stream">数据流</param>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public async Task WriteAsync(Stream stream, String filePath)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (filePath.IsNullOrEmpty()) throw new ArgumentNullException(nameof(filePath));

        // 读取数据并上传（对象存储接口以IPacket传递数据）
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var rs = await Storage.PutAsync(filePath, new ArrayPacket(ms.ToArray()));
        if (rs == null) throw new InvalidOperationException($"上传对象[{filePath}]失败");
    }

    /// <summary>读取文件流</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns>文件流，不存在时返回null</returns>
    public async Task<Stream> ReadAsync(String filePath)
    {
        var info = await Storage.GetAsync(filePath);
        return info?.Data?.GetStream();
    }

    /// <summary>文件是否存在</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public Task<Boolean> ExistsAsync(String filePath) => Storage.ExistsAsync(filePath);

    /// <summary>获取文件直接访问Url。S3签名Url为纯计算，无网络请求</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public String GetUrl(String filePath)
    {
        // S3预签名Url为纯计算；其他实现（如EasyIO）存在网络请求，异常时忽略
        try
        {
            return Storage.GetUrlAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>删除文件</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    public Task DeleteAsync(String filePath) => Storage.DeleteAsync(filePath);
}
