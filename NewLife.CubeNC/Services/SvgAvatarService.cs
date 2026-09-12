using System.Globalization;
using System.Security;
using NewLife.Caching;
using XCode.Membership;

namespace NewLife.Cube.Services;

/// <summary>SVG 文字头像生成服务。头像不存在时根据昵称生成文字头像，主流系统（Google、飞书、钉钉、Teams等）均采用此方案</summary>
/// <remarks>
/// 字符提取规则（参考钉钉/飞书/Teams 主流做法）：
/// <list type="bullet">
///   <item>中文姓名（≤N字）：全取；中文姓名（＞N字）：取前N字，保留姓氏，如"张三丰"取"张三"</item>
///   <item>英文含空格（姓名模式）：取第一个词首字母 + 最后一个词首字母，大写</item>
///   <item>英文无空格（用户名模式）：取前N字母并大写</item>
/// </list>
/// 视觉采用现代主流设计：主色到深色渐变背景 + 半透明装饰圆 + 白色粗体文字，字号随字符数动态调整。
/// SVG 生成为纯字符串拼接（≪1µs），同一用户结果稳定，使用 MemoryCache 缓存 1 小时以减少 GC 压力。
/// </remarks>
public static class SvgAvatarService
{
    #region 颜色常量

    /// <summary>渐变配色池（亮端→暗端）。全用户按用户ID哈希选取，保证同一用户颜色稳定，对齐 ui-avatars 等主流文字头像按名称/ID 取色的做法</summary>
    private static readonly (String Light, String Dark)[] _colors =
    [
        ("#F44336", "#B71C1C"), // 红
        ("#E91E63", "#880E4F"), // 粉
        ("#9C27B0", "#4A148C"), // 紫
        ("#673AB7", "#311B92"), // 深紫
        ("#3F51B5", "#1A237E"), // 靛蓝
        ("#2196F3", "#0D47A1"), // 蓝
        ("#00BCD4", "#006064"), // 青
        ("#009688", "#004D40"), // 青绿
        ("#4CAF50", "#1B5E20"), // 绿
        ("#8BC34A", "#33691E"), // 浅绿
        ("#FFC107", "#FF6F00"), // 琥珀
        ("#FF9800", "#E65100"), // 橙
        ("#FF5722", "#BF360C"), // 深橙
        ("#795548", "#3E2723"), // 棕
        ("#607D8B", "#263238"), // 蓝灰
        ("#9E9E9E", "#424242"), // 灰
    ];

    #endregion

    #region 主入口

    /// <summary>根据用户信息生成 SVG 文字头像内容，优先从缓存获取</summary>
    /// <param name="user">用户对象，读取显示名、ID</param>
    /// <param name="chars">显示字符数，支持 1~3，默认 1</param>
    /// <returns>SVG 字符串</returns>
    public static String Generate(IUser user, Int32 chars = 1)
    {
        // 约束到合法范围，目前支持 1~3
        if (chars < 1) chars = 1;
        if (chars > 3) chars = 3;

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

        // 全用户按用户ID哈希从调色板取色，保证同一用户颜色稳定（主流文字头像服务按名称/ID 哈希取色，不按性别）
        // Math.Abs(Int32) 对 Int32.MinValue 会溢出抛异常，用 Int64 取模避免极端边界崩溃
        var (light, dark) = _colors[Math.Abs((Int64)user.ID) % _colors.Length];

        return BuildSvg(text, light, dark);
    }

    /// <summary>从显示名提取用于头像的字符串</summary>
    /// <param name="name">显示名称</param>
    /// <param name="chars">期望字符数（1~3）</param>
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
            // 中文姓名：≤N字全取，>N字取前N个字（保留姓氏，符合主流习惯）
            if (elements.Count <= chars)
                return String.Concat(elements);

