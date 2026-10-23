using System.Reflection;
using System.Text.Json.Serialization;
using NewLife.Cube.ViewModels;
using NewLife.Serialization;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>登录配置接口契约测试</summary>
public class LoginConfigContractTests
{
    [Fact(DisplayName = "OAuth 提供商字段序列化为 oauth")]
    public void OAuth_Should_Serialize_As_Oauth()
    {
        var property = typeof(LoginConfigModel).GetProperty(nameof(LoginConfigModel.OAuth));

        Assert.NotNull(property);
        Assert.Equal("oauth", SerialHelper.GetName(property!));

        var attribute = property!.GetCustomAttribute<JsonPropertyNameAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal("oauth", attribute!.Name);
    }
}
