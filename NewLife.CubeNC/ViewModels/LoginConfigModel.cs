using NewLife.Common;
using NewLife.Cube.Entity;
using NewLife.Reflection;
using XCode.Membership;

namespace NewLife.Cube.ViewModels;

/// <summary>登录配置模型</summary>
public class LoginConfigModel
{
    private readonly CubeSetting _set = CubeSetting.Current;
    private readonly SysConfig _cubeSet = SysConfig.Current;

    /// <summary>显示名</summary>
    public String DisplayName => _cubeSet.DisplayName;

    /// <summary>Logo图标</summary>
    public String Logo => String.Empty;

    /// <summary>允许登录</summary>
    public Boolean AllowLogin => _set.AllowLogin;

    /// <summary>允许注册</summary>
    public Boolean AllowRegister => _set.AllowRegister;
    //public Boolean AutoRegister => _set.AutoRegister;

    /// <summary>启用短信。包括短信验证码和系统通知</summary>
    public Boolean EnableSms => _set.EnableSms;

    /// <summary>启用短信注册</summary>
    public Boolean EnableSmsRegister => _set.EnableSms;

    /// <summary>启用邮件。包括邮件验证码和系统通知</summary>
    public Boolean EnableMail => _set.EnableMail;

    /// <summary>启用邮件注册</summary>
    public Boolean EnableMailRegister => _set.EnableMail;

    /// <summary>登录提示</summary>
    public String LoginTip => _set.LoginTip;

    /// <summary>提供者</summary>
    public List<OAuthConfigModel> Providers =>
        OAuthConfig.GetVisibles(TenantContext.CurrentId).Select(s =>
        {
            var m = new OAuthConfigModel();
            m.Copy(s);
            return m;
        }).ToList();
}

/// <summary>站点信息模型</summary>
public class SiteInfoModel
{
    private readonly CubeSetting _set = CubeSetting.Current;
    private readonly SysConfig _sys = SysConfig.Current;

    /// <summary>站点名称</summary>
    public String DisplayName => _sys.DisplayName;

    /// <summary>版权。动态替换 {now:yyyy} 等变量</summary>
    public String Copyright => _set.GetCopyright();

    /// <summary>备案号</summary>
    public String Registration => _set.Registration;

    /// <summary>登录提示</summary>
    public String LoginTip => _set.LoginTip;

    /// <summary>登录页Logo。留空时由前端皮肤使用内置默认</summary>
    public String LoginLogo => _set.LoginLogo;

    /// <summary>登录页背景图。留空时由前端皮肤使用内置默认</summary>
    public String LoginBackground => _set.LoginBackground;

    /// <summary>Logo图标</summary>
    public String Logo => String.Empty;
}

/// <summary>OAuth配置模型</summary>
public class OAuthConfigModel
{
    /// <summary>应用名</summary>
    public String Name { get; set; }

    /// <summary>图标</summary>
    public String Logo { get; set; }

    /// <summary>显示名</summary>
    public String NickName { get; set; }
}
