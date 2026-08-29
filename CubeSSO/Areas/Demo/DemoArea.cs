using System.ComponentModel;
using NewLife;
using NewLife.Cube;

namespace CubeSSO.Areas.Demo;

/// <summary>分页演示区域</summary>
[DisplayName("分页演示")]
[Menu(0, false, Icon = "fa-table")]
public class DemoArea : AreaBase
{
    /// <summary>实例化</summary>
    public DemoArea() : base(nameof(DemoArea).TrimEnd("Area")) { }
}
