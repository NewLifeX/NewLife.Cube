using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Widgets;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>工作台部件管理。列出所有注册部件，系统管理员可启用/禁用、调整分组与组内顺序（Parameter 表全局配置，对齐 MVC WidgetController）</summary>
/// <remarks>供 React 皮肤 /Cube/Widget 页面使用：GET 列表 + POST Enable/SaveGroupOrder/SaveGroupItemOrder</remarks>
[DisplayName("工作台部件")]
[CubeArea]
public class WidgetController : ControllerBaseX
{
    private readonly WidgetManager _widgetManager;

    /// <summary>实例化</summary>
    /// <param name="widgetManager">工作台组件管理器（DI 注入单例）</param>
    public WidgetController(WidgetManager widgetManager) => _widgetManager = widgetManager;

    /// <summary>部件列表（含启用状态、分组顺序、组内顺序），供 React 页面渲染</summary>
    /// <returns>部件列表与分组配置</returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet("/api/[area]/[controller]")]
    public ActionResult Index()
    {
        CheckAdmin();

        var list = _widgetManager.Scan().Values.OrderBy(e => e.Sort).ToList();

        // 预计算启用状态（全局禁用过滤）
        var enabled = list.ToDictionary(e => e.Name, e => _widgetManager.IsEnabled(e), StringComparer.OrdinalIgnoreCase);

        // 组顺序
        var groupOrder = _widgetManager.GetGroupOrder();

        // 各组内默认顺序：有配置用配置，无配置用部件 Sort 兜底
        var groups = list.Select(e => e.Category)
            .Where(e => !e.IsNullOrEmpty())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var groupItems = new Dictionary<String, IList<String>>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups)
        {
            var cfg = _widgetManager.GetGroupItemOrder(g);
            groupItems[g] = cfg ?? list.Where(e => e.Category == g).OrderBy(e => e.Sort).Select(e => e.Name).ToList();
        }

        return Json(0, null, new
        {
            widgets = list.Select(e => new
            {
                e.Name,
                e.Title,
                e.Icon,
                e.Cols,
                e.Category,
                e.Permission,
                e.AdminOnly,
                e.Sort,
                Enable = enabled.TryGetValue(e.Name, out var en) ? en : true,
            }),
            groupOrder,
            groupItems,
        });
    }

    /// <summary>启用或禁用部件（Parameter 表 UserID=0，分类 Widget.Enable）</summary>
    /// <param name="name">部件名</param>
    /// <param name="enable">是否启用</param>
    /// <returns>操作结果</returns>
    [HttpPost]
    public ActionResult Enable(String name, Boolean enable)
    {
        CheckAdmin();

        _widgetManager.SetEnabled(name, enable);

        return Json(0, null, enable ? "已启用" : "已禁用");
    }

    /// <summary>保存组件分组顺序（逗号分隔组名，Parameter 全局配置，UserID=0）。未列出的组排到最后并按名称排序</summary>
    /// <param name="groups">逗号分隔的组名</param>
    /// <returns>操作结果</returns>
    [HttpPost]
    public ActionResult SaveGroupOrder(String groups)
    {
        CheckAdmin();

        var list = (groups + "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        _widgetManager.SetGroupOrder(list);

        return Json(0, null, "组顺序已保存");
    }

    /// <summary>保存指定组内的默认部件顺序（逗号分隔部件名，Parameter 全局配置，UserID=0）。未列出的新部件自动排到组内末尾</summary>
    /// <param name="group">组名</param>
    /// <param name="names">逗号分隔的部件名</param>
    /// <returns>操作结果</returns>
    [HttpPost]
    public ActionResult SaveGroupItemOrder(String group, String names)
    {
        CheckAdmin();

        var list = (names + "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        _widgetManager.SetGroupItemOrder(group, list);

        return Json(0, null, "组内顺序已保存");
    }

    private void CheckAdmin()
    {
        var user = ManageProvider.User;
        if (user == null || !user.Roles.Any(e => e.IsSystem)) throw new InvalidOperationException("仅支持系统管理员使用！");
    }
}
