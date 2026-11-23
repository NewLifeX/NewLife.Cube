using System;
using NewLife.Caching;

namespace NewLife.Cube.Services.Notifications;

/// <summary>SSE 票据信息</summary>
public class NotificationStreamTicket
{
    /// <summary>用户编号</summary>
    public Int32 UserId { get; set; }

    /// <summary>是否系统管理员</summary>
    public Boolean Admin { get; set; }
}

/// <summary>通知 SSE 连接票据。避免把访问令牌放进 URL，票据一次性且短时有效</summary>
/// <param name="cacheProvider">缓存提供者</param>
public class NotificationStreamTicketService(ICacheProvider cacheProvider)
{
    private readonly ICache _cache = cacheProvider.Cache;

    /// <summary>签发连接票据</summary>
    /// <param name="userId">用户编号</param>
    /// <param name="ip">客户端地址</param>
    /// <param name="admin">是否系统管理员</param>
    /// <returns>票据</returns>
    public String Issue(Int32 userId, String ip, Boolean admin = false)
    {
        var ticket = Guid.NewGuid().ToString("N");
        _cache.Set($"NotifyTicket:{ticket}", $"{userId}|{ip}|{(admin ? 1 : 0)}", 60);

        return ticket;
    }

    /// <summary>校验并消费连接票据</summary>
    /// <param name="ticket">票据</param>
    /// <param name="ip">客户端地址</param>
    /// <returns>票据信息；无效时返回 null</returns>
    public NotificationStreamTicket Consume(String ticket, String ip)
    {
        if (ticket.IsNullOrEmpty()) return null;

        var key = $"NotifyTicket:{ticket}";
        var value = _cache.Get<String>(key);
        if (value == null) return null;

        var parts = value.Split('|');
        if (parts.Length < 2 || parts[0].ToInt() <= 0) return null;
        if (!parts[1].IsNullOrEmpty() && parts[1] != ip) return null;

        _cache.Remove(key);

        return new NotificationStreamTicket
        {
            UserId = parts[0].ToInt(),
            Admin = parts.Length > 2 && parts[2] == "1",
        };
    }

    /// <summary>校验并消费连接票据，同时校验用户</summary>
    /// <param name="ticket">票据</param>
    /// <param name="userId">用户编号</param>
    /// <param name="ip">客户端地址</param>
    /// <returns>是否有效</returns>
    public Boolean Consume(String ticket, Int32 userId, String ip)
    {
        var info = Consume(ticket, ip);

        return info != null && info.UserId == userId;
    }
}
