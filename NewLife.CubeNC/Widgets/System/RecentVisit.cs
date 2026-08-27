namespace NewLife.Cube.Widgets.System;

/// <summary>最近访问项。整表 JSON 存储于 Parameter（分类 <see cref="QuickLinkWidget.RecentCategory"/>，UserID=当前用户），列表顺序即最近访问顺序（最新在前）</summary>
public class RecentVisit
{
    /// <summary>页面名称。取自菜单显示名</summary>
    public String Name { get; set; }

    /// <summary>页面地址。取自菜单基础地址，不带查询参数</summary>
    public String Url { get; set; }

    /// <summary>菜单图标。可选，缺省使用 fa-history</summary>
    public String Icon { get; set; }

    /// <summary>访问次数。仅记录不参与排序，保留扩展</summary>
    public Int32 Count { get; set; }
}
