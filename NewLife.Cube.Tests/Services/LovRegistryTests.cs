using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Services;
using Xunit;

namespace NewLife.Cube.Tests.Services;

/// <summary>测试枚举：数字值 + Description 标签</summary>
public enum LovTestStatus
{
    /// <summary>启用</summary>
    [Description("启用")]
    Enabled = 1,

    /// <summary>停用</summary>
    [Description("停用")]
    Disabled = 2,
}

/// <summary>测试枚举：LovStringValue 标记，选项值取成员名（字符串）</summary>
[LovStringValue]
public enum LovTestKey
{
    /// <summary>键一</summary>
    [Description("键一")]
    KeyA = 1,

    /// <summary>键二</summary>
    [Description("键二")]
    KeyB = 2,
}

/// <summary>测试控制器桩，用于 [LovList] 描述符构建</summary>
public class LovStubController : ControllerBase
{
    /// <summary>角色列表（[LovList] 声明式列表值集方法）</summary>
    public IActionResult RoleList() => Ok();
}

/// <summary>值集代码注册表（<see cref="LovRegistry"/>）测试：枚举反射选项与 [LovList] 描述符构建</summary>
public class LovRegistryTests
{
    [Fact]
    public void GetEnumOptions_NumericValue_AndDescriptionLabel()
    {
        var options = LovRegistry.GetEnumOptions(typeof(LovTestStatus));

        Assert.Equal(2, options.Count);
        Assert.Equal("1", options[0].Value);
        Assert.Equal("启用", options[0].Label);
        Assert.Equal("2", options[1].Value);
        Assert.Equal("停用", options[1].Label);
    }

    [Fact]
    public void GetEnumOptions_LovStringValue_UsesMemberName()
    {
        // 被 LovStringValue 标记的枚举：选项值取成员名（字符串），而非数字
        var options = LovRegistry.GetEnumOptions(typeof(LovTestKey));

        Assert.Equal(2, options.Count);
        Assert.Equal("KeyA", options[0].Value);
        Assert.Equal("键一", options[0].Label);
        Assert.Equal("KeyB", options[1].Value);
        Assert.Equal("键二", options[1].Label);
    }

    [Fact]
    public void GetEnumName_FromDescription()
    {
        var name = LovRegistry.GetEnumName(typeof(LovTestStatus));
        Assert.Equal(nameof(LovTestStatus), name); // 类型无 DisplayName/Description 时回退类型名

        // 成员标签走 Description；类型名无特性则回退
        Assert.NotEmpty(name);
    }

    [Fact]
    public void FindEnumType_ByFullName()
    {
        var type = LovRegistry.FindEnumType(typeof(LovTestStatus).FullName!);
        Assert.NotNull(type);
        Assert.Equal(typeof(LovTestStatus), type);

        Assert.Null(LovRegistry.FindEnumType("NewLife.Cube.Tests.Services.NoSuchEnum"));
    }

    [Fact]
    public void BuildDescriptor_ParseColumnsAndSearchFields()
    {
        var attr = new LovListAttribute
        {
            LovCode = "List.Test.Role",
            Name = "角色",
            RequestUrl = "/api/Test/RoleList",
            Method = "GET",
            ProxyRequest = false,
            Columns = ["id:编号:80:left", "name:名称:160"],
            SearchFields = ["key:关键字:input:QUERY:false"],
        };

        var method = typeof(LovStubController).GetMethod(nameof(LovStubController.RoleList))!;
        var desc = LovRegistry.BuildDescriptor(attr, method);

        Assert.Equal("List.Test.Role", desc.LovCode);
        Assert.Equal("角色", desc.Name);
        Assert.Equal("/api/Test/RoleList", desc.Config.RequestUrl);
        Assert.Equal("GET", desc.Config.Method);
        Assert.False(desc.Config.ProxyRequest);

        Assert.Equal(2, desc.TableColumns.Count);
        Assert.Equal("id", desc.TableColumns[0].Field);
        Assert.Equal(80, desc.TableColumns[0].Width);
        Assert.Equal("left", desc.TableColumns[0].Align);
        Assert.Equal("名称", desc.TableColumns[1].Title);

        Assert.Single(desc.SearchFields);
        Assert.Equal("key", desc.SearchFields[0].Field);
        Assert.Equal("QUERY", desc.SearchFields[0].ParamType);
        Assert.False(desc.SearchFields[0].Required);
    }

    [Fact]
    public void FindList_Unknown_ReturnsNull()
    {
        Assert.Null(LovRegistry.FindList("List.No.Such"));
    }
}
