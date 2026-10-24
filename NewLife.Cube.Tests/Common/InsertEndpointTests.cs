using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>新增端点行为测试。验证主键约束、校验钩子与落库时机</summary>
public class InsertEndpointTests
{
    #region 测试实体与控制器
    [BindTable("InsertTestEntity", "新增测试实体", ConnName = "Test")]
    private class InsertTestEntity : Entity<InsertTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String? _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, false, 50)]
        public String? Name { get => _Name; set { if (_Name != value) { OnPropertyChanging("Name", value); _Name = value; OnPropertyChanged("Name"); } } }
    }

    /// <summary>测试控制器。OnInsert 用内存计数替代落库</summary>
    private class InsertTestController : EntityController<InsertTestEntity>
    {
        public InsertTestEntity? Inserted;
        public Boolean ValidResult = true;

        protected override Int32 OnInsert(InsertTestEntity entity)
        {
            // 模拟数据库自增
            entity.Id = 7;
            entity["Id"] = 7;
            Inserted = entity;

            return 1;
        }

        protected override Boolean Valid(InsertTestEntity entity, DataObjectMethodType type, Boolean post) => ValidResult;
    }
    #endregion

    private static InsertTestController Create()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new InsertTestController();
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    [Fact(DisplayName = "新增：合法数据落库成功并返回主键")]
    public async Task Insert_ValidData_Succeeds()
    {
        var controller = Create();

        var rs = await controller.Insert(new InsertTestEntity { Name = "合法值" });

        Assert.Equal(0, rs.Code);
        Assert.Equal(7, rs.Data.Id);
        Assert.NotNull(controller.Inserted);
        Assert.Equal("合法值", controller.Inserted!.Name);
    }

    [Fact(DisplayName = "新增：提交内容带自增主键时拒绝")]
    public async Task Insert_WithIdentityKey_Throws()
    {
        var controller = Create();
        var entity = new InsertTestEntity { Id = 5, Name = "带主键" };
        entity["Id"] = 5;

        await Assert.ThrowsAsync<Exception>(() => controller.Insert(entity));
        Assert.Null(controller.Inserted);
    }

    [Fact(DisplayName = "新增：Valid 钩子返回 false 时不落库并返回失败信封")]
    public async Task Insert_ValidHookRejects_ReturnsFailure()
    {
        var controller = Create();
        controller.ValidResult = false;

        var rs = await controller.Insert(new InsertTestEntity { Name = "被拒绝" });

        Assert.NotEqual(0, rs.Code);
        Assert.Null(controller.Inserted);
        // 失败时主键清零，前端才不会显示"保存"按钮
        Assert.Equal(0, rs.Data.Id);
    }
}
