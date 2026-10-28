using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Cube.ViewModels;
using XCode.Membership;
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

    [Fact(DisplayName = "主时间字段收敛为 dtStart/dtEnd 区间并保留显示名")]
    public void BuildSearch_ConvertsMasterTimeToRange()
    {
        var master = User.Meta.Factory.MasterTime;
        Assert.NotNull(master);
        var search = new List<DataField>
        {
            Search("Q", "关键字"),
            Search(master!.Name, "更新时间"),
            Search("Name", "姓名"),
        };

        var items = PageMetaBuilder.BuildSearch(search, User.Meta.Factory);

        var range = items.FirstOrDefault(e => e.TryGetValue("kind", out var kind) && (String)kind == "range");
        Assert.NotNull(range);
        Assert.Equal("dtStart", range!["name"]);
        Assert.Equal("dtEnd", range["end"]);
        Assert.Equal(master.Name, range["field"]);
        Assert.Equal("更新时间", range["displayName"]);
        Assert.DoesNotContain(items, e => e.TryGetValue("name", out var name) && (String)name == master.Name);
    }

    [Fact(DisplayName = "主时间字段为雪花主键时借用创建时间显示名并去掉重复区间项")]
    public void BuildSearch_SnowflakeMasterTime_RemovesDuplicatedTimeField()
    {
        var factory = XCode.Membership.Log.Meta.Factory;
        var master = factory.MasterTime;
        // 日志表主键是雪花编号（DataScale=time），类型不是 DateTime，才会走借用时间字段的分支
        Assert.NotNull(master);
        Assert.NotEqual(typeof(DateTime), master!.Type);

        var search = new List<DataField>
        {
            Search("Q", "关键字"),
            Search("CreateTime", "创建时间"),
            Search("Category", "类别"),
        };

        var items = PageMetaBuilder.BuildSearch(search, factory);

        // 创建时间自身的查询项被摘掉，搜索区只留一个时间区间，标题借用创建时间
        Assert.DoesNotContain(items, e => e.TryGetValue("name", out var name) && (String)name == "CreateTime");
        var ranges = items.Where(e => e.TryGetValue("kind", out var kind) && (String)kind == "range").ToList();
        var range = Assert.Single(ranges);
        Assert.Equal("dtStart", range["name"]);
        Assert.Equal("dtEnd", range["end"]);
        Assert.Equal(master.Name, range["field"]);
        Assert.Equal("创建时间", range["displayName"]);
    }

    [Fact(DisplayName = "不传实体工厂时不生成时间区间 对应 EnableTimeRange=false")]
    public void BuildSearch_DisableTimeRange_OmitsRange()
    {
        var items = PageMetaBuilder.BuildSearch([Search("UpdateTime", "更新时间")], null);

        Assert.DoesNotContain(items, e => e.TryGetValue("kind", out var kind) && (String)kind == "range");
        Assert.Contains(items, e => e.TryGetValue("name", out var name) && (String)name == "UpdateTime");
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

    [Fact(DisplayName = "TraceId 自动生成星尘追踪链接")]
    public void ApplyTraceUrl_BuildsStarWebLink()
    {
        var trace = List("TraceId", "追踪");

        PageMetaBuilder.ApplyTraceUrl([trace], "https://star.example.com/");

        Assert.Equal("https://star.example.com/trace?id={TraceId}", trace.Url);
        Assert.Equal("_blank", trace.Target);
        Assert.Equal("追踪", trace.Text);
    }

    [Fact(DisplayName = "TraceId 链接支持 {traceId} 模板且不覆盖控制器自定义链接")]
    public void ApplyTraceUrl_RespectsTemplateAndCustomUrl()
    {
        var trace = List("TraceId", "追踪");
        PageMetaBuilder.ApplyTraceUrl([trace], "https://star.example.com/trace/{traceId}");
        Assert.Equal("https://star.example.com/trace/{TraceId}", trace.Url);

        var custom = List("TraceId", "追踪");
        custom.Url = "/Admin/Log?trace={TraceId}";
        PageMetaBuilder.ApplyTraceUrl([custom], "https://star.example.com");
        Assert.Equal("/Admin/Log?trace={TraceId}", custom.Url);
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
