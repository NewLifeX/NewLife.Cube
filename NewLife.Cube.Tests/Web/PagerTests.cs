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
}
