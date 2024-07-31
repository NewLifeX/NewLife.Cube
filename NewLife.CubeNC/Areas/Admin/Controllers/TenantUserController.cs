using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode.Membership;
using UserX = XCode.Membership.User;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>租户关系</summary>
[AdminArea]
[Menu(10, false, Icon = "fa-users", Mode = MenuModes.Admin | MenuModes.Tenant)]
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
            var df = AddFormFields.AddDataField("RoleId", "RoleName");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            AddFormFields.RemoveField("RoleName");
        }
        {
            var df = AddFormFields.AddDataField("TenantId", "TenantName");
            df.DataSource = entity => Tenant.FindAllWithCache().OrderByDescending(e => e.Id).ToDictionary(e => e.Id, e => e.Name);
            AddFormFields.RemoveField("TenantName");
        }
        {
            var df = EditFormFields.AddDataField("RoleIds", "RoleNames");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleNames");
        }
        {
            var df = EditFormFields.AddDataField("RoleId", "RoleName");
            df.DataSource = entity => Role.FindAllWithCache().OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
            EditFormFields.RemoveField("RoleName");
        }
        {
            var df = EditFormFields.AddDataField("TenantId", "TenantName");
            df.DataSource = entity => Tenant.FindAllWithCache().OrderByDescending(e => e.Id).ToDictionary(e => e.Id, e => e.Name);
            EditFormFields.RemoveField("TenantName");
        }
    }

    /// <summary>实例化</summary>
    public TenantUserController()
    {
        var tenantId = TenantContext.CurrentId;
        var tenant = Tenant.FindById(tenantId);
        var roleIds = tenant?.RoleIds.SplitAsInt(",");
        // 新增界面
        {
            // 角色组
            var df = AddFormFields.GetField("RoleIds");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => tenantId == 0 || (roleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 角色
            var df = AddFormFields.GetField("RoleId");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => tenantId == 0 || (roleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 用户
            var df = AddFormFields.GetField("UserId");
            var list = TenantUser.FindAllByTenantId(tenantId).Select(e => e.UserId).ToList();
            if (tenantId > 0) list.Add(tenant.ManagerId);
            df.DataSource = entity => UserX.FindAllWithCache().Where(e => !list.Any(x => x == e.ID)).OrderByDescending(e => e.ID).ToDictionary(e => e.ID, e => e.DisplayName);
        }
        {
            // 租户
            var df = AddFormFields.GetField("TenantId");
            df.DataSource = entity => Tenant.FindAllWithCache().Where(e => tenantId == 0 || e.Id == tenantId).OrderByDescending(e => e.Id).ToDictionary(e => e.Id, e => e.Name);
        }

        // 编辑界面
        {
            // 角色组
            var df = EditFormFields.GetField("RoleIds");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => tenantId == 0 || (roleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 角色
            var df = EditFormFields.GetField("RoleId");
            df.DataSource = entity => Role.FindAllWithCache().Where(e => tenantId == 0 || (roleIds?.Contains(e.ID) ?? false)).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            // 用户
            var df = AddFormFields.GetField("UserId");
            var list = TenantUser.FindAllByTenantId(tenantId).Select(e => e.UserId).ToList();
            if (tenantId > 0) list.Add(tenant.ManagerId);
            df.DataSource = entity => UserX.FindAllWithCache().Where(e => !list.Any(x => x == e.ID)).OrderByDescending(e => e.ID).ToDictionary(e => e.ID, e => e.DisplayName);
        }
        {
            // 租户
            var df = AddFormFields.GetField("TenantId");
            df.DataSource = entity => Tenant.FindAllWithCache().Where(e => tenantId == 0 || e.Id == tenantId).OrderByDescending(e => e.Id).ToDictionary(e => e.Id, e => e.Name);
        }
    }

    /// <summary>获取字段信息</summary>
    /// <param name="kind"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    protected override FieldCollection OnGetFields(ViewKinds kind, Object model)
    {
        var rs = base.OnGetFields(kind, model);

        if (TenantContext.CurrentId > 0)
        {
            switch (kind)
            {
                case ViewKinds.Detail:
                case ViewKinds.AddForm:
                case ViewKinds.EditForm:
                    rs.RemoveField("TenantId", "TenantName");
                    break;
                default:
                    break;
            }
        }

        return rs;
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

        var tenantId = TenantContext.CurrentId;

        tenantId = tenantId == 0 ? p["tenantId"].ToInt(-1) : tenantId;

        return TenantUser.Search(tenantId, userId, roleId, enable, start, end, p["q"], p);
    }

    /// <summary>验证数据</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
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