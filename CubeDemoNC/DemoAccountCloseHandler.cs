using NewLife.Cube.Services;
using NewLife.Log;
using XCode.Membership;

namespace CubeDemoNC;

/// <summary>账号注销处理器示例。演示下游系统在用户注销账号时清理关联业务数据</summary>
/// <remarks>
/// 注册方式：<c>services.AddSingleton&lt;IAccountCloseHandler, DemoAccountCloseHandler&gt;()</c>，AddCube 前后均可。
/// 本示例仅在控制台记录日志，真实业务系统应在此删除与用户关联的业务数据（订单、设备、消息等）。
/// </remarks>
public class DemoAccountCloseHandler : IAccountCloseHandler
{
    /// <summary>处理账号注销</summary>
    /// <param name="user">用户快照。注销脱敏前的数据副本</param>
    /// <param name="ip">客户端IP</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public Task HandleAsync(IUser user, String ip, CancellationToken cancellationToken = default)
    {
        XTrace.WriteLine("[DemoAccountCloseHandler] 检测到账号注销，模拟清理下游数据：{0}（ID={1}，Mail={2}，IP={3}）", user?.Name, user?.ID, user?.Mail, ip);
        return Task.CompletedTask;
    }
}
