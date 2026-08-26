using NewLife.Caching;
using NewLife.Cube.Common;
using NewLife.Cube.Entity;
using NewLife.Cube.Models;
using NewLife.Log;
using NewLife.Web;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>验证码服务。短信/邮件验证码发送、图片验证码生成与风控判定，支撑验证码登录/注册/找回/绑定等增强认证能力</summary>
/// <param name="smsService">短信服务</param>
/// <param name="mailService">邮件服务</param>
/// <param name="cacheProvider">缓存提供者</param>
/// <param name="captchaService">图片验证码服务</param>
/// <param name="userService">基础用户服务</param>
/// <param name="tenantContext">租户上下文</param>
public class VerifyCodeService(SmsService smsService, MailService mailService, ICacheProvider cacheProvider, ICaptchaService captchaService, UserService userService, ITenantContext tenantContext)
{
    #region 属性
    private readonly ICache _cache = cacheProvider.Cache;
    private readonly ICaptchaService _captcha = captchaService;
    private readonly UserService _userService = userService;
    private readonly ITenantContext _tenantContext = tenantContext;
    #endregion

    #region 图片验证码与风控
    /// <summary>生成图片验证码</summary>
    /// <returns>验证码 ID（校验时回传）和图片数据（base64 PNG）</returns>
    public CaptchaResult GenerateCaptcha() => _captcha.Generate();

    /// <summary>校验图片验证码，校验成功立即失效（防重放）</summary>
    /// <param name="captchaId">验证码ID</param>
    /// <param name="captchaCode">用户输入</param>
    /// <returns>校验通过返回 true</returns>
    public Boolean ValidateCaptcha(String captchaId, String captchaCode) => _captcha.Validate(captchaId, captchaCode);

    /// <summary>判断指定场景是否需要图片验证码。CaptchaScene 强制要求或风险自适应触发</summary>
    /// <param name="scene">场景位掩码，1=登录，2=注册，4=发验证码</param>
    /// <param name="ip">客户端IP</param>
    /// <param name="username">用户名，用于判断账号维度的登录失败记录，可为空</param>
    /// <param name="deviceId">设备ID，可信设备可豁免风险自适应验证码，可为空</param>
    /// <returns>需要验证码返回 true</returns>
    public Boolean RequireCaptcha(Int32 scene, String ip, String username = null, String deviceId = null)
    {
        var set = CubeSetting.Current;

        // 场景强制：管理员明确要求，不受自适应豁免
        if ((set.CaptchaScene & scene) != 0) return true;

        // 风险自适应：仅当开启且非可信设备时按风险评分决定
        if (!set.CaptchaRisk) return false;
        if (!deviceId.IsNullOrEmpty() && _userService.IsTrustedDevice(deviceId, ip)) return false;

        return GetRiskLevel(ip, username) >= set.CaptchaRiskThreshold;
    }

    /// <summary>计算当前请求环境的风险等级。0=内网，1=公网，2=公网+近期登录失败，3=封禁中</summary>
    /// <param name="ip">客户端IP</param>
    /// <param name="username">用户名，用于判断账号维度的登录失败记录，可为空</param>
    /// <returns>风险等级 0~3</returns>
    public Int32 GetRiskLevel(String ip, String username)
    {
        var level = AuthHelper.IsInnerIp(ip) ? 0 : 1;

        // 近期登录失败记录（IP/账号/子网）提升风险
        var fail = _cache.Get<Int32>($"{AuthCacheKeys.PasswordLoginIpPrefix}{ip}");
        if (!username.IsNullOrEmpty()) fail += _cache.Get<Int32>($"{AuthCacheKeys.PasswordLoginUserPrefix}{username}");
        var ip24 = AuthHelper.GetSubnet24(ip);
        var ip16 = AuthHelper.GetSubnet16(ip);
        if (!ip24.IsNullOrEmpty()) fail += _cache.Get<Int32>($"{AuthCacheKeys.LoginIpSubnet24Prefix}{ip24}");
        if (!ip16.IsNullOrEmpty()) fail += _cache.Get<Int32>($"{AuthCacheKeys.LoginIpSubnet16Prefix}{ip16}");
        if (fail > 0) level++;

        // 达到封禁阈值视为高风险
        var set = CubeSetting.Current;
        if (set.MaxLoginError > 0)
        {
            if (_cache.Get<Int32>($"{AuthCacheKeys.PasswordLoginIpPrefix}{ip}") >= set.MaxLoginError) level = 3;
            if (!username.IsNullOrEmpty() && _cache.Get<Int32>($"{AuthCacheKeys.PasswordLoginUserPrefix}{username}") >= set.MaxLoginError) level = 3;
        }

        return level > 3 ? 3 : level;
    }
    #endregion

