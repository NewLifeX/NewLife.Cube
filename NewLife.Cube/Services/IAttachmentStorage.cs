using NewLife.Cube.Entity;

namespace NewLife.Cube.Services;

/// <summary>附件存储接口。支持本地磁盘与对象存储（OSS/COS/七牛/EasyIO）</summary>
/// <remarks>
/// 魔方附件默认存储于本地磁盘，通过本接口抽象读写路径，可切换为对象存储，
/// 实现多节点分布式部署下附件共用。默认实现为 <see cref="LocalAttachmentStorage"/>，
/// 云存储场景使用 <see cref="ObjectAttachmentStorage"/> 包装 <see cref="NewLife.IO.IObjectStorage"/>。
/// </remarks>
public interface IAttachmentStorage
{
    /// <summary>存储类型。Local本地磁盘，Oss阿里云，Cos腾讯云，Qiniu七牛，EasyIO</summary>
    String Name { get; }

    /// <summary>是否本地磁盘存储</summary>
    Boolean Local { get; }

    /// <summary>写入文件</summary>
    /// <param name="stream">数据流</param>
    /// <param name="filePath">相对路径，如 Category\20260101\123.ext</param>
    /// <returns></returns>
    Task WriteAsync(Stream stream, String filePath);

    /// <summary>读取文件流</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns>文件流，不存在时返回null</returns>
    Task<Stream> ReadAsync(String filePath);

    /// <summary>文件是否存在</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    Task<Boolean> ExistsAsync(String filePath);

    /// <summary>获取文件直接访问Url。本地存储返回null</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns>可直接访问的Url，本地存储返回null</returns>
    String GetUrl(String filePath);

    /// <summary>删除文件</summary>
    /// <param name="filePath">相对路径</param>
    /// <returns></returns>
    Task DeleteAsync(String filePath);
}
