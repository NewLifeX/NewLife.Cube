using System.ComponentModel;
using NewLife.Cube;
using NewLife.Remoting;
using NewLife.Web;
using Xunit;

namespace XUnitTest;

/// <summary>多级排序白名单（OSC-26092694a1）</summary>
public class SortWhitelistTests
{
    static readonly string[] Fields = ["Name", "CreateTime", "Id"];
    static bool Allowed(string name) => name is "Name" or "CreateTime" or "Id";

    [Fact]
    [DisplayName("Name,-CreateTime 编译为 Name asc, CreateTime desc，并清空 Sort")]
    public void Compile_TwoColumns_WritesOrderByAndClearsSort()
    {
        var pager = new Pager { Sort = "Id", Desc = true, OrderBy = "should-be-replaced" };
        SortWhitelist.Apply(pager, "Name,-CreateTime", Fields, Allowed);
        Assert.Null(pager.Sort);
        Assert.Equal("Name asc, CreateTime desc", pager.OrderBy);
        Assert.Equal("Name asc, CreateTime desc", SortWhitelist.Compile("name,-createtime", Fields, Allowed));
    }

    [Fact]
    [DisplayName("不在白名单的 Salary 返回 400")]
    public void Compile_UnknownField_Is400()
    {
        var ex = Assert.Throws<ApiException>(() => SortWhitelist.Compile("Salary", Fields, Allowed));
        Assert.Equal(400, ex.Code);
    }

    [Fact]
    [DisplayName("工厂有字段但不在 search∪list 时 400")]
    public void Compile_NotInSearchOrList_Is400()
    {
        var ex = Assert.Throws<ApiException>(() =>
            SortWhitelist.Compile("Name", Fields, n => n == "Id"));
        Assert.Equal(400, ex.Code);
    }

    [Fact]
    [DisplayName("超过 3 段返回 400")]
    public void Compile_FourSegments_Is400()
    {
        var ex = Assert.Throws<ApiException>(() =>
            SortWhitelist.Compile("Name,Id,CreateTime,Name", Fields, Allowed));
        Assert.Equal(400, ex.Code);
    }

    [Fact]
    [DisplayName("空串、空格、点、括号、引号返回 400")]
    public void Compile_IllegalTokens_Is400()
    {
        foreach (var raw in new[] { "", "Name, Id", "User.Name", "(Name)", "Na\"me", "Name," })
        {
            var ex = Assert.Throws<ApiException>(() => SortWhitelist.Compile(raw, Fields, Allowed));
            Assert.Equal(400, ex.Code);
        }
    }

    [Fact]
    [DisplayName("sorts 参数不存在时保留单列 sort/desc")]
    public void Apply_AbsentSorts_KeepsSingleColumn()
    {
        var pager = new Pager { Sort = "Name", Desc = true };
        SortWhitelist.Apply(pager, null, Fields, Allowed);
        Assert.Equal("Name", pager.Sort);
        Assert.True(pager.Desc);
        Assert.True(string.IsNullOrEmpty(pager.OrderBy));
    }
}
