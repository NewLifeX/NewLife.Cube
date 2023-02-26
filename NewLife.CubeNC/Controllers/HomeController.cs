using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;

namespace NewLife.Cube.Controllers;

/// <summary>主页面</summary>
//[AllowAnonymous]
public class CubeHomeController : ControllerBaseX
{
    /// <summary>主页面</summary>
    /// <returns></returns>
    public ActionResult Index()
    {
        ViewBag.Message = "主页面";

        return View();
    }

    /// <summary>错误</summary>
    /// <returns></returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var model = HttpContext.Items["Exception"] as ErrorModel;
        if (IsJsonRequest)
        {
            if (model?.Exception != null) return Json(500, null, model.Exception);
        }

        return View("Error", model);
    }
}