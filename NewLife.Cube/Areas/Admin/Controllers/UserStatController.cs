using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode.Membership;
using static NewLife.Cube.Entity.UserStat;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>访问统计控制器</summary>
[Area("Admin")]
[Menu(0, false)]
public class UserStatController : ReadOnlyEntityController<UserStat>
{
    static UserStatController()
    {
        ListFields.RemoveField("ID", "CreateTime", "UpdateTime", "Remark");
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<UserStat> Search(Pager p)
    {
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        p.RetrieveState = true;

        return SearchByDate(start, end, p["Q"], p);
    }
}