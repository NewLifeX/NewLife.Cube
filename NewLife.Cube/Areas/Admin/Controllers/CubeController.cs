using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("魔方设置")]
    [Area("Admin")]
    public class CubeController : ConfigController<Setting>
    {
        static CubeController()
        {
            MenuOrder = 34;
        }
    }
}