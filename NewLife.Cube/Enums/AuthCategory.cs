namespace NewLife.Cube.Enums;

/// <summary>统一认证分类，适用于登录与注册接口的 category 字段</summary>
public enum AuthCategory
{
    /// <summary>账号密码（默认）</summary>
    Password = 0,

    /// <summary>手机验证码</summary>
    Mobile = 1,

    /// <summary>邮箱验证码</summary>
    Mail = 2,

    /// <summary>第三方 OAuth</summary>
    OAuth = 3,
}
