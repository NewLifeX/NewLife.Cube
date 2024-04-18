namespace NewLife.Cube.Models;

/// <summary>访问令牌输入参数</summary>
public class TokenInModel
{
    /// <summary>授权类型</summary>
    public String grant_type { get; set; }

    /// <summary>用户名</summary>
    public String UserName { get; set; }

    /// <summary>密码</summary>
    public String Password { get; set; }

    /// <summary>客户端唯一标识。一般是IP@进程</summary>
    public String ClientId { get; set; }

    /// <summary>刷新令牌</summary>
    public String refresh_token { get; set; }
}