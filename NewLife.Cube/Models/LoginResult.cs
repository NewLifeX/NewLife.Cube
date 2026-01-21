using NewLife.Cube.Web.Models;

namespace NewLife.Cube.Models;

/// <summary>登录结果</summary>
public class LoginResult : TokenInfo // TokenModel
{
    //public String AccessToken { get; set; }

    /// <summary>结果</summary>
    public String Result { get; set; }
}
