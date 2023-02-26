using System.ComponentModel;

namespace NewLife.Cube.Cube
{
    /// <summary>魔方管理区域注册</summary>
    [DisplayName("魔方管理")]
    [Menu(-2, true, Icon = "fa-tachometer")]
    public class CubeArea : AreaBase
    {
        /// <inheritdoc />
        public CubeArea() : base(nameof(CubeArea).TrimEnd("Area")) { }
    }
}