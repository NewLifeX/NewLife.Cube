using NewLife.Log;
using NewLife.Messaging;
using NewLife.Serialization;

namespace NewLife.Cube.AI;

/// <summary>检查点结果事件。前端回传结果经事件总线广播到检查点等待方（含其他实例）</summary>
public class CheckpointResultEvent
{
    /// <summary>检查点编号</summary>
    public String CheckpointId { get; set; } = "";

    /// <summary>发起检查点的用户编号，回传时校验防跨用户串扰</summary>
    public Int64 UserId { get; set; }

    /// <summary>前端回传的结果 JSON（<c>{ok,value,error}</c>）</summary>
    public String Result { get; set; } = "";
}

/// <summary>页面检查点服务。AI 工具挂起等待前端页面执行结果回传，参考 StarChat CheckpointService 架构</summary>
/// <remarks>
/// 工作流（与 ask_user / CheckpointService 一致）：
/// <list type="number">
/// <item>工具调用 <see cref="NewCheckpointId"/> 生成检查点编号（或使用 <c>ToolCallContext.ToolCallId</c>），经自身持有的 SSE 写回调下发 run_js 事件给前端</item>
/// <item>工具调用 <see cref="WaitForChoiceAsync"/> 以检查点编号挂起等待（最长 30 秒）</item>
/// <item>前端执行脚本后 <c>POST /Admin/Ai/OperationResult</c> 回传，<see cref="Respond"/> 解除等待</item>
/// <item>工具返回执行结果，LLM 继续推理</item>
/// </list>
/// 分布式语义：挂起状态经 <see cref="IEventBusFactory"/> 事件总线广播，以检查点编号为 topic。
/// 单机（MemoryCache）为按 topic 缓存的进程内总线；集群（FullRedis / 星尘 AppClient）经 Redis 列表/消费组或星尘广播到全部实例，
/// 回传端点即使命中其他实例也能唤醒等待方。检查点绑定当前用户，防止跨用户串扰；超时返回空结果，不阻断对话。
/// </remarks>
/// <param name="factory">事件总线工厂，来自缓存提供者（<c>ICacheProvider.Cache</c>）。null 时使用进程内事件枢纽（单机兜底）</param>
public class PageCheckpointService(IEventBusFactory? factory = null)
{
    #region 属性
    /// <summary>事件总线工厂。null 时使用进程内事件枢纽（单机兜底）</summary>
    private readonly IEventBusFactory? _factory = factory;

    /// <summary>进程内事件枢纽。无工厂时按 topic（检查点编号）创建独立总线，避免跨检查点串扰</summary>
    private readonly EventHub<CheckpointResultEvent> _hub = new();

    private static Int64 _nextSeq = DateTime.UtcNow.Ticks;

    /// <summary>全局实例（进程内兜底）。DI 注册的服务通常优先使用</summary>
    public static PageCheckpointService Instance { get; } = new();
    #endregion

    #region 方法
    /// <summary>生成唯一检查点编号</summary>
    /// <returns>格式为 "js_{序号}" 的唯一编号</returns>
    public static String NewCheckpointId() => $"js_{Interlocked.Increment(ref _nextSeq)}";

    /// <summary>挂起等待前端回传结果。超时返回错误结果（不阻断对话）</summary>
    /// <param name="checkpointId">检查点唯一编号</param>
    /// <param name="userId">发起检查点的用户编号，回传时校验</param>
    /// <param name="timeoutSeconds">等待超时秒数，默认 30</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>前端回传的结果 JSON（<c>{ok,value,error}</c>）；超时或取消则返回错误 JSON</returns>
    public async Task<String> WaitForChoiceAsync(String checkpointId, Int64 userId, Int32 timeoutSeconds = 30, CancellationToken cancellationToken = default)
    {
        var bus = GetBus(checkpointId);
        var clientId = $"cp_{Interlocked.Increment(ref _nextSeq)}";
        var tcs = new TaskCompletionSource<String>(TaskCreationOptions.RunContinuationsAsynchronously);

        // 一次性订阅：校验 userId 后完成并退订（跨用户回传忽略，防串扰）
        IEventHandler<CheckpointResultEvent> handler = new DelegateEventHandler<CheckpointResultEvent>((Action<CheckpointResultEvent, IEventContext?>)((ev, ctx) =>
        {
            if (userId > 0 && ev.UserId > 0 && ev.UserId != userId) return;
            bus.Unsubscribe(clientId);
            tcs.TrySetResult(ev.Result ?? "");
        }));

        await bus.SubscribeAsync(handler, clientId, cancellationToken).ConfigureAwait(false);
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            try
            {
                return await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 超时或取消：返回错误结果，不阻断对话
                return new { ok = false, error = "浏览器操作超时，前端未回传结果" }.ToJson();
            }
        }
        finally
        {
            await bus.UnsubscribeAsync(clientId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>完成一次待处理检查点（由全局 AiController.OperationResult 端点调用）。经事件总线广播，可跨实例唤醒等待方</summary>
    /// <param name="checkpointId">检查点唯一编号</param>
    /// <param name="userId">回传请求的当前用户编号</param>
    /// <param name="result">前端回传的结果 JSON</param>
    /// <returns>是否成功发布。检查点编号为空或发布失败时返回 false</returns>
    public async Task<Boolean> Respond(String checkpointId, Int64 userId, String result)
    {
        if (checkpointId.IsNullOrEmpty()) return false;

        var bus = GetBus(checkpointId);
        try
        {
            var n = await bus.PublishAsync(new CheckpointResultEvent { CheckpointId = checkpointId, UserId = userId, Result = result ?? "" }).ConfigureAwait(false);
            return n > 0;
        }
        catch (Exception ex)
        {
            XTrace.WriteLine("[Checkpoint] 回传发布失败 {0}：{1}", checkpointId, ex.Message);
            return false;
        }
    }

    /// <summary>获取检查点对应的事件总线。有工厂时经工厂创建（分布式）；否则按 topic 使用进程内枢纽（单机）</summary>
    /// <param name="checkpointId">检查点唯一编号</param>
    /// <returns>事件总线</returns>
    private IEventBus<CheckpointResultEvent> GetBus(String checkpointId)
        => _factory != null ? _factory.CreateEventBus<CheckpointResultEvent>(checkpointId) : _hub.GetEventBus(checkpointId);
    #endregion
}
