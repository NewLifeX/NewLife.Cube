using System.Globalization;
using System.Security;
using NewLife.Caching;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>SVG 文字头像生成服务。头像不存在时根据昵称和性别生成文字头像，主流系统（Google、飞书、钉钉、Teams等）均采用此方案</summary>
/// <remarks>
/// 字符提取规则（参考钉钉/飞书/Teams 主流做法）：
/// <list type="bullet">
///   <item>中文姓名（≤N字）：全取；中文姓名（＞N字）：取末尾N字</item>
///   <item>英文含空格（姓名模式）：取第一个词首字母 + 最后一个词首字母，大写</item>
///   <item>英文无空格（用户名模式）：取前N字母并大写</item>
/// </list>
/// SVG 生成为纯字符串拼接（≪1µs），同一用户结果稳定，使用 MemoryCache 缓存 1 小时以减少 GC 压力。
/// </remarks>
public static class SvgAvatarService
{
    #region 颜色常量

    /// <summary>未知性别时按用户ID哈希选取的备用颜色池</summary>
    private static readonly String[] _colors =
    [
        "#607D8B", // 蓝灰
        "#009688", // 青绿
        "#FF9800", // 橙
        "#9C27B0", // 紫
        "#795548", // 棕
        "#00BCD4", // 青
        "#8BC34A", // 浅绿
        "#FF5722", // 深橙
    ];

    /// <summary>男性背景色（蓝色系，参考 Google/Teams 惯例）</summary>
    private const String MaleColor = "#2196F3";

    /// <summary>女性背景色（粉红色系）</summary>
    private const String FemaleColor = "#E91E63";

    #endregion

    #region 主入口

    /// <summary>根据用户信息生成 SVG 文字头像内容，优先从缓存获取</summary>
    /// <param name="user">用户对象，读取显示名、性别、ID</param>
    /// <param name="chars">显示字符数，支持 1 或 2，默认 1</param>
    /// <returns>SVG 字符串</returns>
    public static String Generate(IUser user, Int32 chars = 1)
    {
        // 约束到合法范围，目前支持 1 和 2
        if (chars < 1) chars = 1;
        if (chars > 2) chars = 2;

        // 从缓存获取，key 含 chars 以支持运行时更改配置
        var cacheKey = $"cube:avatar:svg:{user.ID}:{chars}";
        var cached = MemoryCache.Instance.Get<String>(cacheKey);
        if (!cached.IsNullOrEmpty()) return cached;

        var svg = BuildForUser(user, chars);

        // 缓存 1 小时；用户改名/性别后下次请求会命中旧缓存，但头像非敏感数据，延迟可接受
        MemoryCache.Instance.Set(cacheKey, svg, 3600);
        return svg;
    }

    #endregion

    #region 私有实现

    private static String BuildForUser(IUser user, Int32 chars)
    {
        var name = user.DisplayName;
        if (name.IsNullOrEmpty()) name = user.Name;

        var text = ExtractChars(name, chars);

        // 按性别选择背景色；未知性别按用户ID哈希选取，保证同一用户颜色稳定
        var bg = user.Sex switch
        {
            SexKinds.男 => MaleColor,
            SexKinds.女 => FemaleColor,
            _ => _colors[Math.Abs(user.ID) % _colors.Length]
        };

        return BuildSvg(text, bg);
    }

    /// <summary>从显示名提取用于头像的字符串</summary>
    /// <param name="name">显示名称</param>
    /// <param name="chars">期望字符数（1 或 2）</param>
    /// <returns>提取后的文字</returns>
    internal static String ExtractChars(String name, Int32 chars)
    {
        if (name.IsNullOrEmpty()) return "#";
        if (chars <= 1) return GetFirstGrapheme(name);

        // ── 判断名称类型 ──────────────────────────────────────────────
        // 获取所有字素簇
        var elements = GetGraphemes(name);
        if (elements.Count == 0) return "#";

        // 判断是否为中文（CJK 统一汉字 U+4E00-U+9FFF 及扩展区）
        var isCjk = elements.All(IsCjkGrapheme);

        if (isCjk)
        {
            // 中文姓名：≤N字全取，>N字取末尾N个字
            if (elements.Count <= chars)
                return String.Concat(elements);

            return String.Concat(elements.Skip(elements.Count - chars));
        }

        // 英文含空格 → 姓名模式：取第一个词首字母 + 最后一个词首字母（大写）
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var first = Char.ToUpperInvariant(parts[0][0]).ToString();
            var last = Char.ToUpperInvariant(parts[^1][0]).ToString();
            return chars == 1 ? first : first + last;
        }

        // 英文无空格（用户名）：取前 N 字母并大写
        var letters = name.Where(Char.IsLetter).Take(chars).ToList();
        if (letters.Count == 0) return GetFirstGrapheme(name);
        return new String([.. letters]).ToUpperInvariant();
    }

    /// <summary>取字符串第一个 Unicode 字素簇</summary>
    private static String GetFirstGrapheme(String name)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(name);
        return enumerator.MoveNext() ? enumerator.GetTextElement() : "#";
    }

    /// <summary>枚举字符串的所有 Unicode 字素簇</summary>
    private static List<String> GetGraphemes(String name)
    {
        var result = new List<String>();
        var enumerator = StringInfo.GetTextElementEnumerator(name);
        while (enumerator.MoveNext())
            result.Add(enumerator.GetTextElement());
        return result;
    }

    /// <summary>判断一个字素簇是否为 CJK 汉字</summary>
    private static Boolean IsCjkGrapheme(String grapheme)
    {
        if (grapheme.Length == 0) return false;
        var cp = Char.ConvertToUtf32(grapheme, 0);
        // CJK 统一汉字 + 扩展A/B/C/D/E + CJK 兼容汉字
        return (cp >= 0x4E00 && cp <= 0x9FFF)
            || (cp >= 0x3400 && cp <= 0x4DBF)
            || (cp >= 0x20000 && cp <= 0x2A6DF)
            || (cp >= 0xF900 && cp <= 0xFAFF);
    }

    /// <summary>构建 SVG 内容字符串，根据字符数动态调整字号</summary>
    /// <param name="text">中心显示文字</param>
    /// <param name="background">背景色（十六进制）</param>
    /// <returns>SVG 文本</returns>
    private static String BuildSvg(String text, String background)
    {
        // SecurityElement.Escape 防止 XSS（< > & ' "）
        var escaped = SecurityElement.Escape(text);

        // 字号：1字=48px，2字=28px（SVG 100×100 viewBox）
        var fontSize = escaped.Length <= 1 ? 48 : 28;

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 100 100">
              <rect width="100" height="100" fill="{background}" rx="8" ry="8"/>
              <text x="50" y="50" font-size="{fontSize}"
                    font-family="'Microsoft YaHei','PingFang SC',Arial,sans-serif"
                    fill="white" text-anchor="middle" dominant-baseline="central"
                    font-weight="bold">{escaped}</text>
            </svg>
            """;
    }

    #endregion
}
