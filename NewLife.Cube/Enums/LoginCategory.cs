namespace NewLife.Cube.Enums
{
    /// <summary>登录类型</summary>
    public enum LoginCategory
    {
        /// <summary>账号密码登录：默认0</summary>
        Password = 0,
        /// <summary>手机登录</summary>
        Phone = 1,
        /// <summary>邮箱登录</summary>
        Email = 2,
        /// <summary>第三方登录</summary>
        OAuth = 3,
    }
}
