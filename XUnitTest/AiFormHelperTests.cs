using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.AI;
using NewLife.Cube.ViewModels;
using XCode;
using Xunit;

namespace XUnitTest;

/// <summary>AI 表单助手单元测试 — 表单字段 Schema 构建与值类型转换</summary>
public class AiFormHelperTests
{
    #region 测试实体
    /// <summary>AI 表单测试实体。手写实体，仅用于元数据，不访问数据库</summary>
    [BindTable("AiFormTestEntity", "AI表单测试实体", ConnName = "Test")]
    private class AiFormTestEntity : Entity<AiFormTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号（自增主键）</summary>
        [DisplayName("编号")]
        [DataObjectField(true, true, false, 0)]
        public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

        private String _Name;
        /// <summary>名称</summary>
        [DisplayName("名称")]
        [DataObjectField(false, false, false, 50)]
        public String Name { get => _Name; set { if (OnPropertyChanging("Name", value)) { _Name = value; OnPropertyChanged("Name"); } } }

        private StatusKinds _Status;
        /// <summary>状态</summary>
        [DisplayName("状态")]
        [DataObjectField(false, false, false, 0)]
        public StatusKinds Status { get => _Status; set { if (OnPropertyChanging("Status", value)) { _Status = value; OnPropertyChanged("Status"); } } }

