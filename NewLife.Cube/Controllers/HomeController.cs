#if __CORE__
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace NewLife.Cube.Controllers
{
    /// <summary>主页面</summary>
    //[AllowAnonymous]
    public class CubeHomeController : ControllerBaseX
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

#if __CORE__
        /// <summary>错误</summary>
        /// <returns></returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var context = HttpContext.Items["ExceptionContext"] as ExceptionContext;
            if (IsJsonRequest)
            {
                if (context?.Exception != null)
                    return Json(500, null, context.Exception, new { action = context.ActionDescriptor.DisplayName });
            }

            return View("Error", context);
        }
#endif
    }
}