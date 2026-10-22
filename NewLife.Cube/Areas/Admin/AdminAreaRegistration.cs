using System.ComponentModel;

namespace NewLife.Cube.Areas.Admin;

/// <summary>权限管理区域注册</summary>
[DisplayName("系统管理")]
[Description("""
    Core features: users, roles and menus form the basic permission system.
    Core settings: basic settings, system settings, Cube settings and data middleware.
    OAuth: configure WeChat, DingTalk and other third-party SSO sign-ins.
    Security: audit logs and access rules keep the system secure.
    Supporting features: tenants, departments, dictionary parameters, users online and statistics, database management and system information.
    """)]
[Menu(-1, true, Icon = "Monitor", LastUpdate = "20260731")]
public class AdminArea : AreaBase
{
    /// <inheritdoc />
    public AdminArea() : base(nameof(AdminArea).TrimSuffix("Area")) { }
}