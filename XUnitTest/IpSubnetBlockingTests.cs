using System;
using System.Reflection;
using NewLife.Cube.Services;
using Xunit;

namespace XUnitTest;

/// <summary>IP子网封禁策略单元测试</summary>
public class IpSubnetBlockingTests
{
    #region 辅助：通过反射调用私有静态方法

    private static readonly MethodInfo _getSubnet24 = typeof(UserService)
        .GetMethod("GetSubnet24", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo _getSubnet16 = typeof(UserService)
        .GetMethod("GetSubnet16", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static String Subnet24(String ip) => (String)_getSubnet24.Invoke(null, [ip])!;
    private static String Subnet16(String ip) => (String)_getSubnet16.Invoke(null, [ip])!;

    #endregion

    #region GetSubnet24 测试

    [Theory(DisplayName = "GetSubnet24：标准IPv4提取三段前缀")]
    [InlineData("103.125.146.71", "103.125.146")]
    [InlineData("192.168.1.100", "192.168.1")]
    [InlineData("10.0.0.1", "10.0.0")]
    [InlineData("255.255.255.255", "255.255.255")]
    public void GetSubnet24_StandardIpv4_ReturnsThreeSegmentPrefix(String ip, String expected)
    {
        var result = Subnet24(ip);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "GetSubnet24：段数不足时返回原值")]
    [InlineData("192.168")]
    [InlineData("192")]
    public void GetSubnet24_ShortIp_ReturnsOriginal(String ip)
    {
        var result = Subnet24(ip);
        Assert.Equal(ip, result);
    }

    [Theory(DisplayName = "GetSubnet24：空值或null时返回原值")]
    [InlineData("")]
    [InlineData(null)]
    public void GetSubnet24_NullOrEmpty_ReturnsOriginal(String ip)
    {
        var result = Subnet24(ip);
        Assert.Equal(ip, result);
    }

    #endregion

    #region GetSubnet16 测试

    [Theory(DisplayName = "GetSubnet16：标准IPv4提取两段前缀")]
    [InlineData("103.125.146.71", "103.125")]
    [InlineData("192.168.1.100", "192.168")]
    [InlineData("10.0.0.1", "10.0")]
    [InlineData("255.255.255.255", "255.255")]
    public void GetSubnet16_StandardIpv4_ReturnsTwoSegmentPrefix(String ip, String expected)
    {
        var result = Subnet16(ip);
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "GetSubnet16：段数不足时返回原值")]
    [InlineData("192")]
    public void GetSubnet16_ShortIp_ReturnsOriginal(String ip)
    {
        var result = Subnet16(ip);
        Assert.Equal(ip, result);
    }

    [Theory(DisplayName = "GetSubnet16：空值或null时返回原值")]
    [InlineData("")]
    [InlineData(null)]
    public void GetSubnet16_NullOrEmpty_ReturnsOriginal(String ip)
    {
        var result = Subnet16(ip);
        Assert.Equal(ip, result);
    }

    #endregion

    #region 子网前缀一致性测试

    [Fact(DisplayName = "同一/24子网下不同IP具有相同三段前缀")]
    public void GetSubnet24_SameSubnet_SamePrefix()
    {
        var ips = new[] { "103.125.146.36", "103.125.146.54", "103.125.146.71", "103.125.146.44" };
        var prefixes = System.Array.ConvertAll(ips, Subnet24);
        Assert.All(prefixes, p => Assert.Equal("103.125.146", p));
    }

    [Fact(DisplayName = "同一/16子网下不同IP具有相同两段前缀")]
    public void GetSubnet16_SameSubnet_SamePrefix()
    {
        var ips = new[] { "103.125.146.36", "103.125.147.10", "103.125.1.1" };
        var prefixes = System.Array.ConvertAll(ips, Subnet16);
        Assert.All(prefixes, p => Assert.Equal("103.125", p));
    }

    [Fact(DisplayName = "不同/24子网下IP具有不同三段前缀")]
    public void GetSubnet24_DifferentSubnets_DifferentPrefixes()
    {
        var prefix1 = Subnet24("103.125.146.71");
        var prefix2 = Subnet24("103.125.147.71");
        Assert.NotEqual(prefix1, prefix2);
    }

    #endregion
}
