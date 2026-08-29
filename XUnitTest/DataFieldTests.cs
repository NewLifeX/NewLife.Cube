using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using NewLife.Cube.ViewModels;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest;

/// <summary>DataField 视图模型单元测试</summary>
public class DataFieldTests
{
    /// <summary>模拟全局 JsonOptions（驼峰命名 + 中文不转义），与 GetPage 真实序列化一致</summary>
    private static JsonSerializerOptions CreateOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>模拟 OnJsonSerialize 的 FastJson（JsonWriter）序列化，验证 GetPage 实际路径</summary>
    private static String ToFastJson(Object data)
    {
        var writer = new JsonWriter
        {
            Options = new JsonOptions
            {
                PropertyNaming = PropertyNaming.CamelCase,
                IgnoreNullValues = false,
                Int64AsString = true,
            },
        };
        writer.Write(data);
        return writer.GetString();
    }
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

    [Fact]
    [DisplayName("DataField 默认值序列化时忽略 null/0/false/空串噪音")]
    public void Serialize_Default_OmitsNoise()
    {
        var field = new DataField { Name = "ID", DisplayName = "编号", Type = typeof(Int32), PrimaryKey = true };

        var json = JsonSerializer.Serialize(field, CreateOptions());

        // 必要字段保留
        Assert.Contains("\"name\":\"ID\"", json);
        Assert.Contains("\"displayName\":\"编号\"", json);
        Assert.Contains("\"typeName\":\"Int32\"", json);
        Assert.Contains("\"primaryKey\":true", json);

        // 噪音字段被忽略（null / 0 / false / 空串）
        Assert.DoesNotContain("itemType", json);
        Assert.DoesNotContain("length", json);
        Assert.DoesNotContain("precision", json);
        Assert.DoesNotContain("scale", json);
        Assert.DoesNotContain("nullable", json);
        Assert.DoesNotContain("readOnly", json);
        Assert.DoesNotContain("visible", json);
        Assert.DoesNotContain("required", json);
        Assert.DoesNotContain("authority", json);
        Assert.DoesNotContain("extended1", json);
        Assert.DoesNotContain("mapField", json);
        Assert.DoesNotContain("lovCode", json);
        Assert.DoesNotContain("category", json);
        Assert.DoesNotContain("description", json);
    }

    [Fact]
    [DisplayName("DataField 有意义值序列化时保留")]
    public void Serialize_Filled_KeepsMeaningful()
    {
        var field = new DataField
        {
            Name = "Name",
            DisplayName = "名称",
            Description = "登录用户名",
            Category = "基本信息",
            Type = typeof(String),
            ItemType = "text",
            Length = 50,
            Nullable = true,
            Required = true,
            MapField = "LoginName",
            LovCode = "Role",
        };

        var json = JsonSerializer.Serialize(field, CreateOptions());

        Assert.Contains("\"category\":\"基本信息\"", json);
        Assert.Contains("\"description\":\"登录用户名\"", json);
        Assert.Contains("\"itemType\":\"text\"", json);
        Assert.Contains("\"length\":50", json);
        Assert.Contains("\"nullable\":true", json);
        Assert.Contains("\"required\":true", json);
        Assert.Contains("\"mapField\":\"LoginName\"", json);
        Assert.Contains("\"lovCode\":\"Role\"", json);
    }

    [Fact]
    [DisplayName("ListField 默认值序列化时忽略噪音，有意义值保留")]
    public void Serialize_ListField_OmitsNoise()
    {
        var field = new ListField { Name = "Role", DisplayName = "角色", Type = typeof(String) };
        var options = CreateOptions();

        var json = JsonSerializer.Serialize(field, options);

        Assert.DoesNotContain("text", json);
        Assert.DoesNotContain("title", json);
        Assert.DoesNotContain("url", json);
        Assert.DoesNotContain("target", json);
        Assert.DoesNotContain("textAlign", json);
        Assert.DoesNotContain("maxWidth", json);
        Assert.DoesNotContain("dataAction", json);
        // 未 Fill 时 Header 为 null，忽略
        Assert.DoesNotContain("header", json);

        field.Url = "User?roleId={Id}";
        field.TextAlign = TextAligns.Center;
        field.MaxWidth = 120;
        json = JsonSerializer.Serialize(field, options);

        Assert.Contains("\"url\":\"User?roleId={Id}\"", json);
        Assert.Contains("\"textAlign\":2", json);
        Assert.Contains("\"maxWidth\":120", json);
    }

