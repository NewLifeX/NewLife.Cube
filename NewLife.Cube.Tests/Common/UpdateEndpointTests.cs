using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Remoting;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>修改端点行为测试。验证路径主键、主键一致性校验与部分更新语义</summary>
public class UpdateEndpointTests
{
    #region 测试实体与控制器
    [BindTable("UpdateTestEntity", "修改测试实体", ConnName = "Test")]
    private class UpdateTestEntity : Entity<UpdateTestEntity>
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

        private Int32 _Age;
        /// <summary>年龄</summary>
        [DisplayName("年龄")]
        [DataObjectField(false, false, true, 0)]
        public Int32 Age { get => _Age; set { if (OnPropertyChanging("Age", value)) { _Age = value; OnPropertyChanged("Age"); } } }
    }

    /// <summary>测试控制器。用内存行替代数据库，跳过实际落库</summary>
    private class UpdateTestController : EntityController<UpdateTestEntity>
    {
        public List<Int32> Rows = [];

        protected override void WriteLog(String action, Boolean success, String remark) { }

        protected override UpdateTestEntity FindData(Object key)
        {
            var id = key.ToInt();
            if (!Rows.Contains(id)) return null!;

            var entity = new UpdateTestEntity { Id = id, Name = "原值", Age = 30 };
            // 实体索引器的写操作落在扩展字典上，IsNullKey 依赖它判断主键是否为空
            entity["Id"] = id;

            return entity;
        }

        protected override Boolean Valid(UpdateTestEntity entity, DataObjectMethodType type, Boolean post) => true;

        protected override Task<ApiResponse<UpdateTestEntity>> ProcessUpdate(UpdateTestEntity entity) =>
            Task.FromResult(new ApiResponse<UpdateTestEntity> { Code = 0, Data = entity });
    }
    #endregion

    /// <summary>创建测试控制器。临时关闭 OrderByKey 避免构造函数内查询数据库</summary>
    private static UpdateTestController Create(params Int32[] rows)
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new UpdateTestController();
            controller.Rows.AddRange(rows);
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    [Fact(DisplayName = "修改：主键取自路径，只覆盖提交过的字段")]
    public async Task Update_ByPathId_PartialFields()
    {
        var controller = Create(5);

        var rs = await controller.Update("5", new UpdateTestEntity { Name = "新值" });

        Assert.Equal(0, rs.Code);
        Assert.Equal(5, rs.Data!.Id);
        Assert.Equal("新值", rs.Data.Name);
        // 未提交的字段保持库中原值
        Assert.Equal(30, rs.Data.Age);
    }

    [Fact(DisplayName = "修改：body 主键与路径一致时正常通过")]
    public async Task Update_BodyKeyMatchesPath()
    {
        var controller = Create(5);

        var rs = await controller.Update("5", new UpdateTestEntity { Id = 5, Name = "新值" });

        Assert.Equal(0, rs.Code);
    }

    [Fact(DisplayName = "修改：body 主键与路径不一致时返回参数错误")]
    public async Task Update_BodyKeyMismatch_ParamError()
    {
        var controller = Create(5, 6);

        var ex = await Assert.ThrowsAsync<ApiException>(() => controller.Update("5", new UpdateTestEntity { Id = 6, Name = "新值" }));

        Assert.Equal(-2, ex.Code);
    }

    [Fact(DisplayName = "修改：路径主键为空时返回参数错误")]
    public async Task Update_EmptyId_ParamError()
    {
        var controller = Create(5);

        var ex = await Assert.ThrowsAsync<ApiException>(() => controller.Update("", new UpdateTestEntity { Name = "新值" }));

        Assert.Equal(-2, ex.Code);
    }

    [Fact(DisplayName = "修改：记录不存在时抛出业务异常")]
    public async Task Update_MissingRow_Throws()
    {
        var controller = Create(5);

        await Assert.ThrowsAsync<XException>(() => controller.Update("99", new UpdateTestEntity { Name = "新值" }));
    }
}
