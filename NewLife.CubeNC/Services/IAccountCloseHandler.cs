using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>账号注销处理器。用户自助注销账号时被调起，供框架与下游清理与账号关联的个人数据</summary>
/// <remarks>
/// 调用时机：框架完成校验后、账号禁用之前依次调用，入参为注销前的用户快照。
/// 注册要求：必须注册为 Singleton。框架从根容器解析全部实现，不支持 Scoped/Transient。
/// 注册方式：<c>services.AddSingleton&lt;IAccountCloseHandler, MyHandler&gt;()</c>，AddCube 前后任意时机均可，按注册顺序逐个调用。
/// 失败策略：全部尽力而为。单个处理器异常被隔离记录，不影响其它处理器；账号禁用由框架在处理器之后兜底完成。
/// 幂等要求：处理器可能被重复调用，必须自行保证幂等。
/// 清理参考：默认处理器 <see cref="DefaultAccountCloseHandler"/> 负责吊销令牌、解绑三方、清理在线/OAuth日志/通知/验证码/用户参数/租户关系/委托代理，并脱敏用户行。
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
