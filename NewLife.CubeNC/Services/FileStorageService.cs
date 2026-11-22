using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Web;
using NewLife.Data;
using NewLife.IO;
using NewLife.Log;
using NewLife.Reflection;
using Stardust;
using Stardust.Registry;
using Stardust.Services;
using Stardust.Storages;
using XCode;

namespace NewLife.Cube.Services;

/// <summary>文件分布式存储服务</summary>
/// <param name="fileStorage"></param>
public class FileStorageService(IFileStorage fileStorage) : IHostedService
{
    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        // 延迟10秒后初始化，避免和其它服务争抢资源
        // 不阻塞 Host 启动：在后台执行
        _ = InitializeLaterAsync(cancellationToken);

        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InitializeLaterAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(10_000, cancellationToken);
            await fileStorage.InitializeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // 后台初始化失败仅记录日志，不导致主机退出，也不产生未观测异常
            XTrace.WriteException(ex);
        }
    }
}

/// <summary>文件存储服务扩展</summary>
public static class FileStorageExtensions
{
    /// <summary>注册魔方版文件存储</summary>
    /// <param name="services">服务集合</param>
    /// <param name="name">集群名。相同名称的多个应用共享一个文件存储集群，如StarWeb和StarServer。默认为null时使用应用标识，集群部署的多节点应用实例共享文件存储</param>
    /// <returns></returns>
    public static IServiceCollection AddCubeFileStorage(this IServiceCollection services, String name = null)
    {
        // 如果已经注册IFileStorage，则不重复注册
        var descriptor = services.FirstOrDefault(e => e.ServiceType == typeof(IFileStorage));
        if (descriptor != null) return services;

        if (name.IsNullOrEmpty()) name = StarSetting.Current.AppKey;
        if (name.IsNullOrEmpty()) name = SysConfig.Current.Name;

        //services.AddSingleton<IFileStorage, CubeFileStorage>();
        //services.AddSingleton<IFileStorage>(new CubeFileStorage { Name = name });
        services.AddSingleton<CubeFileStorage>();
        services.AddSingleton<IFileStorage>(sp =>
        {
            var storage = sp.GetRequiredService<CubeFileStorage>();
            storage.Name = name;

            // 从配置读取文件存储行为开关
            var set = sp.GetService<CubeSetting>();
            if (set != null)
            {
                storage.EnableProvide = set.FileStorageProvide;
                storage.EnableFetch = set.FileStorageFetch;
            }

            return storage;
        });

        // 仅当提供或拉取任一功能开启时，才启动后台文件存储服务（事件总线订阅/扫描定时）。
        // 注意：IFileStorage 已始终注册，此处是否注册后台服务不影响 CubeController 的激活。
        if (CubeSetting.Current.FileStorageProvide || CubeSetting.Current.FileStorageFetch)
            services.AddHostedService<FileStorageService>();

        return services;
    }

    /// <summary>注册附件存储提供者。根据配置在本地磁盘与对象存储（OSS/COS/七牛/EasyIO）之间切换</summary>
    /// <param name="services">服务集合</param>
    /// <param name="set">魔方设置。为空时使用<see cref="CubeSetting"/>的当前实例</param>
    /// <returns></returns>
    public static IServiceCollection AddCubeAttachmentStorage(this IServiceCollection services, CubeSetting set = null)
    {
        set ??= CubeSetting.Current;

        IAttachmentStorage storage = new LocalAttachmentStorage();
        var type = set.AttachmentStorage;
        if (!type.IsNullOrEmpty() && !type.EqualIgnoreCase("Local"))
        {
            // EasyIO 使用核心库自带客户端，其他类型走S3兼容协议
            IObjectStorage oss;
            if (type.EqualIgnoreCase("EasyIO"))
                oss = new EasyClient { Server = set.ObjectStorageServer, AppId = set.ObjectStorageAppId, Secret = set.ObjectStorageSecret };
            else
                oss = new S3ObjectStorage
                {
                    Server = set.ObjectStorageServer,
                    AppId = set.ObjectStorageAppId,
                    Secret = set.ObjectStorageSecret,
                    Bucket = set.ObjectStorageBucket,
                    Region = set.ObjectStorageRegion,
                };

            storage = new ObjectAttachmentStorage { Storage = oss, Name = type };
        }

        // 静态门面，供 Attachment 实体与控制器访问
        AttachmentProvider.Provider = new AttachmentProvider { Storage = storage };

        XTrace.WriteLine(L.TLog("Attachment storage: {0}", storage.Name));

        return services;
    }
}

/// <summary>魔方文件分布式存储</summary>
public class CubeFileStorage : DefaultFileStorage
{
    // 基类从实例属性读取下载地址。并发拉取时不能互相改掉对方的 token。
    private readonly SemaphoreSlim _pullGate = new(1, 1);

