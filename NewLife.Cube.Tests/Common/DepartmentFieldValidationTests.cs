using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NewLife.Cube.Areas.Admin.Controllers;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>部门表单字段级校验（OSC-260925 审计）：Map 查找展示列不得按展示名取值误报必填。</summary>
/// <remarks>
/// 症状：部门添加（只填名称）返回「添加失败！父级不可以为空！」。
/// 根因：表单分区 SetRelation 把物理列换成查找展示列（ParentID→ParentName，MapField=ParentID），
/// API 输出前会还原物理列（MapCandidateFiller.RestorePhysicalColumnName），用户提交的也是 ParentID；
/// 而 ValidateEntityFields 按展示列取值，entity[ParentName] 仅在已选父级时才有值，ParentID=0（根部门）被误判为空。
/// 修复：按 MapField（物理列）取值与匹配 onlyFields。
/// </remarks>
[Collection("Osc260925Dept")]
public class DepartmentFieldValidationTests
{
    // 纯元数据校验（不触库）：不注册全局连接名，避免改写并行用例的 Cube/Membership 连接

    [Fact(DisplayName = "部门添加：ParentID=0（根部门）不误报「父级不可以为空」")]
    public void Insert_RootDepartment_NoParentError()
    {
        var errors = Validate(new Department { Name = "审计测试部" }, DataObjectMethodType.Insert);
        Assert.True(
            errors == null,
            $"根部门（ParentID=0）不应产生字段错误：{String.Join(";", (errors ?? []).Select(e => e.Message))}");
    }

    [Fact(DisplayName = "部门添加：名称缺失仍报「名称不可以为空」（必填语义不回退）")]
    public void Insert_EmptyName_StillFails()
    {
        var errors = Validate(new Department { Name = null }, DataObjectMethodType.Insert);
        Assert.NotNull(errors);
        Assert.Contains(errors!, e => (e.Message ?? "").Contains("名称"));
    }

    /// <summary>反射调用控制器的字段级校验（与插入/更新校验同源）</summary>
    /// <param name="entity">部门实体</param>
    /// <param name="type">操作类型</param>
    /// <returns>字段错误列表，无错误时 null</returns>
    static List<FieldError> Validate(Department entity, DataObjectMethodType type)
    {
        // 方法为基类 private static（私有成员不随派生类型继承，必须用泛型基类查找）
        var mi = typeof(EntityController<Department, DepartmentModel>).GetMethod("ValidateEntityFields", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi);
        return mi!.Invoke(null, [entity, type, null]) as List<FieldError>;
    }
}
