using NewLife.Cube.ViewModels;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest;

/// <summary>登录配置序列化契约测试</summary>
public class LoginConfigSerializationTests
{
    [Fact(DisplayName = "LoginConfig 的 OAuth 在驼峰策略下输出 oauth 而非 oAuth")]
    public void OAuth_Serializes_As_Lowercase_Oauth()
    {
        var model = new LoginConfigModel();
        var writer = new JsonWriter();
        writer.Options.PropertyNaming = PropertyNaming.CamelCase;
        writer.Write(new { code = 0, data = model });
        var json = writer.GetString();

        Assert.Contains("\"oauth\":", json);
        Assert.DoesNotContain("\"oAuth\":", json);
    }
}
