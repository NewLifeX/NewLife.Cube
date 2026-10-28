using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NewLife.Cube;
using NewLife.Cube.Controllers;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SEC-02/SEC-03/SEC-05 接口收敛测试。覆盖健康检测最小化、接口浏览器默认关闭、
/// 值集查询数量上限与未注册枚举不返回数据。
/// </summary>
public class CubeControllerSecurityTests
{
    private static CubeController CreateController()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("localhost");

        return new CubeController(null!, null!, Array.Empty<EndpointDataSource>())
        {
            ControllerContext = new ControllerContext { HttpContext = ctx },
        };
    }

    private static String Content(ActionResult rs) => Assert.IsType<ContentResult>(rs).Content ?? "";

    [Fact(DisplayName = "健康检测_只返回最小信息_不含服务器指纹")]
    public void Health_ReturnsMinimalInfo()
    {
        var rs = Content(CreateController().Health("probe-1"));

        var json = rs.Replace(" ", "");
        Assert.Contains("\"code\":0", json);
        Assert.Contains("\"status\":\"ok\"", json);
        Assert.Contains("probe-1", json);

        // 不暴露操作系统、程序集版本、端口等指纹字段
        Assert.DoesNotContain("OS", json);
        Assert.DoesNotContain("FileVersion", json);
        Assert.DoesNotContain("Port", json);
    }

    [Fact(DisplayName = "接口浏览器_默认关闭_返回未找到")]
    public void Apis_DisabledByDefault_ReturnsNotFound()
    {
        var prev = CubeSetting.Current.EnableApiExplorer;
        CubeSetting.Current.EnableApiExplorer = false;
        try
        {
            var json = Content(CreateController().Apis()).Replace(" ", "");
            Assert.Contains("\"code\":404", json);
        }
        finally
        {
            CubeSetting.Current.EnableApiExplorer = prev;
        }
    }

    [Fact(DisplayName = "值集查询_超过数量上限_返回参数错误")]
    public void Lookup_TooManyCodes_ReturnsParamError()
    {
        var codes = String.Join(",", new List<String> { "A", "B" });
        for (var i = 0; i < 20; i++) codes += "," + i;

        var json = Content(CreateController().Lookup(codes)).Replace(" ", "");
        Assert.Contains("\"code\":-2", json);
    }

    [Fact(DisplayName = "值集查询_未注册枚举_不返回数据")]
    public void Lookup_UnknownEnum_ReturnsEmpty()
    {
        var json = Content(CreateController().Lookup("This.Type.Does.Not.Exist")).Replace(" ", "");
        Assert.Contains("\"code\":0", json);
        Assert.Contains("\"data\":{}", json);
    }

    [Fact(DisplayName = "值集查询_已加载枚举_返回选项且缓存复用")]
    public void Lookup_KnownEnum_ReturnsOptions()
    {
        var controller = CreateController();
        var json = Content(controller.Lookup("NewLife.Cube.Enums.AuthCategory")).Replace(" ", "");

        Assert.Contains("\"code\":0", json);
        Assert.Contains("Password", json);
        Assert.Contains("Mobile", json);

        // 第二次调用命中缓存，结果一致
        Assert.Equal(json, Content(controller.Lookup("NewLife.Cube.Enums.AuthCategory")).Replace(" ", ""));
    }
}
