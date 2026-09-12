using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Services;

/// <summary>安全事件服务。记录安全防御事件到审计日志，并发送封禁告警站内信</summary>
/// <remarks>
/// 事件写入审计日志表（Category=安全防御），同IP同动作5分钟窗口内去重，避免扫描期间刷爆日志。
/// 封禁告警每小时最多一条，避免高频封禁刷屏。
/// </remarks>
public class SecurityEventService
{
    /// <summary>安全事件类别。写入审计日志时的Category值</summary>
    public const String CategoryName = "安全防御";

    /// <summary>封禁告警动作</summary>
    public const String BlockAlertAction = "SecurityBlock";

    /// <summary>安全自动模式动作。进入临时拦截与自动回落通知</summary>
    public const String AutoAlertAction = "SecurityAuto";

    private readonly ICacheProvider _cacheProvider;
    private readonly ITracer _tracer;

    /// <summary>实例化安全事件服务</summary>
    /// <param name="cacheProvider">缓存提供者</param>
    /// <param name="tracer">追踪器</param>
    public SecurityEventService(ICacheProvider cacheProvider, ITracer tracer)
    {
        _cacheProvider = cacheProvider;
        _tracer = tracer;
    }

    #region 事件写入
    /// <summary>写入安全事件。同IP同动作5分钟内去重</summary>
    /// <param name="action">事件动作。如SQL注入/扫描器/蜜罐探测/自动封禁/扫描探测</param>
    /// <param name="message">事件详情</param>
    /// <param name="ip">来源IP</param>
    /// <param name="user">当前用户，可为空</param>
    /// <param name="blocked">是否已拦截。观察模式为false</param>
    /// <param name="linkId">关联对象。如触发封禁的规则编号</param>
    /// <returns>是否已写入</returns>
    public Boolean Write(String action, String message, String ip, String user = null, Boolean blocked = false, Int64 linkId = 0)
    {
        if (action.IsNullOrEmpty()) return false;

        // 同IP同动作5分钟窗口去重，避免扫描期间日志爆炸
        if (!ip.IsNullOrEmpty())
        {
            var key = $"security:event:{action}:{ip}";
            if (!_cacheProvider.Cache.Add(key, 1, 300)) return false;
        }

        using var span = _tracer?.NewSpan($"security:{action}", new { ip, user });

        try
        {
            var log = new XLog
            {
                Category = CategoryName,
                Action = action,
                Success = blocked,
                Remark = Cut(message, 2000),
                CreateIP = ip,
                LinkID = linkId,
                UserName = user,
                TraceId = DefaultSpan.Current?.TraceId,
            };
            log.Insert();

            DefaultSpan.Current?.AppendTag($"securityEvent={action} ip={ip} blocked={blocked}");

            return true;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);
            return false;
        }
    }
    #endregion

    #region 封禁告警
    /// <summary>发送封禁告警站内信。每小时最多一条，避免高频封禁刷屏</summary>
    /// <param name="ip">被封禁的IP</param>
    /// <param name="reason">封禁原因</param>
    /// <param name="expireTime">解封时间</param>
    public void NotifyBlocked(String ip, String reason, DateTime expireTime)
    {
        XTrace.WriteLine("安全防御封禁 {0}：{1}，解封时间 {2:yyyy-MM-dd HH:mm:ss}", ip, reason, expireTime);

        NotifyOnce(BlockAlertAction, $"安全防御：封禁 {ip}", $"来源IP {ip} 因 {reason} 已被自动封禁，解封时间 {expireTime:yyyy-MM-dd HH:mm:ss}。请登录后台查看安全事件与封禁规则，必要时手动解封。", reason, false);
    }

    /// <summary>发送安全自动模式通知。进入临时拦截与自动回落时使用，同标题每小时最多一条</summary>
    /// <param name="title">标题，如 进入临时拦截</param>
    /// <param name="content">通知内容</param>
    public void NotifyAuto(String title, String content) => NotifyOnce(AutoAlertAction, $"安全防御：{title}", content, title, true);

    /// <summary>发送站内信通知。默认每小时最多一条，可选按标题去重区分不同事件</summary>
    /// <param name="action">通知动作</param>
    /// <param name="title">通知标题</param>
    /// <param name="content">通知内容</param>
    /// <param name="result">结果摘要</param>
    /// <param name="dedupByTitle">是否按标题去重。false表示仅按动作去重</param>
    private void NotifyOnce(String action, String title, String content, String result, Boolean dedupByTitle)
    {
        try
        {
            // 每小时最多一条，避免高频通知刷屏
            var now = DateTime.Now;
            var start = now.AddHours(-1);
            var where = NotificationRecord._.Action == action & NotificationRecord._.CreateTime >= start;
            if (dedupByTitle) where = where & (NotificationRecord._.Title == title);
            if (NotificationRecord.FindCount(where) > 0) return;

            var record = new NotificationRecord
            {
                Action = action,
                Channel = "InApp",
                Title = title,
                Content = content,
                Success = true,
                Result = result,
                CreateTime = now,
            };
            record.Insert();
        }
        catch (Exception ex)
        {
            // 告警不得影响主防御流程
            XTrace.WriteException(ex);
        }
    }
    #endregion

    #region 辅助
    /// <summary>截断字符串</summary>
    /// <param name="text">文本</param>
    /// <param name="max">最大长度</param>
    /// <returns></returns>
    private static String Cut(String text, Int32 max) => text == null || text.Length <= max ? text : text[..max];
    #endregion
}
