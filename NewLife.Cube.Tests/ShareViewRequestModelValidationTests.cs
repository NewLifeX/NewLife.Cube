using NewLife.Cube.ViewModels;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>分享请求模型：可选 ViewId/Slug 不得被 MVC 隐式必填</summary>
public class ShareViewRequestModelValidationTests
{
    [Fact(DisplayName = "ShareViewRequest：ViewId/Slug 不得隐式必填")]
    public void No_Implicitly_Required_Optional_Fields()
    {
        var required = ModelValidationHarness.ImplicitlyRequiredProperties<ShareViewRequest>();
        Assert.True(required.Length == 0, $"以下成员被隐式推断为必填，前端省略字段即 400：{string.Join(",", required)}");
    }

    [Theory(DisplayName = "ShareViewRequest：仅 expireSeconds 或带 slug/viewId 均可通过校验")]
    [InlineData("""{"expireSeconds":86400}""")]
    [InlineData("""{"slug":"ops","expireSeconds":3600}""")]
    [InlineData("""{"viewId":"default","expireSeconds":3600}""")]
    public void PartialBody_Passes_ModelValidation(string json)
    {
        ModelValidationHarness.AssertJsonValid<ShareViewRequest>(json);
    }
}
