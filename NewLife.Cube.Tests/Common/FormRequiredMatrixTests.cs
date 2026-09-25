using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NewLife.Cube;
using NewLife.Cube.ViewModels;
using XCode;
using XCode.DataAccessLayer;
using Xunit;

namespace NewLife.Cube.Tests;

/// <summary>OSC-260925 表单必填矩阵测试实体：覆盖字符串/可空字符串/布尔/数值</summary>
/// <remarks>连接名专用（Osc260925Form），避免改写全局 Cube 连接影响并行用例。</remarks>
[DisplayName("OSC260925 表单矩阵实体")]
[BindTable("Osc260925FormItem", ConnName = "Osc260925Form", DbType = DatabaseType.None)]
public class Osc260925FormItem : Entity<Osc260925FormItem>
{
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [DataObjectField(true, true, false, 0)]
    public Int32 Id { get; set; }

    /// <summary>名称（NOT NULL 字符串：应必填）</summary>
    [DisplayName("名称")]
    [DataObjectField(false, false, false, 50)]
    public String? Name { get; set; }

    /// <summary>备注（可空字符串：不应必填）</summary>
    [DisplayName("备注")]
    [DataObjectField(false, false, true, 100)]
    public String? Remark { get; set; }

    /// <summary>启用（布尔 NOT NULL：不应必填）</summary>
    [DisplayName("启用")]
    [DataObjectField(false, false, false, 0)]
    public Boolean Enable { get; set; }

    /// <summary>版本（数值 NOT NULL：不应必填）</summary>
    [DisplayName("版本")]
    [DataObjectField(false, false, false, 0)]
    public Int32 Version { get; set; }

    /// <summary>编码（NOT NULL 字符串 + 声明默认值：不应必填，由默认值自动填充）</summary>
    [DisplayName("编码")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("Code", "编码", "", DefaultValue = "auto")]
    public String? Code { get; set; }

