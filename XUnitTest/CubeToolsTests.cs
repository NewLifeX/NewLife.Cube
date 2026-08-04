using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife;
using NewLife.AI.Tools;
using NewLife.Cube;
using NewLife.Cube.AI;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using Xunit;

namespace XUnitTest;

/// <summary>魔方 AI 工具集单元测试 — 表单 Schema、填表值转换与实体信息</summary>
public class CubeToolsTests
{
    #region 测试实体
    /// <summary>AI 工具测试实体。手写实体，仅用于元数据，不访问数据库</summary>
    [BindTable("AiToolTestEntity", "AI工具测试实体", ConnName = "Test")]
    private class AiToolTestEntity : Entity<AiToolTestEntity>
    {
        private Int32 _Id;
        /// <summary>编号</summary>
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
        /// <summary>密码（敏感字段，AI 不可填）</summary>
        [BindColumn("Password", "密码", "", ItemType = "password")]
        [DisplayName("密码")]
        [DataObjectField(false, false, false, 50)]
        public String Password { get => _Password; set { if (OnPropertyChanging("Password", value)) { _Password = value; OnPropertyChanged("Password"); } } }

        /// <summary>索引器重写：按字段名读写私有字段</summary>
        public override Object? this[String name]
        {
            get => name switch
            {
                "Id" => _Id,
                "Name" => _Name,
                "Status" => _Status,
                "Password" => _Password,
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

    private static CubeTools<AiToolTestEntity> CreateTools() =>
        new(new Entity<AiToolTestEntity>.DefaultEntityFactory(), null, 0, p => []);
    #endregion

    #region 测试方法
    [Fact]
    [DisplayName("GetFormSchema - 返回表单字段 JSON，含枚举允许值")]
    public void GetFormSchema_ReturnsJson()
    {
        var tools = CreateTools();
        var json = tools.GetFormSchema("add");

        Assert.Contains("\"Name\"", json);
        Assert.Contains("启用", json);
    }

    [Fact]
    [DisplayName("GetFormSchema - 编辑模式返回字段结构与实体描述")]
    public void GetFormSchema_EditMode_ReturnsEntity()
    {
        var tools = CreateTools();
        var json = tools.GetFormSchema("edit");

        Assert.Contains("\"mode\":\"edit\"", json);
        Assert.Contains("AI工具测试实体", json);
        Assert.Contains("\"Name\"", json);
    }

    [Fact]
    [DisplayName("FillForm - 有效值转换，敏感字段跳过")]
    public void FillForm_ConvertsValues_SkipsSensitive()
    {
        var tools = CreateTools();
        var result = tools.FillForm(new Dictionary<String, Object>
        {
            ["Name"] = "张三",
            ["Status"] = "启用",
            ["Password"] = "secret123",
            ["NotExist"] = "xx",
        });

        Assert.False(result.IsError);
        var user = result.Contents.First(c => c.Audience.HasFlag(ToolAudience.User)).Data + "";
        Assert.Contains("fill_form", user);
        Assert.Contains("\"Name\":\"张三\"", user);
        Assert.Contains("\"Status\":1", user);
        // 敏感字段与不存在字段被跳过
        Assert.DoesNotContain("secret123", user);
        Assert.Contains("Password", user);
        Assert.Contains("NotExist", user);
    }

    [Fact]
    [DisplayName("FillForm - 枚举非法值标记失败")]
    public void FillForm_InvalidEnum_MarksError()
    {
        var tools = CreateTools();
        var result = tools.FillForm(new Dictionary<String, Object>
        {
            ["Status"] = "不存在的状态",
        });

        var user = result.Contents.First(c => c.Audience.HasFlag(ToolAudience.User)).Data + "";
        Assert.Contains("errors", user);
        Assert.Contains("Status", user);
    }

    [Fact]
    [DisplayName("GetDataContext - 列表模式返回数据摘要与字段")]
    public void GetDataContext_ListMode()
    {
        var tools = CreateTools();
        var json = tools.GetDataContext();

        Assert.Contains("AI工具测试实体", json);
        Assert.Contains("\"shown\":0", json);
        Assert.Contains("\"Name\"", json);
    }

    [Fact]
    [DisplayName("GetDataContext - 记录模式（编号>0）走记录收集")]
    public void GetDataContext_RecordMode()
    {
        var tools = new CubeTools<AiToolTestEntity>(new Entity<AiToolTestEntity>.DefaultEntityFactory(), null, 1, p => []);
        var json = tools.GetDataContext();

        // 无库环境下 FindByKey 返回 null → 返回"记录不存在"提示，验证分派到记录收集路径
        Assert.Contains("记录不存在", json);
    }

    [Fact]
    [DisplayName("GetSystemInfo - 返回服务器运行指标")]
    public void GetSystemInfo_ReturnsMetrics()
    {
        var tools = CreateTools();
        var json = tools.GetSystemInfo();

        Assert.Contains("cpu", json);
        Assert.Contains("machineName", json);
    }
    #endregion
}
