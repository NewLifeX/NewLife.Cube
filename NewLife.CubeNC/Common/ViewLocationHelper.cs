using System.IO;

namespace NewLife.Cube;

/// <summary>视图定位辅助。按优先级生成候选路径，与 <see cref="ThemeViewLocationExpander"/> 保持一致的查找顺序</summary>
public static class ViewLocationHelper
{
    /// <summary>默认主题名。主题为空时的兜底</summary>
    public const String DefaultTheme = "ACE";

    /// <summary>获取主题视图查找插入规则。键为默认路径锚点，值为在该锚点前插入的主题路径，{0}=视图名，{1}=控制器名，{2}=区域名</summary>
    /// <param name="theme">主题名，空则使用 <see cref="DefaultTheme"/></param>
    /// <returns>插入规则字典。锚点互不相同，遍历顺序不影响插入结果</returns>
    public static IDictionary<String, String> GetThemeInsertRules(String theme)
    {
        theme = String.IsNullOrEmpty(theme) ? DefaultTheme : theme;

        return new Dictionary<String, String>
        {
            ["/Areas/{2}/Views/{1}/{0}.cshtml"] = $"/Areas/{{2}}/Views/{{1}}_{theme}/{{0}}.cshtml",
            ["/Areas/{2}/Views/Shared/{0}.cshtml"] = $"/Areas/{{2}}/Views/{theme}/{{0}}.cshtml",
            ["/Views/Shared/{0}.cshtml"] = $"/Views/{theme}/{{0}}.cshtml",
        };
    }

    /// <summary>把查找模式格式化为具体路径。{0}=视图名，{1}=控制器名，{2}=区域名</summary>
    /// <param name="pattern">查找模式</param>
    /// <param name="area">区域名，可为空</param>
    /// <param name="controller">控制器名，可为空</param>
    /// <param name="view">视图名</param>
    /// <returns>格式化后的路径，模式为空时返回空</returns>
    public static String Format(String pattern, String area, String controller, String view)
    {
        if (String.IsNullOrEmpty(pattern)) return null;

        var p = pattern.Replace("{0}", view);
        if (!String.IsNullOrEmpty(controller)) p = p.Replace("{1}", controller);
        if (!String.IsNullOrEmpty(area)) p = p.Replace("{2}", area);
        return p;
    }

    /// <summary>按优先级从高到低生成指定视图的候选路径</summary>
    /// <param name="area">区域名，可为空</param>
    /// <param name="controller">控制器名，可为空</param>
    /// <param name="view">视图名（不含 .cshtml 扩展名）</param>
    /// <param name="theme">主题名，空则使用 <see cref="DefaultTheme"/></param>
    /// <returns>按优先级排序的候选路径列表</returns>
    public static String[] GetCandidates(String area, String controller, String view, String theme)
    {
        if (String.IsNullOrEmpty(view)) throw new ArgumentNullException(nameof(view));

        theme = String.IsNullOrEmpty(theme) ? DefaultTheme : theme;
        var rules = GetThemeInsertRules(theme);

        var list = new List<String>();
        if (!String.IsNullOrEmpty(area))
        {
            if (!String.IsNullOrEmpty(controller))
            {
                // 1. 控制器×主题：/Areas/{Area}/Views/{Controller}_{Theme}/{View}.cshtml
                list.Add(Format(rules["/Areas/{2}/Views/{1}/{0}.cshtml"], area, controller, view));
                // 2. 控制器：/Areas/{Area}/Views/{Controller}/{View}.cshtml
                list.Add($"/Areas/{area}/Views/{controller}/{view}.cshtml");
            }
            // 3. 区域×主题：/Areas/{Area}/Views/{Theme}/{View}.cshtml
            list.Add(Format(rules["/Areas/{2}/Views/Shared/{0}.cshtml"], area, controller, view));
            // 4. 区域共享：/Areas/{Area}/Views/Shared/{View}.cshtml
            list.Add($"/Areas/{area}/Views/Shared/{view}.cshtml");
        }
        else if (!String.IsNullOrEmpty(controller))
        {
            // 无区域时，控制器级路径（引擎不插入主题变体，这里直接生成）
            list.Add($"/Views/{controller}_{theme}/{view}.cshtml");
            list.Add($"/Views/{controller}/{view}.cshtml");
        }
        // 5. 应用×主题：/Views/{Theme}/{View}.cshtml
        list.Add(Format(rules["/Views/Shared/{0}.cshtml"], area, controller, view));
        // 6. 应用共享：/Views/Shared/{View}.cshtml
        list.Add($"/Views/Shared/{view}.cshtml");

        return list.ToArray();
    }

    /// <summary>判断候选路径对应的物理文件是否存在（相对内容根目录）</summary>
    /// <param name="contentRoot">内容根目录</param>
    /// <param name="path">候选路径，以 / 开头</param>
    /// <returns>是否存在</returns>
    public static Boolean IsPhysicalFile(String contentRoot, String path)
    {
        if (String.IsNullOrEmpty(contentRoot) || String.IsNullOrEmpty(path)) return false;

        var p = path.TrimStart('/');
        return File.Exists(Path.Combine(contentRoot, p));
    }
}
