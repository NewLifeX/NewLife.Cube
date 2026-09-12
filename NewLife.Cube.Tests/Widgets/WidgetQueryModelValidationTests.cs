using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube.Widgets;
using Xunit;

namespace NewLife.Cube.Tests.Widgets;

/// <summary>Widget 查询请求模型校验。守护「非空引用类型属性被 MVC 隐式推断为 [Required]」导致部件取数 400 的问题</summary>
/// <remarks>
/// 症状：POST /Cube/Widget/Query 返回 code=-2，消息形如 “The Extra field is required.”，前端所有实体部件空白。
/// 根因：.NET 6+ 默认 SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=false，
/// 无默认值的非空引用类型属性会被隐式加上 [Required]；[JsonExtensionData] 属性在无未知字段时保持 null 而触发校验失败。
/// </remarks>
public class WidgetQueryModelValidationTests
{
    static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore().AddDataAnnotations();
        return services.BuildServiceProvider();
    }

    [Fact(DisplayName = "WidgetQueryRequest：除语义必填字段外不得被隐式推断为必填")]
    public void No_Implicitly_Required_Properties()
    {
        var sp = BuildServices();
        var provider = sp.GetRequiredService<IModelMetadataProvider>();
        var meta = provider.GetMetadataForType(typeof(WidgetQueryRequest));

        // Mode/TypePath 是语义必填（buildQueryBody 必定填充），其余可选字段必须可空
        String[] intended = [nameof(WidgetQueryRequest.Mode), nameof(WidgetQueryRequest.TypePath)];
        var required = meta.Properties
            .Where(e => !e.ModelType.IsValueType && e.IsRequired)
            .Select(e => e.Name)
            .Where(e => !intended.Contains(e))
            .ToArray();

        Assert.True(required.Length == 0, $"以下属性被隐式推断为必填，会导致省略字段的前端请求 400：{String.Join(",", required)}");
    }

    [Theory(DisplayName = "WidgetQueryRequest：前端省略可选字段的请求体可通过 MVC 校验")]
    // 与 web useWidgetQuery.buildQueryBody 一致：undefined 字段被 JSON.stringify 丢弃
    [InlineData("""{"mode":"aggregate","typePath":"/Admin/User","measure":{"fn":"count"},"buckets":12,"limit":30,"hostValues":{}}""")]
    // 分组 + 宿主联动：extraFilter 的 $host 条件省略 op，且含 hostFilter/linkFilter/hostValues
    [InlineData("""{"mode":"aggregate","typePath":"/Admin/User","measure":{"fn":"count"},"groupBy":"DepartmentID","timeField":"UpdateTime","extraFilter":{"logic":"all","conditions":[{"field":"DepartmentID","value":{"$host":"DepartmentID"}}]},"hostTypePath":"/Admin/Department","hostFilter":{"logic":"all","conditions":[{"field":"DepartmentID","op":"eq","value":1}]},"linkFilter":[{"hostField":"DepartmentID","sourceField":"DepartmentID"}],"hostValues":{"DepartmentID":1}}""")]
    public void Realistic_Body_Passes_ModelValidation(String json)
    {
        var instance = JsonSerializer.Deserialize<WidgetQueryRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        Assert.NotNull(instance);

        var sp = BuildServices();
        var validator = sp.GetRequiredService<IObjectModelValidator>();
        var ctx = new ActionContext(new DefaultHttpContext { RequestServices = sp }, new RouteData(), new ActionDescriptor());
        validator.Validate(ctx, null, String.Empty, instance);

        var errors = ctx.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .Select(e => $"{e.Key}: {e.Value.Errors[0].ErrorMessage}")
            .ToArray();

        Assert.True(errors.Length == 0, $"请求体校验失败：{String.Join(" | ", errors)}");
    }
}
