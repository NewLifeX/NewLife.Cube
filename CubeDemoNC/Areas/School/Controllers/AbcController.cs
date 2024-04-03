using Microsoft.AspNetCore.Mvc;
using NewLife.Log;
using NewLife.School.Entity;

namespace CubeDemo.Areas.School.Controllers
{
    [SchoolArea]
    public class AbcController : Controller
    {
        public IActionResult Index()
        {
            return Content("Hello World!");
        }

        public IActionResult Def()
        {
            var list = Class.FindAll(Class._.Enable == true, null, null, 0, 10);

            return View(list);
        }

        public IActionResult ggg(String name)
        {
            XTrace.WriteLine("name: {0}", name);

            return RedirectToAction("def");
        }
    }
}
