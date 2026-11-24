using NewLife.Caching;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Cube.Services.Notifications;

/// <summary>通知事件类型</summary>
public enum NotificationEventKind
{
    /// <summary>站内信已创建</summary>
    Created = 0,

    /// <summary>外部渠道待投递</summary>
    Dispatch = 1,
}

/// <summary>通知事件。只做跨节点唤醒信号，通知内容仍从数据库读取</summary>
public class NotificationEvent
{
    /// <summary>事件类型</summary>
    public NotificationEventKind Kind { get; set; }

    /// <summary>记录编号。Dispatch 事件为 0</summary>
    public Int64 Id { get; set; }

    /// <summary>用户编号。0 表示管理员广播；Dispatch 事件为 0</summary>
    public Int32 UserId { get; set; }
}

/// <summary>通知事件总线。把站内信创建与投递唤醒事件广播到各节点</summary>
/// <remarks>
/// <para>事件只做唤醒信号，不携带通知正文；正文由消费方按 Id 从数据库读取。不要直接用 NotificationRecord 或 NotifyRequest 充当事件：前者是 43 字段的持久化实体，后者是请求输入，二者都不适合做跨节点传输契约。</para>
/// <para>优先使用 <see cref="IEventBusFactory"/>，其次使用 <see cref="ICacheProvider"/> 创建事件总线；都没有时退化为 no-op，SSE 继续走数据库轮询，投递继续走定时兜底。</para>
/// <para>使用 <see cref="Runtime.ClientId"/> 作为消费组：Redis Stream 下每个实例一组，实现跨实例广播；MemoryCache 下为进程内广播。</para>
/// </remarks>
/// <param name="services">服务提供者</param>
/// <param name="topic">事件主题。为空时使用默认主题</param>
public class NotificationBus(IServiceProvider services, String? topic = null) : IDisposable
{
    /// <summary>默认事件主题</summary>
    public const String Topic = "CubeNotification";

    private const String SubscriberId = "NotificationBus";
    private readonly Object _lock = new();
    private readonly String _topic = topic.IsNullOrEmpty() ? Topic : topic;
    private IEventBus<NotificationEvent>? _bus;
    private Boolean _started;

    /// <summary>收到站内信创建事件。处理器应快速返回，避免阻塞总线消费</summary>
    public event EventHandler<NotificationEvent>? Created;

    /// <summary>收到投递唤醒事件。处理器应快速返回，避免阻塞总线消费</summary>
    public event EventHandler<NotificationEvent>? Dispatch;

    /// <summary>启动订阅。可重复调用</summary>
    public void Start()
    {
        if (_started) return;

        lock (_lock)
        {
            if (_started) return;
            _started = true;

            try
            {
                var bus = CreateEventBus();
                if (bus == null) return;

                bus.SubscribeAsync(e =>
                {
                    try
                    {
                        if (e.Kind == NotificationEventKind.Dispatch)
                            Dispatch?.Invoke(this, e);
                        else
                            Created?.Invoke(this, e);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                    }

                    return Task.CompletedTask;
                }, SubscriberId).ConfigureAwait(false).GetAwaiter().GetResult();

                _bus = bus;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }

    /// <summary>发布事件。失败只记日志，不影响通知主流程</summary>
    /// <param name="e">通知事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task PublishAsync(NotificationEvent e, CancellationToken cancellationToken = default)
    {
        if (e == null) return;

        Start();
        var bus = _bus;
        if (bus == null) return;

        try
        {
            await bus.PublishAsync(e, null, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }
    }

    private IEventBus<NotificationEvent>? CreateEventBus()
    {
        var factory = services.GetService<IEventBusFactory>();
        var bus = factory?.CreateEventBus<NotificationEvent>(_topic, Runtime.ClientId);
        if (bus != null) return bus;

        return services.GetService<ICacheProvider>()?.CreateEventBus<NotificationEvent>(_topic, Runtime.ClientId);
    }

    /// <summary>释放总线</summary>
    public void Dispose()
    {
        var bus = _bus;
        _bus = null;
        if (bus is IDisposable d) d.Dispose();
    }
}