    #region 发送验证码
    /// <summary>发送登录/注册/找回验证码</summary>
    /// <param name="model">验证码模型</param>
    /// <param name="ip">客户端IP</param>
    /// <returns>验证码记录</returns>
    public async Task<VerifyCodeRecord> SendVerifyCode(VerifyCodeModel model, String ip)
    {
        // 图片验证码校验（场景强制 CaptchaScene 或 风险自适应 CaptchaRisk；发码场景可信设备不豁免，防短信轰炸）
        if (RequireCaptcha(4, ip, model.Username, null))
        {
            if (!_captcha.Validate(model.CaptchaId, model.CaptchaCode))
                throw new XException("图片验证码错误或已过期，请刷新后重试");
        }

        var user = model.Username?.Trim() ?? "";
        if (user.IsNullOrEmpty()) throw new XException("账号不能为空");

        if (model.Channel.EqualIgnoreCase("Mail") || ValidFormatHelper.IsEmail(user))
            return await SendMailCode(model, ip);

        if (model.Channel.EqualIgnoreCase("Sms") || ValidFormatHelper.IsMobile(user))
            return await SendSmsCode(model, ip);

        throw new NotSupportedException();
    }

    /// <summary>短信验证码发送逻辑</summary>
    /// <param name="model">验证码模型</param>
    /// <param name="ip">客户端IP</param>
    /// <returns></returns>
    /// <exception cref="XException"></exception>
    private async Task<VerifyCodeRecord> SendSmsCode(VerifyCodeModel model, String ip)
    {
        var mobile = model.Username?.Trim() ?? "";
        if (mobile.IsNullOrEmpty()) throw new XException("手机号不能为空");

        // 校验手机号格式
        if (!ValidFormatHelper.IsMobile(mobile)) throw new XException("手机号格式不正确");

        // 检查短信服务是否启用
        var set = CubeSetting.Current;
        if (!set.EnableSms) throw new XException("短信验证码功能未启用");

        var config = smsService.GetConfig(_tenantContext.TenantId, model.Action);
        if (config == null) throw new XException("短信服务未配置");

        // 检查短信配置是否完整
        if (config.AppKey.IsNullOrEmpty() || config.AppSecret.IsNullOrEmpty())
            throw new XException("短信AccessKey未配置，请在系统参数中配置AppKey和AppSecret");

        if (config.SignName.IsNullOrEmpty())
            throw new XException("短信签名未配置，请在系统参数中配置SignName");

        // 根据 Action 类型选择缓存 key 前缀
        var (ipPrefix, lastSendPrefix, codePrefix) = GetSmsCachePrefix(model.Action);

        var ipKey = $"{ipPrefix}{ip}";

        // 防止频繁发送（IP限制）
        var ipCount = _cache.Get<Int32>(ipKey);
        if (ipCount >= 5) throw new XException("发送频繁，请稍后再试");

        // 防止频繁发送（手机号限制，60秒内只能发一次）
        var lastSend = _cache.Get<DateTime>($"{lastSendPrefix}{mobile}");
        if (lastSend > DateTime.MinValue && (DateTime.Now - lastSend).TotalSeconds < 60)
        {
            var wait = 60 - (Int32)(DateTime.Now - lastSend).TotalSeconds;
            throw new XException($"请{wait}秒后再试");
        }

        try
        {
            // 发送短信验证码
            var code = SmsService.GenerateVerifyCode();
            var rs = await smsService.SendVerifyCode(model.Action, mobile, code, config);
            if (rs == null || !rs.Success)
                throw new XException("短信发送失败");

            // 缓存验证码用于校验
            var codeKey = $"{codePrefix}{mobile}";
            _cache.Set(codeKey, code, config.Expire);

            // 记录发送时间
            _cache.Set($"{lastSendPrefix}{mobile}", DateTime.Now, 60);

            // 累计IP发送次数
            _cache.Increment(ipKey, 1);
            if (ipCount <= 0) _cache.SetExpire(ipKey, TimeSpan.FromMinutes(10));

            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", true, $"手机号：{mobile}", 0, mobile, ip);

            return rs;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", false, $"手机号：{mobile}，错误：{ex.Message}", 0, mobile, ip);
            throw;
        }
    }

