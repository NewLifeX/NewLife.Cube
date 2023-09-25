using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Areas.Cube.Controllers;

/// <summary></summary>
[CubeArea]
[Menu(38, true, Icon = "fa-file-text")]
public class OrderManagerController : EntityController<OrderManager>
{
    /// <summary>根据code获取指令集合</summary>
    /// <param name="codes"></param>
    /// <returns></returns>
    [HttpGet()]
    [EntityAuthorize]
    public IActionResult GetInfo(String codes)
    {
        return Json(0, message: "ok", data: OrderManager.FindAllByCodes(codes));
    }
}
