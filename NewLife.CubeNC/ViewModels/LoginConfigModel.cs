using NewLife.Common;
using NewLife.Cube.Entity;
using XCode.Membership;

namespace NewLife.Cube.ViewModels;

/// <summary>登录能力配置</summary>
public class LoginAbilityModel
{
    /// <summary>允许密码登录</summary>
    public Boolean Password { get; set; }

    /// <summary>允许短信验证码登录</summary>
    public Boolean Sms { get; set; }

    /// <summary>允许邮箱验证码登录</summary>
    public Boolean Mail { get; set; }

    /// <summary>登录时需要图片验证码</summary>
    public Boolean Captcha { get; set; }
}

/// <summary>注册能力配置</summary>
public class RegisterAbilityModel
{
    /// <summary>是否允许注册</summary>
    public Boolean Enabled { get; set; }

    /// <summary>允许用户名密码注册</summary>
    public Boolean Password { get; set; }

    /// <summary>允许手机验证码注册</summary>
    public Boolean Sms { get; set; }

    /// <summary>允许邮箱验证码注册</summary>
    public Boolean Mail { get; set; }

    /// <summary>注册时需要图片验证码</summary>
    public Boolean Captcha { get; set; }
}

/// <summary>安全策略配置</summary>
public class SecurityConfigModel
{
    /// <summary>是否要求 Challenge-Response 加密传输密码。false 时允许明文（兼容旧版）</summary>
    public Boolean ChallengeRequired { get; set; }

    /// <summary>是否开放 MFA 功能。true 时允许用户在个人设置中开启 TOTP</summary>
    public Boolean MfaAvailable { get; set; }
}

/// <summary>OAuth 提供商模型</summary>
public class OAuthProviderModel
{
    /// <summary>应用名</summary>
    public String Name { get; set; }

    /// <summary>图标地址</summary>
    public String Logo { get; set; }

    /// <summary>显示名</summary>
    public String NickName { get; set; }
}

/// <summary>登录配置模型（支持多租户）</summary>
public class LoginConfigModel
{
    private readonly CubeSetting _set;
    private readonly SysConfig _sys;
    private readonly Tenant _tenant;

    /// <summary>实例化登录配置模型</summary>
    /// <param name="tenant">当前租户，为 null 时读取全局配置</param>
    public LoginConfigModel(Tenant tenant = null)
    {
        _set = CubeSetting.Current;
        _sys = SysConfig.Current;
        _tenant = tenant;
    }

    /// <summary>租户Code。有租户时返回租户标识，无租户时为空</summary>
    public String Code => _tenant?.Code;

    /// <summary>系统名称。租户级 Name 优先，回退到全局 DisplayName</summary>
    public String Name => !_tenant?.Name.IsNullOrEmpty() == true ? _tenant.Name : _sys.DisplayName;

    /// <summary>版权信息。动态替换 {now:yyyy} 等变量，留空不显示</summary>
    public String Copyright => _set.GetCopyright();

    /// <summary>备案号。留空不显示</summary>
    public String Registration => _set.Registration;

    /// <summary>Logo图标地址。留空时由前端皮肤使用内置默认</summary>
    public String Logo => String.Empty;

    /// <summary>登录提示。租户级 Remark 优先，回退到全局 LoginTip</summary>
    public String LoginTip => _set.LoginTip;

    /// <summary>登录页 Logo。留空时由前端皮肤使用内置默认</summary>
    public String LoginLogo => _set.LoginLogo;

    /// <summary>登录页背景图。留空时由前端皮肤使用内置默认</summary>
    public String LoginBackground => _set.LoginBackground;

    /// <summary>登录能力配置</summary>
    public LoginAbilityModel Login => new()
    {
        Password = _set.AllowLogin,
        Sms = _set.EnableSms,
        Mail = _set.EnableMail,
        Captcha = (_set.CaptchaScene & 1) != 0,
    };

    /// <summary>注册能力配置</summary>
    public RegisterAbilityModel Register => new()
    {
        Enabled = _set.AllowRegister,
        Password = _set.AllowRegister && _set.AllowLogin,
        Sms = _set.AllowRegister && _set.EnableSms,
        Mail = _set.AllowRegister && _set.EnableMail,
        Captcha = (_set.CaptchaScene & 2) != 0,
    };

    /// <summary>OAuth 提供商列表。仅返回当前租户可见的提供商</summary>
    public List<OAuthProviderModel> OAuth =>
        OAuthConfig.GetVisibles(_tenant != null ? _tenant.Id : TenantContext.CurrentId).Select(s => new OAuthProviderModel
        {
            Name = s.Name,
            Logo = s.Logo,
            NickName = s.NickName,
        }).ToList();

    /// <summary>安全策略配置</summary>
    public SecurityConfigModel Security => new()
    {
        ChallengeRequired = !_set.AllowPlainPassword,
        MfaAvailable = _set.EnableMfa,
    };
}
