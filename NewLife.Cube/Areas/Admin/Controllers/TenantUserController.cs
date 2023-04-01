using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>租户关系</summary>
[AdminArea]
[Menu(10, false)]
public class TenantUserController : EntityController<TenantUser>
{
    static TenantUserController()
    {
        LogOnChange = true;

        //ListFields.RemoveField("Secret", "Logo", "AuthUrl", "AccessUrl", "UserUrl", "Remark");
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<TenantUser> Search(Pager p)
    {
        var tenantId = p["tenantId"].ToInt(-1);
        var userId = p["userId"].ToInt(-1);
        var roleId = p["roleId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        return TenantUser.Search(tenantId, userId, roleId, enable, start, end, p["q"], p);
    }
}