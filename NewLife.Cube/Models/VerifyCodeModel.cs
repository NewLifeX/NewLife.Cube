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

    /// <summary>验证码 ID。调用 /Auth/Captcha 获取，发送验证码时原样回传；仅在发验证码场景需要图片验证码时必填 </summary>
    public String CaptchaId { get; set; }

    /// <summary>验证码用户输入。仅在发验证码场景需要图片验证码时必填 </summary>
    public String CaptchaCode { get; set; }
}
