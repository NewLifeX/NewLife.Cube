using System.ComponentModel;

namespace NewLife.Cube.Areas.Cube;

/// <summary>魔方管理区域注册</summary>
[DisplayName("魔方管理")]
[Menu(-2, true, Icon = "fa-tachometer", LastUpdate = "20240118")]
public class CubeArea : AreaBase
{
    /// <inheritdoc />
    public CubeArea() : base(nameof(CubeArea).TrimEnd("Area")) { }
}