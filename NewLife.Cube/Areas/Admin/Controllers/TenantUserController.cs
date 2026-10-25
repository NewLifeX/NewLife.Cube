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

        // 列表精简：主键与审计字段不上列表（对齐 MVC）
        ListFields.RemoveField("ID", "Remark").RemoveField("CreateUserId", "CreateTime", "CreateIP", "UpdateUserId", "UpdateTime", "UpdateIP");

        // 角色分配：RoleIds 多选 + RoleId 主角色，数据源为角色缓存（对齐 MVC）
        {
            var df = AddFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            AddFormFields.RemoveField("RoleNames");

            df = EditFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleNames");
        }
        {
            var df = AddFormFields.AddDataField("RoleId", "RoleName");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            AddFormFields.RemoveField("RoleName");

            df = EditFormFields.AddDataField("RoleId", "RoleName");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleName");
        }
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