using System.Globalization;
using System.Runtime.CompilerServices;
using NewLife;

namespace NewLife.Cube.Tests;

/// <summary>测试程序集初始化器。固定运行时语言，保证断言与运行环境无关</summary>
/// <remarks>
/// 断言基于简体中文文案，而 CI（无 LANG）默认走英文源文兜底，会导致同一用例本地通过、CI 失败。
/// 英文/多语言行为由显式切换 <see cref="Localization.Culture"/> 的用例覆盖（LocalizationTests、ApiResponseHelperTests 等）。
/// </remarks>
internal static class TestCultureInitializer
{
    [ModuleInitializer]
    public static void Init() => Localization.Culture = CultureInfo.GetCultureInfo("zh-CN");
}
