using System;
using System.ComponentModel;

namespace NewLife.Cube.Admin
{
    /// <summary>权限管理区域注册</summary>
    [DisplayName("系统管理")]
    [Menu(-1, true, Icon = "fa-desktop")]
    public class AdminArea : AreaBase
    {
        /// <summary>区域名</summary>
        public static String AreaName => nameof(AdminArea).TrimEnd("Area");

        /// <summary>菜单顺序。扫描时会反射读取</summary>
        public static Int32 MenuOrder { get; set; } = 1;

        /// <inheritdoc />
        public AdminArea() : base(AreaName) { }
    }
}