using XCode;
using XCode.Membership;
using XLog = XCode.Membership.Log;
using static XCode.Membership.Log;

namespace NewLife.Cube.Widgets.System;

/// <summary>我的动态。当前用户最近7天登录/操作记录</summary>
[Widget("MyActivity", "我的动态", Icon = "fa-bell-o", Cols = 12, Sort = 130, Category = "个人")]
public class MyActivityWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var user = ManageProvider.User;
        var name = user?.Name;
        var now = DateTime.Now;
        // Log.Id 是雪花Id，带时间信息，用 Id 时间范围过滤
        var snow = XLog.Meta.Factory.Snow;

        // 雪花Id，按 Id 降序
        var list = XLog.FindAll(_.UserName == name & _.ID.Between(now.AddDays(-7), now, snow), "ID desc", null, 0, 10);

        return new
        {
            Items = list.Select(e => new { e.CreateTime, e.Action, e.Remark, e.CreateIP }).ToArray(),
        };
    }
}
