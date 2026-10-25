using System.ComponentModel;
using NewLife.Cube.Membership;
using NewLife.Cube.ViewModels;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>菜单控制器</summary>
[DisplayName("菜单")]
[Description("系统操作菜单以及功能目录树。支持排序，不可见菜单仅用于功能权限限制。每个菜单的权限子项由系统自动生成，请不要人为修改")]
[AdminArea]
[Menu(80, true, Icon = "fa-navicon")]
public class MenuController : EntityTreeController<Menu, MenuModel>
{
    static MenuController()
    {
        // 过滤要显示的字段
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6");
        ListFields.RemoveField("Remark");

        // 扩展字段是内部命名标记（Ex6 记录自动菜单规范键或自定义标记），不暴露到表单以免误改
        AddFormFields.RemoveField("Ex5", "Ex6");
        EditFormFields.RemoveField("Ex5", "Ex6");

        // 当前语言预览列：库内保存类型全名，这里展示翻译后的名字
        {
            var df = ListFields.AddListField("LocalizedDisplayName", null, "DisplayName");
            df.DisplayName = "Localized name";
            df.GetValue = e => MenuHelper.GetDisplayName(e as IMenu) ?? "";
        }

        {
            var ff = AddFormFields.GetField("Ex4") as FormField;
            ff.DisplayName = "Help document link";
        }
        {
            var ff = EditFormFields.GetField("Ex4") as FormField;
            ff.DisplayName = "Help document link";
        }
    }

    /// <summary>实体树的数据来自缓存</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Menu> Search(Pager p)
    {
        // 一页显示全部菜单，取自缓存
        p.PageSize = 10000;

        var menus = EntityTree<Menu>.Root.AllChilds.ToList();

        var set = GetSetting();
        if (set != null && !set.Parent.IsNullOrEmpty())
        {
            var pkey = p[set.Parent].ToInt(-1);
            if (pkey >= 0)
            {
                var m = XCode.Membership.Menu.FindByID(pkey);
                menus = EntityTree<Menu>.FindAllChildsByParent(pkey).ToList();
            }
        }

        return menus;
    }
}