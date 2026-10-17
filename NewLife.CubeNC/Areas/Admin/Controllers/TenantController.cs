using System.ComponentModel;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>租户管理</summary>
[AdminArea]
[Menu(75, true, Icon = "fa-user-circle", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class TenantController : EntityController<Tenant, TenantModel>
{
    private readonly ITenantContext _tenantContext;

    static TenantController()
    {
        LogOnChange = true;

        //ListFields.RemoveField("Secret", "Logo", "AuthUrl", "AccessUrl", "UserUrl", "Remark");
        ListFields.RemoveField("Remark")
            .RemoveField("CreateUserId", "CreateTime", "CreateIP", "UpdateUserId", "UpdateTime", "UpdateIP");

        {
            // 成员管理友好界面入口；标准列表可通过管理页内链接访问
            var df = ListFields.AddListField("Users", null, "ManagerName");
            df.DisplayName = "成员";
            df.Url = "/Admin/TenantUser/Manage?tenantId={Id}";
        }

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

        {
            AddFormFields.GroupVisible = (entity, group) => (entity as Tenant).Id == 0 && group != "扩展";
        }
    }

    /// <summary>实例化</summary>
    /// <param name="tenantContext">租户上下文</param>
    public TenantController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
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

        if (_tenantContext.TenantId > 0) PageSetting.EnableAdd = false;

        var managerId = p["managerId"].ToInt(-1);
        //var roleIds = p["roleIds"].SplitAsInt();
        var enable = p["enable"]?.ToBoolean();
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();

        return Tenant.Search(null, managerId, enable, start, end, p["q"], p);
    }

    /// <summary>验证数据</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(Tenant entity, DataObjectMethodType type, Boolean post)
    {
        if (type == DataObjectMethodType.Insert && entity.ManagerId == 0)
        {
            // 当前用户可能为空（异常会话），避免直接解引用导致保存失败
            var current = ManageProvider.Provider?.Current;
            if (current != null) entity.ManagerId = current.ID;
        }

        return base.Valid(entity, type, post);
    }

    /// <summary></summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected override Int32 OnInsert(Tenant entity)
    {
        var result = base.OnInsert(entity);

        // 无有效管理员时不创建成员关系，避免 UserId=0 脏数据
        if (entity.ManagerId > 0) SyncManagerMembership(entity, null);

        return result;
    }

    /// <summary></summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected override Int32 OnUpdate(Tenant entity)
    {
        var oldTenantEntity = Tenant.FindById(entity.Id);
        // 旧记录缺失时仍允许更新租户本身，跳过成员同步
        if (oldTenantEntity != null) SyncManagerMembership(entity, oldTenantEntity);

        return base.OnUpdate(entity);
    }

    /// <summary>同步租户管理员对应的 TenantUser 成员关系。
    /// 变更管理员时禁用旧绑定（若不存在则跳过）；确保新管理员有启用的绑定。
    /// 历史数据常缺 TenantUser，直接解引用会触发 NRE（表现为「保存失败！Object reference...」）。</summary>
    /// <param name="entity">当前提交的租户</param>
    /// <param name="oldEntity">更新前的租户；新增时传 null</param>
    private static void SyncManagerMembership(Tenant entity, Tenant oldEntity)
    {
        // 管理员变更：禁用旧管理员绑定（历史库可能没有该行，必须判空）
        if (oldEntity != null
            && entity.ManagerId != oldEntity.ManagerId
            && oldEntity.ManagerId > 0)
        {
            var oldTu = TenantUser.FindByTenantIdAndUserId(oldEntity.Id, oldEntity.ManagerId);
            if (oldTu != null)
            {
                oldTu.Enable = false;
                oldTu.Save();
            }
        }

        if (entity.ManagerId <= 0) return;

        var tu = TenantUser.FindByTenantIdAndUserId(entity.Id, entity.ManagerId);
        tu ??= new TenantUser
        {
            TenantId = entity.Id,
            UserId = entity.ManagerId
        };

        // 新增时强制启用；更新时跟随租户启用状态
        tu.Enable = oldEntity == null || entity.Enable;
        tu.RoleIds = entity.RoleIds;
        tu.Save();
    }
}