using System.Text.Json.Serialization;

namespace NewLife.Cube.Models;

/// <summary>服务操作结果</summary>
public class ServiceResult
{
    /// <summary>是否成功</summary>
    public Boolean IsSuccess { get; set; }
    /// <summary>消息</summary>
    public String Message { get; set; } = String.Empty;

    /// <summary>MFA 挂起令牌。非空时表示登录层通过但需要完成二步验证，前端应跳转输入 MFA 验证码页并携此令牌调用 POST /Mfa/Verify</summary>
    public String MfaToken { get; set; }

    /// <summary>是否需要图片验证码。true 时前端应展示验证码输入框，并携带 CaptchaId/CaptchaCode 重试</summary>
    public Boolean CaptchaRequired { get; set; }

    /// <summary>图片验证码ID。CaptchaRequired 为 true 时有效，校验时需要回传</summary>
    public String CaptchaId { get; set; }

    /// <summary>图片验证码数据（base64 PNG/SVG）。CaptchaRequired 为 true 时有效，前端可直接展示</summary>
    [JsonPropertyName("image")]
    public String CaptchaImage { get; set; }
}

/// <summary>服务操作结果</summary>
public class ServiceResult<T> : ServiceResult
{
    /// <summary>数据</summary>
    public T Data { get; set; } = default(T);
} 