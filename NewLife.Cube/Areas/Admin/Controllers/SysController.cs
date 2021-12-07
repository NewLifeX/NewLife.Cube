using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Common;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("系统设置")]
    [Area("Admin")]
    [Menu(0, false)]
    public class SysController : ConfigController<SysConfig>
    {
    }
}