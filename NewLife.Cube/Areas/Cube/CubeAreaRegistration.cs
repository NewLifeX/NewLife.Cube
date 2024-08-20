using System.ComponentModel;

namespace NewLife.Cube.Areas.Cube;

/// <summary>魔方管理区域注册</summary>
[DisplayName("魔方管理")]
[Description("""
    魔方提供一些常用的配套功能。
    配套功能：附件、地区、定时任务、应用系统管理等。
    """)]
[Menu(-2, true, Icon = "fa-tachometer", LastUpdate = "20240118")]
public class CubeArea : AreaBase
{
    /// <inheritdoc />
    public CubeArea() : base(nameof(CubeArea).TrimEnd("Area")) { }
}