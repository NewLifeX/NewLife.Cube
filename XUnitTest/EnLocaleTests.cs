using System;
using System.Globalization;
using NewLife;
using Xunit;

namespace XUnitTest;

/// <summary>英文词表测试。中文为源文的元数据在英文界面必须由 Locales/en.yml 提供英文译文</summary>
[Collection("Localization")]
public class EnLocaleTests
{
    private static String Translate(String culture, String template, params Object[] args)
    {
        var old = Localization.Culture;
        try
        {
            Localization.Culture = new CultureInfo(culture);
            return Localization.Translate(template, args);
        }
        finally
        {
            Localization.Culture = old;
        }
    }

    [Theory(DisplayName = "英文词表 - 中文元数据在英文界面输出英文译文")]
    [InlineData("委托代理", "Delegation")]
    [InlineData("应用系统", "Applications")]
    [InlineData("应用日志", "App log")]
    [InlineData("工作台部件", "Dashboard widgets")]
    [InlineData("编号", "ID")]
    [InlineData("名称", "Name")]
    [InlineData("30天活跃。30天活跃用户数", "Active (30d). Active users in the last 30 days")]
    [InlineData("存储类型。Local本地磁盘，Oss阿里云，Cos腾讯云，Qiniu七牛，EasyIO，默认Local", "Storage type. Local disk, Oss (Alibaba Cloud), Cos (Tencent Cloud), Qiniu or EasyIO; defaults to Local")]
    public void MetadataToEnglish(String source, String expected) => Assert.Equal(expected, Translate("en-US", source));

    [Theory(DisplayName = "英文词表 - 中文界面仍显示中文源文")]
    [InlineData("委托代理")]
    [InlineData("应用系统")]
    [InlineData("编号")]
    public void MetadataInChinese(String source) => Assert.Equal(source, Translate("zh-CN", source));

    [Fact(DisplayName = "英文词表 - 带占位符的元数据模板保留占位符")]
    public void PlaceholderTemplate()
    {
        // {type} 是具名占位符，由调用方（MenuHelper）翻译后替换，不经过 String.Format
        Assert.Equal("User management", Translate("en-US", "{type}管理").Replace("{type}", "User"));
        Assert.Equal("Delete User", Translate("en-US", "删除{type}").Replace("{type}", "User"));
        Assert.Equal("用户管理", Translate("zh-CN", "{type}管理").Replace("{type}", "用户"));
    }

    [Fact(DisplayName = "英文词表 - 图表文案按界面语言输出")]
    public void ChartText()
    {
        // 英文源文 key 在英文界面原样输出，中文界面查 zh-Hans
        Assert.Equal("CPU / Memory %", Translate("en-US", "CPU / Memory %"));
        Assert.Equal("CPU / 内存 %", Translate("zh-CN", "CPU / Memory %"));
        Assert.Equal("Memory", Translate("en-US", "Memory"));
        Assert.Equal("内存", Translate("zh-CN", "Memory"));
        Assert.Equal("3 cores", Translate("en-US", "{0} cores", 3));
        Assert.Equal("3 核心", Translate("zh-CN", "{0} cores", 3));
    }
}
