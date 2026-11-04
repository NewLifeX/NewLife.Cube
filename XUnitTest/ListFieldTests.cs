using System;
using System.ComponentModel;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>ListField 列表字段视图模型单元测试</summary>
public class ListFieldTests
{
    [Fact]
    [DisplayName("ListField 默认构造后属性为默认值")]
    public void Constructor_Default_PropertiesHaveDefaults()
    {
        var field = new ListField();

        Assert.Null(field.Url);
        Assert.Null(field.Target);
        Assert.Null(field.DataAction);
        Assert.Null(field.Title);
        Assert.Null(field.Header);
        Assert.Null(field.HeaderTitle);
        Assert.Equal(0, field.MaxWidth);
        Assert.Null(field.Class);
        Assert.Equal(default, field.TextAlign);
    }

    [Fact]
    [DisplayName("ListField 属性设置和读取一致")]
    public void Properties_SetAndGet_Correctly()
    {
        var field = new ListField
        {
            Url = "/Admin/User/Detail?id={ID}",
            Target = "_blank",
            DataAction = "ajax",
            Title = "查看详情",
            Header = "操作",
            HeaderTitle = "可执行的操作",
            MaxWidth = 200,
            TextAlign = TextAligns.Left,
            Class = "text-danger"
        };

        Assert.Equal("/Admin/User/Detail?id={ID}", field.Url);
        Assert.Equal("_blank", field.Target);
        Assert.Equal("ajax", field.DataAction);
        Assert.Equal("查看详情", field.Title);
        Assert.Equal("操作", field.Header);
        Assert.Equal("可执行的操作", field.HeaderTitle);
        Assert.Equal(200, field.MaxWidth);
        Assert.Equal(TextAligns.Left, field.TextAlign);
        Assert.Equal("text-danger", field.Class);
    }

    [Fact]
    [DisplayName("Url 可含模板占位符，设置后原样保留")]
    public void Url_TemplatePlaceholder_KeepAsIs()
    {
        var field = new ListField
        {
            Url = "/Order/Detail?id={Id}&orderNo={OrderNo}"
        };

        Assert.Equal("/Order/Detail?id={Id}&orderNo={OrderNo}", field.Url);
        Assert.Contains("{Id}", field.Url);
        Assert.Contains("{OrderNo}", field.Url);
    }

    [Fact]
    [DisplayName("Header 可为 null，独立于 DisplayName")]
    public void Header_CanBeNull()
    {
        var field = new ListField();
        Assert.Null(field.Header);

        field.DisplayName = "用户名";
        Assert.Null(field.Header);

        field.Header = "用户";
        Assert.Equal("用户", field.Header);
    }

    [Fact]
    [DisplayName("控制器用显示名作单元格模板时，列头仍取字段默认显示名")]
    public void Localize_TemplateDisplayName_KeepsDefaultHeader()
    {
        var fi = User.Meta.Factory.Table.FindByName("Name")!;

        // 同一字段不做模板改造时的列头，作为基准
        var normal = new ListField();
        normal.Fill(fi);
        normal.Localize();
        Assert.False(String.IsNullOrEmpty(normal.Header));

        // 控制器把 DisplayName 改作单元格文字模板，列头不应跟着变成模板原文
        var field = new ListField();
        field.Fill(fi);
        field.DisplayName = "{Name}";
        field.Localize();

        Assert.Equal("{Name}", field.DisplayName);
        Assert.NotEqual("{Name}", field.Header);
        Assert.Equal(normal.Header, field.Header);
    }

    [Fact]
    [DisplayName("控制器自定义列头按当前语言翻译")]
    public void Localize_CustomHeader_Translated()
    {
        var fi = User.Meta.Factory.Table.FindByName("Name")!;

        var field = new ListField();
        field.Fill(fi);
        field.Header = "节点";
        field.DisplayName = "{Name}";
        field.Localize();

        Assert.Equal(CubeLocalization.T("节点"), field.Header);
    }

    [Theory]
    [DisplayName("TextAligns 枚举值正确")]
    [InlineData(TextAligns.Default, 0)]
    [InlineData(TextAligns.Left, 1)]
    [InlineData(TextAligns.Center, 2)]
    [InlineData(TextAligns.Right, 3)]
    public void TextAligns_EnumValues(TextAligns align, Int32 expected)
    {
        Assert.Equal(expected, (Int32)align);
    }
}
