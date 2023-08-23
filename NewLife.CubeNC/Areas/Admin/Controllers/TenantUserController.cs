using Microsoft.AspNetCore.Mvc;
using NewLife.Web;
using XCode.Membership;
using UserX = XCode.Membership.User;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>租户关系</summary>
[Area("Admin")]
[Menu(10, true, Icon = "fa-users")]
public class TenantUserController : EntityController<TenantUser>
{
    static TenantUserController()
    {
        LogOnChange = true;

        ListFields.RemoveField("ID", "Remark").RemoveField("CreateUserId", "CreateTime", "CreateIP", "UpdateUserId", "UpdateTime", "UpdateIP");

        {
            var df = AddFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            AddFormFields.RemoveField("RoleNames");
        }
        {
            var df = EditFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleNames");
        }
    }

    public TenantUserController()
    {
        var TenantId = TenantContext.Current.TenantId;
        var tenant = Tenant.FindById(TenantId);
        var RoleIds = tenant?.RoleIds.SplitAsInt(",");
        // 新增界面
        {
            // 角色组
            var df = AddFormFields.GetField("RoleIds");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => TenantId == 0 || (RoleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 角色
            var df = AddFormFields.GetField("RoleId");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => TenantId == 0 || (RoleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 用户
            var df = AddFormFields.GetField("UserId");
            var list = TenantUser.FindAllByTenantId(TenantId).Select(e => e.UserId);
            df.DataSource = entity => UserX.FindAllWithCache().Where(e => TenantId == 0 || !list.Any() || !list.Contains(e.ID)).OrderByDescending(e => e.ID).ToDictionary(e => e.ID, e => e.DisplayName);
        }

        // 编辑界面
        {
            // 角色组
            var df = EditFormFields.GetField("RoleIds");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => TenantId == 0 || (RoleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 角色
            var df = EditFormFields.GetField("RoleId");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => TenantId == 0 || (RoleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 用户
            var df = EditFormFields.GetField("UserId");
            var list = TenantUser.FindAllByTenantId(TenantId).Select(e => e.UserId);
            df.DataSource = entity => UserX.FindAllWithCache().Where(e => TenantId == 0 || !list.Any() || !list.Contains(e.ID)).OrderByDescending(e => e.ID).ToDictionary(e => e.ID, e => e.DisplayName);
        }
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<TenantUser> Search(Pager p)
    {
        //var tenantId = p["tenantId"].ToInt(-1);
        var userId = p["userId"].ToInt(-1);
        var roleId = p["roleId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        var tenantId = TenantContext.Current.TenantId;

        tenantId = tenantId == 0 ? p["tenantId"].ToInt(-1) : tenantId;

        return TenantUser.Search(tenantId, userId, roleId, enable, start, end, p["q"], p);
    }
}