using System;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
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
        Assert.Equal(0, collection.Count);
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
}
