using System;
using System.Collections.Generic;
using NewLife.Cube.ViewModels;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>meta 视图构建测试</summary>
public class PageMetaBuilderTests
{
    #region 辅助
    private static ListField List(String name, String display) => new() { Name = name, DisplayName = display, Type = typeof(String) };

    private static FormField Form(String name, String display) => new() { Name = name, DisplayName = display, Type = typeof(String) };

    private static SearchField Search(String name, String display) => new() { Name = name, DisplayName = display, Type = typeof(String) };
    #endregion

    [Fact(DisplayName = "字段数组保留 name 并标记虚拟字段")]
    public void BuildFields_KeepsNameAndVirtual()
    {
        var fields = PageMetaBuilder.BuildFields([List("Name", "姓名"), List("DetailLink", "明细")]);

        Assert.Equal(2, fields.Count);
        Assert.Equal("Name", fields[0]["name"]);
        Assert.Equal("姓名", fields[0]["displayName"]);
        Assert.Equal(true, fields[0]["virtual"]);
    }

    [Fact(DisplayName = "搜索项把 Q 收敛为 keyword")]
    public void BuildSearch_ConvertsQToKeyword()
    {
        var items = PageMetaBuilder.BuildSearch([Search("Q", "关键字"), Search("Name", "姓名")]);

        Assert.Equal("q", items[0]["name"]);
        Assert.Equal("keyword", items[0]["kind"]);
        Assert.Equal("Name", items[1]["name"]);
        Assert.False(items[1].ContainsKey("kind"));
    }

    [Fact(DisplayName = "列表字段默认隐藏时输出 visible=false")]
    public void BuildFields_Visible()
    {
        var hidden = List("Remark", "备注");
        hidden.Visible = false;

        var fields = PageMetaBuilder.BuildFields([hidden, List("Name", "姓名")]);

        Assert.Equal(false, fields[0]["visible"]);
        Assert.False(fields[1].ContainsKey("visible"));
    }

    [Fact(DisplayName = "view 与 props 透传到字段字典")]
    public void BuildFields_EmitsViewAndProps()
    {
        var field = Search("Name", "姓名");
        field.View = "selectRange";
        field.Properties["source"] = "org";
        var list = List("Status", "状态");
        list.View = "statusTag";

        var dic = PageMetaBuilder.BuildFields([field, list])[0];
        var listDic = PageMetaBuilder.BuildFields([list])[0];

        Assert.Equal("selectRange", dic["view"]);
        Assert.NotNull(dic["props"]);
        Assert.Equal("statusTag", listDic["view"]);
    }

    [Fact(DisplayName = "add/detail 与 form 属性一致时判定为同一视图")]
    public void SameFields_ComparesProperties()
    {
        var form = new List<DataField> { Form("Name", "姓名") };
        var same = new List<DataField> { Form("Name", "姓名") };
        var changed = new List<DataField> { Form("Name", "名称") };

        Assert.True(PageMetaBuilder.SameFields(form, same));
        Assert.False(PageMetaBuilder.SameFields(form, changed));
    }
}
