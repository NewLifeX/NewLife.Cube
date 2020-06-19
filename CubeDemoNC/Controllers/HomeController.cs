using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;

namespace CubeDemo.Controllers
{
    /// <summary>主页面</summary>
    //[AllowAnonymous]
    public class HomeController : ControllerBaseX
    {
        /// <summary>主页面</summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            ViewBag.Message = "主页面测试";

            return View();
        }
    }
}