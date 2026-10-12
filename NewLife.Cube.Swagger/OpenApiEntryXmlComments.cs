#if NET10_0_OR_GREATER
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NewLife;

namespace NewLife.Cube.Swagger;

/// <summary>项目侧 XML 注释导入（框架级）：读取「已加载程序集」的同名 .xml（<see cref="XmlCommentFiles.Collect"/>），
/// 把项目 <c>///</c> 注释并入内建 OpenAPI 文档（构建期各域契约文件）。
/// 行为镜像 .NET 10 官方 XmlComment 源生成器（Microsoft.AspNetCore.OpenApi.SourceGenerators），
/// 差异仅在读取来源：官方要求项目侧有 AddOpenApi 挂接点；此处由框架统一导入，项目只需开 GenerateDocumentationFile，
/// 零接线。运行时 Scalar/Swagger UI 侧的同名导入见 <see cref="SwaggerService"/>。</summary>
internal sealed class EntryXmlCommentOperationTransformer : IOpenApiOperationTransformer
{
    /// <summary>把 XML 注释的 summary/remarks/参数/返回说明应用到操作上（官方生成器同序）。</summary>
    /// <param name="operation">目标操作</param>
    /// <param name="context">转换上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor controller) return Task.CompletedTask;

        var method = controller.MethodInfo;
        if (method == null) return Task.CompletedTask;

        var id = XmlCommentDocId.Normalize(method.CreateDocId());
        if (!EntryXmlCommentCache.Map.TryGetValue(id, out var comment)) return Task.CompletedTask;

        if (comment.Summary is { } summary) operation.Summary = summary;
        if (comment.Description is { } description)
            operation.Description = description;
        else if (comment.Remarks is { } remarks)
            operation.Description = remarks;

        if (comment.Parameters is { Count: > 0 })
        {
            foreach (var paramComment in comment.Parameters)
            {
                var parameterInfo = method.GetParameters().FirstOrDefault(p => p.Name == paramComment.Key);
                if (parameterInfo == null) continue;

                var parameter = operation.Parameters?.FirstOrDefault(p => p.Name == parameterInfo.Name);
                if (parameter == null) continue;

                var target = UnwrapOpenApiParameter(parameter);
                if (target != null) target.Description = paramComment.Value;
            }
        }

        if (comment.Returns is { } returns && operation.Responses is { Count: 1 })
        {
            foreach (var response in operation.Responses)
            {
                response.Value.Description = returns;
            }
        }

        return Task.CompletedTask;
    }

    private static OpenApiParameter? UnwrapOpenApiParameter(IOpenApiParameter source)
    {
        if (source is OpenApiParameterReference reference) return reference.Target as OpenApiParameter;
        if (source is OpenApiParameter direct) return direct;
        return null;
    }
}

/// <summary>项目侧 XML 注释导入（Schema 侧）：类型/属性注释写入 schema（仅当描述为空时写入，避免覆盖枚举取值表等既有注入）。</summary>
internal sealed class EntryXmlCommentSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <summary>把 XML 注释的 summary 应用到类型/属性 schema 的 description。</summary>
    /// <param name="schema">目标 schema</param>
    /// <param name="context">转换上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var map = EntryXmlCommentCache.Map;
        if (map.Count == 0) return Task.CompletedTask;

        // 类型注释
        if (String.IsNullOrEmpty(schema.Description) && context.JsonTypeInfo?.Type is { } type)
        {
            var tid = XmlCommentDocId.Normalize(type.CreateDocId());
            if (map.TryGetValue(tid, out var typeComment) && typeComment.Summary is { } ts) schema.Description = ts;
        }

        // 属性注释
        if (String.IsNullOrEmpty(schema.Description) && context.JsonPropertyInfo is { AttributeProvider: PropertyInfo propertyInfo })
        {
            var pid = XmlCommentDocId.Normalize(propertyInfo.CreateDocId());
            if (map.TryGetValue(pid, out var propertyComment) && propertyComment.Summary is { } ps) schema.Description = ps;
        }

        return Task.CompletedTask;
    }
}

/// <summary>项目侧 XML 注释缓存：惰性读取 <see cref="XmlCommentFiles.Collect"/> 收集到的全部 .xml，按文档 ID 建索引。</summary>
internal static class EntryXmlCommentCache
{
    private static Dictionary<String, EntryXmlComment>? _map;

    /// <summary>注释索引（键=文档注释 ID；读取失败的文件静默跳过）。</summary>
    public static Dictionary<String, EntryXmlComment> Map => _map ??= Load();

    private static Dictionary<String, EntryXmlComment> Load()
    {
        var map = new Dictionary<String, EntryXmlComment>(StringComparer.Ordinal);
        foreach (var xml in XmlCommentFiles.Collect())
        {
            try
            {
                var doc = XDocument.Load(xml);
                var members = doc.Root?.Element("members");
                if (members == null) continue;

                foreach (var member in members.Elements("member"))
                {
                    var name = member.Attribute("name")?.Value;
                    if (name.IsNullOrEmpty() || map.ContainsKey(name!)) continue;

                    var parameters = member.Elements("param")
                        .Select(e => new KeyValuePair<String, String>(e.Attribute("name")?.Value ?? "", (e.Value ?? "").Trim()))
                        .Where(e => !e.Key.IsNullOrEmpty())
                        .ToList();

                    map[name!] = new EntryXmlComment(
                        Text(member, "summary"),
                        Text(member, "description"),
                        Text(member, "remarks"),
                        Text(member, "returns"),
                        parameters.Count > 0 ? parameters : null);
                }
            }
            catch
            {
                // 注释文件个别损坏不影响整体导入
            }
        }

        return map;
    }

