using NewLife.Cube.ViewModels;

namespace NewLife.Cube;

/// <summary>配置字段本地化助手</summary>
/// <remarks>
/// 配置字段元数据来自静态缓存，缓存中保留英文源文；
/// 本助手在每次请求输出时克隆并按当前请求语言翻译，避免缓存冻结语言。
/// </remarks>
internal static class ConfigFieldLocalizer
{
    /// <summary>本地化配置字段集合</summary>
    /// <param name="fields">原始字段集合</param>
    /// <returns>翻译后的字段副本集合</returns>
    public static IList<DataField> Localize(IList<DataField> fields)
    {
        if (fields == null) return [];

        var list = new List<DataField>(fields.Count);
        foreach (var field in fields)
        {
            var df = field.Clone();
            var display = field.DisplayName;
            var description = field.Description;
            if (description.IsNullOrEmpty() && !display.IsNullOrEmpty())
            {
                // Setting/SocketSetting 只有 [Description]，GetMembers 把整段描述放进了 DisplayName
                var text = L.T(display);
                var (name, desc) = SplitFirstSentence(text);
                df.DisplayName = name;
                df.Description = desc;
            }
            else
            {
                if (!display.IsNullOrEmpty()) df.DisplayName = L.T(display);
                if (!description.IsNullOrEmpty()) df.Description = L.T(description);
            }
            list.Add(df);
        }

        return list;
    }

    /// <summary>从描述中取首句作为显示名</summary>
    /// <param name="description">描述文本</param>
    /// <returns>显示名；空描述返回空字符串</returns>
    public static String GetDisplayName(String description) => SplitFirstSentence(description).Display;

    /// <summary>按句末标点拆分显示名和描述</summary>
    /// <param name="text">完整描述</param>
    /// <returns>显示名与剩余描述</returns>
    public static (String Display, String Description) SplitFirstSentence(String text)
    {
        if (text.IsNullOrEmpty()) return (text, null);

        var index = FindSentenceEnd(text);
        if (index > 0)
        {
            var display = text[..index];
            var description = text[(index + 1)..].Trim();
            return (display, description.IsNullOrEmpty() ? null : description);
        }

        // 泰语等无句末标点的长描述，取首个空格前的短语作为显示名；短标签整体返回
        if (text.Length > 40)
        {
            var space = text.IndexOf(' ');
            if (space > 0) return (text[..space], text[(space + 1)..].Trim());
        }

        return (text, null);
    }

    private static Int32 FindSentenceEnd(String text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch is '。' or '！' or '？' or '।') return i;

            // 英文句点仅在后面是空白或结尾时拆分，避免小数和 URL
            if (ch is '.' or '!' or '?')
            {
                if (i + 1 >= text.Length || Char.IsWhiteSpace(text[i + 1])) return i;
            }
        }

        return -1;
    }
}