    [Fact]
    [DisplayName("SearchField Multiple=true 保留，默认 false 忽略")]
    public void Serialize_SearchField_Multiple()
    {
        var options = CreateOptions();

        var field = new SearchField { Name = "Status", Type = typeof(Int32) };
        var json = JsonSerializer.Serialize(field, options);
        Assert.DoesNotContain("multiple", json);

        field.Multiple = true;
        json = JsonSerializer.Serialize(field, options);
        Assert.Contains("\"multiple\":true", json);
    }

    [Fact]
    [DisplayName("ExpandField 委托成员不参与序列化")]
    public void Serialize_ExpandField_IgnoresDelegates()
    {
        var exp = new ExpandField { Name = "Data", Prefix = "x_", Decode = _ => null, Encode = _ => "{}" };

        var json = JsonSerializer.Serialize(exp, CreateOptions());

        Assert.DoesNotContain("decode", json);
        Assert.DoesNotContain("encode", json);
        Assert.Contains("\"name\":\"Data\"", json);
        Assert.Contains("\"prefix\":\"x_\"", json);
    }

    [Fact]
    [DisplayName("FastJson 序列化 DataField 时忽略 null/0/false 噪音")]
    public void FastJson_Serialize_Default_OmitsNoise()
    {
        var field = new DataField { Name = "ID", DisplayName = "编号", Type = typeof(Int32), PrimaryKey = true };

        var json = ToFastJson(field);

        // 必要字段保留
        Assert.Contains("\"name\":\"ID\"", json);
        Assert.Contains("\"displayName\":\"编号\"", json);
        Assert.Contains("\"typeName\":\"Int32\"", json);
        Assert.Contains("\"primaryKey\":true", json);

        // 噪音字段被忽略（null / 0 / false / 空串）
        Assert.DoesNotContain("itemType", json);
        Assert.DoesNotContain("\"length\"", json);
        Assert.DoesNotContain("precision", json);
        Assert.DoesNotContain("scale", json);
        Assert.DoesNotContain("nullable", json);
        Assert.DoesNotContain("readOnly", json);
        Assert.DoesNotContain("visible", json);
        Assert.DoesNotContain("required", json);
        Assert.DoesNotContain("authority", json);
        Assert.DoesNotContain("mapField", json);
        Assert.DoesNotContain("lovCode", json);
        Assert.DoesNotContain("category", json);
        Assert.DoesNotContain("description", json);
    }

    [Fact]
    [DisplayName("FastJson 序列化 DataField 时保留有意义值")]
    public void FastJson_Serialize_Filled_KeepsMeaningful()
    {
        var field = new DataField
        {
            Name = "Name",
            DisplayName = "名称",
            Description = "登录用户名",
            Category = "基本信息",
            Type = typeof(String),
            ItemType = "text",
            Length = 50,
            Nullable = true,
            Required = true,
            MapField = "LoginName",
            LovCode = "Role",
        };

        var json = ToFastJson(field);

        Assert.Contains("\"category\":\"基本信息\"", json);
        Assert.Contains("\"description\":\"登录用户名\"", json);
        Assert.Contains("\"itemType\":\"text\"", json);
        Assert.Contains("\"length\":50", json);
        Assert.Contains("\"nullable\":true", json);
        Assert.Contains("\"required\":true", json);
        Assert.Contains("\"mapField\":\"LoginName\"", json);
        Assert.Contains("\"lovCode\":\"Role\"", json);
    }

    [Fact]
    [DisplayName("FastJson 序列化 ListField/SearchField 子类字段")]
    public void FastJson_Serialize_SubclassFields()
    {
        var lf = new ListField
        {
            Name = "Role",
            DisplayName = "角色",
            Type = typeof(String),
            Url = "User?roleId={Id}",
            TextAlign = TextAligns.Center,
            MaxWidth = 120,
        };
        var lfJson = ToFastJson(lf);
        Assert.Contains("\"url\":\"User?roleId={Id}\"", lfJson);
        Assert.Contains("\"textAlign\":2", lfJson);
        Assert.Contains("\"maxWidth\":120", lfJson);
        Assert.DoesNotContain("\"text\"", lfJson);
        Assert.DoesNotContain("\"title\"", lfJson);
        Assert.DoesNotContain("\"target\"", lfJson);
        Assert.DoesNotContain("\"dataAction\"", lfJson);

        var sf = new SearchField { Name = "Status", Type = typeof(Int32), Multiple = true };
        var sfJson = ToFastJson(sf);
        Assert.Contains("\"multiple\":true", sfJson);
    }
}
