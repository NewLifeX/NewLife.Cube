using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;

#if __CORE__
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
#else
using System.Web.Mvc;
#endif

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