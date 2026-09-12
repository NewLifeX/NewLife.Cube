using System.ComponentModel;

namespace CubeDemo.Areas.Test;

/// <summary>
/// 测试枚举。用于验证 枚举 / 单选(singleSelect) / 多选(multipleSelect) 三种 LOV 场景。
/// <para>
/// 值集为代码声明（枚举成员 + Description），运行时由 <see cref="NewLife.Cube.Services.LovRegistry"/>
/// 按 <c>Enum.CubeDemo.Areas.Test.测试枚举</c>（FullName）反射直读，无需落库，也无需手工维护。</summary>
/// </para>
/// </summary>
public enum 测试枚举
{
    /// <summary>未知</summary>
    [Description("未知")]
    未知 = 0,

    /// <summary>选项一</summary>
    [Description("选项一")]
    选项一 = 1,

    /// <summary>选项二</summary>
    [Description("选项二")]
    选项二 = 2,

    /// <summary>选项三</summary>
    [Description("选项三")]
    选项三 = 3,
}
