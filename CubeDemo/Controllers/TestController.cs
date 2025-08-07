using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using NewLife.Cube.Extensions;
using NewLife.Web;
using XCode.Statistics;

namespace CubeDemo.Controllers;

//[ApiExplorerSettings(GroupName = "vTest1")]
[AreaBase("vTest1"),]
[Route("[controller]/[action]")]

public class TestController : ControllerBaseX
{
    [HttpGet]
    [HttpPost]
    [AllowAnonymous]
    public ApiResponse<TestClass> HelloList()
    {
        var enity = new TestClass { Name = "测试", Id = 100 };
        var aa = enity.ToOkApiResponse();
        return aa;
    }

    [HttpGet]
    [HttpPost]
    [AllowAnonymous]
    public ApiListResponse<TestClass> HelloList1([FromQuery] Pager pager, [FromBody] TestClass para)
    {
        var p = new Pager(WebHelper.Params) { RetrieveTotalCount = true };

        var list = SearchData1(p);
        return list.ToOkApiResponse().WithList(p);
    }

    [HttpGet]
    [HttpPost]
    [AllowAnonymous]
    public ApiListResponse<TestClass> HelloList2([FromQuery] Pager pager, [FromBody] TestClass para)
    {
        var p = new Pager(WebHelper.Params) { RetrieveTotalCount = true };
        var list = SearchData1(p);
        var ss = list.ToFailApiResponse("");
        return ss.WithList(p);
    }














    private IEnumerable<TestClass> SearchData1(Pager pager)
    {
        pager.PageSize = 11;

        var list = new List<TestClass>();
        for (int i = 0; i < 10; i++)
            list.Add(new TestClass { Name = "测试" + i, Id = i });
        return list;

    }
}