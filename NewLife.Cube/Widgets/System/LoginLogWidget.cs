using NewLife.Cube.Entity;
using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>登录与在线。最近登录记录、当前在线用户（KPI 已提供登录/在线计数，此处仅展示明细）</summary>
[Widget("LoginLog", "Sign-ins and online", Icon = "fa-users", Cols = 4, Sort = 120, Category = "System", AdminOnly = true, WidgetType = WidgetTypes.Content)]
public class LoginLogWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>登录与在线明细匿名对象</returns>
    public Object GetData()
    {
        var now = DateTime.Now;

        // 最近24h登录记录。雪花Id，按 Id 降序，仅展示少量行（工作台卡片空间有限）
        var start = now.AddHours(-24);
        var logins = XLog.FindAllByActionKeywords(["登录", "Sign in"], start, now, 5);

        // 当前在线用户（UserOnline 由定时任务清理 20 分钟不活跃会话，表内即在线会话；非雪花表按自增 Id 降序取最新，仅展示少量行）
        var onlines = UserOnline.FindAll(null, "Id desc", null, 0, 5);

        return new
        {
            Logins = logins.Select(e => new { e.CreateTime, e.UserName, e.Action, e.CreateIP }).ToArray(),
            Onlines = onlines.Select(e => new { e.Name, e.CreateTime, e.OAuthProvider }).ToArray(),
        };
    }
}

