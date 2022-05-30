using NewLife.Data;
using NewLife.Log;
using XCode;
using XCode.Cache;
using XCode.Membership;

namespace NewLife.Cube.Entity
{
    /// <summary>附件。用于记录各系统模块使用的文件</summary>
    public partial class Attachment : Entity<Attachment>
    {
        #region 对象操作
        static Attachment()
        {
            // 累加字段，生成 Update xx Set Count=Count+1234 Where xxx
            //var df = Meta.Factory.AdditionalFields;
            //df.Add(nameof(Size));

            // 过滤器 UserModule、TimeModule、IPModule
            Meta.Modules.Add<UserModule>();
            Meta.Modules.Add<TimeModule>();
            Meta.Modules.Add<IPModule>();
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
        /// <returns>实体列表</returns>
        public static IList<Attachment> Search(String category, String key, String ext, DateTime start, DateTime end, String keyWord, PageParameter page)
        {
            var exp = new WhereExpression();

            if (!category.IsNullOrEmpty()) exp &= _.Category == category;
            if (!key.IsNullOrEmpty()) exp &= _.Key == key;
            if (!ext.IsNullOrEmpty()) exp &= _.Extension == ext;
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
        /// <returns></returns>
        public async Task<Boolean> Fetch(String url, String uploadPath = null, String filePath = null)
        {
            if (url.IsNullOrEmpty()) return false;

            // 提前生成雪花Id，用于保存文件
            var isNew = Id == 0;
            if (Id == 0) Id = Meta.Factory.Snow.NewId();

            // 构造文件路径
            //if (!filePath.IsNullOrEmpty()) FilePath = filePath;
            var file = BuildFilePath(url);
            if (file.IsNullOrEmpty()) return false;

            Source = url;

            if (uploadPath.IsNullOrEmpty()) uploadPath = Setting.Current.UploadPath;

            var fullFile = uploadPath.CombinePath(file).GetBasePath();
            XTrace.WriteLine("抓取附件 {0}，保存到 {1}", url, file);

            fullFile.EnsureDirectory(true);
            //if (File.Exists(fullFile)) File.Delete(fullFile);

            // 抓取并保存
            if (_client == null) _client = new HttpClient();
            var rs = await _client.GetAsync(url);
            var contentType = rs.Content.Headers.ContentType + "";
            if (!contentType.IsNullOrEmpty()) ContentType = contentType;

            {
                using var fs = new FileStream(fullFile, FileMode.OpenOrCreate);
                await rs.Content.CopyToAsync(fs);
                fs.SetLength(fs.Position);
            }

            // 记录文件信息
            var fi = fullFile.AsFile();
            Size = fi.Length;
            Hash = fi.MD5().ToHex();

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

            if (uploadPath.IsNullOrEmpty()) uploadPath = Setting.Current.UploadPath;

            // 保存文件，优先原名字
            var fullFile = uploadPath.CombinePath(file).GetBasePath();
            fullFile.EnsureDirectory(true);

            {
                using var fs = new FileStream(fullFile, FileMode.OpenOrCreate);
                await stream.CopyToAsync(fs);
                fs.SetLength(fs.Position);
            }

            // 记录文件信息
            var fi = fullFile.AsFile();
            Size = fi.Length;
            Hash = fi.MD5().ToHex();

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

            if (uploadPath.IsNullOrEmpty()) uploadPath = Setting.Current.UploadPath;

            return uploadPath.CombinePath(file).GetBasePath();
        }
        #endregion
    }
}