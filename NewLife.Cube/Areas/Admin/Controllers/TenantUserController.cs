using System.ComponentModel;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>租户关系</summary>
[AdminArea]
[Menu(10, false, Icon = "UserFilled", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class TenantUserController : EntityController<TenantUser, TenantUserModel>
{
    static TenantUserController()
    {
        LogOnChange = true;
    }

    /// <summary>搜索数据集（租户模式强制当前租户）</summary>
    protected override IEnumerable<TenantUser> Search(Pager p)
    {
        var userId = p["userId"].ToInt(-1);
        var roleId = p["roleId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        var tenantId = TenantContext.CurrentId;
        tenantId = tenantId == 0 ? p["tenantId"].ToInt(-1) : tenantId;

        return TenantUser.Search(tenantId, userId, roleId, enable, start, end, p["q"], p);
    }

    /// <summary>验证数据：插入时盖章 TenantId</summary>
    protected override Boolean Valid(TenantUser entity, DataObjectMethodType type, Boolean post)
    {
        if (type == DataObjectMethodType.Insert)
        {
            if (entity.TenantId == 0) entity.TenantId = TenantContext.CurrentId;
            if (entity.UserId == 0) entity.UserId = ManageProvider.Provider.Current.ID;
        }

        return base.Valid(entity, type, post);
    }
}
