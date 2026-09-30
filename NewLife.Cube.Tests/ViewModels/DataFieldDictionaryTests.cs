using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Cube.ViewModels;
using Xunit;

namespace NewLife.Cube.Tests.ViewModels;

/// <summary>OSC-2609307879 ToDictionary 在非布尔且 DataSourceMap 非空时不调用委托。</summary>
public class DataFieldDictionaryTests
{
    [Fact(DisplayName = "map 已有一项时委托不被调用；map 为空时委托被调用")]
    public void ToDictionary_Prefers_NonEmpty_Map()
    {
        var called = 0;
        var field = new DataField
        {
            Name = "RoleId",
            Type = typeof(Int32),
            TypeName = "Int32",
            DataSource = _ =>
            {
                called++;
                return new Dictionary<String, String> { ["9"] = "委托" };
            },
            DataSourceMap = new Dictionary<String, String> { ["1"] = "管理员" },
        };

        var dic = field.ToDictionary();
        Assert.Equal(0, called);
        var ds = Assert.IsAssignableFrom<IDictionary>(dic["dataSource"]);
        Assert.Equal("管理员", ds["1"]);

        field.DataSourceMap = null;
        dic = field.ToDictionary();
        Assert.Equal(1, called);
        ds = Assert.IsAssignableFrom<IDictionary>(dic["dataSource"]);
        Assert.Equal("委托", ds["9"]);
    }

    [Fact(DisplayName = "布尔字段即使 map 非空仍走委托，且不把 map 当下拉")]
    public void ToDictionary_Boolean_Ignores_Map()
    {
        var called = 0;
        var field = new DataField
        {
            Name = "Enable",
            Type = typeof(Boolean),
            TypeName = "Boolean",
            DataSource = _ =>
            {
                called++;
                return null;
            },
            DataSourceMap = new Dictionary<String, String> { ["1"] = "是" },
        };

        var dic = field.ToDictionary();
        Assert.Equal(1, called);
        Assert.False(dic.ContainsKey("dataSource"));
    }
}
