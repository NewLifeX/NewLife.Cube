using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Web;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>列表端点行为测试。验证分页参数解析、分页元数据与空列表。</summary>
public class IndexEndpointTests
{
    #region 测试实体与控制器
    [BindTable("IndexTestEntity", "列表测试实体", ConnName = "Test")]
    private class IndexTestEntity : Entity<IndexTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String? _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, true, 50)]
        public String? Name { get => _Name; set { if (_Name != value) { OnPropertyChanging("Name", value); _Name = value; OnPropertyChanged("Name"); } } }
    }

    /// <summary>测试控制器。用内存行替代数据库查询</summary>
    private class IndexTestController : EntityController<IndexTestEntity>
    {
        public List<IndexTestEntity> Rows = [];

        /// <summary>最近一次列表查询使用的分页器，供断言参数解析</summary>
        public Pager? LastPager;

        protected override IEnumerable<IndexTestEntity> SearchData(Pager p)
        {
            LastPager = p;
            p.TotalCount = Rows.Count;
            return Rows;
        }
    }
    #endregion

    private static IndexTestController Create(String query = "")
    {
        var controller = new IndexTestController();
        var context = new DefaultHttpContext();
        if (!query.IsNullOrEmpty()) context.Request.QueryString = new QueryString(query);
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        // WebHelper.Params 走静态 HttpContext.Current 读取查询串，单测里显式注入访问器
        NewLife.Web.HttpContext.Configure(new HttpContextAccessor { HttpContext = context });

        return controller;
    }

    private static IndexTestEntity CreateRow(Int32 id) => new() { Id = id, Name = "行" + id };

    [Fact(DisplayName = "列表：返回数据行与分页总数")]
    public void Index_ReturnsRowsAndTotal()
    {
        var controller = Create();
        controller.Rows.AddRange([CreateRow(1), CreateRow(2), CreateRow(3)]);

        var rs = controller.Index();

        Assert.Equal(0, rs.Code);
        Assert.Equal(3, rs.Data.Count);
        Assert.Equal(3, rs.Page.TotalCount);
        Assert.Equal(1, rs.Page.PageIndex);
        Assert.Equal(20, rs.Page.PageSize);
    }

    [Fact(DisplayName = "列表：请求参数覆盖页码与页大小")]
    public void Index_AppliesPagingParams()
    {
        var controller = Create("?PageIndex=2&PageSize=5");
        controller.Rows.AddRange([CreateRow(1)]);

        controller.Index();

        Assert.NotNull(controller.LastPager);
        Assert.Equal(2, controller.LastPager!.PageIndex);
        Assert.Equal(5, controller.LastPager.PageSize);
    }

    [Fact(DisplayName = "列表：页大小超限时收敛到上限")]
    public void Index_ClampsPageSize()
    {
        var controller = Create("?PageSize=100000");

        controller.Index();

        Assert.Equal(Pager.MaxPageSize, controller.LastPager!.PageSize);
    }

    [Fact(DisplayName = "列表：无数据时返回空列表与零总数")]
    public void Index_Empty_ReturnsEmptyList()
    {
        var controller = Create();

        var rs = controller.Index();

        Assert.Equal(0, rs.Code);
        Assert.Empty(rs.Data);
        Assert.Equal(0, rs.Page.TotalCount);
    }
}
