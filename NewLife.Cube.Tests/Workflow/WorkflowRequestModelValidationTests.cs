using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Controllers;
using NewLife.Cube.Workflow.Controllers;
using Xunit;

namespace NewLife.Cube.Tests.Workflow;

/// <summary>审批流程请求契约校验。守护「非空引用类型被 MVC 隐式必填」导致的 400</summary>
/// <remarks>
/// 症状：打开流程设计页（GET /Cube/Workflow/Definitions 不带 typePath）弹 “The typePath field is required.”；
/// 同族还有 approve(id) 不带 body、createDefinition 省略 lockPolicy/remark 等。
/// 根因与实体部件取数一致：Nullable 标注 + MVC 默认不抑制隐式 [Required]。
/// 该控制器所有请求模型成员与可选参数一律可空，必填语义在动作内部以中文提示校验。
/// </remarks>
public class WorkflowRequestModelValidationTests
{
    [Theory(DisplayName = "工作流请求体：前端真实提交可通过 MVC 校验")]
    // 同意/驳回不带意见：前端 data 可为 undefined（无 body）或 {}
    [InlineData(typeof(WorkflowController.CommentModel), """{}""")]
    [InlineData(typeof(WorkflowController.CommentModel), """{"comment":""}""")]
    [InlineData(typeof(WorkflowController.CommentModel), """{"comment":"同意","attachmentIds":[1,2]}""")]
    // 跳转/回退：targetNodeId 必填由动作校验，comment 可省略
    [InlineData(typeof(WorkflowController.JumpModel), """{"targetNodeId":"n1"}""")]
    [InlineData(typeof(WorkflowController.RollbackModel), """{"targetNodeId":"n1","comment":"退回到上一节点"}""")]
    // 转办/知会/加签：body 可省略 comment
    [InlineData(typeof(WorkflowController.ToModel), """{"to":{"kind":"users","ids":[1]}}""")]
    [InlineData(typeof(WorkflowController.AddSignModel), """{"to":{"kind":"users","ids":[1]},"before":false}""")]
    // 批量同意：comment 可省略
    [InlineData(typeof(WorkflowController.BatchModel), """{"ids":[1,2]}""")]
    // 常用语：texts 可省略（清空）
    [InlineData(typeof(WorkflowController.PhraseModel), """{"texts":["已阅"]}""")]
    [InlineData(typeof(WorkflowController.PhraseModel), """{}""")]
    // 新建/保存草稿：api-core 只传部分字段（Partial<WorkflowDefinitionItem>）
    [InlineData(typeof(WorkflowController.WorkflowDefModel), """{"typePath":"Admin/User","name":"审批"}""")]
    [InlineData(typeof(WorkflowController.WorkflowDefModel), """{"typePath":"Admin/User","name":"审批","graphJson":"{}","startFilter":"{}","lockPolicy":"full","remark":"备注","enable":true}""")]
    // 发起：comment/summary/title 可省略
    [InlineData(typeof(WorkflowController.StartModel), """{"typePath":"Admin/User","keys":["1"],"definitionId":"123"}""")]
    [InlineData(typeof(WorkflowController.StartModel), """{"typePath":"Admin/User","keys":["1","2"],"definitionId":"123","comment":"请审批","summary":"摘要","title":"标题"}""")]
    public void Request_Body_Passes_ModelValidation(Type modelType, String json)
    {
        var model = ModelValidationHarness.Deserialize(json, modelType);
        Assert.NotNull(model);
        ModelValidationHarness.AssertValid(model!);
    }

    [Fact(DisplayName = "工作流请求模型：不允许存在被隐式推断为必填的成员")]
    public void Workflow_Models_Have_No_Implicitly_Required_Members()
    {
        var types = new[]
        {
            typeof(WorkflowController.WorkflowDefModel),
            typeof(WorkflowController.StartModel),
            typeof(WorkflowController.CommentModel),
            typeof(WorkflowController.JumpModel),
            typeof(WorkflowController.AddSignModel),
            typeof(WorkflowController.ToModel),
            typeof(WorkflowController.RollbackModel),
            typeof(WorkflowController.BatchModel),
            typeof(WorkflowController.PhraseModel),
        };

        var bad = new List<String>();
        foreach (var type in types)
        {
            var required = ModelValidationHarness.ImplicitlyRequiredProperties(type);
            if (required.Length > 0) bad.Add($"{type.Name}: {String.Join(",", required)}");
        }

        Assert.True(bad.Count == 0, $"以下请求模型存在隐式必填成员，前端省略字段时会 400：{String.Join(" | ", bad)}");
    }

    [Fact(DisplayName = "工作流控制器：可选查询参数与 FromBody 参数必须可空（允许空 body）")]
    public void Optional_Parameters_Are_Nullable()
    {
        // GET /Cube/Workflow/Definitions 不带 typePath（设计器页加载全部定义）
        var definitions = typeof(WorkflowController).GetMethod(nameof(WorkflowController.GetDefinitions))!;
        Assert.True(ModelValidationHarness.IsNullableParameter(definitions.GetParameters()[0]),
            "GetDefinitions.typePath 必须可空，否则前端 definitions({}) 会 400 “The typePath field is required.”");

        // 所有 [FromBody] 参数可空：前端 data 可省略（axios 不发 body），否则 MVC 报 “A non-empty request body is required.”
        var bad = new List<String>();
        foreach (var method in typeof(WorkflowController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var pi in method.GetParameters())
            {
                if (pi.GetCustomAttribute<FromBodyAttribute>() == null) continue;
                if (pi.ParameterType.IsValueType) continue;
                if (!ModelValidationHarness.IsNullableParameter(pi)) bad.Add($"{method.Name}({pi.Name})");
            }
        }
        foreach (var method in typeof(WidgetController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var pi in method.GetParameters())
            {
                if (pi.GetCustomAttribute<FromBodyAttribute>() == null) continue;
                if (pi.ParameterType.IsValueType) continue;
                if (!ModelValidationHarness.IsNullableParameter(pi)) bad.Add($"WidgetController.{method.Name}({pi.Name})");
            }
        }

        Assert.True(bad.Count == 0, $"以下 FromBody 参数应声明为可空，否则前端不带 body 时会 400：{String.Join(", ", bad)}");
    }
}
