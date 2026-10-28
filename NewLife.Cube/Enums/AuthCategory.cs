namespace NewLife.Cube.Enums;

/// <summary>统一认证分类，适用于登录与注册接口的 category 字段</summary>
/// <remarks>JSON 读写采用字符串（可同时读取原有数字，保证旧客户端兼容），与前端约定的 "" / password / mobile / mail / oauth 保持一致</remarks>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
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
