using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewLife.Cube.Swagger;
using NewLife.Cube.Tests.Membership;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace NewLife.Cube.Tests.Swagger;

/// <summary>
/// SPA-16 Swagger 文档测试。覆盖 AddCubeSwagger 注册 v1 文档与认证方案、按区域分组生成文档（跳过空分组），
/// 以及文档包含谓词按 HTTP 方法与区域过滤。
/// </summary>
[Collection("TenantAuth")]
public class SwaggerRegistrationTests
{
    /// <summary>实例化</summary>
    /// <param name="fixture">多租户认证夹具。复用其 SQLite 库</param>
    public SwaggerRegistrationTests(TenantAuthFixture fixture) { }

    #region 测试控制器
    /// <summary>自定义区域特性，带显示名与文档分组名（对齐 AreaBase 的 IApiDescriptionGroupNameProvider 实现）</summary>
    [DisplayName("测试区域")]
    private class TestAreaAttribute : AreaAttribute, IApiDescriptionGroupNameProvider
    {
        public TestAreaAttribute() : base("Admin") { }

        public String GroupName => "Admin";
    }

    /// <summary>带区域声明的控制器类型，用于区域分组</summary>
    [TestArea]
    private class AdminAreaController : ControllerBase { }

    /// <summary>无区域声明的控制器类型</summary>
    private class PlainController : ControllerBase { }

    /// <summary>固定返回给定分组的 ApiExplorer 提供者</summary>
    private class FakeGroupProvider : IApiDescriptionGroupCollectionProvider
    {
        public ApiDescriptionGroupCollection ApiDescriptionGroups { get; set; } = new([], 1);
    }
    #endregion

    #region 辅助
    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers();
        builder.Services.AddCubeSwagger();

        return builder.Build();
    }

    private static ApiDescription CreateAction(Type controllerType, String httpMethod)
    {
        var desc = new ApiDescription { HttpMethod = httpMethod };
        desc.ActionDescriptor = new ControllerActionDescriptor { ControllerTypeInfo = controllerType.GetTypeInfo() };

        return desc;
    }
    #endregion

    [Fact(DisplayName = "Swagger_注册v1文档与认证方案")]
    public async Task AddCubeSwagger_RegistersV1DocAndScheme()
    {
        var app = CreateApp();
        try
        {
            var options = app.Services.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;

            Assert.Contains("v1", options.SwaggerGeneratorOptions.SwaggerDocs.Keys);
            // 无可用 OAuth 配置时回退 JwtBearer，有配置时登记 OAuth2，两种情况都必须有认证方案
            Assert.NotEmpty(options.SwaggerGeneratorOptions.SecuritySchemes.Keys);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    [Fact(DisplayName = "Swagger_按区域分组生成文档且跳过空分组")]
    public void SwaggerConfigureOptions_AddsAreaDocs()
    {
        var adminAction = CreateAction(typeof(AdminAreaController), "GET");
        var provider = new FakeGroupProvider
        {
            ApiDescriptionGroups = new ApiDescriptionGroupCollection(
            [
                new ApiDescriptionGroup("Admin", [adminAction]),
                new ApiDescriptionGroup("", [CreateAction(typeof(PlainController), "GET")]),
            ], 1),
        };
        var options = new SwaggerGenOptions();

        new SwaggerConfigureOptions(provider).Configure(options);

        var docs = options.SwaggerGeneratorOptions.SwaggerDocs;
        Assert.Contains("Admin", docs.Keys);
        Assert.DoesNotContain("", docs.Keys);
        Assert.Equal("测试区域", docs["Admin"].Title);
    }

    [Fact(DisplayName = "Swagger_文档包含谓词按HTTP方法与区域过滤")]
    public async Task DocInclusionPredicate_FiltersByMethodAndArea()
    {
        var app = CreateApp();
        try
        {
            var predicate = app.Services.GetRequiredService<IOptions<SwaggerGenOptions>>().Value.SwaggerGeneratorOptions.DocInclusionPredicate;

            var adminAction = CreateAction(typeof(AdminAreaController), "GET");
            var noMethodAction = CreateAction(typeof(AdminAreaController), "");

            // v1 汇总全部接口
            Assert.True(predicate("v1", adminAction));
            // 未绑定 HTTP 方法的 action 不进入任何文档
            Assert.False(predicate("v1", noMethodAction));
            // 区域文档只收本区域接口
            Assert.True(predicate("Admin", adminAction));
            Assert.False(predicate("Cube", adminAction));
        }
        finally
        {
            await app.DisposeAsync();
        }
    }
}
