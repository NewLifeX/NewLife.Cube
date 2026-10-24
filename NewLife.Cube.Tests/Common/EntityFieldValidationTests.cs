using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NewLife.Remoting;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>字段级验证集成测试。用 SQLite 真库让 Factory 具备模型元数据，验证新增时的必填与长度校验</summary>
public class EntityFieldValidationTests
{
    #region 测试实体
    [BindTable("ValidTestEntity", "校验测试实体", ConnName = "ValidTest")]
    private class ValidTestEntity : Entity<ValidTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String? _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, false, 20)]
        public String? Name { get => _Name; set { if (_Name != value) { OnPropertyChanging("Name", value); _Name = value; OnPropertyChanged("Name"); } } }
    }
    #endregion

    #region 建库
    private static readonly Object _lock = new();
    private static Boolean _inited;

    /// <summary>建一次性 SQLite 库与表，让 Factory 能读到字段元数据</summary>
    private static void EnsureDb()
    {
        if (_inited) return;
        lock (_lock)
        {
            if (_inited) return;

            var dir = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "CubeValidTests.db");

            DAL.AddConnStr("ValidTest", $"Data Source={file};provider=sqlite;Migration=On", null, "sqlite");
            DAL.Create("ValidTest").SetTables((IDataTable)ValidTestEntity.Meta.Table.DataTable.Clone());

            _inited = true;
        }
    }
    #endregion

    /// <summary>测试控制器。Valid 恒真，把校验交给 Model.xml 元数据那一道</summary>
    private class ValidTestController : EntityController<ValidTestEntity>
    {
        public ValidTestEntity? Inserted;

        protected override Int32 OnInsert(ValidTestEntity entity)
        {
            entity.Id = 1;
            entity["Id"] = 1;
            Inserted = entity;

            return 1;
        }

        protected override Boolean Valid(ValidTestEntity entity, DataObjectMethodType type, Boolean post) => true;

        /// <summary>开启字段级校验（默认关闭，需控制器显式开启）</summary>
        protected override Boolean EnableFieldValidation => true;
    }

    private static ValidTestController Create()
    {
        var controller = new ValidTestController
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        return controller;
    }

    [Fact(DisplayName = "字段级校验：必填字段为空返回字段级错误且不落库")]
    public async Task Insert_RequiredFieldEmpty_ReturnsFieldErrors()
    {
        EnsureDb();
        var controller = Create();

        var entity = new ValidTestEntity { Name = "" };
        entity["Name"] = "";
        var rs = await controller.Insert(entity);

        Assert.NotEqual(0, rs.Code);
        Assert.NotNull(rs.FieldErrors);
        Assert.Contains(rs.FieldErrors!, e => String.Equals(e.Field, "Name", StringComparison.OrdinalIgnoreCase));
        Assert.Null(controller.Inserted);
    }

    [Fact(DisplayName = "字段级校验：超过字段长度返回字段级错误")]
    public async Task Insert_FieldTooLong_ReturnsFieldErrors()
    {
        EnsureDb();
        var controller = Create();

        var entity = new ValidTestEntity { Name = new String('x', 21) };
        entity["Name"] = new String('x', 21);
        var rs = await controller.Insert(entity);

        Assert.NotEqual(0, rs.Code);
        Assert.NotNull(rs.FieldErrors);
        Assert.Contains(rs.FieldErrors!, e => String.Equals(e.Field, "Name", StringComparison.OrdinalIgnoreCase));
        Assert.Null(controller.Inserted);
    }

    [Fact(DisplayName = "字段级校验：合法数据通过校验并落库")]
    public async Task Insert_Valid_Inserts()
    {
        EnsureDb();
        var controller = Create();

        var entity = new ValidTestEntity { Name = "合法值" };
        entity["Name"] = "合法值";
        var rs = await controller.Insert(entity);

        var detail = $"code={rs.Code} msg={rs.Message} errors={String.Join(";", (rs.FieldErrors ?? []).Select(e => e.Field + ":" + e.Message))}";
        Assert.True(rs.Code == 0, detail);
        Assert.NotNull(controller.Inserted);
        Assert.Equal("合法值", controller.Inserted!.Name);
    }
}
