using System.Collections.Concurrent;
using NewLife.Serialization;

namespace NewLife.Cube.AI;

/// <summary>页面检查点服务。AI 工具挂起等待前端页面执行结果回传，参考 StarChat CheckpointService 架构</summary>
/// <remarks>
/// 工作流（与 ask_user / CheckpointService 一致）：
/// <list type="number">
/// <item>工具调用 <see cref="NewCheckpointId"/> 生成检查点编号，经自身持有的 SSE 写回调下发 run_js 事件给前端</item>
/// <item>工具调用 <see cref="WaitForChoiceAsync"/> 以检查点编号挂起等待（最长 30 秒）</item>
/// <item>前端执行脚本后 <c>POST /Admin/Ai/OperationResult</c> 回传，<see cref="Respond"/> 解除等待</item>
/// <item>工具返回执行结果，LLM 继续推理</item>
/// </list>
/// 挂起状态存进程内（跨请求共享，回传是独立 HTTP 请求）；检查点记录绑定当前用户，防止跨用户串扰；
/// 超时返回空结果，不阻断对话。SSE 写回调由工具实例持有（请求内隔离），本服务不依赖任何请求级状态。
/// </remarks>
public class PageCheckpointService
{
    #region 属性
    /// <summary>全局实例（单例）。挂起状态存进程内，回传端点与工具共享</summary>
    public static PageCheckpointService Instance { get; } = new();

    private static Int64 _nextSeq = DateTime.UtcNow.Ticks;
    #endregion

    #region 待处理检查点
    private sealed class Pending
    {
        /// <summary>完成源。前端回传结果后置为完成</summary>
        public TaskCompletionSource<String> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>发起检查点的用户编号</summary>
        public Int64 UserId { get; init; }
    }

    private readonly ConcurrentDictionary<String, Pending> _pendings = new();
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
        var pending = new Pending { UserId = userId };
        _pendings[checkpointId] = pending;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            try
            {
                return await pending.Tcs.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 超时或取消：返回错误结果，不阻断对话
                return new { ok = false, error = "浏览器操作超时，前端未回传结果" }.ToJson();
            }
        }
        finally
        {
            _pendings.TryRemove(checkpointId, out _);
        }
    }

    /// <summary>完成一次待处理检查点（由全局 AiController.OperationResult 端点调用）</summary>
    /// <param name="checkpointId">检查点唯一编号</param>
    /// <param name="userId">回传请求的当前用户编号</param>
    /// <param name="result">前端回传的结果 JSON</param>
    /// <returns>是否成功完成。检查点不存在、用户不匹配或已完成（一次性消费）时返回 false</returns>
    public Boolean Respond(String checkpointId, Int64 userId, String result)
    {
        if (!_pendings.TryGetValue(checkpointId, out var pending)) return false;
        if (pending.UserId != 0 && pending.UserId != userId) return false;

        // 一次性消费：已完成的操作不允许再次回传
        if (pending.Tcs.Task.IsCompleted) return false;

        return pending.Tcs.TrySetResult(result ?? "");
    }
    #endregion
}
