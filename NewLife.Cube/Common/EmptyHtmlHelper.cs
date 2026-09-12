using System.Text.RegularExpressions;

namespace NewLife.Cube;

/// <summary>空富文本帮助。识别富文本编辑器遗留的无内容 HTML（如 &lt;p&gt;&lt;br&gt;&lt;/p&gt;），用于 SSO 同步进出双向过滤，避免空备注在系统间扩散</summary>
/// <remarks>
/// XCode 侧（XCode.Membership.EmptyHtmlHelper）已在用户、部门实体保存时自动清理同类脏值；
/// 魔方此处用于 SSO 沟通两端：服务端 TokenService 出口过滤 detail，客户端 UserBindingService.Fill 入口过滤 detail。
/// </remarks>
public static class EmptyHtmlHelper
{
    private static readonly String[] _EmbedTags = ["<img", "<iframe", "<video", "<audio", "<object", "<embed", "<svg"];

    private static readonly Regex _TagRegex = new("<[^>]*>", RegexOptions.Compiled);

    /// <summary>判断 HTML 是否语义为空。仅含空段落、换行、空白等无内容标签时视为空；含图片、音视频等嵌入内容的不算空</summary>
    /// <param name="html">HTML 字符串</param>
    /// <returns>是否语义为空</returns>
    public static Boolean IsEmptyHtml(String html)
    {
        if (html.IsNullOrEmpty()) return false;

        // 含图片、音视频、附件等嵌入内容的不算空
        foreach (var tag in _EmbedTags)
        {
            if (html.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0) return false;
        }

        // 去掉全部标签与占位空白后无可见文本，则视为空
        var text = _TagRegex.Replace(html, "").Replace("&nbsp;", "").Replace("&#160;", "");
        return text.IsNullOrWhiteSpace();
    }
}
