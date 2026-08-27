namespace NewLife.Cube.Widgets;

/// <summary>工作台卡片类型。决定卡片渲染方式</summary>
public enum WidgetTypes
{
    /// <summary>KPI 小卡。固定样式渲染，数据约定 <c>{ Value, Trend, Url }</c></summary>
    Kpi = 1,

    /// <summary>内容卡。自定义视图渲染，由 <see cref="WidgetAttribute.ViewPath"/> 指定视图路径</summary>
    Content = 2,

    /// <summary>HTML 卡。widget 输出 HTML 字符串，固定视图渲染</summary>
    Html = 3,

    /// <summary>KV 卡。widget 输出键值字典，固定表格视图渲染</summary>
    Kv = 4,

    /// <summary>图表卡。widget 输出 ECharts 图表数据，配置 <see cref="WidgetAttribute.ChartUrl"/> 后定时刷新追加</summary>
    Chart = 5,
}

/// <summary>工作台组件特性。声明组件元数据，与 <see cref="IWidget"/> 配合实现自动注册</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class WidgetAttribute : Attribute
{
    /// <summary>名称。默认取类型名去掉 Widget 后缀</summary>
    public String Name { get; set; }

    /// <summary>标题。卡片头显示</summary>
    public String Title { get; set; }

    /// <summary>图标。FontAwesome 图标名，如 fa-tachometer</summary>
    public String Icon { get; set; }

    /// <summary>栅格宽度。1-12，默认6。小于等于3时按 KPI 小卡渲染</summary>
    public Int32 Cols { get; set; } = 6;

    /// <summary>排序。越小越靠前，默认0。运行时由用户拖拽配置覆盖</summary>
    public Int32 Sort { get; set; }

    /// <summary>分类。系统/个人/通用等，KPI 行按分类与角色分流</summary>
    public String Category { get; set; }

    /// <summary>可见角色。逗号分隔的角色名，空表示所有登录用户可见</summary>
    public String Permission { get; set; }

    /// <summary>仅系统角色可见。true 时普通用户看不到该组件，常用于系统监控类组件</summary>
    public Boolean AdminOnly { get; set; }

    /// <summary>KPI 卡配色。blue/green/cyan/orange/red/purple/grey，默认主色</summary>
    public String Color { get; set; }

    /// <summary>组件类型。WidgetManager 扫描时填充</summary>
    public Type Type { get; set; }

    /// <summary>卡片类型。决定渲染方式，规范要求显式指定；默认内容卡兜底</summary>
    public WidgetTypes WidgetType { get; set; } = WidgetTypes.Content;

    /// <summary>内容卡视图路径。默认 ~/Areas/Admin/Views/Widgets/{Name}.cshtml</summary>
    public String ViewPath { get; set; }

    /// <summary>图表卡数据接口地址。Chart 卡配置后定时刷新追加数据，空表示一次性渲染</summary>
    public String ChartUrl { get; set; }

    /// <summary>图表卡刷新间隔（秒）。大于0时定时刷新追加，默认0</summary>
    public Int32 ChartInterval { get; set; }

    /// <summary>实例化组件特性</summary>
    /// <param name="name">名称</param>
    /// <param name="title">标题</param>
    public WidgetAttribute(String name, String title)
    {
        Name = name;
        Title = title;
    }
}
