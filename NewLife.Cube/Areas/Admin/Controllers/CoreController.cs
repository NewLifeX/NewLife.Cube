using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>设置控制器</summary>
[DisplayName("基本设置")]
[AdminArea]
[Menu(0, false, Icon = "fa-bomb")]
public class CoreController : ConfigController<NewLife.Setting>
{
}