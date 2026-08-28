namespace NewLife.Cube.Widgets;

/// <summary>C# Widget 发现特性。Name 对应 source.widgetName。</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CubeWidgetAttribute : Attribute
{
    /// <summary>唯一名称</summary>
    public String Name { get; }

    /// <summary>标题</summary>
    public String Title { get; }

    /// <summary>建议渲染 kind</summary>
    public String Kind { get; set; } = "metricCard";

    /// <summary>默认栅格宽</summary>
    public Int32 Cols { get; set; } = 3;

    /// <summary>仅系统角色可见</summary>
    public Boolean AdminOnly { get; set; }

    /// <summary>可见角色名，逗号分隔；空表示登录即可</summary>
    public String Permission { get; set; }

    /// <summary>表面：insight,workbench</summary>
    public String Surfaces { get; set; } = "insight,workbench";

    /// <summary>指标卡配色（blue/green/cyan/grey/red/orange）</summary>
    public String Color { get; set; }

    /// <summary>建议图标（fa-* 或 IconPark type）</summary>
    public String Icon { get; set; }

    /// <summary>实例化</summary>
    public CubeWidgetAttribute(String name, String title)
    {
        Name = name;
        Title = title;
    }
}
