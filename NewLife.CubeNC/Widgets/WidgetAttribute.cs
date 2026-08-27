namespace NewLife.Cube.Widgets;

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

    /// <summary>栅格宽度。1-12，默认6</summary>
    public Int32 Cols { get; set; } = 6;

    /// <summary>排序。越小越靠前，默认0</summary>
    public Int32 Sort { get; set; }

    /// <summary>分类。系统/业务等</summary>
    public String Category { get; set; }

    /// <summary>可见角色。逗号分隔的角色名，空表示所有登录用户可见</summary>
    public String Permission { get; set; }

    /// <summary>仅系统角色可见。true 时普通用户看不到该组件，常用于系统监控类组件</summary>
    public Boolean AdminOnly { get; set; }

    /// <summary>组件局部视图路径。默认 ~/Areas/Admin/Views/Widgets/{Name}.cshtml</summary>
    public String ViewName { get; set; }

    /// <summary>实例化组件特性</summary>
    /// <param name="name">名称</param>
    /// <param name="title">标题</param>
    public WidgetAttribute(String name, String title)
    {
        Name = name;
        Title = title;
    }
}
