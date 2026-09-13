using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>DataField 元数据 dataSource 契约：委托 → 枚举反射 → 物化字典（DataSourceMap）。</summary>
/// <remarks>
/// SPA 的 GetPage/GetFields 元数据由 <c>DataField.ToDictionary()</c> 产出（DataField : IDictionarySource）。
/// 该通路必须回退 <see cref="DataField.DataSourceMap"/>，否则 <c>MapCandidateFiller</c> 内联的 Map 外键候选
/// 会被静默丢弃，外键字段退化为数字输入框（RoleID/DepartmentID/CreateUserID 等）。此处钉死优先级与
/// <c>multiple</c> 推断口径，防止回归。
/// </remarks>
public class DataFieldDataSourceMapTests
{
    [Fact(DisplayName = "ToDictionary：无委托时回退 DataSourceMap（Map 外键候选）")]
    public void Fallback_To_DataSourceMap()
    {
        var ff = new FormField
        {
            Name = "RoleID",
            Type = typeof(Int32),
            DataSourceMap = new Dictionary<String, String> { ["1"] = "管理员" },
        };

        var dic = ff.ToDictionary();

        var ds = dic["dataSource"] as IDictionary<String, String>;
        Assert.NotNull(ds);
        Assert.Equal("管理员", ds!["1"]);

        // 物化字典来源不按名称推断多选（枚举/布尔/字典字段不应被误标 multiple）
        Assert.False(dic.ContainsKey("multiple"));
    }

    [Fact(DisplayName = "ToDictionary：委托优先于物化字典")]
    public void Delegate_Wins_Over_DataSourceMap()
    {
        var ff = new FormField
        {
            Name = "RoleID",
            Type = typeof(Int32),
            DataSource = _ => new Dictionary<Object, String> { [1] = "委托角色" },
            DataSourceMap = new Dictionary<String, String> { ["9"] = "字典角色" },
        };

        var ds = ff.ToDictionary()["dataSource"] as IDictionary<String, String>;

        Assert.NotNull(ds);
        Assert.Equal("委托角色", ds!["1"]);
        Assert.False(ds.ContainsKey("9"));
    }

    [Fact(DisplayName = "ToDictionary：委托来源且字段名以 s 结尾仍推断多选")]
    public void Delegate_Infers_Multiple()
    {
        var ff = new FormField
        {
            Name = "RoleIds",
            Type = typeof(String),
            DataSource = _ => new Dictionary<Object, String> { [1] = "管理员" },
        };

        var dic = ff.ToDictionary();

        Assert.True(dic.TryGetValue("multiple", out var multi) && multi is Boolean b && b);
    }

    [Fact(DisplayName = "ToDictionary：枚举字段仍走反射，字典不参与")]
    public void Enum_Keeps_Reflection()
    {
        var ff = new FormField
        {
            Name = "Sex",
            Type = typeof(SexKinds),
            DataSourceMap = new Dictionary<String, String> { ["1"] = "男", ["Male"] = "男" },
        };

        var ds = ff.ToDictionary()["dataSource"] as IDictionary<String, String>;

        Assert.NotNull(ds);
        Assert.True(ds!.ContainsKey("1"), "枚举数值键应存在");
        Assert.False(ds.ContainsKey("Male"), "枚举反射只输出数值键，不受物化字典干扰");
    }

    [Fact(DisplayName = "ToDictionary：既无委托、无字典、非枚举时不输出 dataSource")]
    public void NoSource_No_DataSource()
    {
        var ff = new FormField { Name = "RoleID", Type = typeof(Int32) };

        Assert.False(ff.ToDictionary().ContainsKey("dataSource"));
    }
}
