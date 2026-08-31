using System.ComponentModel;
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

        // 父级：下拉选择。菜单缓存作为数据源，label 用层级路径（如 系统管理/用户），
        // 避免同名菜单混淆；不依赖 FullName 字段数据质量。新增/编辑表单均生效
        foreach (var fields in new[] { AddFormFields, EditFormFields })
        {
            var df = fields.GetField("ParentID");
            df.DataSource = _ => BuildMenuSource();
        }
    }

    /// <summary>构建菜单层级路径字典（菜单缓存 → ID → 显示路径），供父级下拉选择</summary>
    /// <returns>菜单编号到层级路径的映射</returns>
    private static Dictionary<Int32, String> BuildMenuSource()
    {
        var menus = XCode.Membership.Menu.FindAllWithCache().ToList();
        var map = menus.ToDictionary(e => e.ID, e => e);
        var dict = new Dictionary<Int32, String>();
        foreach (var e in menus)
        {
            var parts = new List<String> { e.DisplayName.IsNullOrEmpty() ? e.Name : e.DisplayName };
            var p = e;
            var guard = 0;
            while (p.ParentID > 0 && map.TryGetValue(p.ParentID, out var pp) && pp.ID != p.ID && guard++ < 8)
            {
                parts.Insert(0, pp.DisplayName.IsNullOrEmpty() ? pp.Name : pp.DisplayName);
                p = pp;
            }
            dict[e.ID] = String.Join("/", parts);
        }
        return dict;
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