        private String _Password;
        /// <summary>密码（敏感字段）</summary>
        [BindColumn("Password", "密码", "", ItemType = "password")]
        [DisplayName("密码")]
        [DataObjectField(false, false, false, 50)]
        public String Password { get => _Password; set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } } }

        private String _Remark;
        /// <summary>备注（HTML 富文本）</summary>
        [BindColumn("Remark", "备注", "", ItemType = "html")]
        [DisplayName("备注")]
        [DataObjectField(false, false, false, 500)]
        public String Remark { get => _Remark; set { if (OnPropertyChanging("Remark", value)) { _Remark = value; OnPropertyChanged("Remark"); } } }

        private String _Content;
        /// <summary>内容（Markdown）</summary>
        [BindColumn("Content", "内容", "", ItemType = "markdown")]
        [DisplayName("内容")]
        [DataObjectField(false, false, false, 500)]
        public String Content { get => _Content; set { if (OnPropertyChanging("Content", value)) { _Content = value; OnPropertyChanged("Content"); } } }

        private DateTime _CreateTime;
        /// <summary>创建时间（自动维护字段）</summary>
        [DisplayName("创建时间")]
        [DataObjectField(false, false, false, 0)]
        public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "Name" => _Name,
                "Status" => _Status,
                "Password" => _Password,
                "CreateTime" => _CreateTime,
                "Remark" => _Remark,
                "Content" => _Content,
                _ => base[name],
            };
            set
            {
                switch (name)
                {
                    case "Id": _Id = value.ToInt(); break;
                    case "Name": _Name = value + ""; break;
                    case "Status": _Status = (StatusKinds)value.ToInt(); break;
                    case "Password": _Password = value + ""; break;
                    case "CreateTime": _CreateTime = value.ToDateTime(); break;
                    case "Remark": _Remark = value + ""; break;
                    case "Content": _Content = value + ""; break;
                    default: base[name] = value; break;
                }
            }
        }
    }

    /// <summary>状态枚举</summary>
    private enum StatusKinds
    {
        /// <summary>启用</summary>
        启用 = 1,

        /// <summary>禁用</summary>
        禁用 = 2,
    }
    #endregion

    #region 测试方法
    [Fact]
    [DisplayName("BuildSchema - 排除自增主键，敏感/自动字段标记不可填")]
    public void BuildSchema_ExcludesIdentityPk_MarksFillable()
    {
        var factory = new Entity<AiFormTestEntity>.DefaultEntityFactory();
        var fields = new FieldCollection(factory, ViewKinds.AddForm);
        var schema = AiFormHelper.BuildSchema(fields);

        // 自增主键 Id 不进入 Schema
        Assert.DoesNotContain(schema, f => f.Name == "Id");

        // 业务字段可填
        var name = schema.FirstOrDefault(f => f.Name == "Name");
        Assert.NotNull(name);
        Assert.True(name.Fillable);

        // 敏感字段 Password 存在但不可填
        var pwd = schema.FirstOrDefault(f => f.Name == "Password");
        Assert.NotNull(pwd);
        Assert.False(pwd.Fillable);

        // 自动维护字段 CreateTime 不可填
        var ct = schema.FirstOrDefault(f => f.Name == "CreateTime");
        Assert.NotNull(ct);
        Assert.False(ct.Fillable);
    }

    [Fact]
    [DisplayName("BuildSchema - 枚举字段包含允许值")]
    public void BuildSchema_EnumValues()
    {
        var factory = new Entity<AiFormTestEntity>.DefaultEntityFactory();
        var fields = new FieldCollection(factory, ViewKinds.AddForm);
        var schema = AiFormHelper.BuildSchema(fields);

        var status = schema.FirstOrDefault(f => f.Name == "Status");
        Assert.NotNull(status);
        Assert.NotNull(status.EnumValues);
        Assert.Contains("启用", status.EnumValues);
        Assert.Contains("禁用", status.EnumValues);
    }

    [Fact]
    [DisplayName("BuildSchema - 输出 ItemType，供 AI 判断生成内容格式")]
    public void BuildSchema_ItemType()
    {
        var factory = new Entity<AiFormTestEntity>.DefaultEntityFactory();
        var fields = new FieldCollection(factory, ViewKinds.AddForm);
        var schema = AiFormHelper.BuildSchema(fields);

        // HTML 富文本字段
        var remark = schema.FirstOrDefault(f => f.Name == "Remark");
        Assert.NotNull(remark);
        Assert.Equal("html", remark.ItemType);

        // Markdown 字段
        var content = schema.FirstOrDefault(f => f.Name == "Content");
        Assert.NotNull(content);
        Assert.Equal("markdown", content.ItemType);

        // 普通字符串字段无 ItemType
        var name = schema.First(f => f.Name == "Name");
        Assert.Null(name.ItemType);
    }

    [Fact]
    [DisplayName("BuildSchema - 编辑模式并入已有值")]
    public void BuildSchema_MergeValues()
    {
        var factory = new Entity<AiFormTestEntity>.DefaultEntityFactory();
        var fields = new FieldCollection(factory, ViewKinds.AddForm);
        var values = new Dictionary<String, Object?>
        {
            ["Name"] = "张三",
            ["Status"] = StatusKinds.启用,
        };
        var schema = AiFormHelper.BuildSchema(fields, values);

        var name = schema.First(f => f.Name == "Name");
        Assert.Equal("张三", name.Value);

        var status = schema.First(f => f.Name == "Status");
        Assert.Equal(StatusKinds.启用, status.Value);

        // 未提供的字段值为空
        var ct = schema.First(f => f.Name == "CreateTime");
        Assert.Null(ct.Value);
    }

    [Fact]
    [DisplayName("BuildSchema - 空字符串值归一为 null（引导 AI 生成而非回显空串）")]
    public void BuildSchema_EmptyStringToNull()
    {
        var factory = new Entity<AiFormTestEntity>.DefaultEntityFactory();
        var fields = new FieldCollection(factory, ViewKinds.AddForm);
        var values = new Dictionary<String, Object?>
        {
            ["Name"] = "",
            ["Status"] = StatusKinds.启用,
        };
        var schema = AiFormHelper.BuildSchema(fields, values);

        // 空字符串 → null，LLM 能识别该字段为空、需生成值
        var name = schema.First(f => f.Name == "Name");
        Assert.Null(name.Value);
        // 非空值保持原样
        var status = schema.First(f => f.Name == "Status");
        Assert.Equal(StatusKinds.启用, status.Value);
    }

    [Fact]
    [DisplayName("CoerceValue - 枚举合法值转换，非法值返回 null")]
    public void CoerceValue_Enum_ValidAndInvalid()
    {
        var factory = new Entity<AiFormTestEntity>.DefaultEntityFactory();
        var field = new FieldCollection(factory, ViewKinds.AddForm).First(f => f.Name == "Status");

        Assert.Equal(StatusKinds.启用, AiFormHelper.CoerceValue("启用", field));
        Assert.Equal(StatusKinds.禁用, AiFormHelper.CoerceValue("禁用", field));
        Assert.Null(AiFormHelper.CoerceValue("不存在的状态", field));
        Assert.Null(AiFormHelper.CoerceValue(null, field));
    }

    [Fact]
    [DisplayName("CoerceValue - 字符串/日期/数值类型转换")]
    public void CoerceValue_Types()
    {
        var factory = new Entity<AiFormTestEntity>.DefaultEntityFactory();
        var fields = new FieldCollection(factory, ViewKinds.AddForm);

        var name = fields.First(f => f.Name == "Name");
        Assert.Equal("张三", AiFormHelper.CoerceValue("张三", name));

        var ct = fields.First(f => f.Name == "CreateTime");
        var dt = AiFormHelper.CoerceValue("2026-01-01 10:00:00", ct);
        Assert.Equal(new DateTime(2026, 1, 1, 10, 0, 0), dt);
        // 空字符串 → null
        Assert.Null(AiFormHelper.CoerceValue("", ct));
    }

    [Fact]
    [DisplayName("ParseFieldValues - JSON 对象格式解析")]
    public void ParseFieldValues_ObjectFormat()
    {
        var dic = AiFormHelper.ParseFieldValues("{\"Name\":\"张三\",\"Status\":1}");
        Assert.NotNull(dic);
        Assert.Equal(2, dic!.Count);
        Assert.Equal("张三", dic["Name"]);
        Assert.Equal(1L, dic["Status"].ToLong());
    }

    [Fact]
    [DisplayName("ParseFieldValues - 扁平数组格式兼容（LLM 误生成键值数组）")]
    public void ParseFieldValues_ArrayFormat()
    {
        var dic = AiFormHelper.ParseFieldValues("[\"Name\",\"张三\",\"Status\",1,\"Enable\",true]");
        Assert.NotNull(dic);
        Assert.Equal(3, dic!.Count);
        Assert.Equal("张三", dic["Name"]);
        Assert.Equal(1L, dic["Status"].ToLong());
        Assert.True(dic["Enable"].ToBoolean());
    }

    [Fact]
    [DisplayName("ParseFieldValues - 非法/空参数返回 null 而非抛异常")]
    public void ParseFieldValues_InvalidReturnsNull()
    {
        Assert.Null(AiFormHelper.ParseFieldValues(null));
        Assert.Null(AiFormHelper.ParseFieldValues(""));
        Assert.Null(AiFormHelper.ParseFieldValues("not-json"));
        Assert.Null(AiFormHelper.ParseFieldValues("{}"));
        Assert.Null(AiFormHelper.ParseFieldValues("[]"));
        Assert.Null(AiFormHelper.ParseFieldValues("{\"a\":}"));
    }

    [Fact]
    [DisplayName("IsAutoField - 系统维护字段扩充后不可填，业务字段可填")]
    public void IsAutoField_ExpandedList()
    {
        // 审计字段（创建/更新/注册/登录）
        Assert.True(AiFormHelper.IsAutoField("UpdateUserID"));
        Assert.True(AiFormHelper.IsAutoField("CreateUserID"));
        Assert.True(AiFormHelper.IsAutoField("RegisterTime"));
        Assert.True(AiFormHelper.IsAutoField("RegisterIP"));
        Assert.True(AiFormHelper.IsAutoField("LastLogin"));
        Assert.True(AiFormHelper.IsAutoField("LastLoginIP"));
        // 统计/在线字段
        Assert.True(AiFormHelper.IsAutoField("Logins"));
        Assert.True(AiFormHelper.IsAutoField("Online"));
        Assert.True(AiFormHelper.IsAutoField("OnlineTime"));
        // 原有字段仍生效
        Assert.True(AiFormHelper.IsAutoField("CreateTime"));
        Assert.True(AiFormHelper.IsAutoField("UpdateUser"));
        Assert.True(AiFormHelper.IsAutoField("UpdateIP"));
        // 业务字段不受影响
        Assert.False(AiFormHelper.IsAutoField("Name"));
        Assert.False(AiFormHelper.IsAutoField("Enable"));
        Assert.False(AiFormHelper.IsAutoField("Sex"));
    }
    #endregion
}
