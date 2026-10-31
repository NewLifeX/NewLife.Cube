using System;
using System.Linq;
using NewLife;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>映射字段元数据测试。验证 RoleName→RoleID 这类 Map 字段带出关联键、键类型与数据源</summary>
public class DataFieldMapTests
{
    #region 辅助
    private static FormField CreateFormField(String name)
    {
        var fi = User.Meta.Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        Assert.NotNull(fi);

        var df = new FormField();
        df.Fill(fi!);

        return df;
    }
    #endregion

    [Fact(DisplayName = "映射字段带出关联键与键类型")]
    public void MapField_EmitsMapFieldAndKeyType()
    {
        var df = CreateFormField("RoleName");

        Assert.Equal("RoleID", df.MapField);
        Assert.Equal("Int32", df.MapTypeName);

        var dic = df.ToDictionary();
        Assert.Equal("RoleID", dic["mapField"]);
        Assert.Equal("Int32", dic["mapTypeName"]);
    }

    [Fact(DisplayName = "映射字段的取值键是关联键而不是显示名")]
    public void MapField_ValueKeyIsMappedField()
    {
        var df = CreateFormField("RoleName");

        // 前端按 mapField 读写模型：RoleName 只是显示名，提交的必须是 RoleID
        Assert.Equal("RoleID", df.MapField);
        Assert.NotEqual(df.Name, df.MapField);
    }

    [Fact(DisplayName = "普通字段不下发 mapField/mapTypeName")]
    public void NormalField_HasNoMapMetadata()
    {
        var df = CreateFormField("Name");

        Assert.True(df.MapField.IsNullOrEmpty());
        Assert.True(df.MapTypeName.IsNullOrEmpty());

        var dic = df.ToDictionary();
        Assert.False(dic.ContainsKey("mapField"));
        Assert.False(dic.ContainsKey("mapTypeName"));
    }

    [Fact(DisplayName = "关联表不可用时映射字段降级为键值输入而不是抛异常")]
    public void MapField_WithoutDataSource_FallsBack()
    {
        var df = CreateFormField("RoleName");

        // 单测环境没有可用的关联表连接，ToDictionary 必须吞掉异常并保留 mapField/mapTypeName
        var dic = df.ToDictionary();

        Assert.Equal("RoleID", dic["mapField"]);
        Assert.Equal("Int32", dic["mapTypeName"]);
    }
}
