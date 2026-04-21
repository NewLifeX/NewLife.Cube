namespace NewLife.Cube.Services;

/// <summary>图片验证码服务接口。实现该接口可替换内置 SVG 算术题验证码</summary>
/// <remarks>
/// 默认实现为 <see cref="SvgMathCaptchaService"/>，基于纯 SVG 生成算术题，零外部依赖。
/// 注册自定义实现时使用 TryAddSingleton&lt;ICaptchaService, YourImpl&gt;()。
/// </remarks>
public interface ICaptchaService
{
    /// <summary>生成一个新的图片验证码</summary>
    /// <returns>验证码 ID（用于后续校验）和图片数据（SVG/Base64 PNG 均可）</returns>
    CaptchaResult Generate();

    /// <summary>校验用户输入的验证码</summary>
    /// <param name="captchaId">生成时返回的验证码 ID</param>
    /// <param name="code">用户输入</param>
    /// <returns>校验通过返回 true；验证码不存在、过期或输入错误均返回 false</returns>
    Boolean Validate(String captchaId, String code);
}

/// <summary>图片验证码生成结果</summary>
public class CaptchaResult
{
    /// <summary>验证码 ID。校验时需要回传</summary>
    public String CaptchaId { get; set; }

    /// <summary>图片数据。SVG 文本或 data:image/png;base64,... 格式</summary>
    public String Image { get; set; }
}
