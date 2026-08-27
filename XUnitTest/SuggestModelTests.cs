using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Cube.ViewModels;
using Xunit;

namespace XUnitTest;

/// <summary>SuggestModel 视图模型单元测试</summary>
public class SuggestModelTests
{
    [Fact]
    [DisplayName("SuggestModel 默认构造后属性为默认值")]
    public void Constructor_Default_PropertiesHaveDefaults()
    {
        var model = new SuggestModel();

        Assert.Null(model.Id);
        Assert.Null(model.Value);
        Assert.Null(model.ShowValue);
        Assert.Null(model.Url);
        Assert.Null(model.Fields);
        Assert.Equal("id", model.IdField);
        Assert.Equal("name", model.NameField);
        Assert.Equal("搜索", model.PlaceHolder);
        Assert.Null(model.ConditionFields);
        Assert.Null(model.FillFields);
    }

    [Fact]
    [DisplayName("ConditionFields 属性设置和读取一致")]
    public void ConditionFields_SetAndGet_Correctly()
    {
        var model = new SuggestModel
        {
            ConditionFields = new Dictionary<string, string>
            {
                { "regionId", "region" },
                { "categoryId", "category" }
            }
        };

        Assert.NotNull(model.ConditionFields);
        Assert.Equal(2, model.ConditionFields.Count);
        Assert.Equal("region", model.ConditionFields["regionId"]);
        Assert.Equal("category", model.ConditionFields["categoryId"]);
    }

    [Fact]
    [DisplayName("FillFields 属性设置和读取一致")]
    public void FillFields_SetAndGet_Correctly()
    {
        var model = new SuggestModel
        {
            FillFields = new Dictionary<string, string>
            {
                { "phone", "customerPhone" },
                { "address", "customerAddress" }
            }
        };

        Assert.NotNull(model.FillFields);
        Assert.Equal(2, model.FillFields.Count);
        Assert.Equal("customerPhone", model.FillFields["phone"]);
        Assert.Equal("customerAddress", model.FillFields["address"]);
    }

    [Fact]
    [DisplayName("ConditionFields 和 FillFields 可同时设置")]
    public void ConditionFields_And_FillFields_CanBeSetTogether()
    {
        var model = new SuggestModel
        {
            Id = "customerId",
            Url = "/api/customers?key=",
            ConditionFields = new Dictionary<string, string>
            {
                { "regionId", "region" }
            },
            FillFields = new Dictionary<string, string>
            {
                { "phone", "customerPhone" },
                { "address", "customerAddress" }
            }
        };

        Assert.Equal("customerId", model.Id);
        Assert.Equal("/api/customers?key=", model.Url);
        Assert.Single(model.ConditionFields);
        Assert.Equal(2, model.FillFields.Count);
    }

    [Fact]
    [DisplayName("ConditionFields 为 null 时不影响其他属性")]
    public void ConditionFields_Null_DoesNotAffectOtherProperties()
    {
        var model = new SuggestModel
        {
            Id = "userId",
            Url = "/api/users?key=",
            ConditionFields = null
        };

        Assert.Equal("userId", model.Id);
        Assert.Equal("/api/users?key=", model.Url);
        Assert.Null(model.ConditionFields);
    }

    [Fact]
    [DisplayName("FillFields 为 null 时不影响其他属性")]
    public void FillFields_Null_DoesNotAffectOtherProperties()
    {
        var model = new SuggestModel
        {
            Id = "userId",
            Url = "/api/users?key=",
            FillFields = null
        };

        Assert.Equal("userId", model.Id);
        Assert.Equal("/api/users?key=", model.Url);
        Assert.Null(model.FillFields);
    }
}
