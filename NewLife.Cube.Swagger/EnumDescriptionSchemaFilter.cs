using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NewLife.Cube.Swagger;

/// <summary>Swashbuckle Schema 过滤器：为枚举类型 schema 自动追加 <c>[Description]</c> 取值表（运行时 Swagger UI / Scalar 读 <c>/swagger/*.json</c> 时生效）。</summary>
public sealed class EnumDescriptionSchemaFilter : ISchemaFilter
{
    /// <summary>在 schema 生成后注入枚举取值说明。</summary>
    /// <param name="schema">OpenAPI schema 节点。</param>
    /// <param name="context">含 CLR 类型与成员信息的生成上下文。</param>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete) return;

        var clrType = context.Type;
        if (context.MemberInfo is System.Reflection.PropertyInfo pi)
            clrType = pi.PropertyType;

        var description = concrete.Description;
        EnumDescriptionTable.AppendIfNeeded(ref description, clrType);
        concrete.Description = description;
    }
}
