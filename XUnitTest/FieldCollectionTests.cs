using System;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.Entity;
using NewLife.Cube.ViewModels;
using XCode.Configuration;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>FieldCollection 字段集合视图模型单元测试</summary>
public class FieldCollectionTests
{
    [Fact]
    [DisplayName("FieldCollection 使用 List 构造后为空集合")]
    public void Constructor_WithListKind_EmptyCollection()
    {
        var collection = new FieldCollection(ViewKinds.List);
        Assert.Empty(collection);
    }

    [Fact]
    [DisplayName("FieldCollection 可添加和移除字段")]
    public void AddAndRemove_Field()
    {
        var collection = new FieldCollection(ViewKinds.List);
        var field = new DataField { Name = "UserName", DisplayName = "用户名" };

        collection.Add(field);
        Assert.Single(collection);
        Assert.Contains(field, collection);

        collection.Remove(field);
        Assert.Empty(collection);
    }

    [Fact]
    [DisplayName("FieldCollection 可批量添加字段")]
    public void AddRange_MultipleFields()
    {
        var collection = new FieldCollection(ViewKinds.List);
        collection.Add(new DataField { Name = "Id" });
        collection.Add(new DataField { Name = "Name" });
        collection.Add(new DataField { Name = "CreateTime" });

        Assert.Equal(3, collection.Count);
    }

    [Fact]
    [DisplayName("FieldCollection 遍历顺序与添加顺序一致")]
    public void Enumeration_OrderMatchesInsertion()
    {
        var collection = new FieldCollection(ViewKinds.List);
        collection.Add(new DataField { Name = "A" });
        collection.Add(new DataField { Name = "B" });
        collection.Add(new DataField { Name = "C" });

        var names = collection.Select(f => f.Name).ToList();
        Assert.Equal(["A", "B", "C"], names);
    }

    [Fact]
    [DisplayName("ShowIn 三态按五区过滤：列表与搜索隐藏、详情保留")]
    public void ShowIn_FiltersFiveViews()
    {
        // UserConnect.Avatar: Auto,-List,-Search；未声明的 Add/Edit/Detail 为 Auto（保留）
        Assert.DoesNotContain(new FieldCollection(UserConnect.Meta.Factory, ViewKinds.List), f => f.Name == "Avatar");
        Assert.DoesNotContain(new FieldCollection(UserConnect.Meta.Factory, ViewKinds.Search), f => f.Name == "Avatar");
        Assert.Contains(new FieldCollection(UserConnect.Meta.Factory, ViewKinds.Detail), f => f.Name == "Avatar");
        Assert.Contains(new FieldCollection(UserConnect.Meta.Factory, ViewKinds.AddForm), f => f.Name == "Avatar");
        Assert.Contains(new FieldCollection(UserConnect.Meta.Factory, ViewKinds.EditForm), f => f.Name == "Avatar");

        // AccessRule.ExpireTime: List,Detail 显式显示；其余 Auto（仍保留）
        Assert.Contains(new FieldCollection(AccessRule.Meta.Factory, ViewKinds.List), f => f.Name == "ExpireTime");
        Assert.Contains(new FieldCollection(AccessRule.Meta.Factory, ViewKinds.Detail), f => f.Name == "ExpireTime");
    }

    [Theory]
    [DisplayName("ShowIn 五区三态按 List/Detail/AddForm/EditForm/Search 顺序解析")]
    [InlineData("Y|N|N|Y|N", TriState.Show, TriState.Hide, TriState.Hide, TriState.Show, TriState.Hide)]
    [InlineData("List,-Detail,-Add,-Edit,-Search", TriState.Show, TriState.Hide, TriState.Hide, TriState.Hide, TriState.Hide)]
    [InlineData("Auto", TriState.Auto, TriState.Auto, TriState.Auto, TriState.Auto, TriState.Auto)]
    public void ShowIn_ParsesFiveZones(
        String text,
        TriState list,
        TriState detail,
        TriState add,
        TriState edit,
        TriState search)
    {
        var option = ShowInOption.Parse(text);

        Assert.Equal(list, option.List);
        Assert.Equal(detail, option.Detail);
        Assert.Equal(add, option.AddForm);
        Assert.Equal(edit, option.EditForm);
        Assert.Equal(search, option.Search);
    }

    [Fact]
    [DisplayName("模型 Map 外键在 DataField.Fill 时带出 MapField 与 MapProvider")]
    public void Fill_FromMappedField_CopiesMap()
    {
        var item = AppLog.Meta.Factory.AllFields.FirstOrDefault(f => f.Map != null);
        Assert.NotNull(item);

        var field = new ListField();
        field.Fill(item!);

        Assert.Equal(item!.Name, field.Name);
        Assert.Equal(item.Map!.Name, field.MapField);
        Assert.NotNull(field.MapProvider);
    }

}
