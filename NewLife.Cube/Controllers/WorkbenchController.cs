using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Cube.Widgets;
using NewLife.Serialization;
using NewLife.Security;
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
        // 验签通过（ex == null）且应用有效才放行；验签失败时 ex 非空绝不能放行，防止伪造 JWT 认证绕过
        return app != null && app.Enable && ex == null;
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
    [HttpPut("")]
    [HttpPost("")]
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

    #region 命名工作台（OSC-260902ef43）
    /// <summary>命名工作台列表（仅系统角色；仅含已挂菜单行的槽）</summary>
    [HttpGet("Named")]
    public ActionResult NamedList()
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (!WorkbenchResolver.IsSystem(user)) return Json(403, "仅系统角色可管理命名工作台");
        return Json(0, null, WorkbenchNamedStore.GetVisibleList());
    }

    /// <summary>读取命名工作台（菜单授权只读）。不存在 404，无权 403。</summary>
    [HttpGet("Named/{slug}")]
    public ActionResult NamedGet(String slug)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (!WorkbenchNamedStore.IsValidSlug(slug)) return Json(400, "slug 非法");
        if (!WorkbenchNamedStore.Exists(slug)) return Json(404, "命名工作台不存在");
        if (!WorkbenchNamedStore.IsAccessible(user, slug)) return Json(403, "无权访问该命名工作台");

        var item = WorkbenchNamedStore.FindItem(slug);
        var json = WorkbenchNamedStore.Get(slug);
        Object config = null;
        if (!json.IsNullOrEmpty() &&
            DashboardJson.TryNormalize(json, user, false, DashboardJson.SurfaceWorkbench, out var n, out _))
            config = Decode(n);
        return Json(0, null, new { slug, title = item?.Title, config });
    }

    /// <summary>保存命名工作台（upsert，仅系统角色）：写 Parameter 槽并挂载/更新系统菜单。</summary>
    [HttpPut("Named/{slug}")]
    [HttpPost("Named/{slug}")]
    public ActionResult NamedPut(String slug, [FromBody] WorkbenchNamedPutRequest model)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (!WorkbenchResolver.IsSystem(user)) return Json(403, "仅系统角色可管理命名工作台");
        if (model == null) return Json(400, "请求体无效");
        if (!WorkbenchNamedStore.IsValidSlug(slug)) return Json(400, "slug 非法");
        var title = model.Title?.Trim();
        if (!WorkbenchNamedStore.IsValidTitle(title)) return Json(400, "标题不能为空且不超过 40 字");
        // 另存为（create=true）撞已存在 slug → 409，禁止静默覆盖既有共享看板
        if (model.Create && WorkbenchNamedStore.Exists(slug)) return Json(409, "命名工作台已存在，另存为不允许覆盖（可改用它或先删除）");

        var raw = model.HomeJson ?? "";
        if (raw.Trim().Length == 0) return Json(400, "homeJson 不能为空（清空请用 DELETE）");
        if (!DashboardJson.TryNormalize(raw, user, true, DashboardJson.SurfaceWorkbench, out var n, out var err))
            return Json(400, err);

        WorkbenchNamedStore.Save(slug, title, n);
        WorkbenchNamedStore.MountMenu(slug, title);
        return Json(0, null, new { slug, title });
    }

    /// <summary>下架命名工作台（仅系统角色）：删除菜单行与 Parameter 槽。</summary>
    [HttpDelete("Named/{slug}")]
    public ActionResult NamedDelete(String slug)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");
        if (!WorkbenchResolver.IsSystem(user)) return Json(403, "仅系统角色可管理命名工作台");
        if (!WorkbenchNamedStore.IsValidSlug(slug)) return Json(400, "slug 非法");

        WorkbenchNamedStore.UnmountMenu(slug);
        WorkbenchNamedStore.Delete(slug);
        return Json(0, null, new { slug, deleted = true });
    }

    /// <summary>分享当前工作台：签发 Url 锁定的 UserToken（有效期可配），供匿名以分享者权限打开 embed 页。</summary>
    [HttpPost("Share")]
    [HttpPut("Share")]
    public ActionResult Share([FromBody] ShareViewRequest model)
    {
        var user = Current;
        if (user == null) return Json(401, "未授权");

        var slug = (model?.Slug + "").Trim();
        if (!slug.IsNullOrEmpty())
        {
            if (!WorkbenchNamedStore.IsValidSlug(slug)) return Json(400, "slug 非法");
            if (!WorkbenchNamedStore.Exists(slug)) return Json(404, "命名工作台不存在");
            if (!WorkbenchNamedStore.IsAccessible(user, slug)) return Json(403, "无权分享该命名工作台");
        }

        var expireSec = ClampShareExpireSeconds(model?.ExpireSeconds ?? 0);
        var url = ResolveSharePageUrl(slug);

        try
        {
            var list = UserToken.FindAllByUserID(user.ID);
            var ut = list.FirstOrDefault(e => e.Url.EqualIgnoreCase(url) && e.Enable && e.Expire > DateTime.Now);
            ut ??= new UserToken { UserID = user.ID, Url = url };
            if (ut.Token.IsNullOrEmpty()) ut.Token = Rand.NextString(16);
            ut.Enable = true;
            ut.Expire = DateTime.Now.AddSeconds(expireSec);
            ut.Save();

            WriteLog("分享", true, url);

            return Json(0, null, new
            {
                token = ut.Token,
                expire = ut.Expire,
                path = url,
            });
        }
        catch (Exception ex)
        {
            return Json(500, ex.GetTrue()?.Message ?? "签发分享令牌失败");
        }
    }
    #endregion

    /// <summary>工作台分享页路径：空 slug → /home；命名 → /Workbench/{slug}</summary>
    public static String ResolveSharePageUrl(String slug)
    {
        slug = (slug + "").Trim();
        return slug.IsNullOrEmpty() ? "/home" : "/Workbench/" + slug;
    }

    /// <summary>分享有效秒数：缺省用配置，最短 60 秒，最长 1 年</summary>
    public static Int32 ClampShareExpireSeconds(Int32 expireSec)
    {
        if (expireSec <= 0) expireSec = CubeSetting.Current.ShareExpire;
        if (expireSec < 60) expireSec = 60;
        if (expireSec > 365 * 24 * 3600) expireSec = 365 * 24 * 3600;
        return expireSec;
    }

    /// <summary>将配置 JSON 解成 FastJson 可写出的对象树。
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

/// <summary>命名工作台 PUT 体（OSC-260902ef43）</summary>
public class WorkbenchNamedPutRequest
{
    /// <summary>标题（≤40），同步菜单 DisplayName</summary>
    public String Title { get; set; }

    /// <summary>命名工作台配置 JSON（workbench surface）</summary>
    public String HomeJson { get; set; }

    /// <summary>是否“另存为新建”语义：true 且 slug 已存在 → 409（禁止覆盖既有看板）；缺省 false=更新自身（发布/重命名）</summary>
    public Boolean Create { get; set; }
}
