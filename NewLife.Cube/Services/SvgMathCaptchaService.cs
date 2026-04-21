using NewLife.Caching;
using NewLife.Collections;

namespace NewLife.Cube.Services;

/// <summary>内置 SVG 算术题图片验证码服务。零外部依赖，使用纯 SVG 文本拼接生成干扰线与字符</summary>
/// <remarks>
/// 每次调用 <see cref="Generate"/> 会在缓存中写入一条 TTL=300s 的键值对，key 为 UUID，value 为运算结果字符串。
/// 可通过注册 <see cref="ICaptchaService"/> 自定义实现来替换本服务。
/// </remarks>
public class SvgMathCaptchaService : ICaptchaService
{
    private const String CachePrefix = "Captcha:";
    private const Int32 TtlSeconds = 300;

    private static readonly Random _rnd = new();
    private readonly ICache _cache;

    /// <summary>初始化</summary>
    /// <param name="cacheProvider">缓存提供者</param>
    public SvgMathCaptchaService(ICacheProvider cacheProvider) => _cache = cacheProvider.Cache;

    /// <inheritdoc/>
    public CaptchaResult Generate()
    {
        // 生成算术题（加法或减法，结果 1-20）
        var a = _rnd.Next(1, 15);
        var b = _rnd.Next(1, 10);
        String expr;
        Int32 answer;
        if (a >= b)
        {
            // 加法或减法随机
            if (_rnd.Next(2) == 0) { expr = $"{a} + {b}"; answer = a + b; }
            else { expr = $"{a} - {b}"; answer = a - b; }
        }
        else
        {
            expr = $"{a} + {b}";
            answer = a + b;
        }

        var captchaId = Guid.NewGuid().ToString("N");
        _cache.Set($"{CachePrefix}{captchaId}", answer.ToString(), TtlSeconds);

        return new CaptchaResult
        {
            CaptchaId = captchaId,
            Image = BuildSvg(expr),
        };
    }

    /// <inheritdoc/>
    public Boolean Validate(String captchaId, String code)
    {
        if (captchaId.IsNullOrEmpty() || code.IsNullOrEmpty()) return false;

        var key = $"{CachePrefix}{captchaId}";
        var stored = _cache.Get<String>(key);
        if (stored.IsNullOrEmpty()) return false;

        // 比对后立即删除，防止重放
        _cache.Remove(key);
        return String.Equals(stored.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    #region 辅助

    /// <summary>生成含干扰线、随机旋转字符的 SVG 图像</summary>
    /// <param name="text">要显示的文字（如 "3 + 5 ="）</param>
    /// <returns>SVG 文本</returns>
    private static String BuildSvg(String text)
    {
        const Int32 W = 120, H = 40;
        var sb = Pool.StringBuilder.Get();
        try
        {
            sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{W}' height='{H}' viewBox='0 0 {W} {H}'>");
            // 背景
            sb.Append($"<rect width='{W}' height='{H}' fill='#f5f5f5'/>");

            // 干扰线（3 条）
            for (var i = 0; i < 3; i++)
            {
                var x1 = _rnd.Next(0, W / 3);
                var y1 = _rnd.Next(0, H);
                var x2 = _rnd.Next(W * 2 / 3, W);
                var y2 = _rnd.Next(0, H);
                var color = $"#{_rnd.Next(0x80, 0xD0):x2}{_rnd.Next(0x80, 0xD0):x2}{_rnd.Next(0x80, 0xD0):x2}";
                sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='1'/>");
            }

            // 文字（每个字符单独定位，轻微旋转）
            var display = text + " =";
            var charWidth = (W - 10) / (display.Length > 0 ? display.Length : 1);
            for (var i = 0; i < display.Length; i++)
            {
                var cx = 8 + i * charWidth + charWidth / 2;
                var cy = H / 2 + 5;
                var rotate = _rnd.Next(-12, 13);
                var fontSize = _rnd.Next(16, 22);
                var fgColor = $"#{_rnd.Next(0x10, 0x60):x2}{_rnd.Next(0x10, 0x60):x2}{_rnd.Next(0x10, 0x60):x2}";
                sb.Append($"<text x='{cx}' y='{cy}' text-anchor='middle' fill='{fgColor}' font-size='{fontSize}' " +
                          $"font-family='monospace' transform='rotate({rotate},{cx},{cy})'>{display[i]}</text>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }
        finally
        {
            Pool.StringBuilder.Put(sb);
        }
    }

    #endregion
}
