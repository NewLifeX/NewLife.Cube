using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using NewLife.Common;
using XCode.Membership;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary>视图解析诊断。开发模式下查看视图的候选查找路径与实际命中，用于排查覆盖不生效问题</summary>
/// <remarks>实例化</remarks>
/// <param name="viewEngine">Razor 视图引擎</param>
/// <param name="env">主机环境</param>
[DisplayName("视图解析诊断")]
[CubeArea]
public class ViewTraceController(IRazorViewEngine viewEngine, IWebHostEnvironment env) : ControllerBaseX
{
    /// <summary>诊断页面</summary>
    /// <param name="area">区域名，默认取当前请求</param>
    /// <param name="controller">控制器名，默认取当前请求</param>
    /// <param name="view">视图名，默认取当前动作</param>
    /// <param name="theme">主题名，空则使用魔方默认主题</param>
    /// <returns></returns>
    public ActionResult Index([FromQuery] String area = null, [FromQuery] String controller = null, [FromQuery] String view = null, [FromQuery] String theme = null)
    {
        if (!SysConfig.Current.Develop) throw new InvalidOperationException("仅支持开发模式下使用！");

        var user = ManageProvider.User;
        if (user == null || !user.Roles.Any(e => e.IsSystem)) throw new InvalidOperationException("仅支持系统管理员使用！");

        // 默认取当前请求。area/controller 与路由令牌同名，必须显式从查询串绑定，否则被路由值遮蔽
        if (String.IsNullOrEmpty(area)) area = RouteData.Values["area"] + "";
        if (String.IsNullOrEmpty(controller)) controller = RouteData.Values["controller"] + "";
        if (String.IsNullOrEmpty(view)) view = RouteData.Values["action"] + "";
        if (String.IsNullOrEmpty(theme)) theme = CubeSetting.Current.Theme;

        var candidates = ViewLocationHelper.GetCandidates(area, controller, view, theme);

        // 物理文件存在性（相对内容根）
        var physical = candidates.Select(e => ViewLocationHelper.IsPhysicalFile(env.ContentRootPath, e)).ToArray();

        // 真实视图引擎解析
        var result = Resolve(area, controller, view);

        ViewBag.Area = area;
        ViewBag.Controller = controller;
        ViewBag.View = view;
        ViewBag.Theme = theme;
        ViewBag.Candidates = candidates;
        ViewBag.Physical = physical;
        ViewBag.Result = result;

        return View();
    }

    /// <summary>使用真实视图引擎解析指定视图，返回搜索结果</summary>
    /// <param name="area">区域名</param>
    /// <param name="controller">控制器名</param>
    /// <param name="view">视图名</param>
    /// <returns>视图引擎搜索结果</returns>
    private ViewEngineResult Resolve(String area, String controller, String view)
    {
        var routeData = new RouteData();
        if (!String.IsNullOrEmpty(area))
        {
            routeData.Values["area"] = area;
            routeData.DataTokens["area"] = area;
        }
        routeData.Values["controller"] = controller;
        routeData.Values["action"] = view;

        var actionContext = new ActionContext(HttpContext, routeData, new ControllerActionDescriptor
        {
            ControllerName = controller,
            ActionName = view,
        });

        return viewEngine.FindView(actionContext, view, !view.StartsWith("_"));
    }
}
