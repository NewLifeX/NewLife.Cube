using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Models;
using NewLife.Data;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>角色控制器</summary>
[DisplayName("角色")]
[Description("系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。")]
[AdminArea]
[Menu(90, true, Icon = "fa-user-plus")]
public class RoleController(ITenantContext tenantContext) : EntityController<Role, RoleModel>
{
    static RoleController()
    {
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6", "UpdateUserID", "UpdateIP", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.AddListField("Remark", "UpdateUser");
        }

        {
            var df = AddFormFields.GetField("DataDepartmentIds");
            df.DataSource = entity => Department.FindAllWithCache().Where(x => x.Enable).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
        {
            var df = EditFormFields.GetField("DataDepartmentIds");
            df.DataSource = entity => Department.FindAllWithCache().Where(x => x.Enable).OrderByDescending(e => e.Sort).ToDictionary(e => e.ID, e => e.Name);
        }
    }

    /// <summary>动作执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var bs = this.Bootstrap();
        bs.MaxColumn = 1;

        base.OnActionExecuting(filterContext);
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

        //return Role.Search(p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p["Q"], p);
        return base.Search(p);
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
            entity.TenantId = tenantContext.TenantId;

        var rs = base.Valid(entity, type, post);

        if (post && type is DataObjectMethodType.Insert or DataObjectMethodType.Update)
        {
            // JSON API 请求：权限字符串由前端构建并通过模型绑定写入 entity.Permission
            // 此处需解析字符串并通过 entity.Set() 同步更新 Permissions 字典，确保保存时不会丢失
            if (Request.ContentType != null && Request.ContentType.Contains("application/json"))
            {
                RolePermissionHelper.Apply(entity, entity.Permission);

                // 初始化权限：系统角色（管理员）新增时默认添加全部权限，非系统角色默认不添加任何权限
                if (entity.IsSystem && type == DataObjectMethodType.Insert && entity.Permission.IsNullOrEmpty())
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
            // 保存权限项
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
                    // 如果原来没有权限，这是首次授权，且右边没有勾选任何子项，则授权全部
                    if (!any & !entity.Has(item.ID))
                    {
                        if (!restricted)
                            entity.Set(item.ID);
                        else
                            // 受限管理员默认只授予自己拥有的权限子项，杜绝借助默认全量越界
                            foreach (var pf in item.Permissions)
                            {
                                var flag = (PermissionFlags)pf.Key;
                                if (current != null && current.Has(item, flag)) entity.Set(item.ID, flag);
                            }
                    }
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

        if (!v.EqualIgnoreCase("true", "false")) throw new XException("非法布尔值Request[{0}]={1}", name, v);

        return v.ToBoolean();
    }

    /// <summary>合并导入。查出表中已有数据匹配，能匹配的更新，无法匹配的批量插入</summary>
    /// <param name="factory">实体工厂</param>
    /// <param name="list">新数据列表</param>
    /// <param name="context">导入上下文（含表头与字段）</param>
    /// <returns>受影响行数</returns>
    protected override Int32 OnMerge(IEntityFactory factory, IList<IEntity> list, ImportContext context)
    {
        if (list == null || list.Count == 0) return 0;

        // 查询已有数据
        var olds = Role.FindAll();
        // 重置主键，避免重复
        foreach (var item in list)
        {
            if (item is Role role) role.ID = 0;
        }

        static Boolean match(IEntity e, IModel m)
        {
            var de = (Role)e;
            var dm = (Role)m;
            return de.TenantId == dm.TenantId && de.Name.EqualIgnoreCase(dm.Name);
        }

        return factory.Merge(list, olds.Cast<IEntity>().ToList(), context.Fields, match);
    }
}