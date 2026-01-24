namespace NewLife.Cube.Models;


/// <summary>登录结果</summary>
public class LoginResult : NewLife.Web.TokenModel
{
    //public String AccessToken { get; set; }

    /// <summary>结果</summary>
    public String Result { get; set; }
}

/// <summary>绑定结果</summary>
public class BindResult
{
    /// <summary>是否成功</summary>
    public Boolean Success { get; set; }

    /// <summary>消息</summary>
    public String Message { get; set; }
}

/// <summary>重置结果</summary>
public class ResetResult
{
    /// <summary>是否成功</summary>
    public Boolean Success { get; set; }

    /// <summary>消息</summary>
    public String Message { get; set; }
}