using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>角色控制器</summary>
[DisplayName("角色")]
[Description("系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。")]
[AdminArea]
[Menu(90, true, Icon = "CirclePlus")]
public class RoleController : EntityController<Role, RoleModel>
{
    static RoleController()
    {
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6", "UpdateUserID", "UpdateIP", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.AddListField("Remark", "UpdateUser");
        }
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Role> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var list = new List<Role>();
            var entity = Role.FindByID(id);
            if (entity != null) list.Add(entity);
            return list;
        }

        return Role.Search(p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p["Q"], p);
    }

    /// <summary>验证实体对象</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(Role entity, DataObjectMethodType type, Boolean post)
    {
        // 租户管理员创建角色时，强制填充角色所属租户，确保角色归属对应客户
        if (post && type == DataObjectMethodType.Insert && entity.TenantId == 0)
            entity.TenantId = TenantContext.CurrentId;

        var rs = base.Valid(entity, type, post);

        if (post && type is DataObjectMethodType.Insert or DataObjectMethodType.Update)
        {
            // 租户管理员（在租户上下文中操作、且本身不是系统管理员）只能授予自己拥有的权限，
            // 提交时对越权勾选的权限进行拦截，避免越界授权
            var current = ManageProvider.User as IUser;
            var restricted = TenantContext.CurrentId > 0 && (current == null || !current.Roles.Any(e => e.IsSystem));

            // JSON API 请求：权限字符串由前端构建并通过模型绑定写入 entity.Permission
            // 此处需解析字符串并通过 entity.Set() 同步更新 Permissions 字典，确保保存时不会丢失
            if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
            {
                // 解析权限字符串并通过 entity.Set() 同步更新 Permissions 字典，确保保存时不会丢失；
                // 受限模式（租户管理员）只授予自己拥有的权限位，保留系统管理员授予的其它权限
                RolePermissionHelper.Apply(entity, entity.Permission, current, restricted);

                // 初始化权限：系统角色（管理员）新增时默认添加全部权限，非系统角色默认不添加任何权限
                if (entity.IsSystem && type == DataObjectMethodType.Insert && !restricted && entity.Permission.IsNullOrEmpty())
                {
                    foreach (var item in XCode.Membership.Menu.Root.AllChilds)
                    {
                        entity.Set(item.ID, PermissionFlags.All);
                    }
                }

                // JSON 模式仍需清空缓存，确保后续读取拿到最新数据
                Role.Meta.Session.ClearCache($"{type}-{entity}", true);
                return rs;
            }

            // MVC 表单提交：通过 p{id} / pf{id}_{flag} 字段处理权限
            var menus = XCode.Membership.Menu.Root.AllChilds;
            var dels = new List<Int32>();
            // 遍历所有权限资源
            foreach (var item in menus)
            {
                // 是否授权该项
                var has = GetBool("p" + item.ID);
                if (!has)
                {
                    // 取消整项授权
                    if (!restricted)
                        dels.Add(item.ID);
                    else
                        // 受限管理员只回收自己拥有的权限子项，保留系统管理员授予的其它权限
                        foreach (var pf in item.Permissions)
                        {
                            var flag = (PermissionFlags)pf.Key;
                            if (current != null && current.Has(item, flag)) entity.Reset(item.ID, flag);
                        }
                }
                else
                {
                    // 遍历所有权限子项
                    var any = false;
                    foreach (var pf in item.Permissions)
                    {
                        var flag = (PermissionFlags)pf.Key;

                        // 防止越界授权：受限管理员只能管理自己拥有的权限子项，其它权限保持不变
                        if (restricted && (current == null || !current.Has(item, flag))) continue;

                        var has2 = GetBool("pf" + item.ID + "_" + pf.Key);

                        if (has2)
                            entity.Set(item.ID, flag);
                        else
                            entity.Reset(item.ID, flag);

                        any |= has2;
                    }
                    // 首次授权：系统角色（管理员）默认添加全部权限；非系统角色默认不添加任何权限
                    if (!any & !entity.Has(item.ID) & entity.IsSystem & !restricted) entity.Set(item.ID);
                }
            }
            // 删除已经被放弃权限的项
            foreach (var item in dels)
            {
                if (entity.Has(item)) entity.Permissions.Remove(item);
            }
        }

        // 清空缓存
        if (post) Role.Meta.Session.ClearCache($"{type}-{entity}", true);

        return rs;
    }

    private Boolean GetBool(String name)
    {
        var v = GetRequest(name);
        if (v.IsNullOrEmpty()) return false;

        v = v.Split(",")[0];

        return !v.EqualIgnoreCase("true", "false") ? throw new XException("非法布尔值Request[{0}]={1}", name, v) : v.ToBoolean();
    }

    #region 权限配置

    /// <summary>获取角色权限树。返回菜单树 + 当前角色已授权权限位，供前端弹窗勾选</summary>
    /// <param name="roleId">角色编号</param>
    /// <returns>菜单树，每项含可用权限位（value/name）与已授权位 granted</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public Object PermissionTree(Int32 roleId)
    {
        var role = Role.FindByID(roleId) ?? throw new XException("角色[{0}]不存在", roleId);
        var current = ManageProvider.User as IUser;
        var restricted = TenantContext.CurrentId > 0 && (current == null || !current.Roles.Any(e => e.IsSystem));

        var menus = XCode.Membership.Menu.Root.AllChilds;

        return new
        {
            role = new { role.ID, role.Name },
            tree = BuildPermissionTree(menus, 0, role, current, restricted),
        };
    }

    /// <summary>保存角色权限。权限字符串格式：资源ID#权限位，逗号分隔（如 1#3,2#8）</summary>
    /// <param name="model">角色权限保存模型</param>
    /// <returns>保存结果</returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [HttpPost]
    public Object SavePermission(RolePermissionModel model)
    {
        var role = Role.FindByID(model.RoleId) ?? throw new XException("角色[{0}]不存在", model.RoleId);
        var current = ManageProvider.User as IUser;
        var restricted = TenantContext.CurrentId > 0 && (current == null || !current.Roles.Any(e => e.IsSystem));

        // 受限模式（租户管理员）只授予自己拥有的权限位，保留系统管理员授予的其它权限
        RolePermissionHelper.Apply(role, model.Permission, current, restricted);

        role.Update();

        // 清空缓存，确保后续读取拿到最新数据
        Role.Meta.Session.ClearCache($"SavePermission-{role}", true);

        return new { success = true, roleId = model.RoleId };
    }

    /// <summary>递归构建权限树</summary>
    /// <param name="menus">全部菜单（平铺）</param>
    /// <param name="parentId">父级编号</param>
    /// <param name="role">当前角色，用于读取已授权位</param>
    /// <param name="current">当前登录用户</param>
    /// <param name="restricted">是否受限模式（租户管理员）</param>
    /// <returns>菜单树</returns>
    private static List<Object> BuildPermissionTree(IList<Menu> menus, Int32 parentId, Role role, IUser? current, Boolean restricted)
    {
        var list = new List<Object>();
        foreach (var m in menus.Where(e => e.ParentID == parentId).OrderBy(e => e.Sort))
        {
            // 受限模式只返回自己拥有的权限位，避免越权勾选
            var flags = m.Permissions
                .Where(e => !restricted || current == null || current.Has(m, (PermissionFlags)e.Key))
                .Select(e => new { value = e.Key, name = e.Value })
                .ToList();

            list.Add(new
            {
                id = m.ID,
                name = m.Name,
                displayName = m.DisplayName,
                flags,
                granted = role.Get(m.ID),
                children = BuildPermissionTree(menus, m.ID, role, current, restricted),
            });
        }
        return list;
    }

    #endregion
}