using System.Text.Json.Serialization;
using NewLife.Web;

namespace NewLife.Cube.Models;

/// <summary>登录令牌模型。在 <see cref="TokenModel"/> 基础上补充风险自适应图形验证码字段，供登录接口 data 返回</summary>
public class LoginTokenModel : TokenModel
{
    /// <summary>是否需要图形验证码。true 时前端据此展示验证码输入框</summary>
    public Boolean CaptchaRequired { get; set; }

    /// <summary>图片验证码ID。CaptchaRequired 为 true 时有效，校验时需要回传</summary>
    public String CaptchaId { get; set; }

    /// <summary>图片验证码数据（base64 PNG/SVG）。CaptchaRequired 为 true 时有效，前端可直接展示</summary>
    [JsonPropertyName("image")]
    public String CaptchaImage { get; set; }

    /// <summary>获取验证码的接口地址，前端用于刷新验证码</summary>
    public String CaptchaUrl { get; set; }

    /// <summary>二步验证临时令牌。设备开启 MFA 时随业务码 <see cref="CubeCode.MfaRequired"/> 返回，凭它调用 /Mfa/Verify 完成登录</summary>
    public String MfaToken { get; set; }
}
