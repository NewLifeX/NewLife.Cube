using NewLife.Web;

namespace NewLife.Cube.Models;

/// <summary>登录结果</summary>
public class LoginResult : TokenModel
{
    //public String AccessToken { get; set; }

    /// <summary>结果</summary>
    public String Result { get; set; }
}
