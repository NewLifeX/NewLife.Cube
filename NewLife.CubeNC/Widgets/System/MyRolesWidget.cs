using System.Linq;
using XCode.Membership;

namespace NewLife.Cube.Widgets.System;

/// <summary>我的角色数。普通用户工作台 KPI 指标</summary>
[Widget("MyRoles", "我的角色", Icon = "fa-key", Cols = 3, Sort = 20, Category = "个人", Color = "purple")]
public class MyRolesWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var user = ManageProvider.User;
        var roles = user?.Roles;
        var count = roles?.Length ?? 0;

        return new
        {
            Value = count.ToString(),
            Trend = roles == null || roles.Length == 0 ? "无角色" : String.Join(",", roles.Select(e => e.Name)),
            Url = "",
        };
    }
}
