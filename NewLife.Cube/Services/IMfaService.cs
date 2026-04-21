using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>多因素认证（MFA）服务接口。实现该接口可替换内置 TOTP 实现</summary>
/// <remarks>
/// 默认实现为 <see cref="TotpMfaService"/>，遵循 RFC 6238 TOTP 标准，与 Google Authenticator / 微软 Authenticator 等主流 App 兼容。
/// MFA 数据（密钥、状态、备用码）存储在 User.Extends 字典中，无需新建数据表。
/// 注册自定义实现时使用 TryAddSingleton&lt;IMfaService, YourImpl&gt;()。
/// </remarks>
public interface IMfaService
{
    /// <summary>为用户生成新的 TOTP 密钥，返回激活二维码所需信息</summary>
    /// <param name="user">目标用户</param>
    /// <param name="issuer">签发方名称，通常为系统名称，显示在 Authenticator App 中</param>
    /// <returns>TOTP URI（otpauth://totp/...，可直接生成二维码）和 Base32 密钥（手动输入）</returns>
    MfaSetupResult SetupTotp(IUser user, String issuer);

    /// <summary>激活 TOTP：用户用 App 扫码后输入当前 TOTP 验证，通过则将 MFA 标记为已启用并返回备用码</summary>
    /// <param name="user">目标用户</param>
    /// <param name="code">用户输入的 6 位 TOTP 验证码</param>
    /// <returns>激活成功返回一次性备用码列表（10 组）；失败抛出 InvalidOperationException</returns>
    IReadOnlyList<String> ActivateTotp(IUser user, String code);

    /// <summary>禁用 MFA，清除用户所有 MFA 数据</summary>
    /// <param name="user">目标用户</param>
    void DisableMfa(IUser user);

    /// <summary>校验 MFA 验证码（TOTP 6 位或一次性备用码）</summary>
    /// <param name="user">目标用户</param>
    /// <param name="code">用户输入</param>
    /// <returns>校验通过返回 true；用户未开启 MFA 或校验失败返回 false</returns>
    Boolean Verify(IUser user, String code);

    /// <summary>判断指定用户是否已开启 MFA</summary>
    /// <param name="user">目标用户</param>
    /// <returns>已开启返回 true</returns>
    Boolean IsEnabled(IUser user);

    /// <summary>额发 MFA 挂起令牌（登录校验通过账密但 MFA 尚未验证时调用）</summary>
    /// <param name="userId">用户 ID</param>
    /// <returns>不透明令牌字符串，TTL 300 秒</returns>
    String IssuePendingToken(Int32 userId);

    /// <summary>从挂起令牌中解析用户 ID，解析成功后立即删除令牌（防重放）</summary>
    /// <param name="mfaToken">挂起令牌</param>
    /// <returns>用户 ID；令牌无效或已过期返回 0</returns>
    Int32 ConsumePendingToken(String mfaToken);
}

/// <summary>MFA 初始化结果</summary>
public class MfaSetupResult
{
    /// <summary>标准 TOTP URI（otpauth://totp/...）。前端调用 QRCode 库直接扫码</summary>
    public String TotpUri { get; set; }

    /// <summary>Base32 格式的 TOTP 密钥。供用户在 Authenticator App 中手动输入</summary>
    public String Secret { get; set; }
}

/// <summary>MFA 校验入参（/Auth/Login 触发 MFA 流程时使用）</summary>
public class MfaVerifyModel
{
    /// <summary>MFA 待验证令牌（由 Login 接口下发，仅用一次）</summary>
    public String MfaToken { get; set; }

    /// <summary>用户输入的 6 位 TOTP 验证码或 10 位备用码</summary>
    public String Code { get; set; }
}
