using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
using NewLife.Cube.Entity;
using NewLife.Cube.Extensions;
using NewLife.Cube.Models;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using NewLife.Reflection;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Controllers;

/// <summary>统一认证控制器。为 SPA 前端提供简洁的 /Auth/* 路径，替代旧版 /Admin/User/* 认证路径</summary>
/// <remarks>
/// 旧版 UserController 的登录等接口保持不变，供 SSO 回调和旧版前后端分离项目使用。
/// 本控制器与 UserController 共享 UserService 业务层，不重复实现认证逻辑。
/// </remarks>
[DisplayName("认证")]
[ApiController]
[Produces("application/json")]
[Route("[controller]/[action]")]
public class AuthController : ControllerBaseX
{
    private const String OAuthPendingPrefix = "OAuthPending:";

    private readonly UserService _userService;
    private readonly ICache _cache;
    private readonly ICaptchaService _captcha;

    /// <summary>实例化认证控制器</summary>
    /// <param name="userService">用户服务</param>
    /// <param name="cacheProvider">缓存提供者</param>
    /// <param name="captchaService">验证码服务</param>
    public AuthController(UserService userService, ICacheProvider cacheProvider, ICaptchaService captchaService)
    {
        _userService = userService;
        _cache = cacheProvider.Cache;
        _captcha = captchaService;
    }

    /// <summary>密码登录</summary>
    /// <param name="model">登录模型，包含用户名和密码</param>
    /// <returns>访问令牌和刷新令牌</returns>
    [HttpPost]
    [AllowAnonymous]
    public ApiResponse<TokenModel> Login(LoginModel model)
    {
        var res = new TokenModel();
        if (String.IsNullOrWhiteSpace(model.Username))
            return res.ToFailApiResponse("用户名不能为空");
        if (String.IsNullOrWhiteSpace(model.Password))
            return res.ToFailApiResponse("密码不能为空");

        try
        {
            var loginResult = _userService.Login(model, HttpContext);
            // MFA 拦截：账密通过但需要二步验证
            if (loginResult != null && !loginResult.MfaToken.IsNullOrEmpty())
                return res.ToFailApiResponse($"mfa_required:{loginResult.MfaToken}");
            if (loginResult?.Data == null || loginResult.Data.AccessToken.IsNullOrEmpty())
                return res.ToFailApiResponse(loginResult?.Message);

            res.AccessToken = loginResult.Data.AccessToken;
            res.RefreshToken = loginResult.Data.RefreshToken;
            return res.ToOkApiResponse("登录成功");
        }
        catch (Exception ex)
        {
            return res.ToFailApiResponse(ex.Message);
        }
    }

    /// <summary>发送验证码（手机/邮箱）</summary>
    /// <param name="model">验证码模型，Channel 为 Sms/Mail，Username 为手机号或邮箱</param>
    /// <returns>验证码记录ID</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ApiResponse<Int64>> SendCode(VerifyCodeModel model)
    {
        try
        {
            var ip = UserHost;
            var result = await _userService.SendVerifyCode(model, ip);
            return result.Id.ToOkApiResponse("验证码已发送");
        }
        catch (Exception ex)
        {
            return 0L.ToRemotingErrorApiResponse("发送失败：" + ex.Message);
        }
    }

    /// <summary>刷新令牌</summary>
    /// <param name="model">刷新令牌模型</param>
    /// <returns>新的访问令牌和刷新令牌</returns>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult Refresh(RefreshTokenModel model)
    {
        var userName = model.UserName;
        var refreshToken = model.RefreshToken;

        // 令牌轮换：旧令牌使用后加入黑名单，防止同一刷新令牌被重复使用（重放攻击/令牌泄漏检测）
        var blacklistKey = $"RefreshBlacklist:{refreshToken?.GetHashCode()}";
        if (!refreshToken.IsNullOrEmpty() && _cache.ContainsKey(blacklistKey))
            return Json(-1, "refresh_token 已失效，请重新登录");

        var user = ManageProvider.Provider.FindByName(userName);
        var tokens = HttpContext.RefreshToken(user, refreshToken);

        // 旧刷新令牌加入黑名单，TTL 匹配刷新令牌有效期（7天）
        if (!refreshToken.IsNullOrEmpty())
            _cache.Set(blacklistKey, 1, 7 * 24 * 3600);

        return Json(0, "ok", new { Token = tokens.AccessToken, RefreshToken = tokens.RefreshToken, tokens.ExpireIn });
    }

    /// <summary>获取图片验证码</summary>
    /// <remarks>
    /// 当 CubeSetting.CaptchaScene 中包含对应场景位时，前端须先调用本接口获取验证码，
    /// 再将 captchaId 和用户填写的 captchaCode 随请求一并提交。
    /// 验证码 TTL 为 300 秒，校验成功后立即失效（防重放）。
    /// </remarks>
    /// <returns>captchaId（校验时回传）和 image（SVG 文本）</returns>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult Captcha()
    {
        var result = _captcha.Generate();
        return Json(0, null, result);
    }

