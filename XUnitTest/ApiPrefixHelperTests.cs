using System;
using NewLife.Web;
using Xunit;

namespace XUnitTest;

/// <summary>API前缀辅助方法单元测试</summary>
/// <remarks>验证 WebHelper.TrimApiPrefix：WebAPI版实体/后台控制器路由固定 /api 前缀，菜单/权限/回跳地址需还原为前端路由；MVC版无前缀为no-op</remarks>
public class ApiPrefixHelperTests
{
    [Theory(DisplayName = "TrimApiPrefix 去掉开头的 /api")]
    [InlineData("/api/Admin/User", "/Admin/User")]
    [InlineData("/api/Admin/User/Index", "/Admin/User/Index")]
    [InlineData("/api/Sso/Login?name=NewLife", "/Sso/Login?name=NewLife")]
    public void TrimApiPrefix_RemovesPrefix(String input, String expected)
    {
        Assert.Equal(expected, input.TrimApiPrefix());
    }

    [Fact(DisplayName = "TrimApiPrefix 无前缀/边界值原样返回")]
    public void TrimApiPrefix_NoPrefix_NoChange()
    {
        Assert.Equal("/Admin/User", "/Admin/User".TrimApiPrefix());
        Assert.Equal("/api", "/api".TrimApiPrefix());
        Assert.Equal("/apix/User", "/apix/User".TrimApiPrefix());
        Assert.Equal("", "".TrimApiPrefix());
        Assert.Null(((String)null).TrimApiPrefix());
    }
}
