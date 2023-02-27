using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers;

/// <summary>设置控制器</summary>
[DisplayName("数据中间件")]
[Area("Admin")]
[Menu(0, false)]
public class XCodeController : ConfigController<XCode.Setting>
{
}