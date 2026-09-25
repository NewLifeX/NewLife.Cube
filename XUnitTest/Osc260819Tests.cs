using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace XUnitTest;

/// <summary>OSC-260819e483 P1 测试实体：覆盖主键/必填/可空/布尔 NOT NULL/只读</summary>
[DisplayName("OSC260819 元数据契约源")]
[BindTable("Osc260819Source", ConnName = "Cube", DbType = DatabaseType.None)]
public partial class Osc260819Source : Entity<Osc260819Source>
{
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    public Int32 Id { get; set; }

    /// <summary>名称（NOT NULL 必填）</summary>
    [DisplayName("名称")]
    [DataObjectField(false, false, false, 50)]
    public String Name { get; set; }

    /// <summary>可空描述</summary>
    [DisplayName("可空描述")]
    [DataObjectField(false, false, true, 100)]
    public String Description { get; set; }

    /// <summary>启用（布尔 NOT NULL）</summary>
    [DisplayName("启用")]
    [DataObjectField(false, false, false, 0)]
    public Boolean Enable { get; set; }
}

/// <summary>P1 测试控制器：暴露 protected 的 OnGetFields / PrepareFieldsForApi</summary>
public class Osc260819Controller : ReadOnlyEntityController<Osc260819Source>
{
    /// <summary>获取字段集合（命名避开基类 GetFields Action，避免隐藏警告）</summary>
    /// <param name="kind">字段类型</param>
    /// <returns></returns>
    public FieldCollection GetFieldCollection(ViewKinds kind) => OnGetFields(kind, null);

    /// <summary>物化字段</summary>
    /// <param name="fields">字段列表</param>
    /// <returns></returns>
    public IList<DataField> PrepareFields(IList<DataField> fields) => PrepareFieldsForApi(fields);
}

/// <summary>OSC-260819e483 P1 契约加固：Required 矩阵 / Fill 不写 Required / Stat 安全转换</summary>
/// <remarks>OSC-260925 审计修正：必填矩阵收敛为「仅非空字符串字段」——布尔/数值/枚举/系统列不再误标必填。</remarks>
public class Osc260819Tests
{
    public Osc260819Tests()
    {
        // 隔离测试库，避免污染开发库
        DAL.AddConnStr("Cube", "Data Source=Osc260819Tests;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Fact]
    [DisplayName("P1 Required 矩阵：仅非空字符串必填；PK/只读/可空/布尔/数值/枚举 → false，且 ToDictionary 显式下发 required")]
    public void PrepareFieldsForApi_RequiredMatrix()
    {
        var c = new Osc260819Controller();
        var list = new List<DataField>
        {
            new() { Name = "Id", PrimaryKey = true, Nullable = false, Type = typeof(Int32) },
            new() { Name = "Name", Nullable = false, Type = typeof(String) },
            new() { Name = "Description", Nullable = true, Type = typeof(String) },
            new() { Name = "Enable", Nullable = false, Type = typeof(Boolean) },
            new() { Name = "Version", Nullable = false, Type = typeof(Int32) },
            new() { Name = "Kind", Nullable = false, Type = typeof(DayOfWeek) },
            new() { Name = "ReadOnlyField", Nullable = false, ReadOnly = true, Type = typeof(String) },
            // Map 扩展字段（映射名≠字段名）：不标必填，避免前端拿不到值又被拦截
            new() { Name = "ParentName", Nullable = false, Type = typeof(String), MapField = "ParentID" },
        };

        var prepared = c.PrepareFields(list);

        // 主键 → false
        Assert.False(prepared.First(f => f.Name == "Id").Required);
        // NOT NULL 字符串 → true（名称等业务主字段）
        Assert.True(prepared.First(f => f.Name == "Name").Required);
        // 可空 → false
        Assert.False(prepared.First(f => f.Name == "Description").Required);
        // 布尔 NOT NULL → false（开关无空态，false 是有效值；OSC-260925 收敛）
        Assert.False(prepared.First(f => f.Name == "Enable").Required);
        // 数值 NOT NULL → false（空值不提交时后端按 0/默认值落库，前端不得拦截整单）
        Assert.False(prepared.First(f => f.Name == "Version").Required);
        // 枚举 NOT NULL → false（0/未知为合法值，业务校验交给实体 Valid）
        Assert.False(prepared.First(f => f.Name == "Kind").Required);
        // 只读 → false
        Assert.False(prepared.First(f => f.Name == "ReadOnlyField").Required);
        // Map 扩展字段 → false
        Assert.False(prepared.First(f => f.Name == "ParentName").Required);

        // 显式下发 required（true/false 都要有）：前端以显式值为准，缺省才按 nullable 兜底
        var enableDic = prepared.First(f => f.Name == "Enable").ToDictionary();
        Assert.True(enableDic.ContainsKey("required"));
        Assert.False((Boolean)enableDic["required"]);
        Assert.True((Boolean)prepared.First(f => f.Name == "Name").ToDictionary()["required"]);
    }

    [Fact]
    [DisplayName("P1 真实实体链路：OnGetFields + PrepareFieldsForApi 后 Name required:true，Id/Description/Enable false")]
    public void PrepareFieldsForApi_RealEntity()
    {
        var c = new Osc260819Controller();
        var fields = c.PrepareFields(c.GetFieldCollection(ViewKinds.AddForm));

        Assert.False(fields.First(f => f.Name == "Id").Required);
        Assert.True(fields.First(f => f.Name == "Name").Required);
        Assert.False(fields.First(f => f.Name == "Description").Required);
        // 布尔开关不再误标必填（OSC-260925 审计修正）
        Assert.False(fields.First(f => f.Name == "Enable").Required);
    }

    [Fact]
    [DisplayName("P1 Fill(FieldItem) 不写 Required（OSC-260819e483 冻结：Fill 后 Required 仍 false）")]
    public void Fill_DoesNotSetRequired()
    {
        var fi = Osc260819Source.Meta.AllFields.First(f => f.Name == "Name");

        var df = new DataField(fi);

        Assert.False(df.Required);
    }

    [Fact]
    [DisplayName("P1 PrepareFieldsForApi 对 null 列表 no-op；null 元素跳过不抛")]
    public void PrepareFieldsForApi_NullSafe()
    {
        var c = new Osc260819Controller();

        Assert.Null(c.PrepareFields(null));

        var list = new List<DataField> { null, new() { Name = "X", Nullable = false, Type = typeof(String) } };
        var prepared = c.PrepareFields(list);
        Assert.True(prepared[1].Required);
    }
}