            return String.Concat(elements.Take(chars));
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
        // 孤立代理无法 ConvertToUtf32（抛 ArgumentException），直接按 char 值判断
        var cp = grapheme.Length >= 2 && Char.IsSurrogatePair(grapheme, 0)
            ? Char.ConvertToUtf32(grapheme, 0)
            : grapheme[0];
        // CJK 统一汉字 + 扩展A/B/C/D/E + CJK 兼容汉字
        return (cp >= 0x4E00 && cp <= 0x9FFF)
            || (cp >= 0x3400 && cp <= 0x4DBF)
            || (cp >= 0x20000 && cp <= 0x2A6DF)
            || (cp >= 0xF900 && cp <= 0xFAFF);
    }

    #endregion

    #region 文件辅助

    /// <summary>头像候选扩展名。按优先顺序尝试，找到第一个存在的文件即返回</summary>
    private static readonly String[] _avatarExts = [".png", ".svg", ".jpg", ".gif", ".webp"];

    /// <summary>根据扩展名返回对应的 MIME Content-Type</summary>
    /// <param name="ext">文件扩展名，含点号，如 ".svg"</param>
    /// <returns>MIME 类型字符串</returns>
    public static String GetContentType(String ext) => ext?.ToLowerInvariant() switch
    {
        ".svg" => "image/svg+xml",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/png",
    };

    /// <summary>在头像目录中按扩展名优先级查找指定用户的头像文件</summary>
    /// <param name="avatarPath">头像根目录</param>
    /// <param name="userId">用户编号</param>
    /// <returns>找到时返回 (绝对路径, contentType)，未找到返回 (null, null)</returns>
    public static (String Path, String ContentType) FindAvatarFile(String avatarPath, Int32 userId)
    {
        if (avatarPath.IsNullOrEmpty()) return (null, null);

        foreach (var ext in _avatarExts)
        {
            var path = avatarPath.CombinePath(userId + ext).GetBasePath();
            if (System.IO.File.Exists(path))
                return (path, GetContentType(ext));
        }

        return (null, null);
    }

    #endregion

    #region SVG 构建

    /// <summary>构建 SVG 内容字符串，渐变背景 + 装饰圆 + 动态字号</summary>
    /// <param name="text">中心显示文字</param>
    /// <param name="light">渐变亮端色（十六进制）</param>
    /// <param name="dark">渐变暗端色（十六进制）</param>
    /// <returns>SVG 文本</returns>
    private static String BuildSvg(String text, String light, String dark)
    {
        // 字号随字符数动态调整：CJK 1字=46px，2字=30px，3字=22px（SVG 100×100 viewBox）
        // 纯拉丁（ASCII）字母宽度约为汉字一半，2 字母视觉偏空，字号适当上调保持视觉重量均衡
        var count = new StringInfo(text).LengthInTextElements;
        var isLatin = text.All(c => c < 128);
        var fontSize = isLatin
            ? count switch { 1 => 50, 2 => 36, _ => 28 }
            : count switch { 1 => 46, 2 => 30, _ => 22 };

        // SecurityElement.Escape 防止 XSS（< > & ' "）
        var escaped = SecurityElement.Escape(text);

        // 渐变背景 + 左上/右下半透明装饰圆，参考飞书/Figma 等现代主流设计
        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 100 100">
              <defs>
                <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0%" stop-color="{light}"/>
                  <stop offset="100%" stop-color="{dark}"/>
                </linearGradient>
              </defs>
              <rect width="100" height="100" fill="url(#bg)" rx="14" ry="14"/>
              <circle cx="4" cy="4" r="54" fill="#FFFFFF" opacity="0.13"/>
              <circle cx="98" cy="98" r="28" fill="#FFFFFF" opacity="0.09"/>
              <text x="50" y="50" font-size="{fontSize}"
                    font-family="'Microsoft YaHei','PingFang SC',Arial,sans-serif"
                    fill="#FFFFFF" text-anchor="middle" dominant-baseline="central"
                    font-weight="600">{escaped}</text>
            </svg>
            """;
    }

    #endregion
}
