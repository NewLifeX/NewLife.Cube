using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>设置控制器</summary>
[DisplayName("Data middleware")]
[AdminArea]
[Menu(0, false)]
public class XCodeController : ConfigController<XCodeSetting>
{
}