namespace NewLife.Cube.Widgets;

/// <summary>用户工作台布局项。记录组件排序与隐藏状态，整表 JSON 存储于 Parameter（分类 Widget.Layout，UserID=当前用户）</summary>
public class WidgetLayout
{
    /// <summary>排序值。越小越靠前，拖拽后按显示顺序生成</summary>
    public Int32 Sort { get; set; }

    /// <summary>是否隐藏。true 时工作台不渲染该组件，可在恢复面板重新显示</summary>
    public Boolean Hide { get; set; }
}
