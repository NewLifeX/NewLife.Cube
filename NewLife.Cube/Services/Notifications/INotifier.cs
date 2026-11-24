namespace NewLife.Cube.Services.Notifications;

/// <summary>通知发送入口。业务模块只依赖本接口，不直接依赖 <see cref="NotificationService"/></summary>
/// <remarks>
/// 当前为魔方内部接口；跨宿主契约（Provider/核心信封）等需要时再抽到 NewLife.Core。
/// 接口只接收 <see cref="NotifyRequest"/>；角色/部门/模板/延迟等高级能力请用 <see cref="NotificationService.Publish"/>。
/// </remarks>
/// <example>
/// <code>
/// public class OrderService(INotifier notifier)
/// {
///     public async Task PaidAsync(Order order)
///     {
///         await notifier.SendAsync(new NotifyRequest
///         {
///             BizId = $"order:{order.Id}:paid",
///             UserIds = [order.UserId],
///             Channels = [NotificationChannels.InApp, NotificationChannels.Mail],
///             Title = "订单已支付",
///             Content = $"订单 {order.Code} 已支付成功",
///         });
///     }
/// }
/// </code>
/// </example>
public interface INotifier
{
    /// <summary>发送通知</summary>
    /// <param name="request">模块级通知请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布结果。“已受理并落库”，不代表外部渠道已送达</returns>
    /// <exception cref="ArgumentNullException">请求为空</exception>
    /// <exception cref="ArgumentException">标题和内容为空</exception>
    /// <exception cref="InvalidOperationException">收件人超过上限等业务校验失败</exception>
    Task<NotifyResult> SendAsync(NotifyRequest request, CancellationToken cancellationToken = default);
}
