using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>MVC 模型校验测试助手</summary>
/// <remarks>
/// 项目开启 Nullable 标注后，无默认值的非空引用类型属性/参数会被 MVC 隐式推断为 [Required]
/// （MvcOptions.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes 默认 false）。
/// 前端（api-core）提交时会省略 undefined 字段、甚至不带 body，从而在模型校验阶段 400，
/// 典型报错如 “The Extra field is required.”“The typePath field is required.”。
/// 本助手用真实 MVC 元数据/校验管线还原该判定，便于对请求契约做回归守护。
/// </remarks>
internal static class ModelValidationHarness
{
    /// <summary>构建仅含元数据与校验所需服务的最小 MVC 容器</summary>
    internal static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore().AddDataAnnotations();
        return services.BuildServiceProvider();
    }

    /// <summary>按 MVC 真实管线校验模型，返回形如 “属性: 消息” 的错误项</summary>
    /// <param name="model">待校验模型实例（通常由前端 JSON 反序列化而来）</param>
    /// <returns>校验错误项；空数组表示通过</returns>
    internal static String[] Validate(Object model)
    {
        var sp = BuildServices();
        var validator = sp.GetRequiredService<IObjectModelValidator>();
        var ctx = new ActionContext(new DefaultHttpContext { RequestServices = sp }, new RouteData(), new ActionDescriptor());
        validator.Validate(ctx, null, String.Empty, model);

        return ctx.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .Select(e => $"{e.Key}: {e.Value.Errors[0].ErrorMessage}")
            .ToArray();
    }

    /// <summary>按 Cube 实际运行配置（AddCube 的隐式必填关闭已生效）校验模型</summary>
    /// <param name="model">待校验模型实例（通常由前端 JSON 反序列化而来）</param>
    /// <returns>校验错误项；空数组表示通过</returns>
    /// <remarks>与 CubeService.ConfigureEntityModelValidation 共享同一开关，防止配置被移除而测试不自知。</remarks>
    internal static String[] ValidateAsCube(Object model)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore().AddDataAnnotations();
        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(CubeService.ConfigureEntityModelValidation);
        var sp = services.BuildServiceProvider();

        var validator = sp.GetRequiredService<IObjectModelValidator>();
        var ctx = new ActionContext(new DefaultHttpContext { RequestServices = sp }, new RouteData(), new ActionDescriptor());
        validator.Validate(ctx, null, String.Empty, model);

        return ctx.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .Select(e => $"{e.Key}: {e.Value.Errors[0].ErrorMessage}")
            .ToArray();
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>反序列化前端提交的 JSON 请求体</summary>
    /// <typeparam name="T">目标模型类型</typeparam>
    /// <param name="json">JSON 文本</param>
    /// <returns>模型实例</returns>
    internal static T? Deserialize<T>(String json) => (T?)Deserialize(json, typeof(T));

    /// <summary>按类型反序列化前端提交的 JSON 请求体（供 [Theory] 传 Type 的场景）</summary>
    /// <param name="json">JSON 文本</param>
    /// <param name="type">目标模型类型</param>
    /// <returns>模型实例</returns>
    internal static Object? Deserialize(String json, Type type) => JsonSerializer.Deserialize(json, type, JsonOptions);

    /// <summary>断言模型在真实 MVC 校验下无错误</summary>
    /// <param name="model">模型实例</param>
    internal static void AssertValid(Object model)
    {
        var errors = Validate(model);
        Assert.True(errors.Length == 0, $"请求体未通过 MVC 模型校验：{String.Join(" | ", errors)}");
    }

    /// <summary>解析 JSON 请求体并断言其通过 MVC 校验</summary>
    /// <typeparam name="T">目标模型类型</typeparam>
    /// <param name="json">JSON 文本</param>
    internal static void AssertJsonValid<T>(String json)
    {
        var model = Deserialize<T>(json);
        Assert.NotNull(model);
        AssertValid(model!);
    }

    /// <summary>收集类型上的引用类型属性中，存在隐式必填（无默认值且非可空标注）的成员名</summary>
    /// <typeparam name="T">模型类型</typeparam>
    /// <returns>隐式必填属性名</returns>
    internal static String[] ImplicitlyRequiredProperties<T>() => ImplicitlyRequiredProperties(typeof(T));

    /// <summary>收集类型上的引用类型属性中，存在隐式必填（无默认值且非可空标注）的成员名</summary>
    /// <param name="type">模型类型</param>
    /// <returns>隐式必填属性名</returns>
    internal static String[] ImplicitlyRequiredProperties(Type type)
    {
        var sp = BuildServices();
        var provider = sp.GetRequiredService<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider>();
        var meta = provider.GetMetadataForType(type);

        return meta.Properties
            .Where(e => !e.ModelType.IsValueType && e.IsRequired)
            .Select(e => e.Name)
            .ToArray();
    }

    /// <summary>判断参数的引用类型是否声明为可空</summary>
    /// <param name="parameter">参数</param>
    /// <returns>是否可空</returns>
    internal static Boolean IsNullableParameter(System.Reflection.ParameterInfo parameter) =>
        new System.Reflection.NullabilityInfoContext().Create(parameter).WriteState == System.Reflection.NullabilityState.Nullable;
}
