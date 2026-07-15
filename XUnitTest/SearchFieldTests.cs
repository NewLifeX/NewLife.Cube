using System;
using System.ComponentModel;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>SearchField 搜索字段视图模型单元测试</summary>
public class SearchFieldTests
{
    [Fact]
    [DisplayName("SearchField 默认构造后属性为默认值")]
    public void Constructor_Default_PropertiesHaveDefaults()
    {
        var field = new SearchField();

        Assert.Null(field.Name);
        Assert.False(field.Multiple);
        Assert.Null(field.LovCode);
    }

    [Fact]
    [DisplayName("SearchField 属性设置和读取一致")]
    public void Properties_SetAndGet_Correctly()
    {
        var field = new SearchField
        {
            Name = "Status",
            Multiple = true,
            LovCode = "OrderStatus"
        };

        Assert.Equal("Status", field.Name);
        Assert.True(field.Multiple);
        Assert.Equal("OrderStatus", field.LovCode);
    }

    [Fact]
    [DisplayName("SearchField 继承自 DataField，父类属性可用")]
    public void SearchField_InheritsDataField()
    {
        var field = new SearchField
        {
            Name = "CreateTime",
            DisplayName = "创建时间",
            ItemType = "dateRange"
        };

        Assert.Equal("CreateTime", field.Name);
        Assert.Equal("创建时间", field.DisplayName);
        Assert.Equal("dateRange", field.ItemType);
    }

    [Fact]
    [DisplayName("Multiple 默认值为 false")]
    public void Multiple_DefaultIsFalse()
    {
        var field = new SearchField();
        Assert.False(field.Multiple);

        field.Multiple = true;
        Assert.True(field.Multiple);
    }
}
