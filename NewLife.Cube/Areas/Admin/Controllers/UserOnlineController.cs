using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>用户在线控制器</summary>
[DataPermission(null, "UserID={#userId}")]
[AdminArea]
[Menu(0, false)]
public class UserOnlineController : EntityController<UserOnline, UserOnlineModel>
{
    /// <summary>
    /// 实例化
    /// </summary>
    public UserOnlineController()
    {
        PageSetting.EnableAdd = false;

        ListFields.RemoveField("UserID", "SessionID", "Status", "LastError", "CreateIP", "CreateTime");

        //ListFields.TraceUrl("TraceId");

        {
            var df = ListFields.GetField("Name") as ListField;
            //df.DisplayName = "跟踪";
            df.Url = "/Admin/User?id={UserID}";
            df.DataVisible = e => (e as UserOnline).UserID > 0;
        }
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<UserOnline> Search(Pager p)
    {
        var userid = p["UserID"].ToInt(-1);
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        // 强制当前用户
        if (userid < 0)
        {
            var user = ManageProvider.User;
            if (!user.Roles.Any(e => e.IsSystem)) userid = user.ID;
        }

        return UserOnline.Search(userid, null, start, end, p["Q"], p);
    }

    /// <summary>验证数据</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    protected override Boolean Valid(UserOnline entity, DataObjectMethodType type, Boolean post)
    {
        if (!post) return base.Valid(entity, type, post);

        return type switch
        {
            DataObjectMethodType.Update or DataObjectMethodType.Insert => throw new Exception(L.T("Adding or modifying records is not allowed.")),
            _ => base.Valid(entity, type, post),
        };
    }

    #region 强制下线
    /// <summary>强制指定用户下线。吊销该用户所有令牌并清除其会话，保留在线记录用于审计</summary>
    /// <param name="id">在线记录编号</param>
    /// <returns></returns>
    [DisplayName("强制下线")]
    [EntityAuthorize(PermissionFlags.Delete)]
    [HttpPost]
    public ActionResult Kick(Int32 id)
    {
        var online = UserOnline.FindByID(id);
        if (online == null) return Json(1, L.T("The online record does not exist."));

        // 1. 吊销该用户所有令牌（API/JWT 即时失效）
        var count = UserToken.RevokeByUser(online.UserID);

        // 2. 清除 Session 会话（MVC 用户即时下线）
        if (!online.SessionID.IsNullOrEmpty())
            SessionProvider.Instance.RemoveSession(online.SessionID);

        // 3. 标记记录为已强制下线，不删除（保留审计数据）
        online.Status = L.T("Forced offline");
        online.SaveAsync(3_000);

        // 4. 审计日志
        LogProvider.Provider.WriteLog(L.T("User online"), L.T("Force offline"), true,
            L.T("User [{0}] was forced offline and {1} tokens were revoked.", online.Name, count), online.UserID, online.Name);

        return Json(0, L.T("Forced offline; revoked {0} tokens.", count));
    }
    #endregion
}