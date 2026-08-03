using System;
using System.Text.Json;
using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>
/// 覆盖 <see cref="DefaultLovListDataProxy.ResolveJsonPath"/> 的 JSON 路径解析逻辑。
/// 该静态方法从响应根节点按点号路径抽取子节点，空路径返回根，路径不存在或非对象遍历返回 null。
/// </summary>
public class ResolveJsonPathTests
{
    private static JsonElement Parse(String json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public void EmptyPath_ReturnsRoot()
    {
        var root = Parse("{\"data\":[1,2],\"total\":5}");
        var result = DefaultLovListDataProxy.ResolveJsonPath(root, "");
        Assert.True(result.HasValue);
        Assert.Equal(JsonValueKind.Object, result!.Value.ValueKind);
    }

    [Fact]
    public void SingleSegment_NavigatesToProperty()
    {
        var root = Parse("{\"data\":[1,2],\"total\":5}");
        var result = DefaultLovListDataProxy.ResolveJsonPath(root, "data");
        Assert.True(result.HasValue);
        Assert.Equal(JsonValueKind.Array, result!.Value.ValueKind);
        Assert.Equal(2, result.Value.GetArrayLength());
    }

    [Fact]
    public void NestedPath_NavigatesDeep()
    {
        var root = Parse("{\"result\":{\"records\":[{\"id\":1}]}}");
        var result = DefaultLovListDataProxy.ResolveJsonPath(root, "result.records");
        Assert.True(result.HasValue);
        Assert.Equal(JsonValueKind.Array, result!.Value.ValueKind);
        Assert.Equal(1, result.Value.GetArrayLength());
    }

    [Fact]
    public void MissingProperty_ReturnsNull()
    {
        var root = Parse("{\"data\":[]}");
        var result = DefaultLovListDataProxy.ResolveJsonPath(root, "missing");
        Assert.False(result.HasValue);
    }

    [Fact]
    public void PathIntoNonObject_ReturnsNull()
    {
        var root = Parse("{\"data\":[1,2]}");
        // data 是数组，再取 .x 应失败
        var result = DefaultLovListDataProxy.ResolveJsonPath(root, "data.x");
        Assert.False(result.HasValue);
    }
}
