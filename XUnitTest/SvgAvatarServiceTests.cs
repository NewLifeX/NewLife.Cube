using System;
using System.ComponentModel;
using NewLife.Cube.Services;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>SVG 文字头像生成服务单元测试</summary>
/// <remarks>
/// 覆盖字符提取规则（中文取前N字/英文首字母）与 SVG 视觉结构（渐变背景、装饰圆、动态字号、性别配色）。
/// 通过公开入口 Generate 间接验证，各用例使用不同用户ID避免命中内存缓存。
/// </remarks>
public class SvgAvatarServiceTests
{
    private static Int32 _id;

    /// <summary>创建内存用户，无需数据库</summary>
    private static User CreateUser(String name, String displayName, SexKinds sex = SexKinds.未知)
        => new() { ID = ++_id, Name = name, DisplayName = displayName, Sex = sex };

    /// <summary>生成头像并提取 text 内容</summary>
    private static String Extract(String name, Int32 chars, SexKinds sex = SexKinds.未知)
        => ExtractText(SvgAvatarService.Generate(CreateUser(name, name, sex), chars));

    /// <summary>从 SVG 提取 text 标签内的文字</summary>
    private static String ExtractText(String svg)
    {
        var start = svg.IndexOf('>', svg.IndexOf("<text", StringComparison.Ordinal)) + 1;
        var end = svg.IndexOf("</text>", start, StringComparison.Ordinal);
        return svg[start..end];
    }

    #region 字符提取规则

    [Fact(DisplayName = "中文2字名全取")]
    public void Cjk_TwoChars_Full()
    {
        Assert.Equal("张三", Extract("张三", 2));
    }

    [Fact(DisplayName = "中文3字名取前2字（保留姓氏，核心修复）")]
    public void Cjk_ThreeChars_TakeFirstTwo()
    {
        // 旧逻辑取末尾2字输出"理员"，现在取前2字"管理"
        Assert.Equal("管理", Extract("管理员", 2));
        Assert.Equal("张三", Extract("张三丰", 2));
    }

    [Fact(DisplayName = "中文3字名配置3字全取")]
    public void Cjk_ThreeChars_Full()
    {
        Assert.Equal("管理员", Extract("管理员", 3));
    }

    [Fact(DisplayName = "中文4字复姓取前2字")]
    public void Cjk_FourChars_TakeFirstTwo()
    {
        Assert.Equal("欧阳", Extract("欧阳娜娜", 2));
    }

    [Fact(DisplayName = "中文4字配置3字取前3字")]
    public void Cjk_FourChars_TakeFirstThree()
    {
        Assert.Equal("欧阳娜", Extract("欧阳娜娜", 3));
    }

    [Fact(DisplayName = "中文配置1字取首字")]
    public void Cjk_OneChar()
    {
        Assert.Equal("管", Extract("管理员", 1));
    }

    [Fact(DisplayName = "英文含空格取各词首字母")]
    public void English_Space_NameInitials()
    {
        Assert.Equal("ZS", Extract("Zhang San", 2));
        Assert.Equal("Z", Extract("Zhang San", 1));
    }

    [Fact(DisplayName = "英文用户名取前N字母大写")]
    public void English_Username_FirstLetters()
    {
        Assert.Equal("AD", Extract("admin", 2));
        Assert.Equal("ADM", Extract("admin", 3));
    }

    [Fact(DisplayName = "空名或空显示名回退为#")]
    public void EmptyName_FallbackHash()
    {
        Assert.Equal("#", Extract("", 2));

        var svg = SvgAvatarService.Generate(CreateUser(null, null), 2);
        Assert.Equal("#", ExtractText(svg));
    }

    #endregion

    #region SVG 视觉结构

    [Fact(DisplayName = "SVG 使用渐变背景")]
    public void Svg_HasGradient()
    {
        var svg = SvgAvatarService.Generate(CreateUser("张三", "张三", SexKinds.男), 2);
        Assert.Contains("<linearGradient", svg);
        Assert.Contains("stop-color=\"#2196F3\"", svg); // 男默认蓝色
        Assert.Contains("stop-color=\"#1565C0\"", svg);
    }

    [Fact(DisplayName = "SVG 含装饰圆与圆角")]
    public void Svg_HasDecorativeCircles()
    {
        var svg = SvgAvatarService.Generate(CreateUser("张三", "张三"), 2);
        Assert.Contains("<circle", svg);
        Assert.Contains("rx=\"14\"", svg);
    }

    [Fact(DisplayName = "字号随字符数动态调整")]
    public void Svg_FontSizeByChars()
    {
        Assert.Contains("font-size=\"46\"", SvgAvatarService.Generate(CreateUser("张", "张"), 1));
        Assert.Contains("font-size=\"30\"", SvgAvatarService.Generate(CreateUser("张三", "张三"), 2));
        Assert.Contains("font-size=\"22\"", SvgAvatarService.Generate(CreateUser("管理员", "管理员"), 3));
    }

    [Fact(DisplayName = "性别决定配色")]
    public void Svg_SexColors()
    {
        var male = SvgAvatarService.Generate(CreateUser("张三", "张三", SexKinds.男), 2);
        Assert.Contains("#2196F3", male);

        var female = SvgAvatarService.Generate(CreateUser("李四", "李四", SexKinds.女), 2);
        Assert.Contains("#E91E63", female);
    }

    [Fact(DisplayName = "特殊字符转义防XSS")]
    public void Svg_XssEscaped()
    {
        // 纯特殊字符名回退取首字符"<"，BuildSvg 必须转义为 &lt; 防注入
        var svg = SvgAvatarService.Generate(CreateUser("<", "<"), 2);
        Assert.Contains("&lt;", svg);
    }

    [Fact(DisplayName = "未知性别 Int32.MinValue 不崩溃（Math.Abs 溢出防御）")]
    public void UnknownSex_Int32MinValue_NoCrash()
    {
        var user = new User { ID = Int32.MinValue, Name = "张三", DisplayName = "张三" };
        var svg = SvgAvatarService.Generate(user, 2);
        Assert.Contains("<svg", svg);
    }

    [Fact(DisplayName = "名字含孤立代理字符不崩溃（ConvertToUtf32 防御）")]
    public void LoneSurrogate_NoCrash()
    {
        var user = CreateUser("\uD800名", "\uD800名");
        var svg = SvgAvatarService.Generate(user, 2);
        Assert.Contains("<svg", svg);
    }

    #endregion
}
