using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Remoting;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>删除端点行为测试。验证单条删除、ids 批量删除与上限保护</summary>
public class DeleteEndpointTests
{
    #region 测试实体与控制器
    [BindTable("DeleteTestEntity", "删除测试实体", ConnName = "Test")]
    private class DeleteTestEntity : Entity<DeleteTestEntity>
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
        public String? Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }
    }

    /// <summary>测试控制器。用内存行替代数据库，拦截删除以记录主键</summary>
    private class DeleteTestController : EntityController<DeleteTestEntity>
    {
        public List<Int32> Rows = [];
        public List<Int32> Deleted = [];

        protected override void WriteLog(String action, Boolean success, String remark) { }

        /// <summary>测试辅助：暴露 FindData 供断言</summary>
        public DeleteTestEntity Find(String key) => FindData(key);

        protected override DeleteTestEntity FindData(Object key)
        {
            var id = key.ToInt();
            if (!Rows.Contains(id)) return null!;

            var entity = new DeleteTestEntity { Id = id };
            // 实体索引器的写操作落在扩展字典上，IsNullKey 依赖它判断主键是否为空
            entity["Id"] = id;

            return entity;
        }

        protected override Boolean Valid(DeleteTestEntity entity, DataObjectMethodType type, Boolean post) => true;

        protected override Int32 OnDelete(DeleteTestEntity entity)
        {
            Deleted.Add(entity.Id);
            return 1;
        }
    }
    #endregion

    /// <summary>创建测试控制器并指定查询串。临时关闭 OrderByKey 避免构造函数内查询数据库</summary>
    private static DeleteTestController Create(String query = "", params Int32[] rows)
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new DeleteTestController();
            controller.Rows.AddRange(rows);

            var http = new DefaultHttpContext();
            if (!query.IsNullOrEmpty()) http.Request.QueryString = new QueryString(query);
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    [Fact(DisplayName = "单条删除：主键在路径，删除成功返回被删行")]
    public void Delete_ByPathId()
    {
        var controller = Create("", 1, 2);

        var found = controller.Find("2");
        Assert.NotNull(found);
        Assert.False(((IEntity)found).IsNullKey, $"unique={DeleteTestEntity.Meta.Unique?.Name} id={found.Id}");

        var rs = controller.Delete("2");

        Assert.Equal(0, rs.Code);
        Assert.Equal(2, rs.Data!.Id);
        Assert.Equal([2], controller.Deleted);
    }

    [Fact(DisplayName = "单条删除：记录不存在返回 404 而不是 500")]
    public void Delete_MissingRow_NotFound()
    {
        var controller = Create("", 1);

        var ex = Assert.Throws<ApiException>(() => controller.Delete("99"));

        Assert.Equal(404, ex.Code);
    }

    [Fact(DisplayName = "单条删除：主键为空返回参数错误")]
    public void Delete_EmptyId_ParamError()
    {
        var controller = Create("", 1);

        var ex = Assert.Throws<ApiException>(() => controller.Delete(""));

        Assert.Equal(-2, ex.Code);
    }

    [Fact(DisplayName = "批量删除：ids 逗号分隔删除全部选中行")]
    public void DeleteAll_CommaSeparatedIds()
    {
        var controller = Create("?ids=1,2", 1, 2, 3);

        var rs = controller.DeleteAll(["1,2"]);

        Assert.Equal(0, rs.Code);
        Assert.Equal("2", rs.Data);
        Assert.Equal([1, 2], controller.Deleted);
    }

    [Fact(DisplayName = "批量删除：ids 重复参数同样生效")]
    public void DeleteAll_RepeatedIds()
    {
        var controller = Create("?ids=1&ids=3", 1, 2, 3);

        var rs = controller.DeleteAll(["1", "3"]);

        Assert.Equal(0, rs.Code);
        Assert.Equal([1, 3], controller.Deleted);
    }

    [Fact(DisplayName = "批量删除：不存在的行跳过，不影响其它行")]
    public void DeleteAll_SkipMissingRows()
    {
        var controller = Create("?ids=1,99", 1, 2);

        var rs = controller.DeleteAll(["1,99"]);

        Assert.Equal(0, rs.Code);
        Assert.Equal("1", rs.Data);
        Assert.Equal([1], controller.Deleted);
    }

    [Fact(DisplayName = "批量删除：ids 为空返回参数错误")]
    public void DeleteAll_EmptyIds_ParamError()
    {
        var controller = Create("?ids=", 1);

        var rs = controller.DeleteAll([]);

        Assert.Equal(-2, rs.Code);
        Assert.False(rs.Message.IsNullOrEmpty());
    }

    [Fact(DisplayName = "批量删除：超过上限返回参数错误并提示改用条件删除")]
    public void DeleteAll_ExceedLimit_ParamError()
    {
        var controller = Create("?ids=1", 1);
        var ids = Enumerable.Range(1, EntityController<DeleteTestEntity, DeleteTestEntity>.MaxBatchDelete + 1).Select(e => e.ToString()).ToArray();

        var rs = controller.DeleteAll([String.Join(",", ids)]);

        Assert.Equal(-2, rs.Code);
        // 消息按请求语言翻译，用 L.T 结果做包含判断，避免绑定某一种语言
        Assert.Contains(L.T("Too many ids to delete at once. Use query conditions instead."), rs.Message);
    }

    [Fact(DisplayName = "批量删除：既无 ids 也无业务条件时拒绝删除")]
    public void DeleteAll_NoCondition_ParamError()
    {
        // 条件删除依赖 WebHelper.Params（取当前 Http 上下文），测试里显式配置访问器
        var controller = Create("", 1, 2);
        NewLife.Web.HttpContext.Configure(new HttpContextAccessor { HttpContext = controller.HttpContext });
        try
        {
            var rs = controller.DeleteAll(null!);

            Assert.Equal(-2, rs.Code);
            Assert.False(rs.Message.IsNullOrEmpty());
        }
        finally
        {
            NewLife.Web.HttpContext.Configure(new HttpContextAccessor());
        }
    }
}
