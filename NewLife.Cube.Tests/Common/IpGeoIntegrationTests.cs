using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using NewLife.Cube;
using NewLife.IP;
using NewLife.Net;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// SYS-6 IP 地理信息集成测试。验证 AddCube 注册 IP 解析器、环回与空地址走框架内置处理且不抛异常。
/// </summary>
public class IpGeoIntegrationTests
{
    [Fact(DisplayName = "IP属地_AddCube注册IP解析器")]
    public void AddCube_RegistersIpResolver()
    {
        var services = new ServiceCollection();
        services.AddCube();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(NetHelper.IpResolver);
        Assert.IsType<IpResolver>(NetHelper.IpResolver);
    }

    [Fact(DisplayName = "IP属地_环回地址返回内置描述")]
    public void GetAddress_Loopback_ReturnsBuiltinText()
    {
        var text = IPAddress.Loopback.GetAddress();

        Assert.False(text.IsNullOrEmpty());
    }

    [Fact(DisplayName = "IP属地_空地址与非法地址不抛异常")]
    public void GetAddress_InvalidInput_NoThrow()
    {
        Assert.Equal(String.Empty, "".IPToAddress());
        Assert.Equal(String.Empty, "not-an-ip".IPToAddress());
        // 私网地址在离线地址库下允许为空，但不能抛异常
        Assert.NotNull("192.168.1.1".IPToAddress() ?? String.Empty);
    }
}
