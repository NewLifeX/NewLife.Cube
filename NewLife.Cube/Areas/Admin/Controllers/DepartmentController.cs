using System.ComponentModel;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>部门</summary>
[DataPermission(null, "ManagerID={#userId}")]
[DisplayName("部门")]
[AdminArea]
[Menu(95, true, Icon = "fa-users")]
public class DepartmentController : EntityController<Department, DepartmentModel>
{
    static DepartmentController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveUpdateField();
        ListFields.RemoveCreateField();
        ListFields.RemoveRemarkField();
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
            if (entity != null) list.Add(entity);
            return list;
        }

        var parentId = p["parentId"].ToInt(-1);
        var enable = p["enable"]?.ToBoolean();
        var visible = p["visible"]?.ToBoolean();

        return Department.Search(parentId, enable, visible, p["Q"], p);
    }
}