    /// <summary>获取登录页配置（OAuth 提供商列表、是否允许注册等）</summary>
    /// <param name="tenant">可选租户标识：租户ID整数、tenantCode 或 tenantName；为空时按请求域名自动匹配</param>
    /// <returns>登录配置信息</returns>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult LoginConfig(String tenant = null)
    {
        Tenant t = null;
        if (!tenant.IsNullOrEmpty())
        {
            if (Int32.TryParse(tenant, out var id))
                t = Tenant.FindById(id);
            else
                t = Tenant.FindByCode(tenant) ?? Tenant.Find(Tenant._.Name == tenant);
        }
        // 不改 LoginConfigModel 形状：在 Auth 层附加 EnableTenant / EnableOAuthServer / StartPage（增量字段）
        return Json(0, null, BuildLoginConfigPayload(t));
    }

    /// <summary>
    /// 组装登录页 payload：沿用 LoginConfigModel；oauth 始终来自可见 OAuthConfig（与 EnableOAuthServer 无关）。
    /// EnableOAuthServer 仅表示 Cube 作为 OAuth 服务端；StartPage 供 SPA 登录后落地页。
    /// </summary>
    private static Object BuildLoginConfigPayload(Tenant tenant)
    {
        var model = new LoginConfigModel(tenant);
        var set = CubeSetting.Current;
        return new
        {
            model.Code,
            model.Name,
            model.Copyright,
            model.Registration,
            model.Logo,
            model.LoginTip,
            model.LoginLogo,
            model.LoginBackground,
            model.Login,
            model.Register,
            model.OAuth,
            model.Security,
            EnableTenant = set.EnableTenant,
            EnableOAuthServer = set.EnableOAuthServer,
            StartPage = set.StartPage,
            EChartsTheme = set.EChartsTheme,
            AvatarChars = set.AvatarChars,
            StarWeb = set.StarWeb,
        };
    }

    /// <summary>获取当前登录用户信息</summary>
    /// <returns>用户信息，包含权限和角色</returns>
    [HttpGet]
    [EntityAuthorize]
    public ActionResult Info()
    {
        if (ManageProvider.User is not User user) throw new Exception("当前登录用户无效！");

        user = XCode.Membership.User.FindByKeyForEdit(user.ID);
        if (user == null) throw new Exception("无效用户编号！");

        user["Password"] = null;

        var userInfo = new Areas.Admin.Models.UserInfo();
        userInfo.Copy(user);
        userInfo.SetPermission(user.Roles);
        userInfo.SetRoleNames(user.Roles);

        return Json(0, "ok", userInfo);
    }

    /// <summary>登出</summary>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult Logout()
    {
        ManageProvider.Provider.Logout();
        return Json(0, "ok");
    }

    /// <summary>获取RSA公钥挑战，用于客户端加密密码防明文传输。配合Login接口 ChallengeId + 加密密码实现安全登录</summary>
    /// <remarks>
    /// 流程：GET /Auth/Challenge 获取 challengeId 和 publicKey → 前端用 publicKey 以 RSA-OAEP/SHA-256 加密密码
    /// → POST /Auth/Login 携带 challengeId 和加密后的 password。密钥有效期300秒，使用一次后立即失效防重放。
    /// </remarks>
    /// <returns>挑战标识(challengeId)和PEM格式RSA公钥(publicKey)</returns>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult Challenge()
    {
        var (challengeId, publicKey) = _userService.GetPublicKey();
        return Json(0, null, new { challengeId, publicKey });
    }

    /// <summary>通过验证码重置密码（忘记密码流程）。先调用 SendCode 发送验证码，再调用本接口提交新密码</summary>
    /// <param name="model">重置密码模型，含手机号/邮箱、验证码、新密码、确认密码</param>
    /// <returns>重置结果</returns>
    [HttpPost]
    [AllowAnonymous]
    public ApiResponse<Boolean> ResetPassword(ResetPwdModel model)
    {
        var ip = UserHost;
        var result = _userService.ResetPassword(
            model.Username?.Trim() ?? "",
            model.Code?.Trim() ?? "",
            model.NewPassword?.Trim() ?? "",
            model.ConfirmPassword?.Trim() ?? "",
            model.ChallengeId,
            ip);
        return result.IsSuccess
            ? true.ToOkApiResponse(result.Message)
            : false.ToFailApiResponse(result.Message);
    }