    /// <summary>邮箱发送逻辑</summary>
    /// <param name="model">验证码模型</param>
    /// <param name="ip">客户端IP</param>
    /// <returns></returns>
    /// <exception cref="XException"></exception>
    private async Task<VerifyCodeRecord> SendMailCode(VerifyCodeModel model, String ip)
    {
        var mail = model.Username?.Trim() ?? "";
        if (mail.IsNullOrEmpty()) throw new XException("邮件地址不能为空");

        // 检查邮件服务是否启用
        var set = CubeSetting.Current;
        if (!set.EnableMail) throw new XException("邮件验证码功能未启用");

        var config = mailService.GetConfig(_tenantContext.TenantId, model.Action);

        // 根据 Action 类型选择缓存 key 前缀
        var (ipPrefix, lastSendPrefix, codePrefix) = GetMailCachePrefix(model.Action);

        var ipKey = $"{ipPrefix}{ip}";

        // 防止频繁发送（IP限制）
        var ipCount = _cache.Get<Int32>(ipKey);
        if (ipCount >= 5) throw new XException("发送频繁，请稍后再试");

        // 防止频繁发送
        var lastSend = _cache.Get<DateTime>($"{lastSendPrefix}{mail}");
        if (lastSend > DateTime.MinValue && (DateTime.Now - lastSend).TotalSeconds < 60)
        {
            var wait = 60 - (Int32)(DateTime.Now - lastSend).TotalSeconds;
            throw new XException($"请{wait}秒后再试");
        }

        try
        {
            // 发送邮件验证码
            var code = MailService.GenerateVerifyCode();
            var rs = await mailService.SendVerifyCode(model.Action, mail, code, config);
            if (rs == null || !rs.Success) throw new XException("邮件发送失败");

            // 缓存验证码用于校验
            var codeKey = $"{codePrefix}{mail}";
            _cache.Set(codeKey, code, config.Expire);

            // 记录发送时间
            _cache.Set($"{lastSendPrefix}{mail}", DateTime.Now, 60);

            // 累计IP发送次数
            _cache.Increment(ipKey, 1);
            if (ipCount <= 0) _cache.SetExpire(ipKey, TimeSpan.FromMinutes(10));

            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", true, $"邮箱：{mail}", 0, mail, ip);

            return rs;
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
            LogProvider.Provider.WriteLog(typeof(User), "发送验证码", false, $"邮箱：{mail}，错误：{ex.Message}", 0, mail, ip);
            throw;
        }
    }

    /// <summary>根据Action获取短信缓存前缀</summary>
    /// <param name="action">操作类型：login/bind/reset</param>
    /// <returns>IP前缀、最后发送前缀、验证码前缀</returns>
    private (String ipPrefix, String lastSendPrefix, String codePrefix) GetSmsCachePrefix(String action)
    {
        return action?.ToLower() switch
        {
            "bind" => (AuthCacheKeys.SmsBindIpPrefix, AuthCacheKeys.SmsBindLastSendPrefix, AuthCacheKeys.SmsBindCodePrefix),
            "reset" => (AuthCacheKeys.SmsResetIpPrefix, AuthCacheKeys.SmsResetLastSendPrefix, AuthCacheKeys.SmsResetCodePrefix),
            "register" => (AuthCacheKeys.SmsRegisterIpPrefix, AuthCacheKeys.SmsRegisterLastSendPrefix, AuthCacheKeys.SmsRegisterCodePrefix),
            "login" => (AuthCacheKeys.SmsLoginIpPrefix, AuthCacheKeys.SmsLoginLastSendPrefix, AuthCacheKeys.SmsLoginCodePrefix),
            "notify" => (AuthCacheKeys.SmsNotifyIpPrefix, AuthCacheKeys.SmsNotifyLastSendPrefix, AuthCacheKeys.SmsNotifyCodePrefix),
            _ => (AuthCacheKeys.SmsNotifyIpPrefix, AuthCacheKeys.SmsNotifyLastSendPrefix, AuthCacheKeys.SmsNotifyCodePrefix),
        };
    }

    /// <summary>根据Action获取邮件缓存前缀</summary>
    /// <param name="action">操作类型：login/bind/reset</param>
    /// <returns>IP前缀、最后发送前缀、验证码前缀</returns>
    private (String ipPrefix, String lastSendPrefix, String codePrefix) GetMailCachePrefix(String action)
    {
        return action?.ToLower() switch
        {
            "bind" => (AuthCacheKeys.MailBindIpPrefix, AuthCacheKeys.MailBindLastSendPrefix, AuthCacheKeys.MailBindCodePrefix),
            "reset" => (AuthCacheKeys.MailResetIpPrefix, AuthCacheKeys.MailResetLastSendPrefix, AuthCacheKeys.MailResetCodePrefix),
            "register" => (AuthCacheKeys.MailRegisterIpPrefix, AuthCacheKeys.MailRegisterLastSendPrefix, AuthCacheKeys.MailRegisterCodePrefix),
            "login" => (AuthCacheKeys.MailLoginIpPrefix, AuthCacheKeys.MailLoginLastSendPrefix, AuthCacheKeys.MailLoginCodePrefix),
            "notify" => (AuthCacheKeys.MailNotifyIpPrefix, AuthCacheKeys.MailNotifyLastSendPrefix, AuthCacheKeys.MailNotifyCodePrefix),
            _ => (AuthCacheKeys.MailNotifyIpPrefix, AuthCacheKeys.MailNotifyLastSendPrefix, AuthCacheKeys.MailNotifyCodePrefix),
        };
    }
    #endregion
}
