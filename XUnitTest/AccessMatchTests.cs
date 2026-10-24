using NewLife.Cube.Services;
using Xunit;

namespace XUnitTest;

/// <summary>访问规则匹配单测。覆盖空规则放行、通配符、逗号多值与 `!` 取反</summary>
public class AccessMatchTests
{
    [Fact(DisplayName = "访问规则：空规则放行，规则有值但目标为空则不放行")]
    public void IsMatchPattern_EmptyCases()
    {
        Assert.True(AccessService.IsMatchPattern(null, "/api/user"));
        Assert.True(AccessService.IsMatchPattern("", "/api/user"));
        Assert.False(AccessService.IsMatchPattern("/api/user", null));
        Assert.False(AccessService.IsMatchPattern("/api/user", ""));
    }

    [Fact(DisplayName = "访问规则：精确匹配与通配符")]
    public void IsMatchPattern_Wildcard()
    {
        Assert.True(AccessService.IsMatchPattern("/api/user", "/api/user"));
        Assert.False(AccessService.IsMatchPattern("/api/user", "/api/order"));
        Assert.True(AccessService.IsMatchPattern("/api/*", "/api/user"));
        Assert.False(AccessService.IsMatchPattern("/api/*", "/admin/user"));
        Assert.True(AccessService.IsMatchPattern("*", "/any/url"));
    }

    [Fact(DisplayName = "访问规则：逗号分隔任一项命中即通过")]
    public void IsMatchPattern_CommaList()
    {
        Assert.True(AccessService.IsMatchPattern("/api/user,/api/order", "/api/order"));
        Assert.False(AccessService.IsMatchPattern("/api/user,/api/order", "/api/role"));

        // 路径匹配区分大小写（与 StringHelper.IsMatch 一致，契合 Linux 路径语义）
        Assert.False(AccessService.IsMatchPattern("/API/USER", "/api/user"));
    }

    [Fact(DisplayName = "访问规则：`!` 前缀表示取反")]
    public void IsMatchPattern_Negation()
    {
        Assert.False(AccessService.IsMatchPattern("!/api/user", "/api/user"));
        Assert.True(AccessService.IsMatchPattern("!/api/user", "/api/order"));

        // 多值时全部都不命中才通过
        Assert.False(AccessService.IsMatchPattern("!/api/user,!/api/order", "/api/order"));
        Assert.True(AccessService.IsMatchPattern("!/api/user,!/api/order", "/api/role"));
    }

    [Fact(DisplayName = "访问规则：IP 网段与用户代理匹配")]
    public void IsMatchPattern_IpAndUserAgent()
    {
        Assert.True(AccessService.IsMatchPattern("192.168.*", "192.168.1.10"));
        Assert.False(AccessService.IsMatchPattern("192.168.*", "10.0.0.1"));
        Assert.True(AccessService.IsMatchPattern("*bot*", "Mozilla/5.0 (compatible; Googlebot/2.1)"));
    }
}
