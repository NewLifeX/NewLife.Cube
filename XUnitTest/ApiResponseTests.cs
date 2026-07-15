using System;
using System.ComponentModel;
using NewLife.Web;
using Xunit;

namespace XUnitTest;

/// <summary>PageModel 分页模型单元测试</summary>
public class ApiResponseTests
{
    [Fact]
    [DisplayName("PageModel 默认构造后属性为默认值")]
    public void PageModel_Default_Values()
    {
        var pm = new PageModel();

        Assert.Equal(0, pm.PageIndex);
        Assert.Equal(0, pm.PageSize);
        Assert.Equal(0L, pm.TotalCount);
    }

    [Fact]
    [DisplayName("PageModel 可设置分页参数")]
    public void PageModel_SetAndGet()
    {
        var pm = new PageModel
        {
            PageIndex = 1,
            PageSize = 20,
            TotalCount = 100
        };

        Assert.Equal(1, pm.PageIndex);
        Assert.Equal(20, pm.PageSize);
        Assert.Equal(100, pm.TotalCount);
    }

    [Fact]
    [DisplayName("PageModel 不同分页场景")]
    public void PageModel_DifferentScenarios()
    {
        // 100条数据，每页20条
        var pm = new PageModel { PageIndex = 1, PageSize = 20, TotalCount = 100 };
        Assert.Equal(100, pm.TotalCount);

        // 0条数据
        pm = new PageModel { PageIndex = 1, PageSize = 20, TotalCount = 0 };
        Assert.Equal(0L, pm.TotalCount);

        // LongTotalCount 字符串类型
        pm = new PageModel { LongTotalCount = "1000" };
        Assert.Equal("1000", pm.LongTotalCount);
    }
}