    /// <summary>获取/设置 字段值。模拟 xcode 生成实体的索引器重写（基类索引器不反射读取普通字段，测试实体必须同构）</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object? this[String name]
    {
        get => name switch
        {
            "Id" => Id,
            "Name" => Name,
            "Remark" => Remark,
            "Enable" => Enable,
            "Version" => Version,
            "Code" => Code,
            _ => base[name],
        };
        set
        {
            switch (name)
            {
                case "Id": Id = Convert.ToInt32(value); break;
                case "Name": Name = Convert.ToString(value); break;
                case "Remark": Remark = Convert.ToString(value); break;
                case "Enable": Enable = Convert.ToBoolean(value); break;
                case "Version": Version = Convert.ToInt32(value); break;
                case "Code": Code = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
}

/// <summary>测试控制器：暴露 protected 的 OnGetFields / PrepareFieldsForApi</summary>
public class Osc260925FormController : ReadOnlyEntityController<Osc260925FormItem>
{
    /// <summary>获取字段集合（命名避开基类 GetFields Action，避免隐藏警告）</summary>
    /// <param name="kind">字段类型</param>
    /// <returns></returns>
    public FieldCollection GetFieldCollection(ViewKinds kind) => OnGetFields(kind, null);

    /// <summary>物化字段</summary>
    /// <param name="fields">字段列表</param>
    /// <returns></returns>
    public List<DataField> PrepareFields(List<DataField> fields) => PrepareFieldsForApi(fields);
}

/// <summary>测试控制器（实体写通道）：用于反射调用字段级校验</summary>
public class Osc260925FormEntityController : EntityController<Osc260925FormItem, Osc260925FormItem>
{
}

/// <summary>
/// 实体表单必填矩阵（OSC-260925）：仅「非空、无默认值、非审计」的字符串字段必填。
/// </summary>
/// <remarks>
/// 症状：部门添加表单 10 项误必填（层级/排序/启用/可见/管理者/扩展1-3 等），空值被
/// 「X不可以为空！」拦截，添加/编辑整单不可保存。根因：旧矩阵把数据库 NOT NULL
/// 列直接等同表单必填（含布尔开关与数值列）。现由生成的类定义自动推断（FormRequiredHelper）：
/// 布尔/数值/枚举/可空/带默认值/审计列均非必填，required 以 true/false 显式下发；
/// 后端 ValidateEntityFields 与前端 required 同源同套推断。
/// </remarks>
[Collection("Osc260925Form")]
public class FormRequiredMatrixTests
{
    /// <summary>构造：隔离测试库（专用连接名，不碰全局 Cube/Membership）</summary>
    public FormRequiredMatrixTests()
    {
        DAL.AddConnStr("Osc260925Form", "Data Source=Osc260925Form;Mode=Memory;Cache=Shared", null, "SQLite");
    }

    [Fact(DisplayName = "必填矩阵：字符串 NOT NULL → true；可空/布尔/数值 → false 且显式下发")]
    public void RequiredMatrix_OnlyStringFields()
    {
        var c = new Osc260925FormController();
        var list = new List<DataField>
        {
            new() { Name = "Id", PrimaryKey = true, Nullable = false, Type = typeof(Int32) },
            new() { Name = "Name", Nullable = false, Type = typeof(String) },
            new() { Name = "Remark", Nullable = true, Type = typeof(String) },
            new() { Name = "Enable", Nullable = false, Type = typeof(Boolean) },
            new() { Name = "Version", Nullable = false, Type = typeof(Int32) },
            // Map 扩展字段（映射名≠字段名）：不标必填
            new() { Name = "ParentName", Nullable = false, Type = typeof(String), MapField = "ParentID" },
        };

        var prepared = c.PrepareFields(list);

        Assert.True(prepared.First(f => f.Name == "Name").Required);
        Assert.False(prepared.First(f => f.Name == "Id").Required);
        Assert.False(prepared.First(f => f.Name == "Remark").Required);
        Assert.False(prepared.First(f => f.Name == "Enable").Required);
        Assert.False(prepared.First(f => f.Name == "Version").Required);
        Assert.False(prepared.First(f => f.Name == "ParentName").Required);

        // required 显式下发（true/false 都要有键）：只发 true 时前端会按 nullable 兜底把布尔/数值改回必填
        var enableDic = prepared.First(f => f.Name == "Enable").ToDictionary();
        Assert.True(enableDic.ContainsKey("required"));
        Assert.False((Boolean)enableDic["required"]);
        Assert.True((Boolean)prepared.First(f => f.Name == "Name").ToDictionary()["required"]);
    }

    [Fact(DisplayName = "真实实体链路：OnGetFields + PrepareFieldsForApi 后仅名称为必填")]
    public void RequiredMatrix_RealEntity()
    {
        var c = new Osc260925FormController();
        var fields = c.PrepareFields(c.GetFieldCollection(ViewKinds.AddForm));

        Assert.True(fields.First(f => f.Name == "Name").Required);
        Assert.False(fields.First(f => f.Name == "Remark").Required);
        Assert.False(fields.First(f => f.Name == "Enable").Required);
        Assert.False(fields.First(f => f.Name == "Version").Required);
    }

    [Fact(DisplayName = "自动推断：默认值列与审计列不标必填（由生成的类定义元数据得出）")]
    public void RequiredMatrix_DefaultValue_And_Audit()
    {
        var c = new Osc260925FormController();
        var fields = c.PrepareFields(c.GetFieldCollection(ViewKinds.AddForm));

        // 编码：NOT NULL 字符串，但 BindColumn 声明了 DefaultValue="auto" → 数据库/实体自动填，不标必填
        Assert.False(fields.First(f => f.Name == "Code").Required);
        Assert.Equal("auto", FormRequiredHelper.GetDefaultValue(fields.First(f => f.Name == "Code")));

        // 审计列：即使 NOT NULL 字符串也由拦截器维护，不标必填
        Assert.True(FormRequiredHelper.IsAuditField("CreateUser"));
        var audit = c.PrepareFields([new() { Name = "CreateUser", Nullable = false, Type = typeof(String) }]);
        Assert.False(audit[0].Required);
    }

    [Fact(DisplayName = "字段级校验与必填推断同源：默认值字符串留空不报错，名称留空仍报错")]
    public void ValidateEntityFields_Uses_Inference()
    {
        var mi = typeof(EntityController<Osc260925FormItem, Osc260925FormItem>)
            .GetMethod("ValidateEntityFields", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mi);

        // Code（NOT NULL + DefaultValue）留空 → 不报「编码不可以为空」
        var ok = mi!.Invoke(null, [new Osc260925FormItem { Name = "x" }, DataObjectMethodType.Insert, null]) as List<FieldError>;
        Assert.True(ok == null, $"默认值字段不应产生字段错误：{String.Join(";", (ok ?? []).Select(e => e.Message))}");

        // Name 留空 → 仍报「名称不可以为空」（必填语义不回退）
        var bad = mi.Invoke(null, [new Osc260925FormItem(), DataObjectMethodType.Insert, null]) as List<FieldError>;
        Assert.NotNull(bad);
        Assert.Contains(bad!, e => (e.Message ?? "").Contains("名称"));
    }
}
