using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NewLife.Cube.Areas.Cube.Controllers;
using NewLife.Cube.ViewModels;
using NewLife.Cube.Workflow;
using NewLife.Cube.Workflow.Entity;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>流程定义添加表单收敛与新建默认值。</summary>
/// <remarks>
/// 需求：添加记录表单名称置顶、实体路径为友好名下拉、其余字段自动默认（表单不再出现）、备注长度对齐其它实体。
/// 编辑表单字段集必须保持原样：编辑保存为整表 PUT 回填，删字段会把图 JSON 等未提交列清空。
/// </remarks>
public class WorkflowDefinitionControllerTests
{
    public WorkflowDefinitionControllerTests()
    {
        DAL.AddConnStr("Cube", "Data Source=Osc260925DefCube;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Log", "Data Source=Osc260925DefLog;Mode=Memory;Cache=Shared", null, "SQLite");
        DAL.AddConnStr("Workflow", "Data Source=Osc260925DefForm;Mode=Memory;Cache=Shared", null, "SQLite");
        WorkflowTestDb.EnsureWorkflowTables();
        WorkflowDefinition.FindAll().Delete();
    }

    [Fact(DisplayName = "添加表单：名称置顶、实体下拉、其余字段不出现")]
    public void AddForm_Only_Name_TypePath_Remark()
    {
        var names = GetFormFields("AddFormFields").Select(e => e.Name).ToList();

        // 前端渲染会过滤主键；名称必须是可见的第一字段，实体紧随其后
        var visible = names.Where(n => n != "Id").ToList();
        Assert.Equal("Name", visible[0]);
        Assert.Equal("TypePath", visible[1]);
        Assert.Contains("Remark", visible);

        // 图 JSON、发起条件、版本、锁策略、启用、发布、租户不再出现在添加表单（由默认值与设计器维护）
        Assert.DoesNotContain("GraphJson", names);
        Assert.DoesNotContain("PublishedGraphJson", names);
        Assert.DoesNotContain("StartFilter", names);
        Assert.DoesNotContain("LockPolicy", names);
        Assert.DoesNotContain("Version", names);
        Assert.DoesNotContain("Published", names);
        Assert.DoesNotContain("Enable", names);
        Assert.DoesNotContain("TenantId", names);
        Assert.DoesNotContain("TenantName", names);

        // 编辑表单保持原字段集（PUT 整表回填，删字段会清空未提交列）
        var edit = GetFormFields("EditFormFields").Select(e => e.Name).ToList();
        Assert.Contains("GraphJson", edit);
        Assert.Contains("StartFilter", edit);
    }

    [Fact(DisplayName = "实体路径：添加/编辑表单均为友好名下拉候选")]
    public void TypePath_Has_DataSource()
    {
        foreach (var prop in new[] { "AddFormFields", "EditFormFields" })
        {
            var tf = GetFormFields(prop).FirstOrDefault(e => e.Name == "TypePath");
            Assert.NotNull(tf);
            // 只验证已挂载候选委托（与 /Cube/Automation/Entities 同口径，生成逻辑在浏览器实测覆盖）；
            // 不在单测里调用委托：会触达 Menu/Membership 查询，污染其它用例的库上下文
            Assert.NotNull(tf!.DataSource);
        }
    }

    [Fact(DisplayName = "新建默认值：表单通道补齐启用/图草稿，实体直插保持原样")]
    public void Insert_Defaults()
    {
        // 表单通道（控制器 Add → OnInsert）：模拟添加表单只提交 名称/实体/备注，其余自动默认
        var viaForm = new WorkflowDefinition
        {
            TypePath = "Cube/NotificationRecord",
            Name = "表单" + Guid.NewGuid().ToString("N")[..8],
        };
        InvokeOnInsert(viaForm);

        var got = WorkflowDefinition.FindById(viaForm.Id);
        Assert.NotNull(got);
        Assert.True(got!.Enable);
        Assert.Equal(1, got.Version);
        Assert.Equal(WorkflowStatuses.LockFull, got.LockPolicy);
        Assert.Equal("{}", got.StartFilter);
        Assert.Equal("{}", got.GraphJson);
        Assert.False(got.Published);

        // 实体直插（引擎/测试通道）：不代为修改，避免污染非表单场景
        var raw = new WorkflowDefinition
        {
            TypePath = "Cube/NotificationRecord",
            Name = "直插" + Guid.NewGuid().ToString("N")[..8],
        };
        raw.Insert();

        var got2 = WorkflowDefinition.FindById(raw.Id);
        Assert.NotNull(got2);
        Assert.False(got2!.Enable);
    }

    /// <summary>反射调用控制器 OnInsert（等价添加记录表单提交）</summary>
    /// <param name="entity">待插入实体</param>
    static void InvokeOnInsert(WorkflowDefinition entity)
    {
        var ctrl = new WorkflowDefinitionController();
        var mi = typeof(WorkflowDefinitionController).GetMethod("OnInsert", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);
        mi!.Invoke(ctrl, [entity]);
    }

    /// <summary>反射获取控制器静态表单字段集合（触发静态构造完成定制）</summary>
    /// <param name="name">AddFormFields / EditFormFields</param>
    /// <returns>字段集合</returns>
    static List<DataField> GetFormFields(String name)
    {
        RuntimeHelpers.RunClassConstructor(typeof(WorkflowDefinitionController).TypeHandle);
        var pi = typeof(WorkflowDefinitionController).GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        Assert.NotNull(pi);
        var fc = pi!.GetValue(null) as FieldCollection;
        Assert.NotNull(fc);
        return fc!;
    }
}
