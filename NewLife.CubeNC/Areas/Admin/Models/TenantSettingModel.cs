namespace NewLife.Cube.Areas.Admin.Models;

/// <summary>租户配置模型</summary>
public class TenantSettingModel
{
    /// <summary>当前用户所属租户信息集合</summary>
    public Dictionary<Int32, String> Tenants { get; set; }

    /// <summary>当前用户名称</summary>
    public String UserName { get; set; }

    /// <summary>s</summary>
    public TenantSettingModel() { }

    /// <summary></summary>
    /// <param name="name"></param>
    public TenantSettingModel(String name)
    {
        UserName = name;
    }
}
