using System;
using System.Globalization;
using NewLife;
using Xunit;

namespace XUnitTest;

/// <summary>英文元数据词表测试。中文源文元数据在英文界面由 Locales/en.yml 提供译文</summary>
[Collection("Localization")]
public class EnglishMetadataTests
{
    private static String Translate(String culture, String source)
    {
        var old = Localization.Culture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo(culture);
            return Localization.Translate(source);
        }
        finally
        {
            Localization.Culture = old;
        }
    }

    [Theory(DisplayName = "英文词表 - 内置菜单名不再回退中文")]
    [InlineData("系统管理", "System")]
    [InlineData("魔方管理", "Cube management")]
    [InlineData("数据库", "Database")]
    [InlineData("值集管理", "Value set management")]
    [InlineData("视图解析诊断", "View resolution diagnostics")]
    public void MenuNames(String source, String expected) => Assert.Equal(expected, Translate("en-US", source));

    [Theory(DisplayName = "英文词表 - 实体与字段元数据不再回退中文")]
    [InlineData("用户", "User")]
    [InlineData("角色", "Role")]
    [InlineData("应用系统", "Applications")]
    [InlineData("地区", "Area")]
    [InlineData("编号", "ID")]
    public void EntityMetadata(String source, String expected) => Assert.Equal(expected, Translate("en-US", source));

    [Fact(DisplayName = "英文词表 - 未登记词条按源文回退")]
    public void UnknownKeyFallsBack() => Assert.Equal("未登记的自定义名称", Translate("en-US", "未登记的自定义名称"));

    [Fact(DisplayName = "英文词表 - 中文界面仍使用中文源文")]
    public void ChineseKeepsSource() => Assert.Equal("系统管理", Translate("zh-CN", "系统管理"));
}