    /// <summary>实例化魔方文件分布式存储</summary>
    /// <param name="setting">魔方设置</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="tracer">跟踪器</param>
    /// <param name="log">日志</param>
    public CubeFileStorage(CubeSetting setting, IServiceProvider serviceProvider, ITracer tracer, ILog log)
    {
        //NodeName = Environment.MachineName;
        RootPath = setting.UploadPath;
        DownloadUri = "/cube/file?id={Id}";

        ServiceProvider = serviceProvider;
        Tracer = tracer;
        Log = log;
    }

    /// <summary>获取本地文件的元数据</summary>
    protected override IFileInfo GetLocalFileMeta(Int64 attachmentId, String path)
    {
        //if (path.IsNullOrEmpty()) throw new ArgumentNullException(nameof(path));

        var att = Attachment.FindById(attachmentId);
        if (att == null) return null;

        return new NewFileInfo
        {
            Id = att.Id,
            Name = att.FileName,
            Path = att.FilePath,
            Hash = att.Hash,
            Length = att.Size,
        };
    }

    /// <summary>扫描本地文件</summary>
    public override async Task<Int32> ScanFilesAsync(DateTime startTime, CancellationToken cancellationToken = default)
    {
        if (FileRequestBus is StarEventBus<FileRequest> bus && !bus.IsReady) return -1;

        return await base.ScanFilesAsync(startTime, cancellationToken);
    }

    /// <summary>获取本地不存在的附件列表。用于文件同步</summary>
    /// <param name="startTime">从指定时间开始遍历</param>
    /// <returns></returns>
    protected override IEnumerable<IFileInfo> GetMissingAttachments(DateTime startTime)
    {
        var exp = new WhereExpression();
        if (startTime.Year > 2000) exp &= Attachment._.CreateTime >= startTime;

        var page = new PageParameter { PageIndex = 1, PageSize = 100, Sort = Attachment._.Id, Desc = true };

        while (true)
        {
            var list = Attachment.FindAll(exp, page);
            if (list == null || list.Count == 0) break;

            foreach (var att in list)
            {
                // 云存储附件不参与P2P同步，本地不会存放，无需请求
                if (!att.IsLocalStorage()) continue;

                var filePath = att.GetFilePath(RootPath);
                if (!filePath.IsNullOrEmpty() && !File.Exists(filePath))
                {
                    yield return new NewFileInfo
                    {
                        Id = att.Id,
                        Name = att.FileName,
                        Path = att.FilePath,
                        Hash = att.Hash,
                        Length = att.Size,
                    };
                }
            }

            if (list.Count < page.PageSize) break;
            page.PageIndex++;
        }
    }

    /// <summary>按附件校验配置，为非公开分类附上短时分享令牌后再拉取。</summary>
    /// <param name="file">待拉取文件</param>
    /// <param name="sourceNode">提供文件的节点</param>
    /// <param name="cancellationToken">取消标记</param>
    protected override async Task FetchFileAsync(IFileInfo file, String sourceNode, CancellationToken cancellationToken)
    {
        var shareUrl = ResolvePullUrl(file);

        // 不带令牌的拉取也要进同一把锁。否则并发时会读到别人刚写上 token 的 DownloadUri。
        await _pullGate.WaitAsync(cancellationToken);
        var oldUri = DownloadUri;
        try
        {
            if (!shareUrl.IsNullOrEmpty()) DownloadUri = shareUrl;
            await base.FetchFileAsync(file, sourceNode, cancellationToken);
        }
        finally
        {
            DownloadUri = oldUri;
            _pullGate.Release();
        }
    }

    /// <summary>开启附件校验且分类不在公开列表时，节点互拉需要短时令牌。</summary>
    /// <param name="validateAttachment">是否校验附件访问</param>
    /// <param name="publicCategories">公开分类，逗号分隔</param>
    /// <param name="category">附件分类</param>
    /// <returns>需要附带短时令牌时为 true</returns>
    internal static Boolean NeedSyncPullToken(Boolean validateAttachment, String publicCategories, String category)
    {
        if (!validateAttachment) return false;
        if (publicCategories.IsNullOrEmpty()) return true;

        var cats = publicCategories.Split(',');
        return !cats.Any(c => c.Trim().EqualIgnoreCase(category));
    }

    private static String ResolvePullUrl(IFileInfo file)
    {
        if (file == null || file.Id <= 0) return null;

        var att = Attachment.FindById(file.Id);
        if (att == null) return null;

        var set = CubeSetting.Current;
        if (!NeedSyncPullToken(set.ValidateAttachment, set.PublicAttachmentCategories, att.Category)) return null;

        var share = ViewHelper.GetAttachmentShareUrl(att, att.CreateUserId);
        if (share.IsNullOrEmpty() || share.IndexOf("token=", StringComparison.OrdinalIgnoreCase) < 0) return null;
        return share;
    }
}

