using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>账号注销处理器。用户自助注销账号时被调起，供下游模块清理与账号关联的业务数据</summary>
/// <remarks>
/// 调用时机：框架完成账号注销核心动作（吊销全部令牌、解绑第三方、清理在线记录、禁用并脱敏账号）之后。
/// 失败策略：尽力而为。单个处理器异常会被隔离记录，不影响其它处理器，也不影响账号注销的成功结果。
/// 幂等要求：处理器可能被重复调用，必须自行保证幂等。
/// 注册方式：<c>services.AddSingleton&lt;IAccountCloseHandler, MyHandler&gt;()</c>，AddCube 前后任意时机注册均可，注销时按注册顺序逐个调用。
/// 作用域：每次注销使用独立服务作用域解析，支持 Singleton/Scoped/Transient 生命周期。
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;IAccountCloseHandler, MyAccountCloseHandler&gt;();
/// </code>
/// </example>
public interface IAccountCloseHandler
{
    /// <summary>处理账号注销。可在此删除与账号关联的下游业务数据</summary>
    /// <param name="user">用户快照。注销脱敏前的数据副本，含 ID/Name/Mail/Mobile 等原始信息</param>
    /// <param name="ip">客户端IP</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task HandleAsync(IUser user, String ip, CancellationToken cancellationToken = default);
}
