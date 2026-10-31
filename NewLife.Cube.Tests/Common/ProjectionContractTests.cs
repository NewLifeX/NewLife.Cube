using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using NewLife.Web;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>接口回包投影契约测试。回包 = 字段集合 × 行值，只写字段不参与投影。</summary>
public class ProjectionContractTests
{
    #region 测试实体与控制器
    [BindTable("ProjectionTestEntity", "投影测试实体", ConnName = "Test")]
    private class ProjectionTestEntity : Entity<ProjectionTestEntity>
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

        private String? _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [DataObjectField(false, false, true, 50)]
        public String? Remark { get => _Remark; set { if (_Remark != value) { OnPropertyChanging("Remark", value); _Remark = value; OnPropertyChanged("Remark"); } } }

        private String? _Password;
        /// <summary>密码</summary>
        [DisplayName("密码")]
        [DataObjectField(false, false, true, 200)]
        public String? Password { get => _Password; set { if (_Password != value) { OnPropertyChanging("Password", value); _Password = value; OnPropertyChanged("Password"); } } }
    }

    /// <summary>测试控制器。用内存行替代数据库查询</summary>
    private class ProjectionTestController : EntityController<ProjectionTestEntity>
    {
        public List<ProjectionTestEntity> Rows = [];

        static ProjectionTestController()
        {
            // 列表移出名称列，并连同主键一起移出：验证投影只下发列表字段、且主键恒在
            ListFields.RemoveField("Name", "Id");
            // 详情移出备注，编辑表单保留：验证详情回包补齐编辑表单字段
            DetailFields.RemoveField("Remark");
        }

        protected override void WriteLog(String action, Boolean success, String remark) { }

        protected override IEnumerable<ProjectionTestEntity> SearchData(Pager p)
        {
            p.TotalCount = Rows.Count;
            return Rows;
        }

        protected override ProjectionTestEntity FindData(Object key)
        {
            var id = key.ToInt();
            var entity = Rows.Find(e => e.Id == id);
            if (entity != null) entity["Id"] = id;

            return entity!;
        }

        protected override Boolean Valid(ProjectionTestEntity entity, DataObjectMethodType type, Boolean post) => true;

        /// <summary>暴露投影方法，供契约测试直接断言</summary>
        public ApiRow Project(ProjectionTestEntity entity, IList<DataField> fields) => OnProject(entity, fields);
    }
    #endregion

    private static ProjectionTestController Create()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new ProjectionTestController();
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // WebHelper.Params 走静态 HttpContext.Current 读取查询串，单测里显式注入访问器
            NewLife.Web.HttpContext.Configure(new HttpContextAccessor { HttpContext = controller.ControllerContext.HttpContext });

            return controller;
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    private static ProjectionTestEntity CreateRow(Int32 id) => new() { Id = id, Name = "行" + id, Remark = "备注" + id, Password = "$sha512$测试哈希" };

    [Fact(DisplayName = "列表：只下发列表字段，主键恒在，只写字段不出现")]
    public void Index_ProjectsByListFields()
    {
        var controller = Create();
        controller.Rows.Add(CreateRow(1));

        var rs = controller.Index();

        var row = Assert.Single(rs.Data);
        // 主键被移出列表字段也恒下发
        Assert.Equal(1, row["Id"]);
        // 移出列表字段的列不下发
        Assert.False(row.ContainsKey("Name"));
        // 只写字段不参与投影
        Assert.False(row.ContainsKey("Password"));
        // 列表字段照常下发
        Assert.Equal("备注1", row["Remark"]);
    }

    [Fact(DisplayName = "详情：补齐编辑表单字段，只写字段不出现")]
    public void Detail_IncludesEditFormFields()
    {
        var controller = Create();
        controller.Rows.Add(CreateRow(5));

        var rs = controller.Detail("5");

        // 详情移出的备注由编辑表单字段补齐（详情接口是编辑回显数据源）
        Assert.Equal("备注5", rs.Data["Remark"]);
        Assert.Equal("行5", rs.Data["Name"]);
        Assert.False(rs.Data.ContainsKey("Password"));
    }

    [Fact(DisplayName = "投影：字段集合内的扩展值随行内联")]
    public void Project_MergesExtendItems()
    {
        var controller = Create();
        var entity = CreateRow(1);
        ((IExtend)entity).Items["AvatarImage"] = "url://1";

        // 虚拟列不在实体表结构中，索引器与属性反射都取不到值，由扩展字典补齐
        var row = controller.Project(entity, [new DataField { Name = "Name" }, new DataField { Name = "AvatarImage" }]);

        Assert.Equal("url://1", row["AvatarImage"]);
        Assert.Equal("行1", row["Name"]);
    }

    [Fact(DisplayName = "投影：字段集合之外的扩展值不下发")]
    public void Project_SkipsExtendItemsOutsideFields()
    {
        var controller = Create();
        var entity = CreateRow(1);
        ((IExtend)entity).Items["InternalNote"] = "内部备注";

        var row = controller.Project(entity, [new DataField { Name = "Name" }]);

        Assert.False(row.ContainsKey("InternalNote"));
        Assert.Equal("行1", row["Name"]);
    }

    [Fact(DisplayName = "投影：行级不可见字段即使有扩展值也不下发")]
    public void Project_SkipsInvisibleFieldExtendValue()
    {
        var controller = Create();
        var entity = CreateRow(1);
        ((IExtend)entity).Items["AvatarImage"] = "url://1";

        var hidden = new DataField { Name = "AvatarImage", DataVisible = _ => false };
        var row = controller.Project(entity, [hidden, new DataField { Name = "Name" }]);

        Assert.False(row.ContainsKey("AvatarImage"));
    }
}
