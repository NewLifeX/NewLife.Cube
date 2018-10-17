#if __CORE__
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace NewLife.Cube.Controllers
{
    /// <summary>主页面</summary>
    //[AllowAnonymous]
    public class CubeHomeController : Controller
    {
        /// <summary>主页面</summary>
        /// <returns></returns>
#if !__CORE__
        [RequireSsl]
#endif
        public ActionResult Index()
        {
            ViewBag.Message = "主页面";

            return View();
        }
    }
}