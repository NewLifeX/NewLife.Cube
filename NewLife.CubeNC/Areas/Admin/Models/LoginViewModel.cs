using NewLife.Cube.Entity;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>登录视图模型</summary>
public class LoginViewModel
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>Logo</summary>
    public String Logo { get; set; }

    /// <summary>允许登录</summary>
    public Boolean AllowLogin { get; set; }

    /// <summary>允许注册</summary>
    public Boolean AllowRegister { get; set; }

    /// <summary>启用短信验证码登录</summary>
    public Boolean EnableSms { get; set; }

    /// <summary>启用邮箱验证码登录</summary>
    public Boolean EnableMail { get; set; }

    /// <summary>是否启用密码复杂度校验。false 时前端仅要求密码非空，不做复杂度校验</summary>
    public Boolean EnablePasswordComplexity { get; set; }

    /// <summary>密码强度正则。* 表示无限制，前端可用于客户端校验提示</summary>
    public String PasswordStrength { get; set; }

    /// <summary>登录时是否需要图片验证码。CaptchaScene 强制 或 风险自适应触发</summary>
    public Boolean RequireCaptcha { get; set; }

    /// <summary>注册时是否需要图片验证码。CaptchaScene 强制 或 风险自适应触发</summary>
    public Boolean RequireCaptchaRegister { get; set; }

    ///// <summary>自动注册</summary>
    //public Boolean AutoRegister { get; set; }

    /// <summary>登录提示</summary>
    public String LoginTip { get; set; }

    /// <summary>资源地址。指向CDN，如 https://sso.newlifex.com/content/，留空表示使用本地</summary>
    public String ResourceUrl { get; set; }

    /// <summary>返回地址</summary>
    public String ReturnUrl { get; set; }

    /// <summary>OAuth系统集合</summary>
    public IList<OAuthConfig> OAuthItems { get; set; }
}