using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Web;
using XCode.Membership;
using UserX = XCode.Membership.User;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>租户关系</summary>
[AdminArea]
[Menu(10, false, Icon = "fa-users", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class TenantUserController : EntityController<TenantUser, TenantUserModel>
{
    private readonly ITenantContext _tenantContext;

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
        {
            // 用户搜索式选择器，替换大下拉框，方便按编码/名称/昵称/手机号搜索
            var ff = AddFormFields.GetField("UserId") as FormField;
            if (ff != null) ff.GroupView = "_Form_UserId";
            var ef = EditFormFields.GetField("UserId") as FormField;
            if (ef != null) ef.GroupView = "_Form_UserId";
        }
    }

    /// <summary>实例化</summary>
    /// <param name="tenantContext">租户上下文</param>
    public TenantUserController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
        var tenantId = _tenantContext.TenantId;
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

        if (_tenantContext.TenantId > 0)
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

        var tenantId = _tenantContext.TenantId;

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
            if (entity.TenantId == 0) entity.TenantId = _tenantContext.TenantId;
            if (entity.UserId == 0) entity.UserId = ManageProvider.Provider.Current.ID;

            // 同一租户下不允许重复绑定同一用户
            if (post && entity.TenantId > 0 && entity.UserId > 0)
            {
                var tu = TenantUser.FindByTenantIdAndUserId(entity.TenantId, entity.UserId);
                if (tu != null) throw new Exception("该用户已存在于当前租户！");
            }
        }

        return base.Valid(entity, type, post);
    }

    #region 成员管理（友好界面）
    /// <summary>租户成员管理页。友好界面，方便把用户添加到租户下</summary>
    /// <param name="tenantId">租户编号。租户管理员上下文时忽略，使用当前租户</param>
    [EntityAuthorize(PermissionFlags.Detail)]
    public ActionResult Manage(Int32 tenantId = 0)
    {
        var tid = GetEffectiveTenantId(tenantId);
        var tenant = Tenant.FindById(tid);
        if (tenant == null) throw new InvalidOperationException($"租户[{tid}]不存在或已删除");

        var p = new Pager();
        var list = TenantUser.Search(tid, 0, 0, null, DateTime.MinValue, DateTime.MinValue, p["q"], p);

        ViewBag.Tenant = tenant;
        ViewBag.TenantId = tid;

        return View("Manage", list);
    }

    /// <summary>添加成员</summary>
    /// <param name="tenantId">租户编号</param>
    /// <param name="userId">用户编号</param>
    /// <param name="roleIds">角色组</param>
    [HttpPost]
    [EntityAuthorize(PermissionFlags.Insert)]
    public ActionResult AddMember(Int32 tenantId, Int32 userId, Int32[] roleIds)
    {
        var tid = GetEffectiveTenantId(tenantId);
        if (tid <= 0) throw new InvalidOperationException("请选择租户");
        if (userId <= 0) throw new InvalidOperationException("请选择用户");

        var tu = TenantUser.FindByTenantIdAndUserId(tid, userId);
        if (tu != null) throw new InvalidOperationException("该用户已存在于当前租户！");

        tu = new TenantUser
        {
            TenantId = tid,
            UserId = userId,
            RoleIds = roleIds?.Join(","),
            Enable = true,
        };
        tu.Insert();

        LogProvider.Provider.WriteLog("租户", "添加成员", true, $"添加用户[{userId}]到租户[{tid}]", userId, userId + "", ip: HttpContext.GetUserHost());

        return Json(0, "添加成功", new { tu.Id, UserName = UserX.FindByID(userId)?.ToString() });
    }

    /// <summary>移除成员</summary>
    /// <param name="id">成员编号</param>
    [HttpPost]
    [EntityAuthorize(PermissionFlags.Delete)]
    public ActionResult RemoveMember(Int32 id)
    {
        var tu = TenantUser.FindById(id);
        if (tu == null) return Json(0, "成员不存在");

        var tid = GetEffectiveTenantId(tu.TenantId);
        var tenant = Tenant.FindById(tid);
        if (tenant != null && tenant.ManagerId == tu.UserId) throw new InvalidOperationException("不能移除租户管理员");
        if (tu.UserId == ManageProvider.Provider.Current.ID) throw new InvalidOperationException("不能移除自己");

        tu.Delete();

        LogProvider.Provider.WriteLog("租户", "移除成员", true, $"从租户[{tid}]移除用户[{tu.UserId}]", tu.UserId, tu.UserId + "", ip: HttpContext.GetUserHost());

        return Json(0, "移除成功");
    }

    /// <summary>启用或禁用成员</summary>
    /// <param name="id">成员编号</param>
    /// <param name="enable">是否启用</param>
    [HttpPost]
    [EntityAuthorize(PermissionFlags.Update)]
    public ActionResult ToggleMember(Int32 id, Boolean enable)
    {
        var tu = TenantUser.FindById(id);
        if (tu == null) return Json(0, "成员不存在");

        var tid = GetEffectiveTenantId(tu.TenantId);
        var tenant = Tenant.FindById(tid);
        if (tenant != null && tenant.ManagerId == tu.UserId && !enable) throw new InvalidOperationException("不能禁用租户管理员");

        tu.Enable = enable;
        tu.Update();

        LogProvider.Provider.WriteLog("租户", enable ? "启用成员" : "禁用成员", true, $"租户[{tid}]用户[{tu.UserId}]", tu.UserId, tu.UserId + "", ip: HttpContext.GetUserHost());

        return Json(0, enable ? "已启用" : "已禁用");
    }

    /// <summary>解析生效租户。租户管理员上下文忽略参数，平台管理员使用参数；越权操作其他租户直接拒绝</summary>
    /// <param name="tenantId">请求租户编号</param>
    /// <returns>生效租户编号</returns>
    private Int32 GetEffectiveTenantId(Int32 tenantId)
    {
        var ctx = _tenantContext.TenantId;
        if (ctx > 0)
        {
            if (tenantId > 0 && tenantId != ctx) throw new InvalidOperationException("无权操作其他租户成员");
            return ctx;
        }

        return tenantId;
    }
    #endregion
}