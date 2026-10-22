using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Cube.Controllers;
using NewLife.Cube.Models;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>站点配置接口测试</summary>
public class SiteConfigTests
{
    [Fact]
    [DisplayName("站点配置接口应允许匿名访问且路由为 Cube/Site")]
    public void SiteEndpoint_Should_Be_Anonymous()
    {
        var routes = typeof(CubeController).GetCustomAttributes<RouteAttribute>(inherit: false);
        Assert.Contains(routes, e => e.Template == "[controller]/[action]");

        var action = typeof(CubeController).GetMethod(nameof(CubeController.Site));
        Assert.NotNull(action);
        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.NotNull(action.GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    [DisplayName("站点配置应返回语言选项与 AI 开关")]
    public void SiteConfig_Should_Expose_Language_And_Features()
    {
        var model = new SiteConfigModel(null);

        Assert.False(model.Name.IsNullOrEmpty());
        Assert.NotEmpty(model.Language.Options);
        Assert.Contains(model.Language.Options, e => e.Name == "zh-CN");
        Assert.NotNull(model.Features.Ai);
    }

    [Fact]
    [DisplayName("站点配置应合并租户品牌字段")]
    public void SiteConfig_Should_Merge_Tenant_Brand()
    {
        var tenant = new Tenant
        {
            Code = "acme",
            Name = "Acme",
            Logo = "/logo.png",
        };

        var model = new SiteConfigModel(tenant);

        Assert.Equal("Acme", model.Name);
        Assert.Equal("acme", model.TenantCode);
        Assert.Equal("/logo.png", model.Logo);
    }

    [Fact]
    [DisplayName("站点配置应写入公共缓存头并支持 304")]
    public void SiteEndpoint_Should_Emit_Cache_Headers_And_304()
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        var controller = new CubeController(null, null, [])
        {
            ControllerContext = new ControllerContext { HttpContext = http },
        };

        var result = controller.Site();
        Assert.IsType<ContentResult>(result);

        var etag = http.Response.Headers.ETag.ToString();
        Assert.False(etag.IsNullOrEmpty());
        Assert.Equal("public, max-age=60", http.Response.Headers["Cache-Control"].ToString());
        Assert.Contains("X-Tenant", http.Response.Headers["Vary"].ToString());

        http.Request.Headers.IfNoneMatch = etag;
        var cached = controller.Site();

        Assert.IsType<EmptyResult>(cached);
        Assert.Equal(304, http.Response.StatusCode);
    }

    [Fact]
    [DisplayName("站点配置语言选项应复用 Core LanguageInfo 字段")]
    public void SiteConfig_Language_Options_Should_Serialize_Core_Fields()
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        var controller = new CubeController(null, null, [])
        {
            ControllerContext = new ControllerContext { HttpContext = http },
        };

        var result = Assert.IsType<ContentResult>(controller.Site());

        Assert.Contains("\"options\":[{\"name\":", result.Content);
        Assert.Contains("\"nativeName\":", result.Content);
        Assert.Contains("\"englishName\":", result.Content);
        Assert.DoesNotContain("\"rtl\":", result.Content);
    }
}

