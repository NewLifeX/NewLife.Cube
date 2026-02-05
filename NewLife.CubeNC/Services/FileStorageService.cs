using NewLife.Caching;
using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Data;
using NewLife.Log;
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
        await Task.Delay(10_000, cancellationToken);
        await fileStorage.InitializeAsync(cancellationToken);
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

        services.AddHostedService<FileStorageService>();

        return services;
    }
}

/// <summary>魔方文件分布式存储</summary>
public class CubeFileStorage : DefaultFileStorage
{
    private ICacheProvider _cacheProvider;
    private IRegistry? _registry;

    /// <summary>实例化魔方文件分布式存储</summary>
    public CubeFileStorage(CubeSetting setting, IServiceProvider serviceProvider, ICacheProvider cacheProvider, ITracer tracer, ILog log)
    {
        //NodeName = Environment.MachineName;
        RootPath = setting.UploadPath;
        DownloadUri = "/cube/file?id={Id}";

        ServiceProvider = serviceProvider;
        Tracer = tracer;
        Log = log;

        _cacheProvider = cacheProvider;
        //_registry = registry;
        _registry = serviceProvider.GetService<IRegistry>();
    }

    /// <summary>初始化</summary>
    protected override Task OnInitializedAsync(CancellationToken cancellationToken)
    {
        // 优先Redis队列作为事件总线，其次使用星尘事件总线
        var cache = _cacheProvider.Cache;
        var type = cache.GetType();
        if (type != typeof(MemoryCache) && type != typeof(Cache))
            SetEventBus(_cacheProvider);
        else if (_registry is AppClient client)
            SetEventBus(client);
        else
            SetEventBus(_cacheProvider);

        return base.OnInitializedAsync(cancellationToken);
    }

    /// <summary>获取本地文件的元数据</summary>
    protected override IFileInfo GetLocalFileMeta(Int64 attachmentId, String path)
    {
        //if (path.IsNullOrEmpty()) throw new ArgumentNullException(nameof(path));

        var att = Attachment.FindById(attachmentId);

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
}
