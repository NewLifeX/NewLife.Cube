namespace NewLife.Cube.Services;

/// <summary>认证缓存键。集中管理登录/验证码/风控等缓存前缀，供基础与增强认证服务共享</summary>
internal static class AuthCacheKeys
{
    #region 密码登录防爆破
    /// <summary>密码登录用户名错误次数缓存前缀</summary>
    public const String PasswordLoginUserPrefix = "CubeLogin:";
    /// <summary>密码登录IP错误次数缓存前缀。与用户名前缀区分，避免用户名恰为IP字符串时缓存键冲突</summary>
    public const String PasswordLoginIpPrefix = "CubeLogin:IP:";
    /// <summary>登录IP三段子网错误次数缓存前缀（/24 段，如103.125.146.*）</summary>
    public const String LoginIpSubnet24Prefix = "CubeLogin:subnet24:";
    /// <summary>登录IP两段子网错误次数缓存前缀（/16 段，如103.125.*.*）</summary>
    public const String LoginIpSubnet16Prefix = "CubeLogin:subnet16:";
    #endregion

    #region 短信验证码
    /// <summary>短信登录IP发送限制缓存前缀</summary>
    public const String SmsLoginIpPrefix = "SmsLogin:IP:";
    /// <summary>短信登录最后发送时间缓存前缀</summary>
    public const String SmsLoginLastSendPrefix = "SmsLogin:LastSend:";
    /// <summary>短信登录验证码缓存前缀</summary>
    public const String SmsLoginCodePrefix = "SmsLogin:Code:";
    /// <summary>短信登录错误次数缓存前缀</summary>
    public const String SmsLoginErrorPrefix = "SmsLogin:Error:";
    /// <summary>短信登录IP错误次数缓存前缀</summary>
    public const String SmsLoginErrorIpPrefix = "SmsLogin:Error:IP:";
    /// <summary>短信绑定手机IP发送限制缓存前缀</summary>
    public const String SmsBindIpPrefix = "SmsBind:IP:";
    /// <summary>短信绑定手机最后发送时间缓存前缀</summary>
    public const String SmsBindLastSendPrefix = "SmsBind:LastSend:";
    /// <summary>短信绑定手机验证码缓存前缀</summary>
    public const String SmsBindCodePrefix = "SmsBind:Code:";
    /// <summary>短信重置密码IP发送限制缓存前缀</summary>
    public const String SmsResetIpPrefix = "SmsReset:IP:";
    /// <summary>短信重置密码最后发送时间缓存前缀</summary>
    public const String SmsResetLastSendPrefix = "SmsReset:LastSend:";
    /// <summary>短信重置密码验证码缓存前缀</summary>
    public const String SmsResetCodePrefix = "SmsReset:Code:";
    /// <summary>短信注册IP发送限制缓存前缀</summary>
    public const String SmsRegisterIpPrefix = "SmsRegister:IP:";
    /// <summary>短信注册最后发送时间缓存前缀</summary>
    public const String SmsRegisterLastSendPrefix = "SmsRegister:LastSend:";
    /// <summary>短信注册验证码缓存前缀</summary>
    public const String SmsRegisterCodePrefix = "SmsRegister:Code:";
    /// <summary>短信通知IP发送限制缓存前缀</summary>
    public const String SmsNotifyIpPrefix = "SmsNotify:IP:";
    /// <summary>短信通知最后发送时间缓存前缀</summary>
    public const String SmsNotifyLastSendPrefix = "SmsNotify:LastSend:";
    /// <summary>短信通知验证码缓存前缀</summary>
    public const String SmsNotifyCodePrefix = "SmsNotify:Code:";
    /// <summary>短信激活IP发送限制缓存前缀</summary>
    public const String SmsActivateIpPrefix = "SmsActivate:IP:";
    /// <summary>短信激活最后发送时间缓存前缀</summary>
    public const String SmsActivateLastSendPrefix = "SmsActivate:LastSend:";
    /// <summary>短信激活验证码缓存前缀</summary>
    public const String SmsActivateCodePrefix = "SmsActivate:Code:";
    #endregion

    #region 邮件验证码
    /// <summary>邮件登录IP发送限制缓存前缀</summary>
    public const String MailLoginIpPrefix = "MailLogin:IP:";
    /// <summary>邮件登录最后发送时间缓存前缀</summary>
    public const String MailLoginLastSendPrefix = "MailLogin:LastSend:";
    /// <summary>邮件登录验证码缓存前缀</summary>
    public const String MailLoginCodePrefix = "MailLogin:Code:";
    /// <summary>邮件登录错误次数缓存前缀</summary>
    public const String MailLoginErrorPrefix = "MailLogin:Error:";
    /// <summary>邮件登录IP错误次数缓存前缀</summary>
    public const String MailLoginErrorIpPrefix = "MailLogin:Error:IP:";
    /// <summary>邮件绑定IP发送限制缓存前缀</summary>
    public const String MailBindIpPrefix = "MailBind:IP:";
    /// <summary>邮件绑定最后发送时间缓存前缀</summary>
    public const String MailBindLastSendPrefix = "MailBind:LastSend:";
    /// <summary>邮件绑定验证码缓存前缀</summary>
    public const String MailBindCodePrefix = "MailBind:Code:";
    /// <summary>邮件重置密码IP发送限制缓存前缀</summary>
    public const String MailResetIpPrefix = "MailReset:IP:";
    /// <summary>邮件重置密码最后发送时间缓存前缀</summary>
    public const String MailResetLastSendPrefix = "MailReset:LastSend:";
    /// <summary>邮件重置密码验证码缓存前缀</summary>
    public const String MailResetCodePrefix = "MailReset:Code:";
    /// <summary>邮件注册IP发送限制缓存前缀</summary>
    public const String MailRegisterIpPrefix = "MailRegister:IP:";
    /// <summary>邮件注册最后发送时间缓存前缀</summary>
    public const String MailRegisterLastSendPrefix = "MailRegister:LastSend:";
    /// <summary>邮件注册验证码缓存前缀</summary>
    public const String MailRegisterCodePrefix = "MailRegister:Code:";
    /// <summary>邮件通知IP发送限制缓存前缀</summary>
    public const String MailNotifyIpPrefix = "MailNotify:IP:";
    /// <summary>邮件通知最后发送时间缓存前缀</summary>
    public const String MailNotifyLastSendPrefix = "MailNotify:LastSend:";
    /// <summary>邮件通知验证码缓存前缀</summary>
    public const String MailNotifyCodePrefix = "MailNotify:Code:";
    /// <summary>邮件激活IP发送限制缓存前缀</summary>
    public const String MailActivateIpPrefix = "MailActivate:IP:";
    /// <summary>邮件激活最后发送时间缓存前缀</summary>
    public const String MailActivateLastSendPrefix = "MailActivate:LastSend:";
    /// <summary>邮件激活令牌缓存前缀</summary>
    public const String MailActivateCodePrefix = "MailActivate:Code:";
    /// <summary>邮件激活链接令牌缓存前缀。一次性，用于激活链接校验</summary>
    public const String MailActivateLinkPrefix = "MailActivate:Link:";
    #endregion
}
