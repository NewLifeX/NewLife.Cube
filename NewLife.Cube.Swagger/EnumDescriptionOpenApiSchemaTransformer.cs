#if NET10_0_OR_GREATER
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NewLife.Cube.Swagger;

/// <summary>内建 OpenAPI Schema Transformer：与 <see cref="EnumDescriptionSchemaFilter"/> 同逻辑，供构建期契约导出。</summary>
public sealed class EnumDescriptionOpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <summary>为枚举 schema 注入取值表。</summary>
    /// <param name="schema">目标 schema。</param>
    /// <param name="context">转换上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        Type? clrType = null;

        var prop = context.JsonPropertyInfo;
        if (prop != null)
        {
            clrType = prop.PropertyType;
            if (clrType == null && prop.AttributeProvider is System.Reflection.PropertyInfo pi)
                clrType = pi.PropertyType;
        }

        clrType ??= context.JsonTypeInfo?.Type;

        var description = schema.Description;
        EnumDescriptionTable.AppendIfNeeded(ref description, clrType);
        schema.Description = description;

        return Task.CompletedTask;
    }
}
#endif
