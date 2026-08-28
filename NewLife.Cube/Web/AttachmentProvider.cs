using NewLife.Cube.Services;

namespace NewLife.Cube.Web
{
    /// <summary>附件提供者。附件读写统一入口，支持本地磁盘与对象存储（OSS/COS/七牛）</summary>
    /// <remarks>
    /// 默认使用本地磁盘存储（<see cref="LocalAttachmentStorage"/>），
    /// 配置云存储后由 <see cref="NewLife.Cube.CubeService"/> 在启动时切换为对象存储。
    /// <see cref="NewLife.Cube.Entity.Attachment"/> 实体的 SaveFile/GetFilePath 与 CubeController 下载均经由本提供者。
    /// </remarks>
    public class AttachmentProvider
    {
        #region 属性
        /// <summary>存储实现。默认本地磁盘存储</summary>
        public IAttachmentStorage Storage { get; set; } = new LocalAttachmentStorage();

        /// <summary>当前附件存储类型。Local/Oss/Cos/Qiniu/EasyIO</summary>
        public String Name => Storage?.Name;
        #endregion

        #region 静态
        /// <summary>默认提供者</summary>
        public static AttachmentProvider Provider { get; set; } = new AttachmentProvider();
        #endregion

        #region 方法
        /// <summary>写入文件</summary>
        /// <param name="stream">数据流</param>
        /// <param name="filePath">相对路径</param>
        /// <returns></returns>
        public Task WriteAsync(Stream stream, String filePath) => Storage.WriteAsync(stream, filePath);

        /// <summary>读取文件流</summary>
        /// <param name="filePath">相对路径</param>
        /// <returns>文件流，不存在时返回null</returns>
        public Task<Stream> ReadAsync(String filePath) => Storage.ReadAsync(filePath);

        /// <summary>文件是否存在</summary>
        /// <param name="filePath">相对路径</param>
        /// <returns></returns>
        public Task<Boolean> ExistsAsync(String filePath) => Storage.ExistsAsync(filePath);

        /// <summary>获取文件直接访问Url。本地存储返回null</summary>
        /// <param name="filePath">相对路径</param>
        /// <returns>可直接访问的Url，本地存储返回null</returns>
        public String GetUrl(String filePath) => Storage.GetUrl(filePath);

        /// <summary>删除文件</summary>
        /// <param name="filePath">相对路径</param>
        /// <returns></returns>
        public Task DeleteAsync(String filePath) => Storage.DeleteAsync(filePath);
        #endregion
    }
}