using System;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>租户管理员关系齐平（供控制器与单测共用）</summary>
public static class TenantManagerHelper
{
    /// <summary>确保租户 Manager 存在 Enable 的 TenantUser，并同步 RoleIds</summary>
    public static TenantUser EnsureManagerTenantUser(Tenant entity, Boolean enable)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var tuEntity = TenantUser.FindByTenantIdAndUserId(entity.Id, entity.ManagerId);
        tuEntity ??= new TenantUser
        {
            TenantId = entity.Id,
            UserId = entity.ManagerId
        };

        tuEntity.Enable = enable;
        tuEntity.RoleIds = entity.RoleIds;
        tuEntity.Save();
        return tuEntity;
    }
}
