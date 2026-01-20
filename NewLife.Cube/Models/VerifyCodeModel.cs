using NewLife.Cube.Areas.Admin.Models;

namespace NewLife.Cube.Models;

/// <summary>验证码模型</summary>
public class VerifyCodeModel : ICubeModel
{
    /// <summary>渠道。Sms/Mail等</summary>
    public String Channel { get; set; }

    /// <summary>用户名</summary>
    public String Username { get; set; }

    /// <summary>验证码</summary>
    public String Code { get; set; }

    /// <summary>动作</summary>
    public String Action { get; set; }
}
