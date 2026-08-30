namespace NewLife.Cube.Widgets;

/// <summary>工作台组件接口。实现此接口并标注 <see cref="WidgetAttribute"/> 的类将被工作台自动发现并渲染</summary>
public interface IWidget
{
    /// <summary>获取组件数据。返回的数据模型由组件局部视图渲染</summary>
    Object GetData();
}
