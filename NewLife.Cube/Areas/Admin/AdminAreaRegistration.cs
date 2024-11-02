using System.ComponentModel;

namespace NewLife.Cube.Areas.Admin;

/// <summary>权限管理区域注册</summary>
[DisplayName("系统管理")]
[Description("""
    核心功能：用户、角色、菜单，构成基本权限体系。
    核心配置：基本设置、系统设置、魔方设置、数据中间件。
    OAuth功能：OAuth配置微信钉钉等多个第三方SSO登录。
    安全功能：审计日志、访问规则，保障系统安全。
    配套功能：租户、部门、字典参数、用户在线与统计、数据库管理、系统信息。
    """)]
[Menu(-1, true, Icon = "fa-desktop", LastUpdate = "20240118")]
public class AdminArea : AreaBase
{
    /// <inheritdoc />
    public AdminArea() : base(nameof(AdminArea).TrimEnd("Area")) { }
}