using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;
using static XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>登录与在线。最近登录记录、当前在线用户（KPI 已提供登录/在线计数，此处仅展示明细）</summary>
[Widget("LoginLog", "登录与在线", Icon = "fa-users", Cols = 4, Sort = 120, Category = "系统", AdminOnly = true)]
public class LoginLogWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>登录与在线明细匿名对象</returns>
    public Object GetData()
    {
        var now = DateTime.Now;
        // Log.Id 是雪花Id，带时间信息，用 Id 时间范围过滤
        var snow = XLog.Meta.Factory.Snow;

        // 最近24h登录记录。雪花Id，按 Id 降序
        var start = now.AddHours(-24);
        var logins = XLog.FindAll(_.Action.Contains("登录") & _.ID.Between(start, now, snow), "ID desc", null, 0, 10);

        // 当前在线用户（UserOnline 由定时任务清理 20 分钟不活跃会话，表内即在线会话；非雪花表按自增 Id 降序取最新）
        var onlines = UserOnline.FindAll(null, "ID desc", null, 0, 10);

        return new
        {
            Logins = logins.Select(e => new { e.CreateTime, e.UserName, e.Action, e.CreateIP }).ToArray(),
            Onlines = onlines.Select(e => new { e.Name, e.CreateTime, e.OAuthProvider }).ToArray(),
        };
    }
}
