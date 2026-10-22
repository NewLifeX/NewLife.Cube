using System.ComponentModel;

namespace NewLife.Cube.Areas.Cube;

/// <summary>魔方管理区域注册</summary>
[DisplayName("魔方管理")]
[Description("""
    Cube provides a set of common supporting features.
    Supporting features: attachments, areas, scheduled jobs, application management and more.
    """)]
[Menu(-2, true, Icon = "Odometer", LastUpdate = "20260731")]
public class CubeArea : AreaBase
{
    /// <inheritdoc />
    public CubeArea() : base(nameof(CubeArea).TrimSuffix("Area")) { }
}