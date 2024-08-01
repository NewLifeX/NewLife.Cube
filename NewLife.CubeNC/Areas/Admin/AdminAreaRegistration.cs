using System.ComponentModel;
using XCode;
using XCode.Membership;
using NewLife.Cube.Areas.Admin.Controllers;

namespace NewLife.Cube.Areas.Admin;

/// <summary>权限管理区域注册</summary>
[DisplayName("系统管理")]
[Menu(-1, true, Icon = "fa-desktop", LastUpdate = "20240118")]
public class AdminArea : AreaBase
{
    /// <inheritdoc />
    public AdminArea() : base(nameof(AdminArea).TrimEnd("Area"))
    {
        // 修正Main
        var mf = ManageProvider.Menu;
        var menu = mf?.FindByFullName("NewLife.Cube.Admin.Controllers.IndexController.Main");
        if (menu != null)
        {
            menu.FullName = typeof(IndexController).FullName + ".Main";
            (menu as IEntity).Update();
        }
    }
}