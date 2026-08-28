using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.Widgets;
using NewLife.Serialization;
using XCode.Membership;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace NewLife.Cube.Controllers;

/// <summary>首页工作台 API（OSC-26082815a1）</summary>
[DisplayName("首页工作台")]
[Route("Cube/Workbench")]
public class WorkbenchController(TokenService tokenService) : ControllerBaseX
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

    /// <summary>解析当前用户工作台</summary>
    [HttpGet]
    public ActionResult Get()
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        var r = WorkbenchResolver.Resolve(user);
        return Json(0, null, new
        {
            source = r.Source,
            roleId = r.RoleId,
            config = Decode(r.ConfigJson),
        });
    }

    /// <summary>保存个人工作台。空串清除个人域。同时接受 PUT/POST（禁 PUT 环境回落）。</summary>
    [HttpPut]
    [HttpPost]
    public ActionResult Put([FromBody] WorkbenchPutRequest model, Int32 clear = 0)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (model == null) return Json(400, "请求体无效");

        var raw = model.HomeJson;
        if (clear == 1 && raw == null) raw = "";
        if (raw == null) return Json(400, "homeJson 不能为空");

        if (raw.Length == 0 || raw.Trim().Length == 0)
        {
            UserProfile.UpsertForUser(user.ID, new UserProfileModel { HomeJson = "" });
            return Json(0, null, new { source = "cleared" });
        }

        if (!DashboardJson.TryNormalize(raw, user, true, DashboardJson.SurfaceWorkbench, out var n, out var err))
            return Json(400, err);
        UserProfile.UpsertForUser(user.ID, new UserProfileModel { HomeJson = n });
        return Json(0, null, Decode(n));
    }

    /// <summary>读取角色工作台模板</summary>
    [HttpGet("Role/{roleId:int}")]
    public ActionResult GetRole(Int32 roleId)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (!WorkbenchResolver.IsSystem(user)) return Json(403, "仅系统角色可管理角色工作台");
        var role = Role.FindByID(roleId);
        if (role == null) return Json(404, "角色不存在");
        var json = WorkbenchRoleStore.Get(roleId);
        Object config = null;
        if (WorkbenchResolver.IsConfigured(json) &&
            DashboardJson.TryNormalize(json, user, false, DashboardJson.SurfaceWorkbench, out var n, out _))
            config = Decode(n);
        return Json(0, null, new { roleId, config });
    }

    /// <summary>保存角色工作台模板。同时接受 PUT/POST。</summary>
    [HttpPut("Role/{roleId:int}")]
    [HttpPost("Role/{roleId:int}")]
    public ActionResult PutRole(Int32 roleId, [FromBody] WorkbenchPutRequest model)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (!WorkbenchResolver.IsSystem(user)) return Json(403, "仅系统角色可管理角色工作台");
        var role = Role.FindByID(roleId);
        if (role == null) return Json(404, "角色不存在");
        if (model == null) return Json(400, "请求体无效");
        var raw = model.HomeJson ?? "";
        if (raw.Trim().Length == 0)
        {
            WorkbenchRoleStore.Clear(roleId);
            return Json(0, null, new { roleId, config = (Object)null });
        }
        if (!DashboardJson.TryNormalize(raw, user, true, DashboardJson.SurfaceWorkbench, out var n, out var err))
            return Json(400, err);
        WorkbenchRoleStore.Save(roleId, n);
        return Json(0, null, new { roleId, config = Decode(n) });
    }

    /// <summary>
    /// 将配置 JSON 解成 FastJson 可写出的对象树。
    /// 禁止返回 <c>JsonElement</c>：ControllerBaseX 用 FastJson 序列化时只会打出 <c>{"valueKind":1}</c>，前端拿不到 widgets。
    /// </summary>
    public static Object Decode(String json)
    {
        if (json.IsNullOrWhiteSpace()) return null;
        try
        {
            return JsonParser.Decode(json);
        }
        catch
        {
            return json;
        }
    }
}

/// <summary>工作台 PUT 体</summary>
public class WorkbenchPutRequest
{
    /// <summary>首页工作台 JSON；空串清除个人域</summary>
    public String HomeJson { get; set; }
}
