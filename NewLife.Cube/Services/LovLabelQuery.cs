namespace NewLife.Cube.Services;

/// <summary>List.* 批量标签的取数计划。</summary>
public sealed class ListLabelPlan
{
    /// <summary>none / keyed / scan</summary>
    public String Mode { get; set; } = "none";

    /// <summary>最多翻页数。0 表示不请求。</summary>
    public Int32 MaxPages { get; set; }

    /// <summary>每页条数。</summary>
    public Int32 PageSize { get; set; }
}

/// <summary>
/// List.* 标签反查计划。搜索字段与取值字段同名时一次按键取回；否则最多扫 10 页。
/// </summary>
public static class LovLabelQuery
{
    /// <summary>翻页扫描上限。第 11 页及之后不扫。</summary>
    public const Int32 ListLabelScanPages = 10;

    /// <summary>单次取值上限，也是可翻页扫描的页大小。</summary>
    public const Int32 ListLabelValueCap = 500;

    /// <summary>
    /// 规划一次 List.* 标签查询。
    /// pendingCount 小于等于 0 时不发请求。
    /// </summary>
    /// <param name="valueField">取值字段</param>
    /// <param name="searchFieldNames">搜索字段名</param>
    /// <param name="pendingCount">待翻译键数量，已截断到 500</param>
    /// <param name="pageable">远端是否分页</param>
    /// <returns>取数计划</returns>
    public static ListLabelPlan PlanListLabelQuery(String? valueField, IEnumerable<String?>? searchFieldNames, Int32 pendingCount, Boolean pageable)
    {
        if (pendingCount <= 0) return new ListLabelPlan { Mode = "none", MaxPages = 0, PageSize = 0 };

        var field = valueField?.Trim() ?? "";
        var keyed = !field.IsNullOrEmpty()
            && searchFieldNames != null
            && searchFieldNames.Any(name => name.EqualIgnoreCase(field));
        if (keyed)
        {
            return new ListLabelPlan
            {
                Mode = "keyed",
                MaxPages = 1,
                PageSize = Math.Min(ListLabelValueCap, pendingCount),
            };
        }

        if (pageable)
        {
            return new ListLabelPlan
            {
                Mode = "scan",
                MaxPages = ListLabelScanPages,
                PageSize = ListLabelValueCap,
            };
        }

        return new ListLabelPlan
        {
            Mode = "scan",
            MaxPages = 1,
            PageSize = Math.Min(ListLabelValueCap, Math.Max(200, pendingCount)),
        };
    }
}
