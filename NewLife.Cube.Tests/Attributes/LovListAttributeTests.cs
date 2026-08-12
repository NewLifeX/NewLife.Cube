using System;
using NewLife.Cube;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>
/// 覆盖 <see cref="LovListAttribute"/> 的默认值约定：
/// 默认 GET、分页开启、值/标签字段为 id/name、不代理请求，与前端 direct-request 的默认行为一致。
/// </summary>
public class LovListAttributeTests
{
    [Fact]
    public void Defaults_AreSane()
    {
        var attr = new LovListAttribute();

        Assert.Equal("GET", attr.Method);
        Assert.True(attr.Pageable);
        Assert.Equal("id", attr.ValueField);
        Assert.Equal("name", attr.LabelField);
        Assert.False(attr.ProxyRequest);
        Assert.Equal("pageIndex", attr.PageNumField);
        Assert.Equal("pageSize", attr.PageSizeField);
    }

    [Fact]
    public void LovCodePrefix_Convention()
    {
        // 列表型值集约定 LovCode 以 List. 开头（RegisterList 会校验）
        var attr = new LovListAttribute { LovCode = "List.CubeDemo.Role" };
        Assert.StartsWith("List.", attr.LovCode, StringComparison.OrdinalIgnoreCase);
    }
}
