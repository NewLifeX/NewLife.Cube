using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube;
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

        // 扩展字段是内部扩展位，不暴露到表单以免误改
        AddFormFields.RemoveField("Ex5", "Ex6");
        EditFormFields.RemoveField("Ex5", "Ex6");

        // 显示名：展示最终效果。源文语言用库值（管理员改名在此生效），其它语言查词表即时翻译
        {
            var df = ListFields.GetField("DisplayName") as ListField;
            if (df != null) df.GetValue = e => MenuHelper.GetDisplayName(e as IMenu) ?? "";
        }

        // 落库值：开发者语言快照，留空表示由扫描自动填充
        {
            var df = ListFields.AddListField("StoredDisplayName", null, "DisplayName");
            df.DisplayName = "Stored name";
            df.GetValue = e => (e as IMenu)?.DisplayName ?? "";
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

    /// <summary>刷新显示名。按 FullName 重新解析代码源文，覆盖选中菜单的库值</summary>
    /// <returns>刷新结果，前端提示后刷新页面</returns>
    /// <remarks>显示名落库的是开发者语言快照，代码改名后不会自动跟进，由管理员在此显式重置。</remarks>
    [EntityAuthorize(PermissionFlags.Update)]
    [ToolbarButton("Refresh menu names", PermissionFlags.Update, Method = "POST", Icon = "fa-refresh")]
    public ActionResult RefreshName()
    {
        var ids = GetRequest("keys").SplitAsInt();
        if (ids.Length == 0) return Json(new { message = L.T("Please select the menus to refresh first.") });

        var count = 0;
        foreach (var id in ids)
        {
            var entity = XCode.Membership.Menu.FindByID(id);
            if (entity != null && MenuHelper.RefreshDisplayName(entity) && entity.Update() > 0) count++;
        }

        XCode.Membership.Menu.Meta.Session.ClearCache("RefreshName", true);

        return Json(new { message = L.T("Refreshed {0} menus", count), data = count, url = "[refresh]" });
    }
}