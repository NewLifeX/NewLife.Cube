using NewLife.Cube.Web;
using NewLife.Data;
using NewLife.Log;
using XCode;
using XCode.Cache;
using XCode.Membership;

namespace NewLife.Cube.Entity;

/// <summary>附件。用于记录各系统模块使用的文件</summary>
public partial class Attachment : Entity<Attachment>
{
    #region 对象操作
    static Attachment()
    {
        // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
        var df = Meta.Factory.AdditionalFields;
        df.Add(nameof(Downloads));

        // 过滤器 UserInterceptor、TimeInterceptor、IPInterceptor
        Meta.Interceptors.Add<UserInterceptor>();
        Meta.Interceptors.Add<TimeInterceptor>();
        Meta.Interceptors.Add<IPInterceptor>();
        Meta.Interceptors.Add<TraceInterceptor>();
    }

    /// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>
    /// <param name="isNew">是否插入</param>
    public override void Valid(Boolean isNew)
    {
        // 如果没有脏数据，则不需要进行任何处理
        if (!HasDirty) return;

        //// 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框
        //if (FileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(FileName), "文件名不能为空！");

        var len = _.FileName.Length;
        if (len > 0 && !FileName.IsNullOrEmpty() && FileName.Length > len) FileName = FileName[^len..];

        len = _.Title.Length;
        if (len > 0 && !Title.IsNullOrEmpty() && Title.Length > len) Title = Title[..len];

        base.Valid(isNew);
    }

    /// <summary>删除。同步删除存储文件（本地磁盘或云存储），避免遗留孤儿文件</summary>
    /// <returns></returns>
    protected override Int32 OnDelete()
    {
        // 删除低频操作，阻塞等待可接受；DeleteFileAsync内部已捕获异常，不影响记录删除
        DeleteFileAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        return base.OnDelete();
    }
    #endregion

    #region 扩展属性
    #endregion

    #region 扩展查询
    /// <summary>根据编号查找</summary>
    /// <param name="id">编号</param>
    /// <returns>实体对象</returns>
    public static Attachment FindById(Int64 id)
    {
        if (id <= 0) return null;

        //// 实体缓存
        //if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.ID == id);

        // 单对象缓存
        return Meta.SingleCache[id];

        //return Find(_.ID == id);
    }

    /// <summary>根据分类查找</summary>
    /// <param name="category">分类</param>
    /// <returns>实体列表</returns>
    public static IList<Attachment> FindAllByCategory(String category) => FindAll(_.Category == category);

    /// <summary>根据分类和业务主键查找附件</summary>
    /// <param name="category">分类</param>
    /// <param name="key">业务主键</param>
    /// <returns>实体列表</returns>
    public static IList<Attachment> FindAllByCategoryAndKey(String category, String key) => FindAll(_.Category == category & _.Key == key);

    /// <summary>根据路径查找</summary>
    /// <param name="filePath">路径</param>
    /// <returns>实体列表</returns>
    public static IList<Attachment> FindAllByFilePath(String filePath)
    {
        if (filePath.IsNullOrEmpty()) return new List<Attachment>();

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.FilePath.EqualIgnoreCase(filePath));