    private static String? Text(XElement member, String tag) => member.Element(tag)?.Value?.Trim();
}

/// <summary>XML 注释条目（精简自官方 XmlComment 记录）。</summary>
internal sealed record EntryXmlComment(String? Summary, String? Description, String? Remarks, String? Returns, List<KeyValuePair<String, String>>? Parameters);

/// <summary>文档注释 ID 生成/归一。精简自 .NET 10 官方 XmlComment 源生成器的 DocumentationCommentIdHelper，
/// 行为对齐官方解析（含嵌套类型 '+'→'.'、`/`` 泛型哨兵、返回值后缀归一）。</summary>
internal static class XmlCommentDocId
{
    /// <summary>生成方法的文档注释 ID（如 <c>M:Ns.Type.Method(Param1,Param2)~Ret</c>）。</summary>
    /// <param name="method">方法</param>
    /// <returns>文档注释 ID</returns>
    public static String CreateDocId(this MethodInfo method)
    {
        var sb = new StringBuilder();
        sb.Append("M:");
        if (method.DeclaringType != null) sb.Append(GetTypeDocId(method.DeclaringType, includeGenericArguments: false, omitGenericArity: false));
        sb.Append('.');

        if (method.IsConstructor)
        {
            sb.Append(method.IsStatic ? "#cctor" : "#ctor");
        }
        else
        {
            sb.Append(method.Name);
            if (method.IsGenericMethod)
            {
                sb.Append("``");
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", method.GetGenericArguments().Length);
            }
        }

        var parameters = method.GetParameters();
        if (parameters.Length > 0)
        {
            sb.Append('(');
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(GetTypeDocId(parameters[i].ParameterType, includeGenericArguments: true, omitGenericArity: true));
            }
            sb.Append(')');
        }

        if (method.ReturnType != typeof(void))
        {
            sb.Append('~');
            sb.Append(GetTypeDocId(method.ReturnType, includeGenericArguments: true, omitGenericArity: true));
        }

        return sb.ToString();
    }

    /// <summary>生成类型的文档注释 ID（如 <c>T:Ns.Outer.Inner`1</c>）。</summary>
    /// <param name="type">类型</param>
    /// <returns>文档注释 ID</returns>
    public static String CreateDocId(this Type type) => "T:" + GetTypeDocId(type, includeGenericArguments: false, omitGenericArity: false);

    /// <summary>生成属性的文档注释 ID（如 <c>P:Ns.Type.Prop</c>）。</summary>
    /// <param name="property">属性</param>
    /// <returns>文档注释 ID</returns>
    public static String CreateDocId(this PropertyInfo property)
    {
        var sb = new StringBuilder();
        sb.Append("P:");
        if (property.DeclaringType != null) sb.Append(GetTypeDocId(property.DeclaringType, includeGenericArguments: false, omitGenericArity: false));
        sb.Append('.');
        sb.Append(property.Name);

        var indexParams = property.GetIndexParameters();
        if (indexParams.Length > 0)
        {
            sb.Append('(');
            for (var i = 0; i < indexParams.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(GetTypeDocId(indexParams[i].ParameterType, includeGenericArguments: true, omitGenericArity: false));
            }
            sb.Append(')');
        }

        return sb.ToString();
    }

    /// <summary>归一化文档注释 ID：普通方法去掉返回值后缀（转换运算符保留），与源码 <c>///</c> 生成的 ID 对齐。</summary>
    /// <param name="docId">文档注释 ID</param>
    /// <returns>归一化后的 ID</returns>
    public static String Normalize(String docId)
    {
        var tildeIndex = docId.IndexOf('~');
        if (tildeIndex == -1) return docId;

        // 转换运算符保留返回值后缀
        if (docId.Contains("op_Implicit") || docId.Contains("op_Explicit")) return docId;

        return docId[..tildeIndex];
    }

    private static String GetTypeDocId(Type type, Boolean includeGenericArguments, Boolean omitGenericArity)
    {
        if (type.IsGenericParameter)
        {
            if (type.DeclaringMethod != null) return "``" + type.GenericParameterPosition;
            if (type.DeclaringType != null) return "`" + type.GenericParameterPosition;
            return type.Name;
        }

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var fullName = genericDef.FullName ?? genericDef.Name;

            var sb = new StringBuilder(fullName.Length);
            for (var i = 0; i < fullName.Length; i++)
            {
                var c = fullName[i];
                if (c == '+')
                    sb.Append('.');
                else if (c == '`')
                    break;
                else
                    sb.Append(c);
            }

            if (!omitGenericArity)
            {
                sb.Append('`');
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", genericDef.GetGenericArguments().Length);
            }

            if (includeGenericArguments && !type.IsGenericTypeDefinition)
            {
                var typeArgs = type.GetGenericArguments();
                sb.Append('{');
                for (var i = 0; i < typeArgs.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(GetTypeDocId(typeArgs[i], includeGenericArguments, omitGenericArity));
                }
                sb.Append('}');
            }

            return sb.ToString();
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }
}
#endif
