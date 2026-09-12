using System.ComponentModel;
using Stardust;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>星尘设置控制器</summary>
[DisplayName("星尘设置")]
[AdminArea]
[Menu(0, false, Icon = "Star")]
public class StarController : ConfigController<StarSetting>
{
}
