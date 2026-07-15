using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>DataField 视图模型单元测试</summary>
public class DataFieldTests
{
    [Fact]
    [DisplayName("DataField 默认构造后属性为默认值")]
    public void Constructor_Default_PropertiesHaveDefaults()
    {
        var field = new DataField();

        Assert.Null(field.Name);
        Assert.Null(field.DisplayName);
        Assert.Null(field.Description);
        Assert.Null(field.Category);
        Assert.Null(field.Type);
        Assert.Null(field.ItemType);
        Assert.Equal(default, field.Length);
        Assert.Equal(default, field.Precision);
        Assert.Equal(default, field.Scale);
        Assert.False(field.Nullable);
        Assert.False(field.PrimaryKey);
        Assert.False(field.ReadOnly);
        Assert.False(field.Visible);
        Assert.False(field.Required);
        Assert.Null(field.MapField);
        Assert.Null(field.LovCode);
        Assert.NotNull(field.Properties);
    }

    [Fact]
    [DisplayName("DataField 属性设置和读取一致")]
    public void Properties_SetAndGet_Correctly()
    {
        var field = new DataField
        {
            Name = "UserName",
            DisplayName = "用户名",
            Description = "用户登录名称",
            Category = "基本信息",
            ItemType = "text",
            Length = 50,
            Nullable = false,
            PrimaryKey = false,
            ReadOnly = true,
            Visible = false,
            Required = true,
            MapField = "Name",
            LovCode = "Role"
        };

        Assert.Equal("UserName", field.Name);
        Assert.Equal("用户名", field.DisplayName);
        Assert.Equal("用户登录名称", field.Description);
        Assert.Equal("基本信息", field.Category);
        Assert.Equal("text", field.ItemType);
        Assert.Equal(50, field.Length);
        Assert.False(field.Nullable);
        Assert.False(field.PrimaryKey);
        Assert.True(field.ReadOnly);
        Assert.False(field.Visible);
        Assert.True(field.Required);
        Assert.Equal("Name", field.MapField);
        Assert.Equal("Role", field.LovCode);
    }

    [Fact]
    [DisplayName("ToString 返回格式化的字段信息")]
    public void ToString_ReturnsFormattedInfo()
    {
        var field = new DataField
        {
            Name = "Id",
            DisplayName = "编号",
            Type = typeof(Int32)
        };

        var str = field.ToString();
        Assert.Contains("Id", str);
        Assert.Contains("编号", str);
        Assert.Contains("Int32", str);
    }

    [Fact]
    [DisplayName("TypeName 返回类型名称，Type为null时返回null")]
    public void TypeName_ReturnsTypeNameOrNull()
    {
        var field = new DataField();
        Assert.Null(field.TypeName);

        field.Type = typeof(String);
        Assert.Equal("String", field.TypeName);

        field.Type = typeof(Int32);
        Assert.Equal("Int32", field.TypeName);
    }

    [Fact]
    [DisplayName("Properties 字典不区分大小写")]
    public void Properties_CaseInsensitive()
    {
        var field = new DataField();
        field.Properties["Key"] = "Value";

        Assert.Equal("Value", field.Properties["KEY"]);
        Assert.Equal("Value", field.Properties["key"]);
    }

    [Fact]
    [DisplayName("扩展属性可正常设置和读取")]
    public void ExtendedProperties_SetAndGet()
    {
        var field = new DataField
        {
            Extended1 = "扩展1",
            Extended2 = "扩展2",
            Extended3 = "扩展3"
        };

        Assert.Equal("扩展1", field.Extended1);
        Assert.Equal("扩展2", field.Extended2);
        Assert.Equal("扩展3", field.Extended3);
    }
}
