using System;

namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>修改密码模型</summary>
/// <remarks>
/// 修改密码只依赖三个密码字段：用户名与 SSO 渠道分别取自当前登录用户与会话，
/// 因此 `Name` / `SsoName` 允许为空（历史字段保留，便于老客户端透传）。
/// </remarks>
public class ChangePasswordModel : ICubeModel
{
    /// <summary>用户名。为空时取当前登录用户</summary>
    public String? Name { get; set; }

    /// <summary>Sso登录渠道。为空时取会话</summary>
    public String? SsoName { get; set; }

    /// <summary>旧密码</summary>
    public String? OldPassword { get; set; }

    /// <summary>新密码</summary>
    public String NewPassword { get; set; }

    /// <summary>确认密码</summary>
    public String NewPassword2 { get; set; }
}