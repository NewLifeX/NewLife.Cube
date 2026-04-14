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
[DisplayName("认证")]
[ApiController]
[Produces("application/json")]
[Route("[controller]/[action]")]
public class AuthController : ControllerBaseX
{
    private readonly UserService _userService;
    private readonly ICache _cache;

    /// <summary>实例化认证控制器</summary>
    /// <param name="userService">用户服务</param>
    /// <param name="cacheProvider">缓存提供者</param>
    public AuthController(UserService userService, ICacheProvider cacheProvider)
    {
        _userService = userService;
        _cache = cacheProvider.Cache;
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

    /// <summary>验证码登录</summary>
    /// <param name="model">登录模型，Username 为手机号/邮箱，Password 为验证码，LoginCategory 设为 Phone/Email</param>
    /// <returns>访问令牌和刷新令牌</returns>
    [HttpPost]
    [AllowAnonymous]
    public ApiResponse<TokenModel> LoginByCode(LoginModel model)
    {
        var res = new TokenModel();
        if (String.IsNullOrWhiteSpace(model.Username))
            return res.ToFailApiResponse("手机号/邮箱不能为空");
        if (String.IsNullOrWhiteSpace(model.Password))
            return res.ToFailApiResponse("验证码不能为空");

        try
        {
            var loginResult = _userService.Login(model, HttpContext);
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

    /// <summary>刷新令牌</summary>
    /// <param name="model">刷新令牌模型</param>
    /// <returns>新的访问令牌和刷新令牌</returns>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult Refresh(RefreshTokenModel model)
    {
        var userName = model.UserName;
        var refreshToken = model.RefreshToken;
        var user = ManageProvider.Provider.FindByName(userName);

        var tokens = HttpContext.RefreshToken(user, refreshToken);

        return Json(0, "ok", new { Token = tokens.AccessToken, RefreshToken = tokens.RefreshToken, tokens.ExpireIn });
    }

    /// <summary>获取登录页配置（OAuth 提供商列表、是否允许注册等）</summary>
    /// <returns>登录配置信息</returns>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult LoginConfig() => Json(0, null, new LoginConfigModel());

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

    /// <summary>获取RSA公钥挑战，用于客户端加密密码防明文传输。配合Login接口Pkey+加密密码实现安全登录</summary>
    /// <remarks>
    /// 流程：GET /Auth/Challenge 获取 pkey 和 publicKey → 前端用 publicKey 以 RSA-OAEP/SHA-256 加密密码
    /// → POST /Auth/Login 携带 pkey 和加密后的 password。密钥有效期300秒，使用一次后立即失效防重放。
    /// </remarks>
    /// <returns>挑战密钥ID(pkey)和PEM格式RSA公钥(publicKey)</returns>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult Challenge()
    {
        var (pkey, publicKey) = _userService.GetPublicKey();
        return Json(0, null, new { pkey, publicKey });
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
            ip);
        return result.IsSuccess
            ? true.ToOkApiResponse(result.Message)
            : false.ToFailApiResponse(result.Message);
    }
}
