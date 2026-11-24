using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>租户管理</summary>
[AdminArea]
[Menu(75, true, Icon = "Avatar", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class TenantController : EntityController<Tenant, TenantModel>
{
    static TenantController()
    {
        LogOnChange = true;

        //ListFields.RemoveField("Secret", "Logo", "AuthUrl", "AccessUrl", "UserUrl", "Remark");
    }

    /// <summary>删除前校验：仍有成员的租户不允许直接删除</summary>
    /// <remarks>TenantUser 没有级联删除，直接删租户会留下指向已删除租户的孤儿关系，影响后续成员查询与租户隔离判断</remarks>
    /// <param name="entity">租户</param>
    /// <returns>受影响行数</returns>
    protected override Int32 OnDelete(Tenant entity)
    {
        var count = TenantUser.FindAllByTenantId(entity.Id).Count;
        if (count > 0) throw new XException(L.T("Tenant [{0}] still has {1} member(s); remove them before deleting.", entity.Name, count));

        return base.OnDelete(entity);
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Tenant> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var entity = Tenant.FindById(id);
            if (entity != null) return new[] { entity };
        }

        var managerId = p["managerId"].ToInt(-1);
        //var roleIds = p["roleIds"].SplitAsInt();
        var enable = p["enable"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        return Tenant.Search(null, managerId, enable, start, end, p["q"], p);
    }
}