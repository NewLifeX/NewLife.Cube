using System.ComponentModel;

namespace NewLife.Cube.Enums
{
    /// <summary>登录类型</summary>
    public enum LoginType
    {
        /// <summary>账号密码密码</summary>
        [Description("账号密码登录")]
        Password = 1,

        /// <summary>手机登录</summary>
        [Description("手机登录")]
        Tel = 2,

        /// <summary>邮箱登录</summary>
        [Description("邮箱登录")]
        Email = 3,

        /// <summary>第三方OAuth登录</summary>
        [Description("第三方OAuth登录")]
        OAuth = 4


    }
}
