using NewLife.Caching;
using NewLife.Log;
using NewLife.Messaging;
using XCode.Configuration;

namespace NewLife.Cube.Services;

/// <summary>集群配置同步宿主服务。启动定时检查，并经事件总线在集群内广播配置变更</summary>
/// <remarks>
/// 事件总线为可选能力：容器中存在 IEventBusFactory 或 ICacheProvider 时，订阅 <see cref="Topic"/> 主题接收其它实例的变更通知，
/// 立即检查对应作用域；都不存在时只依靠 ConfigSyncManager 的定时轮询（默认 60 秒），不报错也不影响应用启动。
/// </remarks>
public class CubeConfigSyncHostedService : IHostedService
{
    /// <summary>事件主题</summary>
    public const String Topic = "CubeConfigSync";

    private readonly ConfigSyncManager _manager;
    private readonly IServiceProvider _services;
    private readonly String _clientId;

    private IEventBus<ConfigSyncEvent>? _bus;
    private IEventHandler<ConfigSyncEvent>? _handler;

    /// <summary>实例化</summary>
    /// <param name="manager">配置同步管理器</param>
    /// <param name="services">服务提供者，用于可选获取事件总线工厂或缓存提供者</param>
    public CubeConfigSyncHostedService(ConfigSyncManager manager, IServiceProvider services)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _services = services ?? throw new ArgumentNullException(nameof(services));

        _clientId = Runtime.ClientId;
        if (_clientId.IsNullOrEmpty()) _clientId = $"{Environment.MachineName}@{Environment.ProcessId}";
    }

    /// <summary>启动。先订阅集群广播，再启动定时同步；总线异常不影响应用启动</summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _bus = CreateEventBus();
            if (_bus != null)
            {
                _handler = new DelegateEventHandler<ConfigSyncEvent>((Action<ConfigSyncEvent, IEventContext?>)((e, ctx) =>
                {
                    // 同一总线可能承载多个应用的事件，只处理本应用的变更
                    if (!e.AppId.EqualIgnoreCase(_manager.AppId)) return;

                    _manager.CheckNow(e.Scope);
                }));

                await _bus.SubscribeAsync(_handler, _clientId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                XTrace.WriteLine("ConfigSync[{0}] no event bus available, fall back to {1}s polling", _manager.AppId, _manager.Period);
            }
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        _manager.Changed += OnChanged;
        _manager.Start();
    }

    /// <summary>停止。退订事件、停止定时检查并释放总线</summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _manager.Changed -= OnChanged;
        _manager.Stop();

        _handler = null;

        var bus = _bus;
        _bus = null;
        if (bus == null) return;

        try
        {
            await bus.UnsubscribeAsync(_clientId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        bus.TryDispose();
    }

    /// <summary>创建事件总线。优先使用事件总线工厂，其次用缓存提供者创建，均不可用时返回空</summary>
    /// <returns>事件总线；没有可用通道时返回 null</returns>
    private IEventBus<ConfigSyncEvent>? CreateEventBus()
    {
        var factory = _services.GetService<IEventBusFactory>();
        if (factory != null)
        {
            var bus = factory.CreateEventBus<ConfigSyncEvent>(Topic, _clientId);
            if (bus != null) return bus;
        }

        return _services.GetService<ICacheProvider>()?.CreateEventBus<ConfigSyncEvent>(Topic, _clientId);
    }

    private void OnChanged(Object? sender, ConfigSyncEvent e) => _ = PublishAsync(e);

    /// <summary>发布配置变更事件。失败只记日志，不影响本地保存结果</summary>
    /// <param name="e">配置变更事件</param>
    private async Task PublishAsync(ConfigSyncEvent e)
    {
        var bus = _bus;
        if (bus == null) return;

        try
        {
            // 携带客户端标识，默认总线按 ClientId 排除自身订阅者，避免本实例重复检查
            var ctx = new EventContext { Topic = Topic, ClientId = _clientId };
            await bus.PublishAsync(e, ctx).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            XTrace.WriteLine("ConfigSync[{0}] publish {1} change failed: {2}", _manager.AppId, e.Scope, ex.Message);
        }
    }
}
