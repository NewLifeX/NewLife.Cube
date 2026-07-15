using System;
using System.ComponentModel;
using NewLife.Cube.ViewModels;
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
