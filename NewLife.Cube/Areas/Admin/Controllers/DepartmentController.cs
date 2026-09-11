using System.ComponentModel;
using NewLife.Web;
using XCode.Membership;
using static XCode.Membership.Department;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>部门</summary>
[DataPermission(null, "ManagerID={#userId}")]
[DisplayName("部门")]
[AdminArea]
[Menu(95, true, Icon = "UserFilled")]
public class DepartmentController : EntityController<Department, DepartmentModel>
{
    static DepartmentController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveUpdateField();
        ListFields.RemoveCreateField();
        ListFields.RemoveRemarkField();

        // 管理者：后端 Search 不支持按管理者过滤（数字搜索实际无效），且用户量巨大无法全量字典，对齐 MVC 从搜索区移除
        SearchFields.RemoveField("ManagerId");

        // 父级：下拉选择。部门缓存作为数据源（对齐 MVC _SelectDepartment 部门选择），单选提交 parentId=ID。
        // label 用层级路径（如 总公司/行政部），避免同名部门混淆；不依赖 FullName 字段的数据质量
        {
            var df = SearchFields.GetField(_.ParentID);
            df.DataSource = _ =>
            {
                var deps = Department.FindAllWithCache().ToList();
                var map = deps.ToDictionary(e => e.ID, e => e);
                var dict = new Dictionary<Int32, String>();
                foreach (var e in deps)
                {
                    var parts = new List<String> { e.Name };
                    var p = e;
                    var guard = 0;
                    while (p.ParentID > 0 && map.TryGetValue(p.ParentID, out var pp) && pp.ID != p.ID && guard++ < 8)
                    {
                        parts.Insert(0, pp.Name);
                        p = pp;
                    }
                    dict[e.ID] = String.Join("/", parts);
                }
                return dict;
            };
        }
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Department> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var list = new List<Department>();
            var entity = Department.FindByID(id);
            // 分页查询由 WhereBuilder 按管理者过滤，本分支需自行判断，避免绕过数据权限
            if (entity != null && CanView(entity)) list.Add(entity);
            return list;
        }

        var parentId = p["parentId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();
        var visible = p["visible"]?.ToBoolean();

        return Department.Search(parentId, enable, visible, p["Q"], p);
    }

    /// <summary>查找单行数据，并判断数据权限</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected override Department FindData(Object key)
    {
        var entity = Find(key);

        // 不用 WhereBuilder.Eval：部门实现 ITenantScope，无租户上下文时求值恒为 false；
        // 可见性以管理者字段为准（与 [DataPermission] 的列表过滤一致）
        if (entity != null && !CanView(entity)) throw new InvalidOperationException($"非法访问数据[{key}]");

        return entity;
    }

    /// <summary>判断当前用户能否查看指定部门。系统角色不受限，普通用户仅限自己管理的部门</summary>
    /// <param name="entity">部门</param>
    /// <returns></returns>
    protected virtual Boolean CanView(Department entity)
    {
        var user = ManageProvider.User;
        if (user == null || user.Roles.Any(e => e.IsSystem)) return true;

        return entity.ManagerId == user.ID;
    }
}