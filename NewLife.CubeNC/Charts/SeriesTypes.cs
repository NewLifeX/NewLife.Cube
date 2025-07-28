namespace NewLife.Cube.Charts;

/// <summary>系列类型</summary>
public enum SeriesTypes
{
    /// <summary>折线图</summary>
    Line = 1,

    /// <summary>柱状图</summary>
    Bar,

    /// <summary>饼图</summary>
    Pie,

    /// <summary>散点图</summary>
    Scatter,

    /// <summary>地图</summary>
    Map,

    /// <summary>K线图</summary>
    Candlestick,

    /// <summary>雷达图</summary>
    Radar,

    /// <summary>箱形图</summary>
    Boxplot,

    /// <summary>热力图</summary>
    Heatmap,

    /// <summary>关系图</summary>
    Graph,

    /// <summary>路径图</summary>
    Lines,

    /// <summary>树图</summary>
    Tree,

    /// <summary>矩形树图</summary>
    Treemap,

    /// <summary>旭日图</summary>
    Sunburst,

    /// <summary>平行坐标系</summary>
    Parallel,

    /// <summary>桑基图</summary>
    Sankey,

    /// <summary>漏斗图</summary>
    Funnel,

    /// <summary>仪表盘</summary>
    Gauge,

    /// <summary>象形柱图</summary>
    PictorialBar,

    /// <summary>主题河流</summary>
    ThemeRiver,

    /// <summary>自定义系列</summary>
    Custom,

    /// <summary>三维折线图</summary>
    Line3D,

    /// <summary>三维飞线图</summary>
    Lines3D,

    /// <summary>三维柱状图</summary>
    Bar3D,

    /// <summary>特效散点图</summary>
    EffectScatter,
}