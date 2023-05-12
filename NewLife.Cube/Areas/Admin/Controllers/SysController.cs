using System.ComponentModel;
using NewLife.Common;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>系统设置控制器</summary>
[DisplayName("系统设置")]
[AdminArea]
[Menu(0, false)]
public class SysController : ConfigController<SysConfig>
{
}