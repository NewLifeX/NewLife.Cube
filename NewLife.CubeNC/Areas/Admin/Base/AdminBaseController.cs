using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.Admin;

namespace NewLife.CubeNC.Areas.Admin.Base
{
    [AdminArea]
    public class AdminBaseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}