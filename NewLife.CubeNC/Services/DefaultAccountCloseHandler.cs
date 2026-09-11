using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>账号注销默认处理器。清理框架侧个人数据并脱敏用户行；账号禁用由框架在处理器之后兜底完成</summary>
/// <remarks>
/// 由 AddCube 通过 TryAddEnumerable 注册为默认实现，下游可继续追加自定义 <see cref="IAccountCloseHandler"/>。
/// 清理范围：吊销全部令牌、解绑第三方、清理在线记录、OAuth日志、通知记录、验证码记录、用户字典参数、租户关系、委托代理；
/// 随后脱敏用户行的敏感字段（保留 ID/Name，不含 Enable）。
/// 失败策略：尽力而为，异常由调用方隔离记录。
/// </remarks>
public class DefaultAccountCloseHandler : IAccountCloseHandler
{
    /// <summary>处理账号注销</summary>
    /// <param name="user">用户快照。注销前的数据副本</param>
    /// <param name="ip">客户端IP</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public Task HandleAsync(IUser user, String ip, CancellationToken cancellationToken = default)
    {
        if (user == null || user.ID <= 0) return Task.CompletedTask;

        // 吊销全部令牌，使其立即失效
        UserToken.RevokeByUser(user.ID);

        // 解绑第三方
        UserConnect.Delete(UserConnect._.UserID == user.ID);

        // 清理在线记录
        UserOnline.Delete(UserOnline._.UserID == user.ID);

        // OAuth 日志。含第三方 OpenID 关联与访问令牌等敏感信息
        OAuthLog.Delete(OAuthLog._.UserId == user.ID);

        // 通知记录。站内信/短信/邮件等个人消息
        NotificationRecord.Delete(NotificationRecord._.UserId == user.ID);

        // 验证码记录。按用户编号，以及按注销前联系方式（未登录发码时 UserId 可能为 0）
        VerifyCodeRecord.Delete(VerifyCodeRecord._.UserId == user.ID);
        if (!user.Mail.IsNullOrEmpty()) VerifyCodeRecord.Delete(VerifyCodeRecord._.Target == user.Mail);
        if (!user.Mobile.IsNullOrEmpty()) VerifyCodeRecord.Delete(VerifyCodeRecord._.Target == user.Mobile);

        // 用户字典参数。保留 UserID=0 的系统参数
        Parameter.Delete(Parameter._.UserID == user.ID);

        // 租户关系
        TenantUser.Delete(TenantUser._.UserId == user.ID);

        // 委托代理。委托人与代理人双向
        PrincipalAgent.Delete(PrincipalAgent._.PrincipalId == user.ID);
        PrincipalAgent.Delete(PrincipalAgent._.AgentId == user.ID);

        // 脱敏用户行（保留 ID/Name 防重名与审计；Enable 不动，由框架兜底禁用）
        var entity = User.FindByID(user.ID);
        if (entity != null)
        {
            entity.Password = null;
            entity.Mail = null;
            entity.MailVerified = false;
            entity.Mobile = null;
            entity.MobileVerified = false;
            entity.DisplayName = null;
            entity.Avatar = null;
            entity.Code = null;
            entity.Age = 0;
            entity.Birthday = DateTime.MinValue;
            entity.LastLoginIP = null;
            entity.RegisterIP = null;
            entity.AreaId = 0;
            entity.Ex1 = 0;
            entity.Ex2 = 0;
            entity.Ex3 = 0;
            entity.Ex4 = null;
            entity.Ex5 = null;
            entity.Ex6 = null;
            entity.Update();
        }

        return Task.CompletedTask;
    }
}
