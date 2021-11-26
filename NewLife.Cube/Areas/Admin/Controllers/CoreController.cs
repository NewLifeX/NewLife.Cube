using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>设置控制器</summary>
    [DisplayName("基本设置")]
    [Area("Admin")]
    [Menu(0, false, Icon = "fa-bomb")]
    public class CoreController : ConfigController<NewLife.Setting>
    {
    }
}