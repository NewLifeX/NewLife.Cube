using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>用户在线控制器</summary>
[DataPermission(null, "UserID={#userId}")]
[Area("Admin")]
[Menu(0, false)]
public class UserOnlineController : EntityController<UserOnline>
{
    /// <summary>
    /// 实例化
    /// </summary>
    public UserOnlineController()
    {
        PageSetting.EnableAdd = false;

        ListFields.RemoveField("UserID", "SessionID", "Status", "LastError", "CreateIP", "CreateTime");

        ListFields.TraceUrl("TraceId");

        {
            var df = ListFields.GetField("Name") as ListField;
            df.Url = "/Admin/User/Edit?id={UserID}";
            df.Target = "_blank";
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
            DataObjectMethodType.Update or DataObjectMethodType.Insert => throw new Exception("不允许添加/修改记录"),
            _ => base.Valid(entity, type, post),
        };
    }
}