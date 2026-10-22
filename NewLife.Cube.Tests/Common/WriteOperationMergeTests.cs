using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube;
using XCode;
using Xunit;

namespace NewLife.Cube.Tests.Common;

/// <summary>写操作部分更新语义测试。验证"提交多少改多少"：未提交保持原值，显式 null 清空</summary>
public class WriteOperationMergeTests
{
    #region 测试实体与模型
    [BindTable("MergeTestEntity", "合并测试实体", ConnName = "Test")]
    private class MergeTestEntity : Entity<MergeTestEntity>
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

        private String? _Remark;
        /// <summary>备注</summary>
        [DisplayName("备注")]
        [DataObjectField(false, false, true, 200)]
        public String? Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }
    }

    private class MergeTestModel
    {
        public Int32 Id { get; set; }
        public String? Name { get; set; }
        public String? Remark { get; set; }
    }

    private class EntityMergeController : EntityController<MergeTestEntity>
    {
        protected override void WriteLog(String action, Boolean success, String remark) { }

        public void Merge(MergeTestEntity target, MergeTestEntity source) => MergeSubmitted(target, source);

        public Task<ISet<String>> SubmittedFields() => GetSubmittedFieldsAsync();
    }

    private class ModelMergeController : EntityController<MergeTestEntity, MergeTestModel>
    {
        protected override void WriteLog(String action, Boolean success, String remark) { }

        public void Copy(MergeTestEntity target, MergeTestModel model, ISet<String> fields) => CopySubmitted(target, model, fields, "Id");
    }
    #endregion

    /// <summary>创建测试控制器。临时关闭 OrderByKey 避免构造函数内 Meta.Count 查询数据库</summary>
    private static T Create<T>(Func<T> factory) where T : class
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            return factory();
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    [Fact(DisplayName = "实体模型：无请求体时按脏字段覆盖，未提交字段保持原值")]
    public void Merge_Entity_KeepsUnsubmitted()
    {
        var controller = Create(() => new EntityMergeController());
        var target = new MergeTestEntity { Id = 1, Name = "A", Age = 10, Remark = "keep" };
        var source = new MergeTestEntity { Id = 1, Name = "B" };

        controller.Merge(target, source);

        Assert.Equal("B", target.Name);
        Assert.Equal(10, target.Age);
        Assert.Equal("keep", target.Remark);
    }

    [Fact(DisplayName = "POCO 模型：只覆盖请求 JSON 中出现的字段，显式 null 清空")]
    public void Copy_ProvidedFields_Only()
    {
        var controller = Create(() => new ModelMergeController());
        var target = new MergeTestEntity { Id = 1, Name = "A", Age = 10, Remark = "keep" };
        var model = new MergeTestModel { Id = 1, Name = "B", Remark = null };

        controller.Copy(target, model, new HashSet<String>(StringComparer.OrdinalIgnoreCase) { "name", "remark" });

        Assert.Equal("B", target.Name);
        Assert.Null(target.Remark);
        Assert.Equal(10, target.Age);
    }

    [Fact(DisplayName = "POCO 模型：未命中的字段一律不改")]
    public void Copy_EmptyFields_KeepsAll()
    {
        var controller = Create(() => new ModelMergeController());
        var target = new MergeTestEntity { Id = 1, Name = "A", Age = 10, Remark = "keep" };
        var model = new MergeTestModel { Id = 1, Name = "B", Remark = null };

        controller.Copy(target, model, new HashSet<String>(StringComparer.OrdinalIgnoreCase));

        Assert.Equal("A", target.Name);
        Assert.Equal("keep", target.Remark);
    }

    [Fact(DisplayName = "能从请求 JSON 解析出实际提交的顶层字段名")]
    public async Task SubmittedFields_ReadsJsonKeys()
    {
        var controller = Create(() => new EntityMergeController());
        var http = new DefaultHttpContext
        {
            Request =
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"id\":1,\"name\":\"B\",\"remark\":null}")),
                ContentType = "application/json",
            }
        };
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = http };

        var fields = await controller.SubmittedFields();

        Assert.NotNull(fields);
        Assert.Contains("id", fields!);
        Assert.Contains("name", fields!);
        Assert.Contains("remark", fields!);
        Assert.Equal(3, fields!.Count);
    }
}
