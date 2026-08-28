using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife;
using NewLife.Cube.Automation;
using NewLife.Cube.Services;
using NewLife.Cube.Widgets;
using NewLife.Reflection;
using NewLife.Remoting;
using XCode;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Controllers;

/// <summary>页面仪表盘 Widget API（OSC-2608280e9e）</summary>
[DisplayName("页面仪表盘")]
[Route("Cube/Widget")]
public class WidgetController(TokenService tokenService) : ControllerBaseX
{
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var allowAnonymous = descriptor?.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).FirstOrDefault();
        if (allowAnonymous == null && !ValidateToken())
        {
            context.Result = Json(401, "未授权");
            return;
        }
        base.OnActionExecuting(context);
    }

    Boolean ValidateToken()
    {
        // 优先走统一登录/分享令牌（含 UserToken 短令牌）
        var user = ManageProvider.Provider.TryLogin(HttpContext);
        if (user != null) return true;
        if (ManageProvider.User != null) return true;
        var token = CubeController.GetToken(HttpContext);
        if (token.IsNullOrEmpty()) return false;
        var ap = tokenService.FindBySecret(token);
        if (ap != null && ap.Enable) return true;
        var set = CubeSetting.Current;
        var (app, ex) = tokenService.TryDecodeToken(token, set.JwtSecret);
        return app != null && app.Enable && ex != null;
    }

    IUser Current => ManageProvider.User as IUser;

    /// <summary>当前用户有查看权的实体列表</summary>
    [HttpGet("Sources")]
    public ActionResult Sources()
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        var list = new List<IDictionary<String, Object>>();
        var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in EntityPageRegistry.GetAll())
        {
            var url = kv.Value?.Url;
            if (url.IsNullOrEmpty()) continue;
            var typePath = AutomationPaths.NormalizeTypePath(url);
            if (typePath.IsNullOrEmpty() || !seen.Add(typePath)) continue;
            if (!AutomationAuth.HasPermission(user, typePath, PermissionFlags.Detail)) continue;
            var display = ResolveEntityDisplayName(kv.Key);
            list.Add(new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase)
            {
                ["typePath"] = typePath,
                ["displayName"] = display,
                ["name"] = kv.Key.Name,
            });
        }
        var data = list.OrderBy(e => e["displayName"] + "").ToList();
        return Json(0, null, data);
    }

    /// <summary>平台 kind + 当前用户可见的 named Widget。surface 默认 insight。</summary>
    [HttpGet("Catalog")]
    public ActionResult Catalog(String surface = "insight")
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        var workbench = DashboardJson.IsWorkbench(surface);
        var kinds = workbench
            ? new[]
            {
                new { kind = "metricCard", title = "指标卡", providers = new[] { "entity.aggregate", "named" }, defaultW = 3 },
                new { kind = "miniChart", title = "迷你图表", providers = new[] { "entity.aggregate" }, defaultW = 6 },
                new { kind = "miniKanban", title = "数据看板", providers = new[] { "entity.list" }, defaultW = 6 },
                new { kind = "dataList", title = "数据列表", providers = new[] { "entity.list" }, defaultW = 6 },
                new { kind = "dataCard", title = "数据卡片", providers = new[] { "entity.list" }, defaultW = 6 },
            }
            : new[]
            {
                new { kind = "metricCard", title = "指标卡", providers = new[] { "entity.aggregate", "named" }, defaultW = 3 },
                new { kind = "miniChart", title = "迷你图表", providers = new[] { "entity.aggregate" }, defaultW = 6 },
            };
        var named = CubeWidgetManager.CatalogFor(user, workbench ? DashboardJson.SurfaceWorkbench : DashboardJson.SurfaceInsight).Select(e => new
        {
            e.Name,
            e.Title,
            e.Kind,
            e.Cols,
            e.AdminOnly,
            e.Surfaces,
            e.Color,
            e.Icon,
        }).ToList();
        return Json(0, null, new { kinds, named });
    }

    /// <summary>只读聚合或列表查询</summary>
    [HttpPost("Query")]
    public ActionResult Query([FromBody] WidgetQueryRequest req)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        try
        {
            var data = WidgetQueryService.Execute(user, req);
            return Json(0, null, data);
        }
        catch (ApiException ex)
        {
            return Json(ex.Code, ex.Message);
        }
    }

    /// <summary>named Widget 取数</summary>
    [HttpGet("Data")]
    public ActionResult Data(String name, String hostTypePath = null)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (name.IsNullOrEmpty()) return Json(400, "name 不能为空");
        var reg = CubeWidgetManager.Find(name);
        if (reg == null || !CubeWidgetManager.Visible(reg, user)) return Json(404, "部件不存在");
        try
        {
            var w = reg.Create();
            var payload = w.GetData(new WidgetContext
            {
                User = user,
                HostTypePath = hostTypePath,
            });
            return Json(0, null, payload);
        }
        catch (Exception ex)
        {
            return Json(500, ex.Message);
        }
    }

    /// <summary>实体中文显示名：DisplayName → Description 首段 → 表 DisplayName → 类型名</summary>
    static String ResolveEntityDisplayName(Type entityType)
    {
        if (entityType == null) return "";
        var dn = entityType.GetDisplayName();
        if (!dn.IsNullOrEmpty() && !dn.EqualIgnoreCase(entityType.Name)) return dn;

        var desc = entityType.GetDescription();
        if (!desc.IsNullOrEmpty())
        {
            var cut = desc.IndexOfAny(['。', '.', '\n', '，', ',']);
            if (cut > 0) desc = desc[..cut].Trim();
            if (!desc.IsNullOrEmpty() && !desc.EqualIgnoreCase(entityType.Name)) return desc;
        }

        try
        {
            var tableDn = entityType.AsFactory()?.Table?.DataTable?.DisplayName;
            if (!tableDn.IsNullOrEmpty()) return tableDn;
        }
        catch { /* ignore */ }

        return dn.IsNullOrEmpty() ? entityType.Name : dn;
    }
}
