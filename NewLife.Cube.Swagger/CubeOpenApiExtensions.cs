#if NET9_0_OR_GREATER
#nullable enable
using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube;

namespace NewLife.Cube.Swagger;

/// <summary>
/// .NET 内建 OpenAPI(Microsoft.AspNetCore.OpenApi) 与魔方 Area 的集成：反射 <see cref="AreaBase"/> 区域特性，
/// 为每个 Area 自动注册一份具名文档并重写 ShouldInclude 归组，使 dotnet build 即产出各域 openapi.json。
/// 由 <see cref="SwaggerService.AddCubeSwagger"/> 内部调用，消费者无需再手写。
/// </summary>
public static class CubeOpenApiExtensions
{
    /// <param name="hostAssembly">指定扫描源；为空时扫描 AppDomain 已加载的所有程序集
    /// （ApiDescription.Server 构建期生成 host 里 Assembly.GetEntryAssembly 不可靠，故不依赖它）。</param>
    public static IServiceCollection AddCubeOpenApiByAreas(this IServiceCollection services, Assembly? hostAssembly = null)
    {
        services.AddOpenApi();   // 默认文档 v1：无组端点（全局/健康检查等）

        var assemblies = hostAssembly is not null ? new[] { hostAssembly } : AppDomain.CurrentDomain.GetAssemblies();

        var groups = assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => !t.IsAbstract && t != typeof(AreaBase) && typeof(AreaBase).IsAssignableFrom(t))
            .Select(GroupNameOf)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var name in groups)
        {
            var group = name!;
            // 具名文档 + 重写 ShouldInclude：把该区域组的端点纳入本文档。
            // （不重写则：魔方 descriptor 级组名的端点会被内建默认 v1 剔除、又进不了朴素具名文档 → 两头落空。）
            // 大小写不敏感：框架会把文档名/组归一小写（文件 MindHub.Api_test.json、title | test）。
            services.AddOpenApi(group, options =>
                options.ShouldInclude = desc => string.Equals(desc.GroupName, group, StringComparison.OrdinalIgnoreCase));
        }

        // 项目侧 XML 注释统一导入（框架级）：为 v1 与全部具名文档附加注释变换器——
        // 项目只需开 GenerateDocumentationFile，无需任何接线（读取来源与行为见 OpenApiEntryXmlComments.cs）。
        // 注：注释导入器依赖 .NET 10 的 Microsoft.OpenApi 2.x 对象模型，故仅在 net10 目标注册。
#if NET10_0_OR_GREATER
        services.ConfigureAll<OpenApiOptions>(options =>
        {
            options.AddOperationTransformer(new EntryXmlCommentOperationTransformer());
            options.AddSchemaTransformer(new EntryXmlCommentSchemaTransformer());
        });
#endif

        return services;
    }

    // GetTypes() 可能因个别依赖加载不全抛 ReflectionTypeLoadException，取可加载的部分即可。
    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t is not null).Select(t => t!); }
        catch { return Array.Empty<Type>(); }
    }

    private static string? GroupNameOf(Type t)
    {
        try
        {
            if (t.GetConstructor(Type.EmptyTypes) is not null && Activator.CreateInstance(t) is AreaBase ab && !string.IsNullOrEmpty(ab.GroupName))
                return ab.GroupName;
        }
        catch { /* 特性构造有副作用则忽略，走名字退化 */ }
        return t.Name.EndsWith("Area", StringComparison.Ordinal) ? t.Name[..^4] : t.Name;
    }
}
#endif
