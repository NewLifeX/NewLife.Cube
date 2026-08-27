namespace NewLife.Cube.Widgets;

/// <summary>工作台组件及其数据。工作台预取组件数据后连同元数据一起交给视图渲染</summary>
public class WidgetData
{
    /// <summary>组件元数据</summary>
    public WidgetInfo Info { get; set; }

    /// <summary>组件数据。由组件局部视图渲染</summary>
    public Object Data { get; set; }
}
