using Microsoft.Extensions.DependencyInjection.Extensions;
using NewLife.Common;
using NewLife.Cube.Services;
using XCode.Configuration;

namespace NewLife.Cube;

/// <summary>集群配置同步服务注册扩展</summary>
/// <remarks>
/// 启用后，配置页的读取显示"本地为底 + 数据库覆盖"的合并视图，保存时同时写入 Parameter 与本地文件，
/// 并经事件总线通知集群内其它实例立即检查；未注册事件总线时退化为 ConfigSyncManager 的定时轮询。
/// </remarks>
public static class CubeConfigSyncExtensions
{
    /// <summary>添加集群配置同步</summary>
    /// <param name="services">服务容器</param>
    /// <param name="configure">同步管理器配置回调，用于设置应用标识并注册参与同步的配置类型</param>
    /// <returns>服务容器，支持链式调用</returns>
    /// <example>
    /// <code>
    /// services.AddConfigSync(sync =&gt;
    /// {
    ///     sync.AppId = "MyApp";
    ///     sync.AddCommon();
    ///     sync.Add&lt;MySetting&gt;("My");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddConfigSync(this IServiceCollection services, Action<ConfigSyncManager>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // 未指定应用标识时取系统名称，保证同一应用的各实例共用同一个 Parameter 命名空间
        var manager = new ConfigSyncManager(SysConfig.Current.Name);
        configure?.Invoke(manager);

        services.TryAddSingleton(manager);
        services.AddHostedService<CubeConfigSyncHostedService>();

        return services;
    }
}
