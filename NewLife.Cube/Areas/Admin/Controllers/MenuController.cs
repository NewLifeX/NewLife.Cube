using System.ComponentModel;
using NewLife.Cube.ViewModels;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>菜单控制器</summary>
[DisplayName("菜单")]
[Description("系统操作菜单以及功能目录树。支持排序，不可见菜单仅用于功能权限限制。每个菜单的权限子项由系统自动生成，请不要人为修改")]
[AdminArea]
[Menu(80, true, Icon = "Menu")]
public class MenuController : EntityTreeController<Menu, MenuModel>
{
    static MenuController()
    {
        // 过滤要显示的字段
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveField("Remark");
    }

    /// <summary>表单/详情中权限目录只读，避免与角色授权串混淆</summary>
    /// <param name="kind">视图种类</param>
    /// <param name="model">当前模型</param>
    /// <returns>字段集合</returns>
    protected override FieldCollection OnGetFields(ViewKinds kind, Object model)
    {
        var fields = base.OnGetFields(kind, model);
        if (kind is ViewKinds.AddForm or ViewKinds.EditForm or ViewKinds.Detail)
        {
            var df = fields.GetField("Permission");
            if (df != null) df.ReadOnly = true;
        }
        return fields;
    }

    /// <summary>验证实体对象</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(Menu entity, DataObjectMethodType type, Boolean post)
    {
        var rs = base.Valid(entity, type, post);

        // 清空缓存
        if (post) XCode.Membership.Menu.Meta.Session.ClearCache($"{type}-{entity}", true);

        return rs;
    }
}