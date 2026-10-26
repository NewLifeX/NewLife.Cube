using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.Tests.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS-5 系统信息测试。覆盖服务器信息接口返回运行时、GC、内存与时间等关键字段（不依赖宿主启动）。
/// </summary>
[Collection("TenantAuth")]
public class SystemInfoTests
{
    private sealed class FakeEnv : IWebHostEnvironment
    {
        public String ApplicationName { get; set; } = "NewLife.Cube.Tests";
        public String WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public String EnvironmentName { get; set; } = "Test";
        public String ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact(DisplayName = "系统信息_返回运行时GC内存与服务器时间")]
    public void Main_ReturnsServerInfo()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("localhost");
        ctx.Connection.LocalPort = 5000;
        ctx.Connection.RemotePort = 54321;

        var controller = new IndexController(null!, new FakeEnv(), null!, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = ctx },
        };

        var rs = Assert.IsType<ContentResult>(controller.Main());
        var json = rs.Content ?? "";

        Assert.Contains("\"code\":0", json.Replace(" ", ""));
        Assert.Contains("IsServerGC", json);
        Assert.Contains("serverTime", json);
        Assert.Contains("memory", json);
        Assert.Contains("openTime", json);
        Assert.Contains(AppContext.BaseDirectory.Replace("\\", "\\\\"), json.Replace("\\\\", "\\\\"));
    }
}
