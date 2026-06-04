using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.AI;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>通知记录</summary>
/// <remarks>实例化</remarks>
/// <param name="ai"></param>
[DataPermission(null, "UserId={#userId}")]
[DisplayName("通知记录")]
[AdminArea]
[Menu(0, false)]
public class NotificationRecordController(IAIService ai) : ReadOnlyEntityController<NotificationRecord>
{
    static NotificationRecordController()
    {
        ListFields.RemoveField("Id");
        ListFields.RemoveUpdateField();

        ListFields.TraceUrl("TraceId");
        //{
        //    var df = ListFields.GetField("TraceId") as ListField;
        //    df.DisplayName = "跟踪";
        //    df.Url = StarHelper.BuildUrl("{TraceId}");
        //    df.DataVisible = (e, f) => !(e as NotificationRecord).TraceId.IsNullOrEmpty();
        //}
    }

    /// <summary>已重载。</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        base.OnActionExecuting(filterContext);

        // 指定了用户
        var userid = GetRequest("userId").ToInt(-1);
        if (userid > 0)
        {
            PageSetting.NavView = "_User_Nav";
            PageSetting.EnableNavbar = false;
        }
    }

    /// <summary>搜索</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<NotificationRecord> Search(Pager p)
    {
        var tenantId = p["tenantId"].ToInt(-1);
        var channel = p["channel"];
        var userId = p["userId"].ToInt(-1);
        var read = p["read"]?.ToBoolean();
        var success = p["success"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        var key = p["Q"];

        if (p.Sort.IsNullOrEmpty()) p.Sort = NotificationRecord._.Id.Desc();

        return NotificationRecord.Search(tenantId, channel, userId, null, read, success, start, end, key, p);
    }

    #region AI 润色
    /// <summary>AI 润色通知内容。将原始通知文案改写为目标风格</summary>
    /// <param name="title">通知标题</param>
    /// <param name="content">原始内容</param>
    /// <param name="style">目标风格（正式/友好/简洁）</param>
    /// <returns></returns>
    [DisplayName("AI 润色")]
    [HttpPost]
    public async Task<ActionResult> AiPolish(String title, String content, String style)
    {
        var set = CubeSetting.Current;
        if (!set.AISwitch) return Json(500, null, "AI 未启用，请在系统设置中开启");

        if (title.IsNullOrEmpty() && content.IsNullOrEmpty()) return Json(500, null, "请填写通知标题或内容");

        if (style.IsNullOrEmpty()) style = "正式";

        var result = await ai.PolishNotificationAsync(title, content, style);

        return Json(0, null, new { result });
    }
    #endregion
}