using System;
using System.ComponentModel;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Admin
{
    /// <summary>权限管理区域注册</summary>
    [DisplayName("系统管理")]
    public class AdminArea : AreaBase
    {
        /// <summary>区域名</summary>
        public static String AreaName => nameof(AdminArea).TrimEnd("Area");

        /// <inheritdoc />
        public AdminArea() : base(AreaName) { }
    }
}