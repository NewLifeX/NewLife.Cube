namespace NewLife.Cube.Widgets;

/// <summary>工作台组件元数据</summary>
public class WidgetInfo
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>标题</summary>
    public String Title { get; set; }

    /// <summary>图标</summary>
    public String Icon { get; set; }

    /// <summary>栅格宽度。1-12</summary>
    public Int32 Cols { get; set; } = 6;

    /// <summary>排序。越小越靠前</summary>
    public Int32 Sort { get; set; }

    /// <summary>分类</summary>
    public String Category { get; set; }

    /// <summary>可见角色。逗号分隔，空表示所有登录用户可见</summary>
    public String Permission { get; set; }

    /// <summary>仅系统角色可见</summary>
    public Boolean AdminOnly { get; set; }

    /// <summary>局部视图路径。默认 ~/Areas/Admin/Views/Widgets/{Name}.cshtml</summary>
    public String ViewName { get; set; }

    /// <summary>组件类型</summary>
    public Type Type { get; set; }
}
