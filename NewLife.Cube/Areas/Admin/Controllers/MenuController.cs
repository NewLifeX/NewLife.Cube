using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube.Membership;
using NewLife.Cube.Models;
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

        // 扩展字段是内部命名标记，不暴露到表单以免误改
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
            var parts = new List<String> { MenuHelper.GetDisplayName(e) ?? e.Name };
            var p = e;
            var guard = 0;
            while (p.ParentID > 0 && map.TryGetValue(p.ParentID, out var pp) && pp.ID != p.ID && guard++ < 8)
            {
                parts.Insert(0, MenuHelper.GetDisplayName(pp) ?? pp.Name);
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

    /// <summary>刷新显示名。按 FullName 重新解析代码源文，覆盖选中菜单的库值</summary>
    /// <returns>刷新结果。data.url 为 [refresh] 时前端提示后刷新列表</returns>
    /// <remarks>显示名落库的是开发者语言快照，代码改名后不会自动跟进，由管理员在此显式重置。</remarks>
    [EntityAuthorize(PermissionFlags.Update)]
    [ToolbarButton("Refresh menu names", PermissionFlags.Update, Method = "POST", Icon = "fa-refresh")]
    [HttpPost("/api/[area]/[controller]/refreshName")]
    public ApiResponse<Object> RefreshName()
    {
        var ids = GetRequest("keys").SplitAsInt();
        if (ids.Length == 0) return new ApiResponse<Object> { Code = CubeCode.ParamError.ToInt(), Message = L.T("Please select the menus to refresh first.") };

        var count = 0;
        foreach (var id in ids)
        {
            var entity = XCode.Membership.Menu.FindByID(id);
            if (entity != null && MenuHelper.RefreshDisplayName(entity) && entity.Update() > 0) count++;
        }

        XCode.Membership.Menu.Meta.Session.ClearCache("RefreshName", true);

        return new ApiResponse<Object> { Code = 0, Message = L.T("Refreshed {0} menus", count), Data = new { count, url = "[refresh]" } };
    }
}