    /// <summary>查询OAuth回跳待注册预填信息</summary>
    /// <param name="token">临时令牌</param>
    /// <returns>预填信息</returns>
    [HttpGet]
    [AllowAnonymous]
    public ApiResponse<OAuthPendingInfoModel> OAuthPendingInfo(String token)
    {
        var model = new OAuthPendingInfoModel();
        if (token.IsNullOrEmpty()) return model.ToFailApiResponse("token不能为空");

        var data = _cache.Get<OAuthPendingInfoModel>($"{OAuthPendingPrefix}{token}");
        if (data == null) return model.ToFailApiResponse("OAuth预填信息不存在或已过期");

        return data.ToOkApiResponse("ok");
    }

    /// <summary>统一注册（用户名密码/手机验证码/邮箱验证码/OAuth回跳绑定）</summary>
    /// <param name="model">注册模型</param>
    /// <returns>注册并登录后的令牌</returns>
    [HttpPost]
    [AllowAnonymous]
    public ApiResponse<TokenModel> Register(AuthRegisterModel model)
    {
        var res = new TokenModel();
        var registerResult = _userService.Register(model, HttpContext);
        if (!registerResult.IsSuccess || registerResult.Data == null)
            return res.ToFailApiResponse(registerResult.Message);

        res.AccessToken = registerResult.Data.AccessToken;
        res.RefreshToken = registerResult.Data.RefreshToken;
        return res.ToOkApiResponse(registerResult.Message ?? "注册成功");
    }

    /// <summary>当前用户第三方绑定列表（可见 OAuth ⟕ UserConnect）</summary>
    [HttpGet]
    [EntityAuthorize]
    public ActionResult Binds()
    {
        if (ManageProvider.User is not User user) return Json(-1, "未登录");

        user = XCode.Membership.User.FindByKeyForEdit(user.ID);
        if (user == null) return Json(-1, "无效用户");

        var connects = UserConnect.FindAllByUserID(user.ID);
        var oauthItems = OAuthConfig.GetValids(TenantContext.CurrentId, GrantTypes.AuthorizationCode);

        var providers = oauthItems.Select(o =>
        {
            var conn = connects.FirstOrDefault(c => c.Provider.EqualIgnoreCase(o.Name));
            return new
            {
                id = o.ID,
                name = o.Name,
                nickName = o.NickName,
                logo = o.Logo,
                bound = conn != null && conn.Enable,
                connectId = conn?.ID ?? 0,
                connectNickName = conn?.NickName,
            };
        }).ToList();

        return Json(0, "ok", new { providers });
    }

    /// <summary>当前用户可切换的租户列表</summary>
    [HttpGet]
    [EntityAuthorize]
    public ActionResult Tenants()
    {
        if (ManageProvider.User is not User user) return Json(-1, "未登录");
        return Json(0, "ok", BuildTenantList(user));
    }

    /// <summary>切换当前租户</summary>
    [HttpPost]
    [EntityAuthorize]
    public ActionResult SwitchTenant([FromBody] SwitchTenantModel model)
    {
        if (ManageProvider.User is not User user) return Json(-1, "未登录");
        if (!CubeSetting.Current.EnableTenant) return Json(-1, "未开启多租户");

        var tenantId = model?.TenantId ?? -1;
        var isAdmin = user.Roles != null && user.Roles.Any(e => e.IsSystem);
        var list = TenantUser.FindAllByUserId(user.ID).Where(e => e.Enable).ToList();

        if (tenantId == 0)
        {
            if (!isAdmin) return Json(-1, "无权切换到平台");
        }
        else if (tenantId > 0)
        {
            if (!list.Any(e => e.TenantId == tenantId)) return Json(-1, "无权切换到该租户");
        }
        else
        {
            return Json(-1, "无效租户");
        }

        HttpContext.SaveTenant(tenantId);
        return Json(0, "ok", BuildTenantList(user));
    }

    private static Object BuildTenantList(User user)
    {
        // 多租户关闭：不返回可切换列表，前端据此隐藏顶栏/表单租户 UI
        if (!CubeSetting.Current.EnableTenant)
        {
            return new
            {
                enableTenant = false,
                currentId = 0,
                currentCode = "",
                items = Array.Empty<Object>(),
            };
        }

        var currentId = TenantContext.CurrentId;
        var current = Tenant.FindById(currentId);
        var isAdmin = user.Roles != null && user.Roles.Any(e => e.IsSystem);
        var items = new List<Object>();

        if (isAdmin)
            items.Add(new { id = 0, code = "", name = "平台" });

        foreach (var tu in TenantUser.FindAllByUserId(user.ID).Where(e => e.Enable))
        {
            var t = Tenant.FindById(tu.TenantId);
            if (t == null || !t.Enable) continue;
            items.Add(new { id = t.Id, code = t.Code ?? "", name = t.Name });
        }

        return new
        {
            enableTenant = true,
            currentId,
            currentCode = current?.Code ?? "",
            items,
        };
    }
}

/// <summary>切换租户请求</summary>
public class SwitchTenantModel
{
    /// <summary>目标租户 Id；0 表示平台（仅系统管理员）</summary>
    public Int32 TenantId { get; set; }
}
