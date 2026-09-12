namespace NewLife.Cube.Widgets;

/// <summary>KPI 小卡数据模型。公开类型，承载 { Value, Trend, Url } 渲染契约，
/// WidgetManager 将组件返回的任意对象（含匿名类型）归一化为此模型，保证跨程序集工作台渲染安全</summary>
public class KpiModel
{
    /// <summary>主数值</summary>
    public String Value { get; set; }

    /// <summary>趋势描述。可为简单 HTML 片段</summary>
    public String Trend { get; set; }

    /// <summary>点击跳转地址</summary>
    public String Url { get; set; }
}
