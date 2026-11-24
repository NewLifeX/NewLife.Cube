using NewLife.Data;

namespace NewLife.Cube.Services.Notifications;

/// <summary>模块级通知请求。只保留最常用的 8 个字段，并允许下游扩展</summary>
/// <remarks>
/// 面向普通业务模块：发给指定用户、默认站内信、标题内容直填。
/// 角色/部门/全员/管理员、模板、延迟、共享已读、外部目标等高级能力请使用
/// <see cref="NotificationRequest"/> + <see cref="NotificationService.Publish"/>。
/// 下游可通过 <see cref="Items"/> 追加自定义字段，随请求进入模板渲染与业务载荷。
/// </remarks>
public class NotifyRequest : IExtend
{
    /// <summary>业务消息编号。相同编号可用于幂等/去重</summary>
    public String? BizId { get; set; }

    /// <summary>收件人用户编号</summary>
    public Int32[]? UserIds { get; set; }

    /// <summary>投递渠道。为空时默认站内信</summary>
    public String[]? Channels { get; set; }

    /// <summary>标题</summary>
    public String? Title { get; set; }

    /// <summary>内容</summary>
    public String? Content { get; set; }

    /// <summary>点击跳转地址</summary>
    public String? Link { get; set; }

    /// <summary>分类。默认 Business</summary>
    public String? Category { get; set; }

    /// <summary>级别。默认 Info</summary>
    public String? Level { get; set; }

    /// <summary>扩展数据。下游可写入自定义字段</summary>
    public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>设置 或 获取 扩展数据项</summary>
    /// <param name="key">键</param>
    /// <returns>扩展数据</returns>
    public Object? this[String key]
    {
        get => Items.TryGetValue(key, out var value) ? value : null;
        set => Items[key] = value;
    }
}

/// <summary>模块级通知结果。只保留 4 个字段，并允许下游扩展</summary>
public class NotifyResult : IExtend
{
    /// <summary>业务消息编号</summary>
    public String? BizId { get; set; }

    /// <summary>收件人数量</summary>
    public Int32 Recipients { get; set; }

    /// <summary>写入记录数量</summary>
    public Int32 Records { get; set; }

    /// <summary>是否因去重或无收件人而跳过</summary>
    public Boolean Skipped { get; set; }

    /// <summary>扩展数据。下游实现可写入自定义字段</summary>
    public IDictionary<String, Object?> Items { get; } = new Dictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>设置 或 获取 扩展数据项</summary>
    /// <param name="key">键</param>
    /// <returns>扩展数据</returns>
    public Object? this[String key]
    {
        get => Items.TryGetValue(key, out var value) ? value : null;
        set => Items[key] = value;
    }
}
