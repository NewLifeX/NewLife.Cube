using System.ComponentModel;
using NewLife.Cube.Models;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>部门</summary>
[DataPermission(null, "ManagerID={#userId}")]
[DisplayName("部门")]
[AdminArea]
[Menu(95, true, Icon = "fa-users", Mode = MenuModes.Admin | MenuModes.Tenant)]
public class DepartmentController : EntityController<Department, DepartmentModel>
{
    static DepartmentController()
    {
        LogOnChange = true;

        ListFields.RemoveField("Id", "TenantId", "Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveUpdateField();
        ListFields.RemoveCreateField();
        ListFields.RemoveRemarkField();

        //// 创建表单不需要创建更新信息字段
        //AddFormFields.RemoveCreateField();
        //AddFormFields.RemoveUpdateField();
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

    /// <summary>验证数据</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(Department entity, DataObjectMethodType type, Boolean post)
    {
        if (/*!post &&*/ type == DataObjectMethodType.Insert)
        {
            if (entity.TenantId == 0) entity.TenantId = TenantContext.CurrentId;
            if (entity.ManagerId == 0) entity.ManagerId = ManageProvider.Provider.Current.ID;
        }

        return base.Valid(entity, type, post);
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
        var olds = Department.FindAll();
        // 重置主键，避免重复
        foreach (var item in list)
        {
            if (item is Department dep) dep.ID = 0;
        }

        static Boolean match(IEntity e, IModel m)
        {
            var de = (Department)e;
            var dm = (Department)m;
            return de.TenantId == dm.TenantId && de.ParentID == dm.ParentID && de.Name.EqualIgnoreCase(dm.Name);
        }

        return factory.Merge(list, olds.Cast<IEntity>().ToList(), context.Fields, match);
    }
}