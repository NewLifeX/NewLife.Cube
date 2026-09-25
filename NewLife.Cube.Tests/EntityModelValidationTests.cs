using System;
using NewLife.Cube.Workflow.Entity;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>实体模型绑定：非空 String 属性不得被 MVC 隐式必填（实体页按需提交回归守护）。</summary>
/// <remarks>
/// 症状：添加/编辑记录返回 code=-2 “The X field is required.”，实体页写入整体不可用。
/// 根因：项目启用 Nullable 标注后，实体（TModel）无默认值的 String 属性被 MVC 隐式推断为 [Required]；
/// 前端表单按需提交（省略 undefined 字段）即触发拦截。
/// 修复：CubeService.AddCube 统一关闭该推断（ConfigureEntityModelValidation），本测试与其共享开关。
/// </remarks>
public class EntityModelValidationTests
{
    [Fact(DisplayName = "实体模型：未提交的 String 字段不再被隐式必填（WorkflowDefinition）")]
    public void WorkflowDefinition_PartialBody_Passes_UnderCubeOptions()
    {
        // 模拟添加表单按需提交：只带表单字段（名称 + 实体路径），其余字段不提交
        var model = new WorkflowDefinition { Name = "表单验证", TypePath = "School/Class" };

        // 默认 MVC 管线会报隐式必填（问题存在，测试具备区分度）
        var before = ModelValidationHarness.Validate(model);
        Assert.NotEmpty(before);

        // Cube 实际配置（ConfigureEntityModelValidation）下应通过
        var after = ModelValidationHarness.ValidateAsCube(model);
        Assert.Empty(after);
    }

    [Fact(DisplayName = "实体模型：未提交的 String 字段不再被隐式必填（WorkflowDefinitionModel）")]
    public void DefinitionModel_PartialBody_Passes_UnderCubeOptions()
    {
        var model = new WorkflowDefinitionModel { Name = "表单验证", TypePath = "School/Class" };

        var after = ModelValidationHarness.ValidateAsCube(model);
        Assert.Empty(after);
    }
}
