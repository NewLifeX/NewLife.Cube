using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>系统设置控制器</summary>
    [DisplayName("魔方设置")]
    [Area("Admin")]
    public class CubeController : ConfigController<Setting>
    {
        static CubeController() => MenuOrder = 34;


        /// <summary>
        /// 获取登录设置
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult GetLoginConfig()
        {
            return Ok(data: new LoginConfigModel());
        }
    }
}