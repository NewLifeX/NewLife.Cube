using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Caching;
using NewLife.Cube.Areas.Admin.Models;
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
/// <remarks>实例化认证控制器</remarks>
/// <param name="userService">用户服务</param>
/// <param name="verifyCode">验证码服务</param>
/// <param name="authEnhanced">增强认证服务</param>
/// <param name="cacheProvider">缓存提供者</param>
[DisplayName("认证")]
[ApiController]
[Produces("application/json")]
[Route("[controller]/[action]")]
[Menu(0, false, Mode = MenuModes.Admin | MenuModes.Tenant)]
public class AuthController(UserService userService, VerifyCodeService verifyCode, AuthEnhancedService authEnhanced, ICacheProvider cacheProvider) : ControllerBaseX
{
    private const String OAuthPendingPrefix = "OAuthPending:";
    private readonly ICache _cache = cacheProvider.Cache;

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
            var loginResult = authEnhanced.Login(model, HttpContext);
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
            var result = await verifyCode.SendVerifyCode(model, ip);
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
    /// 当 CubeSetting.CaptchaScene 中包含对应场景位或风险自适应触发时，前端须先调用本接口获取验证码，
    /// 再将 captchaId 和用户填写的 captchaCode 随请求一并提交。
    /// 验证码 TTL 为 300 秒，校验成功后立即失效（防重放）。
    /// </remarks>
    /// <returns>captchaId（校验时回传）和 image（SVG 文本）</returns>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult Captcha()
    {
        var result = verifyCode.GenerateCaptcha();
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

        var model = new LoginConfigModel(t);

        // 风险自适应验证码：按当前请求环境动态判定（CaptchaScene 强制 或 风险触发）
        var ip = HttpContext.GetUserHost();
        var deviceId = HttpContext.Request.Cookies["CubeDeviceId"];
        if (deviceId.IsNullOrEmpty()) deviceId = HttpContext.Request.Cookies["CubeDeviceId0"];
        model.ApplyRiskCaptcha(
            verifyCode.RequireCaptcha(1, ip, null, deviceId),
            verifyCode.RequireCaptcha(2, ip, null, deviceId),
            verifyCode.RequireCaptcha(4, ip, null, null));

        return Json(0, null, model);
    }

    /// <summary>获取当前登录用户信息</summary>
    /// <returns>用户信息，包含权限和角色</returns>
    [HttpGet]
    [EntityAuthorize]
    [Menu(0, false, Mode = MenuModes.Admin | MenuModes.Tenant)]
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
        var (challengeId, publicKey) = userService.GetPublicKey();
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
        var result = authEnhanced.ResetPassword(
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
        var registerResult = authEnhanced.Register(model, HttpContext);
        if (!registerResult.IsSuccess || registerResult.Data == null)
            return res.ToFailApiResponse(registerResult.Message);

        res.AccessToken = registerResult.Data.AccessToken;
        res.RefreshToken = registerResult.Data.RefreshToken;
        return res.ToOkApiResponse(registerResult.Message ?? "注册成功");
    }
}
