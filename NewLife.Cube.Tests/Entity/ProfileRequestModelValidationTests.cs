using System;
using System.Linq;
using System.Text.Json;
using NewLife.Cube.Entity;
using Xunit;

namespace NewLife.Cube.Tests.Entity;

/// <summary>呈现/视图/评论请求模型的隐式必填守护</summary>
/// <remarks>
/// 症状：刷新实体页时前端顶部报错 “The Remark field is required.; The CardJson field is required.; …”（8 项），
/// 即 `PUT /Cube/ViewProfile` 返回 400。
/// 根因：模型成员无从值、又是非空引用类型（`Nullable=annotations` 下 MVC 仍按 CLR 类型判定），
/// .NET 6+ 默认 `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=false`，
/// 于是被隐式加上 `[Required]`；前端 `api-core` 提交时会省略 `undefined` 字段（`PUT` 只带变更域），
/// 缺字段即 400。历史上同类问题已出现三次（Widget 查询、Workflow 请求、本次呈现配置）。
/// 修复：请求模型的引用类型成员一律声明为可空（空值语义为“不覆盖”）。
/// </remarks>
public class ProfileRequestModelValidationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact(DisplayName = "ViewProfileModel：不得存在隐式必填成员")]
    public void ViewProfileModel_No_Implicitly_Required()
    {
        var required = ModelValidationHarness.ImplicitlyRequiredProperties<ViewProfileModel>();

        Assert.True(required.Length == 0, $"以下成员被隐式推断为必填，前端省略字段即 400：{String.Join(",", required)}");
    }

    [Fact(DisplayName = "UserProfileModel：不得存在隐式必填成员")]
    public void UserProfileModel_No_Implicitly_Required()
    {
        var required = ModelValidationHarness.ImplicitlyRequiredProperties<UserProfileModel>();

        Assert.True(required.Length == 0, $"以下成员被隐式推断为必填，前端省略字段即 400：{String.Join(",", required)}");
    }

    [Fact(DisplayName = "EntityCommentModel：不得存在隐式必填成员")]
    public void EntityCommentModel_No_Implicitly_Required()
    {
        var required = ModelValidationHarness.ImplicitlyRequiredProperties<EntityCommentModel>();

        Assert.True(required.Length == 0, $"以下成员被隐式推断为必填，前端省略字段即 400：{String.Join(",", required)}");
    }

    [Theory(DisplayName = "ViewProfile：前端局部域提交可通过 MVC 校验")]
    // web stores/viewProfile.ts：非个人域且未改动视图时，payload 删掉 viewsJson/view/activeViewId/columnsJson
    [InlineData("""{"typePath":"Admin/User","pageSize":20,"queriesJson":"[]","activeQueryId":""}""")]
    // 仪表盘域单独提交（views.openDashboard 保存）
    [InlineData("""{"typePath":"Admin/User","dashboardJson":"{\"version\":1,\"widgets\":[]}"}""")]
    // 清空视图域
    [InlineData("""{"typePath":"Admin/User","viewsJson":""}""")]
    public void ViewProfile_PartialBody_Passes_ModelValidation(String json)
    {
        var model = JsonSerializer.Deserialize<ViewProfileModel>(json, JsonOptions);
        Assert.NotNull(model);

        ModelValidationHarness.AssertValid(model!);
    }

    [Theory(DisplayName = "UserProfile：前端局部域提交可通过 MVC 校验")]
    [InlineData("""{"layoutJson":"{\"mode\":\"side\"}"}""")]
    [InlineData("""{"themeJson":"{\"primaryColor\":\"#165DFF\"}","version":1}""")]
    [InlineData("""{"homeJson":"{\"version\":1,\"widgets\":[]}"}""")]
    public void UserProfile_PartialBody_Passes_ModelValidation(String json)
    {
        var model = JsonSerializer.Deserialize<UserProfileModel>(json, JsonOptions);
        Assert.NotNull(model);

        ModelValidationHarness.AssertValid(model!);
    }

    [Theory(DisplayName = "EntityComment：评论提交可通过 MVC 校验")]
    [InlineData("""{"category":"Admin/User","linkId":1,"content":"第一条评论"}""")]
    [InlineData("""{"category":"Admin/User","linkId":1,"parentId":3,"content":"回复"}""")]
    public void EntityComment_PartialBody_Passes_ModelValidation(String json)
    {
        var model = JsonSerializer.Deserialize<EntityCommentModel>(json, JsonOptions);
        Assert.NotNull(model);

        ModelValidationHarness.AssertValid(model!);
    }
}
