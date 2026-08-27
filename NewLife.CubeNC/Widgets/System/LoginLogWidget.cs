using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>登录与在线。最近24h登录记录、最近30分钟在线用户</summary>
[Widget("LoginLog", "登录与在线", Icon = "fa-users", Cols = 6, Sort = 120, Category = "系统", AdminOnly = true)]
public class LoginLogWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>登录与在线统计匿名对象</returns>
    public Object GetData()
    {
        var now = DateTime.Now;
        // Log.Id 是雪花Id，带时间信息，用 Id 时间范围过滤
        var snow = XLog.Meta.Factory.Snow;

        // 最近24h登录记录
        var start = now.AddHours(-24);
        var logins = XLog.FindAll(XLog._.Action.Contains("登录") & XLog._.ID.Between(start, now, snow), "CreateTime desc", null, 0, 10);
        var loginCount = XLog.FindCount(XLog._.Action.Contains("登录") & XLog._.ID.Between(start, now, snow));

        // 最近30分钟在线用户（UserOnline 非雪花表，仍用 CreateTime）
        var onlineStart = now.AddMinutes(-30);
        var onlines = UserOnline.FindAll(UserOnline._.CreateTime >= onlineStart, "CreateTime desc", null, 0, 10);
        var onlineCount = UserOnline.FindCount(UserOnline._.CreateTime >= onlineStart);

        return new
        {
            LoginCount24h = loginCount,
            OnlineCount = onlineCount,
            Logins = logins.Select(e => new { e.CreateTime, e.UserName, e.Action, e.CreateIP }).ToArray(),
            Onlines = onlines.Select(e => new { e.Name, e.CreateTime, e.OAuthProvider }).ToArray(),
        };
    }
}
