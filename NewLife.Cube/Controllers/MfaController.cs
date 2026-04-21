using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Services;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Controllers;

/// <summary>多因素认证控制器。提供 TOTP 的开通、激活、禁用和登录二步验证接口</summary>
/// <remarks>
/// Setup/Activate/Disable 需要已登录（EntityAuthorize）。
/// Verify 端点接受 MFA 挂起令牌（由 Login 接口在账密通过但 MFA 未验证时签发），不需要完整登录态。
/// </remarks>
[DisplayName("多因素认证")]
[ApiController]
[Produces("application/json")]
[Route("[controller]/[action]")]
public class MfaController : ControllerBaseX
{
    private readonly IMfaService _mfa;
    private readonly TotpMfaService _totp;

    /// <summary>初始化 MFA 控制器</summary>
    /// <param name="mfaService">MFA 服务</param>
    public MfaController(IMfaService mfaService)
    {
        _mfa = mfaService;
        _totp = mfaService as TotpMfaService;
    }

    /// <summary>初始化 TOTP 设置，返回二维码 URI 和手动输入密钥</summary>
    /// <remarks>
    /// 用户在个人安全设置页面点击"开启两步验证"时调用。
    /// 返回的 totpUri 可直接传给二维码库生成扫码图。
    /// 初始化后密钥暂存，须调用 /Mfa/Activate 完成激活。
    /// </remarks>
    /// <returns>totpUri 和 secret</returns>
    [HttpGet]
    [EntityAuthorize]
    public ActionResult Setup()
    {
        if (!CubeSetting.Current.EnableMfa)
            return Json(-1, "系统未开启 MFA 功能");

        var user = ManageProvider.User as User;
        if (user == null) return Json(-1, "当前登录用户无效");

        var sysName = NewLife.Common.SysConfig.Current.DisplayName;
        var result = _mfa.SetupTotp(user, sysName.IsNullOrEmpty() ? "NewLife.Cube" : sysName);
        return Json(0, null, result);
    }

    /// <summary>激活 TOTP：校验用户扫码后的第一个验证码，通过后正式启用 MFA 并返回备用码</summary>
    /// <param name="code">6 位 TOTP 验证码</param>
    /// <returns>备用码列表（10 组，每组 10 位，只显示一次，请妥善保管）</returns>
    [HttpPost]
    [EntityAuthorize]
    public ActionResult Activate(String code)
    {
        if (code.IsNullOrEmpty()) return Json(-1, "验证码不能为空");

        var user = ManageProvider.User as User;
        if (user == null) return Json(-1, "当前登录用户无效");

        try
        {
            var backupCodes = _mfa.ActivateTotp(user, code);
            return Json(0, "MFA 已激活", new { backupCodes });
        }
        catch (InvalidOperationException ex)
        {
            return Json(-1, ex.Message);
        }
    }

    /// <summary>禁用 MFA，清除当前用户所有 MFA 数据</summary>
    /// <param name="code">当前有效的 6 位 TOTP 验证码（防止他人擅自关闭）</param>
    [HttpPost]
    [EntityAuthorize]
    public ActionResult Disable(String code)
    {
        if (code.IsNullOrEmpty()) return Json(-1, "验证码不能为空");

        var user = ManageProvider.User as User;
        if (user == null) return Json(-1, "当前登录用户无效");

        // 禁用前须通过一次 MFA 校验，防止会话劫持后被关闭
        if (!_mfa.Verify(user, code)) return Json(-1, "验证码错误");

        _mfa.DisableMfa(user);
        return Json(0, "MFA 已禁用");
    }

    /// <summary>查询当前用户 MFA 启用状态</summary>
    /// <returns>enabled: bool</returns>
    [HttpGet]
    [EntityAuthorize]
    public ActionResult Status()
    {
        var user = ManageProvider.User as User;
        if (user == null) return Json(-1, "当前登录用户无效");

        return Json(0, null, new { enabled = _mfa.IsEnabled(user), available = CubeSetting.Current.EnableMfa });
    }

    /// <summary>二步验证登录。账密通过后携带 mfaToken + code 完成 MFA 校验，成功后下发正式 AccessToken</summary>
    /// <remarks>
    /// 调用流程：
    /// 1. POST /Auth/Login → 账密正确但用户已开启 MFA → 返回 code=449，data.mfaToken
    /// 2. 用户打开 Authenticator App 查看 6 位验证码（或使用备用码）
    /// 3. POST /Mfa/Verify { mfaToken, code } → 返回 accessToken / refreshToken
    /// </remarks>
    /// <param name="model">MFA 验证模型，包含 mfaToken 和 code</param>
    [HttpPost]
    [AllowAnonymous]
    public ActionResult Verify(MfaVerifyModel model)
    {
        if (model == null || model.MfaToken.IsNullOrEmpty())
            return Json(-1, "mfaToken 不能为空");
        if (model.Code.IsNullOrEmpty())
            return Json(-1, "验证码不能为空");

        // 消费挂起令牌
        if (_totp == null) return Json(-1, "MFA 服务不支持令牌验证");

        var userId = _totp.ConsumePendingToken(model.MfaToken);
        if (userId <= 0) return Json(-1, "mfaToken 无效或已过期，请重新登录");

        var user = XCode.Membership.User.FindByID(userId);
        if (user == null) return Json(-1, "用户不存在");
        if (!user.Enable) return Json(-1, "用户已禁用");

        if (!_mfa.Verify(user, model.Code))
            return Json(-1, "MFA 验证码错误");

        // MFA 通过，签发正式令牌
        var set = CubeSetting.Current;
        var tokens = HttpContext.IssueTokenAndRefreshToken(user, TimeSpan.FromSeconds(set.TokenExpire));

        return Json(0, "登录成功", new
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            tokens.ExpireIn,
        });
    }
}