        return FindAll(_.FilePath == filePath);
    }

    /// <summary>根据扩展名查找</summary>
    /// <param name="extension">扩展名</param>
    /// <returns>实体列表</returns>
    public static IList<Attachment> FindAllByExtension(String extension)
    {
        if (extension.IsNullOrEmpty()) return new List<Attachment>();

        // 实体缓存
        if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => e.Extension.EqualIgnoreCase(extension));

        return FindAll(_.Extension == extension);
    }
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="category">分类</param>
    /// <param name="key">业务关键字</param>
    /// <param name="ext">扩展名</param>
    /// <param name="start">关键字</param>
    /// <param name="end">关键字</param>
    /// <param name="keyWord">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <param name="storage">存储类型。Local需兼容历史空值</param>
    /// <returns>实体列表</returns>
    public static IList<Attachment> Search(String category, String key, String ext, DateTime start, DateTime end, String keyWord, PageParameter page, String storage = null)
    {
        var exp = new WhereExpression();

        if (!category.IsNullOrEmpty()) exp &= _.Category == category;
        if (!key.IsNullOrEmpty()) exp &= _.Key == key;
        if (!ext.IsNullOrEmpty()) exp &= _.Extension == ext;
        if (!storage.IsNullOrEmpty())
        {
            if (storage.EqualIgnoreCase("Local"))
                exp &= _.Storage.IsNullOrEmpty() | _.Storage == "Local";
            else
                exp &= _.Storage == storage;
        }
        exp &= _.Id.Between(start, end, Meta.Factory.Snow);
        if (!keyWord.IsNullOrEmpty()) exp &= _.FileName == keyWord | _.Extension == keyWord | _.ContentType.Contains(keyWord) | _.FilePath.StartsWith(keyWord) | _.Title.Contains(keyWord);

        return FindAll(exp, page);
    }

    // Select Count(ID) as ID,Category From Attachment Where CreateTime>'2020-01-24 00:00:00' Group By Category Order By ID Desc limit 20
    private static readonly FieldCache<Attachment> _CategoryCache = new FieldCache<Attachment>(nameof(Category))
    {
        //Where = _.CreateTime > DateTime.Today.AddDays(-30) & Expression.Empty
    };

    /// <summary>获取分类列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>
    /// <returns></returns>
    public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();
    #endregion

    #region 业务操作
    /// <summary>
    /// 生成文件路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public String BuildFilePath(String path = null)
    {
        var file = FilePath;

        // 文件名
        if (FileName.IsNullOrEmpty() && !file.IsNullOrEmpty()) FileName = Path.GetFileName(file);
        if (FileName.IsNullOrEmpty() && !path.IsNullOrEmpty()) FileName = Path.GetFileName(path);

        // 后缀
        var ext = Extension;
        if (ext.IsNullOrEmpty() && !FileName.IsNullOrEmpty()) ext = Path.GetExtension(FileName);
        if (ext.IsNullOrEmpty() && !FilePath.IsNullOrEmpty()) ext = Path.GetExtension(FilePath);
        if (ext.IsNullOrEmpty() && !path.IsNullOrEmpty()) ext = Path.GetExtension(path);
        Extension = ext;

        // 构造文件路径
        if (file.IsNullOrEmpty())
        {
            if (Id == 0 || Category.IsNullOrEmpty()) return null;

            var time = UploadTime;
            if (time.Year < 2000) time = DateTime.Today;

            FilePath = file = $"{Category}\\{time:yyyyMMdd}\\{Id}{ext}";
        }

        return file;
    }

    private static HttpClient _client;
    /// <summary>抓取附件</summary>
    /// <param name="url">远程地址</param>
    /// <param name="uploadPath">上传目录</param>
    /// <param name="filePath">文件名，如未指定则自动生成</param>
    /// <param name="client">指定定制化HttpClient，默认为空，由内部实例化。语雀SDK抓取附件时需要</param>
    /// <returns></returns>
    public async Task<Boolean> Fetch(String url, String uploadPath = null, String filePath = null, HttpClient client = null)
    {
        if (url.IsNullOrEmpty()) return false;

        // 清理url的#后续部分
        var p = url.IndexOf('#');
        if (p > 0) url = url[..p];

        // 提前生成雪花Id，用于保存文件
        var isNew = Id == 0;
        if (Id == 0) Id = Meta.Factory.Snow.NewId();

        // 构造文件路径
        //if (!filePath.IsNullOrEmpty()) FilePath = filePath;
        var file = filePath;
        if (file.IsNullOrEmpty()) file = BuildFilePath(url);
        if (file.IsNullOrEmpty()) return false;

        Source = url;

        // 记录存储类型
        var provider = AttachmentProvider.Provider;
        Storage = provider.Name;

        // 抓取
        client ??= _client ??= new HttpClient();
        using var rs = await client.GetAsync(url);
        var contentType = rs.Content.Headers.ContentType + "";
        if (!contentType.IsNullOrEmpty()) ContentType = contentType;

        var stream = await rs.Content.ReadAsStreamAsync();
        if (provider.Storage.Local)
        {
            if (uploadPath.IsNullOrEmpty()) uploadPath = CubeSetting.Current.UploadPath;

            var fullFile = uploadPath.CombinePath(file).GetBasePath();
            XTrace.WriteLine("抓取附件 {0}，保存到 {1}", url, file);

            fullFile.EnsureDirectory(true);
            //if (File.Exists(fullFile)) File.Delete(fullFile);

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

            // 记录文件信息
            var fi = fullFile.AsFile();
            Size = fi.Length;
            Hash = fi.MD5().ToHex();
        }
        else
        {
            // 对象存储：单遍计算哈希与大小
            using var hashStream = new HashStream(stream);
            await provider.WriteAsync(hashStream, file);
            Size = hashStream.Length;
            Hash = hashStream.Hash;
        }

        //Save();
        if (isNew)
            Insert();
        else
            Update();

        return true;
    }

    /// <summary>保存单个文件</summary>
    /// <param name="stream">文件</param>
    /// <param name="uploadPath">上传目录，默认使用UploadPath配置</param>
    /// <param name="filePath">文件名，如未指定则自动生成</param>
    /// <returns></returns>
    public async Task<Boolean> SaveFile(Stream stream, String uploadPath = null, String filePath = null)
    {
        if (stream == null) return false;

        // 提前生成雪花Id，用于保存文件
        var isNew = Id == 0;
        if (Id == 0) Id = Meta.Factory.Snow.NewId();

        // 构造文件路径
        //if (!filePath.IsNullOrEmpty()) FilePath = filePath;
        var file = BuildFilePath(filePath);
        if (file.IsNullOrEmpty()) return false;

        // 记录存储类型
        var provider = AttachmentProvider.Provider;
        Storage = provider.Name;

        if (provider.Storage.Local)
        {
            if (uploadPath.IsNullOrEmpty()) uploadPath = CubeSetting.Current.UploadPath;

            // 保存文件，优先原名字
            var fullFile = uploadPath.CombinePath(file).GetBasePath();
            fullFile.EnsureDirectory(true);
            DefaultSpan.Current?.AppendTag($"fullFile={fullFile}");

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

            // 记录文件信息
            var fi = fullFile.AsFile();
            Size = fi.Length;
            Hash = fi.MD5().ToHex();
        }
        else
        {
            // 对象存储：单遍计算哈希与大小
            using var hashStream = new HashStream(stream);
            await provider.WriteAsync(hashStream, file);
            Size = hashStream.Length;
            Hash = hashStream.Hash;
        }

        //Save();
        if (isNew)
            Insert();
        else
            Update();

        return true;
    }

    /// <summary>获取文件路径，用于读取附件</summary>
    /// <param name="uploadPath"></param>
    /// <returns></returns>
    public String GetFilePath(String uploadPath = null)
    {
        var file = FilePath;
        if (file.IsNullOrEmpty()) return null;

        if (uploadPath.IsNullOrEmpty()) uploadPath = CubeSetting.Current.UploadPath;

        return uploadPath.CombinePath(file).GetBasePath();
    }

    /// <summary>是否本地磁盘存储。历史附件未记录存储类型时按本地处理</summary>
    /// <returns></returns>
    public Boolean IsLocalStorage() => Storage.IsNullOrEmpty() || Storage.EqualIgnoreCase("Local");

    /// <summary>存储类型名称。用于界面展示</summary>
    public String StorageName => Storage switch
    {
        "Oss" => "阿里云OSS",
        "Cos" => "腾讯云COS",
        "Qiniu" => "七牛",
        "EasyIO" => "EasyIO",
        _ => "本地",
    };

    /// <summary>获取附件直接访问Url。本地存储返回null，云存储返回预签名Url</summary>
    /// <returns>可直接访问的Url，本地存储返回null</returns>
    public String GetUrl() => IsLocalStorage() ? null : AttachmentProvider.Provider.GetUrl(FilePath);

    /// <summary>删除附件文件。同步清理本地磁盘或云存储中的文件</summary>
    /// <returns></returns>
    public async Task<Boolean> DeleteFileAsync()
    {
        var file = FilePath;
        if (file.IsNullOrEmpty()) return false;

        try
        {
            await AttachmentProvider.Provider.DeleteAsync(file);
            return true;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            return false;
        }
    }
    #endregion

    #region 辅助
    /// <summary>哈希流。读取数据时同步计算MD5哈希与总字节数，用于对象存储上传时单遍计算元数据</summary>
    private class HashStream : Stream
    {
        private readonly Stream _stream;
        private readonly System.Security.Cryptography.IncrementalHash _hash = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.MD5);
        private Int64 _length;

        public HashStream(Stream stream) => _stream = stream;

        /// <summary>已读取字节数</summary>
        public Int64 Length2 => _length;

        /// <summary>MD5哈希（十六进制）。读取完成后有效</summary>
        public String Hash => _hash.GetHashAndReset().ToHex();

        public override Boolean CanRead => _stream.CanRead;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => _length;
        public override Int64 Position { get => _length; set => throw new NotSupportedException(); }

        public override Int32 Read(Span<Byte> buffer)
        {
            var n = _stream.Read(buffer);
            if (n > 0)
            {
                _hash.AppendData(buffer[..n]);
                _length += n;
            }
            return n;
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => Read(buffer.AsSpan(offset, count));

        public override async ValueTask<Int32> ReadAsync(Memory<Byte> buffer, CancellationToken cancellationToken = default)
        {
            var n = await _stream.ReadAsync(buffer, cancellationToken);
            if (n > 0)
            {
                _hash.AppendData(buffer.Span[..n]);
                _length += n;
            }
            return n;
        }

        public override Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush() { }
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

        protected override void Dispose(Boolean disposing)
        {
            _hash.Dispose();
            base.Dispose(disposing);
        }
    }
    #endregion
}