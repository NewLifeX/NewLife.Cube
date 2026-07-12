using System.ComponentModel;

namespace CubeDemo.Areas.Test;

/// <summary>
/// 测试枚举。用于验证 枚举 / 单选(singleSelect) / 多选(multipleSelect) 三种 LOV 场景。
/// <para>
/// <see cref="NewLife.Cube.Services.LovAutoRegisterService"/> 会在启动时扫描
/// <c>CubeDemo.Areas.Test</c> 命名空间下的枚举，自动将其注册为
/// <c>Enum.CubeDemo.Areas.Test.测试枚举</c> 值集，无需手工维护 <c>LovDefinition</c> 记录。
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
