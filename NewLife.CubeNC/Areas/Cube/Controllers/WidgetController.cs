using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Widgets;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>工作台部件管理。列出所有注册部件，系统管理员可启用/禁用（Parameter 表全局配置）</summary>
/// <remarks>实例化</remarks>
/// <param name="widgetManager">工作台组件管理器</param>
[DisplayName("工作台部件")]
[CubeArea]
[Menu(60, true, Icon = "fa-th-large", LastUpdate = "20260827")]
public class WidgetController(WidgetManager widgetManager) : ControllerBaseX
{
    /// <summary>部件列表</summary>
    public ActionResult Index()
    {
        CheckAdmin();

        var list = widgetManager.Scan().Values.OrderBy(e => e.Sort).ToList();

        // 预计算启用状态供视图渲染（视图不直接访问 DI 服务）
        ViewBag.Widgets = list;
        ViewBag.Enabled = list.ToDictionary(e => e.Name, e => widgetManager.IsEnabled(e));

        return View();
    }

    /// <summary>启用或禁用部件（Parameter 表 UserID=0，分类 Widget.Enable）</summary>
    /// <param name="name">部件名</param>
    /// <param name="enable">是否启用</param>
    /// <returns></returns>
    [HttpPost]
    public ActionResult Enable(String name, Boolean enable)
    {
        CheckAdmin();

        widgetManager.SetEnabled(name, enable);

        return Json(0, null, enable ? "已启用" : "已禁用");
    }

    private void CheckAdmin()
    {
        var user = ManageProvider.User;
        if (user == null || !user.Roles.Any(e => e.IsSystem)) throw new InvalidOperationException("仅支持系统管理员使用！");
    }
}
