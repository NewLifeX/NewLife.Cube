using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>详情端点行为测试。验证主键校验、数据不存在与数据权限校验</summary>
public class DetailEndpointTests
{
    #region 测试实体与控制器
    [BindTable("DetailTestEntity", "详情测试实体", ConnName = "Test")]
    private class DetailTestEntity : Entity<DetailTestEntity>
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

    /// <summary>测试控制器。用内存行替代数据库</summary>
    private class DetailTestController : ReadOnlyEntityController<DetailTestEntity>
    {
        public List<Int32> Rows = [];
        public Int32 ValidCalls;

        protected override DetailTestEntity FindData(Object key)
        {
            var id = key.ToInt();
            if (!Rows.Contains(id)) return null!;

            var entity = new DetailTestEntity { Id = id, Name = "行" + id };
            // 索引器的写操作落在扩展字典上，IsNullKey 依赖它判断主键是否为空
            entity["Id"] = id;

            return entity;
        }

        protected override Boolean Valid(DetailTestEntity entity, DataObjectMethodType type, Boolean post)
        {
            ValidCalls++;
            return true;
        }
    }
    #endregion

    private static DetailTestController Create(params Int32[] rows)
    {
        var controller = new DetailTestController();
        controller.Rows.AddRange(rows);
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext() };

        return controller;
    }

    [Fact(DisplayName = "详情：按路径主键返回记录并执行数据权限校验")]
    public void Detail_ByPathId_ReturnsRow()
    {
        var controller = Create(5);

        var rs = controller.Detail("5");

        Assert.Equal(0, rs.Code);
        Assert.Equal(5, rs.Data.Id);
        Assert.Equal("行5", rs.Data.Name);
        Assert.Equal(1, controller.ValidCalls);
    }

    [Fact(DisplayName = "详情：主键为空抛参数异常")]
    public void Detail_EmptyId_Throws()
    {
        var controller = Create(5);

        Assert.Throws<XException>(() => controller.Detail(""));
        Assert.Throws<XException>(() => controller.Detail(null!));
    }

    [Fact(DisplayName = "详情：记录不存在抛异常且不做数据权限校验")]
    public void Detail_MissingRow_Throws()
    {
        var controller = Create(5);

        Assert.Throws<XException>(() => controller.Detail("99"));
        Assert.Equal(0, controller.ValidCalls);
    }
}
