namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>角色权限保存模型</summary>
public class RolePermissionModel
{
    /// <summary>角色编号</summary>
    public Int32 RoleId { get; set; }

    /// <summary>权限字符串。格式：资源ID#权限位，逗号分隔，如 1#3,2#8</summary>
    public String? Permission { get; set; }
}
