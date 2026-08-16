using System.ComponentModel;
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
    }

    /// <summary>搜索数据集</summary>
    protected override IEnumerable<Tenant> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var entity = Tenant.FindById(id);
            if (entity != null) return new[] { entity };
        }

        if (TenantContext.CurrentId > 0) PageSetting.EnableAdd = false;

        var managerId = p["managerId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        return Tenant.Search(null, managerId, enable, start, end, p["q"], p);
    }

    /// <summary>验证数据</summary>
    protected override Boolean Valid(Tenant entity, DataObjectMethodType type, Boolean post)
    {
        if (type == DataObjectMethodType.Insert)
        {
            if (entity.ManagerId == 0) entity.ManagerId = ManageProvider.Provider.Current.ID;
        }

        return base.Valid(entity, type, post);
    }

    /// <summary>插入后确保管理员存在 Enable 的 TenantUser</summary>
    protected override Int32 OnInsert(Tenant entity)
    {
        var result = base.OnInsert(entity);

        var tuEntity = TenantUser.FindByTenantIdAndUserId(entity.Id, entity.ManagerId);
        tuEntity ??= new TenantUser
        {
            TenantId = entity.Id,
            UserId = entity.ManagerId
        };

        tuEntity.Enable = true;
        tuEntity.RoleIds = entity.RoleIds;
        tuEntity.Save();

        return result;
    }

    /// <summary>更新时同步 Manager 的 TenantUser</summary>
    protected override Int32 OnUpdate(Tenant entity)
    {
        var oldTenantEntity = Tenant.FindById(entity.Id);
        var tuEntity = TenantUser.FindByTenantIdAndUserId(oldTenantEntity.Id, oldTenantEntity.ManagerId);

        if (entity.ManagerId != oldTenantEntity.ManagerId && tuEntity != null)
        {
            tuEntity.Enable = false;
            tuEntity.Save();
        }

        var newTuEntity = TenantUser.FindByTenantIdAndUserId(entity.Id, entity.ManagerId);
        newTuEntity ??= new TenantUser
        {
            TenantId = entity.Id,
            UserId = entity.ManagerId
        };

        newTuEntity.Enable = entity.Enable;
        newTuEntity.RoleIds = entity.RoleIds;
        newTuEntity.Save();

        return base.OnUpdate(entity);
    }
}
