using System;
using System.ComponentModel;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>FormField 表单字段视图模型单元测试</summary>
public class FormFieldTests
{
    [Fact]
    [DisplayName("FormField 默认构造后 Expand 为 null")]
    public void Constructor_Default_ExpandIsNull()
    {
        var field = new FormField();

        Assert.Null(field.Expand);
    }

    [Fact]
    [DisplayName("FormField 继承自 DataField，父类属性可用")]
    public void FormField_InheritsDataField()
    {
        var field = new FormField
        {
            Name = "Email",
            DisplayName = "邮箱",
            Required = true,
            ReadOnly = false
        };

        Assert.Equal("Email", field.Name);
        Assert.Equal("邮箱", field.DisplayName);
        Assert.True(field.Required);
        Assert.False(field.ReadOnly);
    }

    [Fact]
    [DisplayName("FormField Expand 可设置扩展字段")]
    public void Expand_SetAndGet()
    {
        var field = new FormField();
        Assert.Null(field.Expand);

        var expand = new ExpandField { Retain = true };
        field.Expand = expand;
        Assert.Same(expand, field.Expand);
        Assert.True(field.Expand.Retain);
    }
}
