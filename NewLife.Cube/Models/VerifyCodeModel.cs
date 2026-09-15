using NewLife.Cube.Areas.Admin.Models;

namespace NewLife.Cube.Models;

/// <summary>验证码模型</summary>
public class VerifyCodeModel : ICubeModel
{
    /// <summary>渠道。Sms/Mail等</summary>
    public String Channel { get; set; }

    /// <summary>用户名</summary>
    public String Username { get; set; }

    ///// <summary>验证码</summary>
    //public String Code { get; set; }

    /// <summary>动作</summary>
    public String Action { get; set; }

    // 必须声明为可空（与 LoginModel 的 CaptchaId/CaptchaCode 一致）：项目开启 Nullable(annotations) 且控制器带
    // [ApiController] 时，无默认值的非空引用类型属性会被框架隐式推断为 [Required] 并自动 400，导致“仅需要图片验证码
    // 时才用”的 CaptchaId/CaptchaCode 被当成必填，绑定手机等无图片验证码步骤的发送码被误拦
    // （症状：POST /Auth/SendCode 报 “The CaptchaId field is required.”）。真实必填校验由 SendVerifyCode 内
    // 按 RequireCaptcha(4,…) 配置执行；CaptchaScene=0 时（见 VerifyCodeService）发码不要求图片验证码。
    /// <summary>验证码 ID。调用 /Auth/Captcha 获取，发送验证码时原样回传；仅在发验证码场景需要图片验证码时必填 </summary>
    public String? CaptchaId { get; set; }

    /// <summary>验证码用户输入。仅在发验证码场景需要图片验证码时必填 </summary>
    public String? CaptchaCode { get; set; }
}
