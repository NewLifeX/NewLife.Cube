using System;
using System.Collections.Generic;
using NewLife.Web;
using Xunit;

namespace NewLife.Cube.Tests.Web;

/// <summary>分页器参数解析</summary>
public class PagerTests
{
    private static Pager Create(params (String Key, String Value)[] items)
    {
        var dic = new Dictionary<String, String>();
        foreach (var (key, value) in items) dic[key] = value;
        return new Pager(dic);
    }

    [Fact]
    [System.ComponentModel.DisplayName("分页 - 请求参数的 pageSize 上限与 0 的语义")]
    public void PageSizeClamp()
    {
        // 正常值原样保留
        Assert.Equal(50, Create(("PageSize", "50")).PageSize);

        // 0 与负数不再表示“不分页”，回退到默认大小，避免一次拉走整表
        Assert.Equal(20, Create(("PageSize", "0")).PageSize);
        Assert.Equal(20, Create(("PageSize", "-5")).PageSize);

        // 超大值截断到上限
        Assert.Equal(Pager.MaxPageSize, Create(("PageSize", "1000000")).PageSize);

        // 非法文本回退默认值
        Assert.Equal(20, Create(("PageSize", "abc")).PageSize);
    }

    [Fact]
    [System.ComponentModel.DisplayName("分页 - 构造后仍可显式设为不分页")]
    public void PageSizeZeroAfterConstruct()
    {
        // 内部场景（删除全部、分片导出）需要不分页，构造后赋 0 不受上限约束
        var p = Create(("PageSize", "1000000"));
        p.PageSize = 0;
        Assert.Equal(0, p.PageSize);
    }

    [Fact]
    [System.ComponentModel.DisplayName("分页 - orderBy 复合排序被接收且不进入查询条件")]
    public void OrderByAccepted()
    {
        var p = Create(("orderBy", "Name,CreateTime desc"));

        Assert.Equal("Name,CreateTime desc", p.OrderBy);
        // 排序参数不应被当成查询条件拼进 URL，也不应出现在 where 部分
        var url = p.GetBaseUrl(true, false, false).ToString();
        Assert.DoesNotContain("orderBy", url);
    }

    [Fact]
    [System.ComponentModel.DisplayName("分页 - orderBy 非法表达式被忽略")]
    public void OrderByRejected()
    {
        // 子查询、函数调用、注释、分号等一律丢弃，交给数据层的必须是纯字段名
        foreach (var text in new[] { "Name;drop table User", "(select 1)", "Name--", "Name/**/", "1=1", "Name desc, (" })
        {
            Assert.False(Pager.IsValidOrderBy(text), text);
            Assert.Null(Create(("orderBy", text)).OrderBy);
        }

        Assert.True(Pager.IsValidOrderBy("Name"));
        Assert.True(Pager.IsValidOrderBy("A.Name desc, B.CreateTime asc"));
        Assert.True(Pager.IsValidOrderBy("Name DESC"));
    }

    [Fact]
    [System.ComponentModel.DisplayName("分页 - orderBy 优先级高于 sort")]
    public void OrderByWinsOverSort()
    {
        var p = Create(("Sort", "Id"), ("Desc", "true"), ("orderBy", "Name"));

        Assert.Equal("Id", p.Sort);
        Assert.Equal("Name", p.OrderBy);
    }
}
