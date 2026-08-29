using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube;
using XCode.Membership;

namespace CubeSSO.Areas.Demo.Controllers;

/// <summary>海量数据免查总数分页演示</summary>
/// <remarks>
/// 通过 PageSetting.EnableTotalCount = false 跳过SelectCount总数查询，
/// 列表页不显示总条数与页码，仅提供上一页/下一页翻页。
/// </remarks>
[DisplayName("免查总数分页")]
[Description("演示海量数据列表跳过SelectCount总数查询，仅显示上一页/下一页")]
[DemoArea]
[Menu(0, false, Icon = "fa-list")]
public class UserListController : ReadOnlyEntityController<User>
{
    static UserListController()
    {
        // 演示列表不展示敏感字段
        ListFields.RemoveField("Password,Secret,Mail,Mobile,Code,Token,Question,Answer,OnlineTime,Ex1,Ex2,Ex3,Ex4,Ex5,Ex6");
        ListFields.RemoveUpdateField();
        ListFields.RemoveRemarkField();
    }

    /// <summary>执行前。演示海量数据列表跳过总数查询</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        PageSetting.EnableTotalCount = false;

        base.OnActionExecuting(filterContext);
    }
}
